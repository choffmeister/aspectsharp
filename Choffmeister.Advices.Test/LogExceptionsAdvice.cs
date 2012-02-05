using System;

namespace Choffmeister.Advices.Test
{
    public class LogExceptionsAdvice : AdviceAttribute
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