using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LibGit2Sharp
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    [Conditional("CodeGeneration")]
    public class CustomMarshalerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMarshalerAttribute"/> class.
        /// </summary>
        /// <param name="customMarshaler">The type that derives from ICustomMarshaler.</param>
        /// <param name="friendlyType">The type to expose in the generated overload.</param>
        public CustomMarshalerAttribute(Type customMarshaler, Type friendlyType)
        {   
        }
    }
}
