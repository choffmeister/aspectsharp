using System;
using System.Collections.Generic;

namespace Choffmeister.Advices.Test
{
    public sealed class ByPassAdviceAttribute : AdviceAttribute
    {
        public override object Execute(MulticastDelegate dele, ParameterCollection parameters)
        {
            return null;
        }
    }
}