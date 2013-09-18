using System;
using AspectSharp.Advices;

namespace AspectSharp.TestsExecutable
{
    public sealed class SkipAdviceAttribute : AdviceAttribute
    {
        public override object Execute(MulticastDelegate dele, ParameterCollection parameters)
        {
            return null;
        }
    }
}
