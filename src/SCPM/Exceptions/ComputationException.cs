using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SCPM.Exceptions
{
    [Serializable]
    public class ComputationException : Exception
    {
        public ComputationException() { }
        public ComputationException(string message) : base(message) { }
        public ComputationException(string message, Exception inner) : base(message, inner) { }
        protected ComputationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
