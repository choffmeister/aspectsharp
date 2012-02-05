using System;
using System.Collections.Generic;

namespace Choffmeister.Advices.Test
{
    public sealed class TraceAdviceAttribute : AdviceAttribute
    {
        private string _prefix;
        private string _suffix;

        public string Name { get; set; }

        public TraceAdviceAttribute()
            : this(null)
        {
        }

        public TraceAdviceAttribute(string prefix)
            : this(prefix, null)
        {
        }

        public TraceAdviceAttribute(string prefix, string suffix)
        {
            _prefix = prefix;
            _suffix = suffix;
        }

        public override object Execute(MulticastDelegate dele, ParameterCollection parameters)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            
            Console.WriteLine("Prefix: {0}", _prefix);
            Console.WriteLine("Suffix: {0}", _suffix);
            Console.WriteLine("Name: {0}", this.Name);

            Console.WriteLine("Intercepted {0}::{1}", dele.Target.GetType().FullName, dele.Method.Name);

            if (parameters.Dictionary.Count > 0)
            {
                Console.WriteLine("Args:");

                foreach (KeyValuePair<string, object> kvp in parameters.Dictionary)
                {
                    Console.WriteLine("  {0} -> {1}", kvp.Key, kvp.Value);
                }
            }

            Console.ResetColor();

            // invoke the intercepted method
            object result = dele.DynamicInvoke(parameters.AllParameterValues);

            Console.ForegroundColor = ConsoleColor.Green;

            if (dele.Method.ReturnType != typeof(void))
            {
                Console.WriteLine("Return:");
                Console.WriteLine("  {0}", result);
            }

            Console.ResetColor();

            return result;
        }
    }
}