using System;
using Choffmeister.Advices.Weaver;
using System.Reflection;
using System.IO;
namespace Choffmeister.Advices.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            WeaveCurrentAssembly();

            AnnotatedClass c1 = new AnnotatedClass();
            c1.Void();
            c1.Foo("hi", 23, DateTime.Now);
            c1.Bar("Tom");
            c1.Number = 23;

            Console.ReadKey();
        }

        private static void WeaveCurrentAssembly()
        {
            string currentAssemblyPath = Assembly.GetEntryAssembly().Location;
            string currentDirectory = Path.GetDirectoryName(currentAssemblyPath);
            string weavedAssemblyPath = currentDirectory + "\\" + Path.GetFileNameWithoutExtension(currentAssemblyPath) + "_WEAVED" + Path.GetExtension(currentAssemblyPath);

            if (Path.GetFileNameWithoutExtension(currentAssemblyPath).EndsWith("_WEAVED"))
            {
                return;
            }
            
            using (Stream inputStream = Assembly.GetEntryAssembly().GetFile("Choffmeister.Advices.Test.exe"))
            {
                using (Stream outputStream = File.Open(weavedAssemblyPath, FileMode.Create))
                {
                    ILWeaver weaver = new ILWeaver();
                    weaver.Weave(inputStream, outputStream);
                }
            }
        }
    }
}