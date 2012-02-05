using System;

namespace Choffmeister.Advices.Test
{
    public sealed class SkipAdviceAttribute : AdviceAttribute
    {
        public override object Execute(MulticastDelegate dele, ParameterCollection parameters)
        {
            return null;
        }
    }
}