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
    public class DataContext : IDataContext
    {
        #region Variables
        private DataSourceOptions _options = new DataSourceOptions();
        private IDbConnection _connection;
        private readonly PropertyInfo[] _repositories;
        private static bool _initialized = false;
        private static readonly System.Threading.ReaderWriterLockSlim _lock = new System.Threading.ReaderWriterLockSlim();
        #endregion

        #region Properties
        internal IDbConnection Connection { get { return _connection; } }

        internal PropertyInfo[] Repositories
        {
            get { return _repositories; }
        }
        #endregion

        #region Constructors
        public DataContext(string connectionString)
        {
            // Check if configuration has the connection string with the specified name.
            _connection = DataContext.CreateSqlConnection(connectionString);
            //_connection = DataContext.CreateMySqlConnection(connectionString);

            // Initialize all repositories.
            _repositories = this.GetRepositories().ToArray();
            var gtype = typeof(Repository<>);
            foreach (var prop in _repositories)
            {
                var gargs = new[] { prop.PropertyType.GetGenericArguments()[0] };
                var rtype = gtype.MakeGenericType(gargs);
                var repo = Activator.CreateInstance(rtype, this);
                prop.SetValue(this, repo);
            }
        }
        
        public DataContext(IOptions<DataSourceOptions> options) : this(options.Value)
        {
            
        }

        public DataContext(DataSourceOptions options) : this(options.ConnectionString)
        {
            _options = options;
        }
        #endregion

        #region Methods
        public void Initialize()
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (!_initialized)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        if (_initialized)
                            return;

                        var dname = this.GetDataSourceName();
                        if (_options.Drop)
                        {
                            Execute(String.Format(Resources.SQL.DropDatabase, dname));
                        }
                        else if (_options.Clear)
                        {
                            Execute(Resources.SQL.ClearObjects);
                        }

                        if (_options.Create)
                        {
                            Execute(String.Format(Resources.SQL.CreateDatabase, dname));
                        }

                        if (_options.Initialize)
                        {
                            var repos = GetAllRepositories();
                            foreach (var repo in repos)
                            {
                                var create = repo.GetType().GetMethod("Create");
                                create.Invoke(repo, null);
                            }
                        }

                        if (_options.Seed)
                        {
                            Seed();
                        }
                        _initialized = true;
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
        }

        private static MySqlConnection CreateMySqlConnection(string cs)
        {
            var scsb = new MySqlConnectionStringBuilder(cs);
            var connection = new MySqlConnection(scsb.ConnectionString);
            return connection;
        }

        private static SqlConnection CreateSqlConnection(string cs)
        {
            var scsb = new SqlConnectionStringBuilder(cs);
            var connection = new SqlConnection(scsb.ConnectionString);
            return connection;
        }

        private Repository[] GetAllRepositories()
        {
            var repos = new List<Repository>();

            OrderDependencies(_repositories.Select(p => (Repository)p.GetValue(this)), repos);

            return repos.ToArray();
        }

        private void OrderDependencies(IEnumerable<Repository> sort, IList<Repository> sorted)
        {
            foreach (var repo in sort)
            {
                if (sorted.Any(r => r.GetType() == repo.GetType()))
                {
                    continue;
                }

                if (!repo.HasDependency)
                {
                    // If it has no dependencies it goes to the front of the line.
                    sorted.Insert(0, repo);
                }
                else
                {
                    OrderDependencies(repo.Dependencies, sorted);
                    sorted.Add(repo);
                }
            }
        }

        public void Dispose()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        internal void OpenConnection()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
        }

        protected virtual void Seed()
        {

        }

        public int Execute(string sql)
        {
            OpenConnection();
            return _connection.Execute(sql);
        }
        #endregion
    }
}
