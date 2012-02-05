using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Choffmeister.Advices.Weaver
{
    public class ILWeaver
    {
        public void Weave(Stream inputStream, Stream outputStream)
        {
            this.Weave(inputStream).Write(outputStream);
        }

        public void Weave(string inputFilePath, string outputFilePath)
        {
            using (FileStream inputStream = File.Open(inputFilePath, FileMode.Open, FileAccess.Read))
            {
                using (FileStream outputStream = File.Open(outputFilePath, FileMode.Create))
                {
                    this.Weave(inputStream).Write(outputStream);
                }
            }
        }

        public AssemblyDefinition Weave(Stream inputStream)
        {
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(inputStream);

            TypeDefinition weaveAttributeType = assembly.ImportType(typeof(AdviceAttribute));
            TypeDefinition paraCollType = assembly.ImportType(typeof(ParameterCollection));
            MethodReference paraCollCtor = assembly.ImportDefaultConstructor(paraCollType);
            MethodReference paraCollAddMethod = assembly.ImportMethod(paraCollType, "Add");

            var markedMethods = assembly.MainModule.Types
                .SelectMany(n => n.Methods)
                .SelectMany(n => n.CustomAttributes, (m, a) => new { Method = m, Attribute = a })
                .Where(n => IsSubClass(n.Attribute.AttributeType, weaveAttributeType))
                .ToList();

            foreach (var markedMethod in markedMethods)
            {
                MethodDefinition method = markedMethod.Method;
                CustomAttribute attribute = markedMethod.Attribute;
                TypeDefinition type = method.DeclaringType;

                ILProcessor processor = method.Body.GetILProcessor();
                Instruction first;
                Instruction last;

                method.Body.SimplifyMacros();

                // clone method to __xxx method and override xxx with empty method
                MethodDefinition interceptedMethod = new MethodDefinition("__" + method.Name, method.Attributes, method.ReturnType);
                method.Parameters.ToList().ForEach(n => interceptedMethod.Parameters.Add(n));
                method.Body.Variables.ToList().ForEach(n => interceptedMethod.Body.Variables.Add(n));
                method.Body.Instructions.ToList().ForEach(n => interceptedMethod.Body.Instructions.Add(n));
                method.Body.Instructions.Clear();
                method.Body.Variables.Clear();

                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                type.Methods.Add(interceptedMethod);

                GetFirstAndLastInstructions(method.Body, out first, out last);

                // instantiate parameter collection
                // TODO: check why we have to reimport here
                VariableDefinition paraCollVariable = new VariableDefinition(method.Module.Import(paraCollType));
                method.Body.Variables.Add(paraCollVariable);
                processor.InsertBefore(first, Instruction.Create(OpCodes.Newobj, paraCollCtor));
                processor.InsertBefore(first, Instruction.Create(OpCodes.Stloc, paraCollVariable));

                // fill parameter collection
                foreach (var p in method.Parameters)
                {
                    processor.InsertBefore(first, Instruction.Create(OpCodes.Ldloc, paraCollVariable));
                    processor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, p.Name));
                    processor.InsertBefore(first, Instruction.Create(OpCodes.Ldarg, p));
                    if (p.ParameterType.IsValueType)
                        processor.InsertBefore(first, Instruction.Create(OpCodes.Box, p.ParameterType));

                    processor.InsertBefore(first, Instruction.Create(OpCodes.Callvirt, paraCollAddMethod));
                }

                // instantiate attribute
                Tuple<List<Instruction>, VariableDefinition> instantiate = CreateInstantiateAttribute(attribute);
                instantiate.Item1.ForEach(n => processor.InsertBefore(first, n));
                method.Body.Variables.Add(instantiate.Item2);

                // create delegate
                VariableDefinition delegateVariable;
                MethodDefinition delegateCtor;
                MethodDefinition invoke;
                EmitCreateDelegateToLocalMethod(method.Module, processor, method.DeclaringType, method, out delegateVariable, out delegateCtor, out invoke);
                processor.InsertBefore(first, Instruction.Create(OpCodes.Ldloc, instantiate.Item2));
                processor.InsertBefore(first, Instruction.Create(OpCodes.Ldftn, interceptedMethod));
                processor.InsertBefore(first, Instruction.Create(OpCodes.Newobj, delegateCtor));
                processor.InsertBefore(first, Instruction.Create(OpCodes.Stloc, delegateVariable));

                // invokde advice
                processor.InsertBefore(first, Instruction.Create(OpCodes.Ldloc, instantiate.Item2));
                processor.InsertBefore(first, Instruction.Create(OpCodes.Ldloc, delegateVariable));
                processor.InsertBefore(first, Instruction.Create(OpCodes.Ldloc, paraCollVariable));
                processor.InsertBefore(first, Instruction.Create(OpCodes.Callvirt, attribute.AttributeType.Resolve().Methods.Single(n => n.Name == "Execute")));

                method.Body.OptimizeMacros();
                method.CustomAttributes.Remove(attribute);
            }

            return assembly;
        }

        private Tuple<List<Instruction>, VariableDefinition> CreateInstantiateAttribute(CustomAttribute attribute)
        {
            TypeDefinition attributeType = attribute.AttributeType.Resolve();
            MethodDefinition attributeCtor = attributeType.Methods
                .Single(n =>
                {
                    if (n.Name != ".ctor")
                        return false;

                    if (n.Parameters.Count != attribute.ConstructorArguments.Count)
                        return false;

                    for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
                    {
                        if (attribute.ConstructorArguments[i].Type != n.Parameters[i].ParameterType)
                            return false;
                    }

                    return true;
                });

            // instantiate attribute
            List<Instruction> instructions = new List<Instruction>();
            instructions.AddRange(attribute.ConstructorArguments.Select(n => CreatePushArgument(n.Type, n.Value)));
            instructions.Add(Instruction.Create(OpCodes.Newobj, attributeCtor));

            // store attribute
            VariableDefinition attributeLoc = new VariableDefinition(attributeType);
            instructions.Add(Instruction.Create(OpCodes.Stloc, attributeLoc));

            // set properties
            foreach (var prop in attribute.Properties)
            {
                MethodDefinition setMethod = attributeType.Methods.Single(n => n.Name == "set_" + prop.Name);

                instructions.Add(Instruction.Create(OpCodes.Ldloc, attributeLoc));
                instructions.Add(CreatePushArgument(prop.Argument.Type, prop.Argument.Value));
                instructions.Add(Instruction.Create(OpCodes.Callvirt, setMethod));
            }

            return Tuple.Create(instructions, attributeLoc);
        }

        public static void EmitCreateDelegateToLocalMethod(ModuleDefinition module, ILProcessor worker, TypeDefinition declaringType, MethodDefinition source, out VariableDefinition dlg, out MethodDefinition ctor, out MethodDefinition invok)
        {
            // Define some variables
            var body = worker.Body;
            var method = body.Method;
            var newdlg = new VariableDefinition(module.Import(typeof(Delegate)));
            body.Variables.Add(newdlg);

            var multiDelegateType = module.Import(typeof(MulticastDelegate));
            var voidType = module.Import(typeof(void));
            var objectType = module.Import(typeof(object));
            var nativeIntType = module.Import(typeof(IntPtr));
            var asyncCallbackType = module.Import(typeof(AsyncCallback));
            var asyncResultType = module.Import(typeof(IAsyncResult));

            // Create new delegate type
            var dlgtype = new TypeDefinition(declaringType.Namespace, method.Name + "_Delegate" + declaringType.NestedTypes.Count,
                TypeAttributes.Sealed | TypeAttributes.NestedAssembly | TypeAttributes.RTSpecialName, multiDelegateType);
            declaringType.NestedTypes.Add(dlgtype);
            dlgtype.IsRuntimeSpecialName = true;

            var constructor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.CompilerControlled | MethodAttributes.RTSpecialName | MethodAttributes.SpecialName | MethodAttributes.HideBySig, voidType);
            dlgtype.Methods.Add(constructor);
            constructor.Parameters.Add(new ParameterDefinition("object", ParameterAttributes.None, objectType));
            constructor.Parameters.Add(new ParameterDefinition("method", ParameterAttributes.None, nativeIntType));
            constructor.IsRuntime = true;

            var begininvoke = new MethodDefinition("BeginInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, asyncResultType);
            dlgtype.Methods.Add(begininvoke);
            foreach (var para in source.Parameters)
            {
                begininvoke.Parameters.Add(new ParameterDefinition(para.Name, para.Attributes, para.ParameterType));
            }
            begininvoke.Parameters.Add(new ParameterDefinition("callback", ParameterAttributes.None, asyncCallbackType));
            begininvoke.Parameters.Add(new ParameterDefinition("object", ParameterAttributes.None, objectType));
            begininvoke.IsRuntime = true;

            var endinvoke = new MethodDefinition("EndInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, voidType);
            dlgtype.Methods.Add(endinvoke);
            endinvoke.Parameters.Add(new ParameterDefinition("result", ParameterAttributes.None, asyncResultType));
            endinvoke.IsRuntime = true;
            endinvoke.ReturnType = source.ReturnType;

            var invoke = new MethodDefinition("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, voidType);
            dlgtype.Methods.Add(invoke);
            foreach (var para in source.Parameters)
            {
                invoke.Parameters.Add(new ParameterDefinition(para.Name, para.Attributes, para.ParameterType));
            }
            invoke.IsRuntime = true;
            invoke.ReturnType = source.ReturnType;

            ctor = constructor;
            dlg = newdlg;
            invok = invoke;
        }

        private void GetFirstAndLastInstructions(MethodBody body, out Instruction first, out Instruction last)
        {
            first = body.Instructions.First();
            last = body.Instructions.Last();
            while (last.OpCode != OpCodes.Ret) last = last.Previous;
        }

        // TODO: map all types
        private Instruction CreatePushArgument(TypeReference type, object value)
        {
            switch (type.FullName)
            {
                case "System.String":
                    return Instruction.Create(OpCodes.Ldstr, (string)value);
                case "System.Int32":
                    return Instruction.Create(OpCodes.Ldc_I4, (int)value);
                case "System.Int64":
                    return Instruction.Create(OpCodes.Ldc_I8, (long)value);
                default:
                    throw new NotSupportedException(string.Format("Type '{0}' is not supported by Choffmeister.AopWeaving", type.FullName));
            }
        }

        private bool IsSubClass(TypeReference type, TypeReference baseType)
        {
            if (type == null)
                return false;

            if (type.FullName == baseType.FullName)
                return true;

            return IsSubClass(type.Resolve().BaseType, baseType);
        }
    }
}