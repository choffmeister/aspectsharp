using System;
using System.IO;
using System.Reflection;
using Choffmeister.Advices.Weaver;

namespace Choffmeister.Advices.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            CurrentAssemblyWeaver weaver = new CurrentAssemblyWeaver();
            weaver.WeaveCurrentAssembly();

            AnnotatedClass c1 = new AnnotatedClass();
            c1.Void1();
            c1.Void2();
            c1.Foo("hi", 23, DateTime.Now);
            c1.Bar("Tom");
            c1.Number = 23;

            Console.ReadKey();
        }
    }

    // TODO: advice anotation does not work at this special class by an yet unknown reason...
    public class CurrentAssemblyWeaver
    {
        public void WeaveCurrentAssembly()
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