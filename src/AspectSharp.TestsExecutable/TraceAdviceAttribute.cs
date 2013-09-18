using System;
using System.Collections.Generic;
using AspectSharp.Advices;

namespace AspectSharp.TestsExecutable
{
    public sealed class TraceAdviceAttribute : AdviceAttribute
    {
        private string _prefix;
        private string _suffix;
        private string _allTypes;

        public string Name { get; set; }

        public TraceAdviceAttribute()
            : this(null)
        {
        }

        public TraceAdviceAttribute(sbyte _sbyte, byte _byte, short _short, ushort _ushort, int _int, uint _uint, long _long, ulong _ulong, float _float, double _double, char _char, bool _bool, string _string, Type _type)
        {
            _allTypes = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13}", _sbyte, _byte, _short, _ushort, _int, _uint, _long, _ulong, _float, _double, _char, _bool, _string, _type);
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

            if (_allTypes != null)
            {
                Console.WriteLine("All types: {0}", _allTypes);
            }

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
            object result = null;// dele.DynamicInvoke(parameters.AllParameterValues);

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