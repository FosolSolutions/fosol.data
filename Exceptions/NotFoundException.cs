using Fosol.Data.Extensions.Repositories;
using Fosol.Data.Extensions.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data.Exceptions
{
    public class NotFoundException : DataException
    {
        public NotFoundException()
        {

        }

        public NotFoundException(string message) : base(message)
        {

        }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        {

        }


        public NotFoundException(Repository repository) : base($"{repository.GetTableName()} does not contain a record that matches the specified key.")
        {

        }
    }
}
