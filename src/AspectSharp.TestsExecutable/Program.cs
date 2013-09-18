using System;

namespace AspectSharp.TestsExecutable
{
    public class Program
    {
        public static void Main(string[] args)
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
        }
    }
}
