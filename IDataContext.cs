using Dapper;
using Fosol.Core.Extensions.Generics;
using Fosol.Data.Extensions.DataContexts;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Fosol.Data
{
    public interface IDataContext : IDisposable
    {
        #region Variables
        #endregion

        #region Properties
        #endregion

        #region Methods
        int Execute(string sql);
        #endregion
    }
}
