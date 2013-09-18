using System;
using System.IO;
using System.Reflection;
using AspectSharp.Weaver;
using Mono.Cecil;
using NUnit.Framework;
using System.Net;
using AspectSharp.TestsExecutable;
using NUnit.Framework.Constraints;

namespace AspectSharp.Tests
{
    [TestFixture]
    public class ILWeaverTest
    {
        private readonly string _executable = "AspectSharp.TestsExecutable.exe";
        private readonly string _executableWeaved = "AspectSharp.TestsExecutable.Weaved.exe";

        [Test]
        public void TestWeave()
        {
            using (FileStream input = new FileStream(_executable, FileMode.Open, FileAccess.Read))
            using (FileStream output = new FileStream(_executableWeaved, FileMode.Create, FileAccess.Write))
            {
                var weaver = new ILWeaver();
                var assemblyDefinition = weaver.Weave(input, new string[0], false);
                assemblyDefinition.Write(output);
            }

            string weavedOutput = this.GetWeavedOutput();

            Assert.That(weavedOutput, Is.Not.StringContaining("Original Void2"), "Execution of AnnotatedClass.Void2 should have been skipped");
        }

        private string GetWeavedOutput()
        {
            var assembly = Assembly.LoadFrom(_executableWeaved);
            var main = assembly.GetType("AspectSharp.TestsExecutable.Program").GetMethod("Main", BindingFlags.Static | BindingFlags.Public);

            StringWriter writer = new StringWriter();
            Console.SetOut(writer);

            main.Invoke(null, new object[1] { new string[0] });

            return writer.ToString();
        }
    }
}
