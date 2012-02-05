using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Choffmeister.Advices
{
    public abstract class BeforeAdviceAttribute : AdviceAttribute
    {
        public abstract void ExecuteBefore(ParameterCollection parameters);

        public override object Execute(MulticastDelegate dele, ParameterCollection parameters)
        {
            this.ExecuteBefore(parameters);

            return dele.DynamicInvoke(parameters.AllParameterValues);
        }
    }
}
