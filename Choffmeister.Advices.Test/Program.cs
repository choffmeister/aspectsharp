using System;
using System.IO;
using Choffmeister.Advices.Weaver;
using Mono.Cecil;
using System.Reflection;

namespace Choffmeister.Advices.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            WeaveMe();

            AnnotatedClass c1 = new AnnotatedClass();
            c1.Void1();
            c1.Void2();

            try
            {
                c1.Throws("My message!");
            }
            catch (Exception)
            {
            }

            c1.Foo("hi", 23, DateTime.Now);
            c1.Bar("Tom");
            c1.Number = 23;

            Console.ReadKey();
        }

        private static void WeaveMe()
        {
            if (Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) == "WEAVED")
            {
                return;
            }

            ILWeaver weaver = new ILWeaver();
            AssemblyDefinition assemblyDefinition = null;

            using (FileStream input = Assembly.GetEntryAssembly().GetFile("Choffmeister.Advices.Test.exe"))
            {
                assemblyDefinition = weaver.Weave(input, new string[0]);
            }

            using (FileStream output = File.Open("WEAVED.exe", FileMode.Create))
            {
                assemblyDefinition.Write(output);
            }
        }
    }
}