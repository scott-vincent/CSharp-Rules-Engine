using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Exceptions
{
    public class RulesException : Exception
    {
        public RulesException()
        {
        }

        public RulesException(string message) : base(message)
        {
        }

        public RulesException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
