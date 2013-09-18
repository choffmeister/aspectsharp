using System;
using AspectSharp.Advices;

namespace AspectSharp.TestsExecutable
{
    public class LogExceptionsAdviceAttribute : AdviceAttribute
    {
        public override object Execute(MulticastDelegate dele, ParameterCollection parameters)
        {
            try
            {
                return dele.DynamicInvoke(parameters.AllParameterValues);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Red;

                Console.WriteLine(ex.Message);

                Console.ResetColor();

                throw;
            }
        }
    }
}
