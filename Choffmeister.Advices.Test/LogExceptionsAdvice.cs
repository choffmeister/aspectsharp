using System;

namespace Choffmeister.Advices.Test
{
    public class LogExceptionsAdvice : CatchAdviceAttribute
    {
        public override object ExecuteOnException(Exception exception, ParameterCollection parameters)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Red;

            Console.WriteLine(exception.Message);

            Console.ResetColor();

            throw exception;
        }
    }
}