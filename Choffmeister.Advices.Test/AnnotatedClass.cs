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
        public void Void1()
        {
            Console.WriteLine("Void1");
        }

        [TraceAdvice(127, 255, 32767, 65535, 2147483647, 4294967295, 9223372036854775807, 18446744073709551615, 12.3f, 26.6, 'A', true, "foo")]
        public void Void2()
        {
            Console.WriteLine("Void2");
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