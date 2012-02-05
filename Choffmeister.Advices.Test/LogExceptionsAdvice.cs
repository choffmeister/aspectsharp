using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Choffmeister.Advices.Test
{
    public class LogExceptionsAdvice : CatchAdviceAttribute
    {
        public override object ExecuteOnException(Exception exception, ParameterCollection parameters)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Red;

            Console.WriteLine(exception.Message);

            throw exception;
        }
    }
}
