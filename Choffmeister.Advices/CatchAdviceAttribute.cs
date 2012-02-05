using System;

namespace Choffmeister.Advices
{
    public abstract class CatchAdviceAttribute : AdviceAttribute
    {
        public abstract object ExecuteOnException(Exception exception, ParameterCollection parameters);

        public override object Execute(MulticastDelegate dele, ParameterCollection parameters)
        {
            try
            {
                return dele.DynamicInvoke(parameters.AllParameterValues);
            }
            catch (Exception ex)
            {
                return this.ExecuteOnException(ex, parameters);
            }
        }
    }
}