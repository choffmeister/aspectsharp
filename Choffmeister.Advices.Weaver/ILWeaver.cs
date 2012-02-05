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
                .Where(n => n.Attribute.AttributeType.IsSubClassOf(weaveAttributeType))
                .ToList();

            foreach (var markedMethod in markedMethods)
            {
                MethodDefinition method = markedMethod.Method;
                CustomAttribute attribute = markedMethod.Attribute;
                TypeDefinition type = method.DeclaringType;

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

                ILProcessor processor = method.Body.GetILProcessor();
                Instruction ret = method.Body.Instructions.First();

                // instantiate parameter collection
                // TODO: check why we have to reimport here
                VariableDefinition paraCollVariable = new VariableDefinition(method.Module.Import(paraCollType));
                method.Body.Variables.Add(paraCollVariable);
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Newobj, paraCollCtor));
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Stloc, paraCollVariable));

                // fill parameter collection
                foreach (var p in method.Parameters)
                {
                    processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, paraCollVariable));
                    processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldstr, p.Name));
                    processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldarg, p));
                    if (p.ParameterType.IsValueType)
                        processor.InsertBefore(ret, Instruction.Create(OpCodes.Box, p.ParameterType));

                    processor.InsertBefore(ret, Instruction.Create(OpCodes.Callvirt, paraCollAddMethod));
                }

                // instantiate attribute
                Tuple<List<Instruction>, VariableDefinition> instantiate = CreateInstantiateAttribute(attribute);
                instantiate.Item1.ForEach(n => processor.InsertBefore(ret, n));
                method.Body.Variables.Add(instantiate.Item2);

                // create delegate
                VariableDefinition delegateVariable;
                MethodDefinition delegateCtor;
                MethodDefinition invoke;
                EmitCreateDelegateToLocalMethod(method.Module, processor, method.DeclaringType, method, out delegateVariable, out delegateCtor, out invoke);
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, instantiate.Item2));
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldftn, interceptedMethod));
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Newobj, delegateCtor));
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Stloc, delegateVariable));

                // invoke advice
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, instantiate.Item2));
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, delegateVariable));
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Ldloc, paraCollVariable));
                processor.InsertBefore(ret, Instruction.Create(OpCodes.Callvirt, attribute.AttributeType.Resolve().Methods.Single(n => n.Name == "Execute")));

                // if method has no return value, pop the return value from advice invoke
                if (method.ReturnType == method.Module.TypeSystem.Void)
                    processor.InsertBefore(ret, Instruction.Create(OpCodes.Pop));

                method.Body.OptimizeMacros();
                method.CustomAttributes.Remove(attribute);
            }

            return assembly;
        }

        private Tuple<List<Instruction>, VariableDefinition> CreateInstantiateAttribute(CustomAttribute attribute)
        {
            TypeDefinition attributeType = attribute.AttributeType.Resolve();
            MethodDefinition attributeCtor = attributeType.Methods
                .Where(n => n.Name == ".ctor")
                .Where(n => n.Parameters.Count == attribute.ConstructorArguments.Count)
                .Single(n =>
                {
                    for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
                    {
                        if (attribute.ConstructorArguments[i].Type != n.Parameters[i].ParameterType)
                            return false;
                    }

                    return true;
                });

            // instantiate attribute
            List<Instruction> instructions = new List<Instruction>();
            instructions.AddRange(attribute.ConstructorArguments.Select(n => n.Type.CreateLoadConstantInstruction(n.Value)));
            instructions.Add(Instruction.Create(OpCodes.Newobj, attributeCtor));

            // store attribute
            VariableDefinition attributeLoc = new VariableDefinition(attributeType);
            instructions.Add(Instruction.Create(OpCodes.Stloc, attributeLoc));

            // set properties
            foreach (var prop in attribute.Properties)
            {
                MethodDefinition setMethod = attributeType.Methods.Single(n => n.Name == "set_" + prop.Name);

                instructions.Add(Instruction.Create(OpCodes.Ldloc, attributeLoc));
                instructions.Add(prop.Argument.Type.CreateLoadConstantInstruction(prop.Argument.Value));
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
    }
}