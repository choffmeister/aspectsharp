using System;

namespace AspectSharp.Advices
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
