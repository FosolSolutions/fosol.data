using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data.Exceptions
{
    public abstract class DataException : Exception
    {
        public DataException()
        {

        }

        public DataException(string message) : base(message)
        {

        }

        public DataException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
