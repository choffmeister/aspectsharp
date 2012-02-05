﻿using System;

namespace Choffmeister.Advices
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
    public abstract class AdviceAttribute : Attribute
    {
        public AdviceAttribute()
        {
        }

        public abstract object Execute(MulticastDelegate dele, ParameterCollection parameters);
    }
}