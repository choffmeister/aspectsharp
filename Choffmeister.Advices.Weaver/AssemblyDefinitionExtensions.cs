using System;
using System.Linq;
using Mono.Cecil;

namespace Choffmeister.Advices.Weaver
{
    public static class AssemblyDefinitionExtensions
    {
        public static TypeDefinition ImportType(this AssemblyDefinition assembly, Type type)
        {
            return ImportType(assembly, assembly.MainModule.Import(type)).Resolve();
        }

        public static TypeDefinition ImportType(this AssemblyDefinition assembly, TypeReference type)
        {
            return assembly.MainModule.Import(type).Resolve();
        }

        public static MethodReference ImportDefaultConstructor(this AssemblyDefinition assembly, TypeDefinition type)
        {
            return ImportMethod(assembly, type, n => n.Name == ".ctor" && n.Parameters.Count == 0);
        }

        public static MethodReference ImportMethod(this AssemblyDefinition assembly, TypeDefinition type, string methodName)
        {
            return ImportMethod(assembly, type, n => n.Name == methodName);
        }

        public static MethodReference ImportMethod(this AssemblyDefinition assembly, TypeDefinition type, Func<MethodDefinition, bool> methodSelector)
        {
            MethodReference method = type.Methods.Single(methodSelector);
            return assembly.MainModule.Import(method);
        }
    }
}