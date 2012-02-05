using System;

namespace Choffmeister.Advices.Test
{
    public class AnnotatedClass
    {
        public int Number { get; set; }

        public AnnotatedClass()
        {
            Console.WriteLine("Ctor");
        }

        [TraceAdvice("Pre-A")]
        public void Void()
        {
        }

        [TraceAdvice("Pre-B", "Post-B")]
        public string Foo(string name, int number, object obj)
        {
            Console.WriteLine("Foo {0} {1} {2}", name, number, obj);

            return "HI";
        }

        [TraceAdvice("Pre-C", Name = "Third letter")]
        public string Bar(string name)
        {
            Console.WriteLine("Bar {0}", name);

            return name;
        }
    }
}