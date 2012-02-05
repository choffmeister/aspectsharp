using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Choffmeister.Advices
{
    public abstract class AfterAdviceAttribute : AdviceAttribute
    {
        public abstract object ExecuteAfter(object result, ParameterCollection parameters);

        public override object Execute(MulticastDelegate dele, ParameterCollection parameters)
        {
            object result = dele.DynamicInvoke(parameters.AllParameterValues);

            return this.ExecuteAfter(result, parameters);

        }
    }
}
