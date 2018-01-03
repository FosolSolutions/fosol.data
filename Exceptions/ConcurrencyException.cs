using Fosol.Data.Extensions.Repositories;
using Fosol.Data.Extensions.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data.Exceptions
{
    public class ConcurrencyException : DataException
    {
        public ConcurrencyException()
        {

        }

        public ConcurrencyException(string message) : base(message)
        {

        }

        public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {

        }


        public ConcurrencyException(Repository repository) : base($"{repository.GetTableName()} cannot be updated because it has already been updated by someone else.")
        {

        }
    }
}
