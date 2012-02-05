using System;

namespace Choffmeister.Advices.Test
{
    internal class Program
    {
        private static void Main(string[] args)
        {
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
    }
}