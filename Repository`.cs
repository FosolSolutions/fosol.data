using Dapper;
using Fosol.Data.Extensions.PropertyInfos;
using Fosol.Data.Extensions.Repositories;
using Fosol.Data.Extensions.Types;
using Fosol.Core.Extensions.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Dynamic;
using System.Data.SqlClient;
using System.Data;
using Fosol.Data.Exceptions;
using Fosol.Core.Extensions.Types;
using Fosol.Core;

namespace Fosol.Data
{
    public class Repository<TEntity> : Repository, IEnumerable<TEntity>, IEnumerable
        where TEntity : class
    {
        #region Variables
        private readonly DataContext _context;
        private readonly static PropertyInfo[] _properties;
        private readonly static PropertyInfo[] _primarykeys;
        private readonly static PropertyInfo[] _foreignkeys;
        private readonly static PropertyInfo[] _unmappedKeys;
        private readonly static PropertyInfo[] _computed;
        private readonly static IGrouping<string, PropertyInfo>[] _indexes;
        internal readonly Repository[] _dependencies;
        private static System.Threading.ReaderWriterLockSlim _lock = new System.Threading.ReaderWriterLockSlim();
        private readonly Dictionary<string, TEntity> _entities = new Dictionary<string, TEntity>();

        public Type EntityType { get; } = typeof(TEntity);
        #endregion

        #region Properties
        public override bool HasDependency { get { return _foreignkeys.Length > 0; } }

        public override Repository[] Dependencies { get { return _dependencies; } }
        #endregion

        #region Constructors
        static Repository()
        {
            var type = typeof(TEntity);
            _properties = type.GetEntityProperties().ToArray();
            _primarykeys = type.GetPrimaryKeys().ToArray();
            _foreignkeys = type.GetForeignKeys().ToArray();
            _unmappedKeys = type.GetUnmappedKeys().ToArray();
            _computed = type.GetComputed().ToArray();
            _indexes = type.GetIndexes().ToArray();
        }

        public Repository(DataContext context)
        {
            _context = context;

            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_dependencies == null)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        var deps = new List<Repository>();
                        foreach (var prop in _foreignkeys.Concat(_unmappedKeys))
                        {
                            var fkey = prop.GetForeignKey();
                            var is_enumerable = prop.PropertyType.IsEnumerable();
                            var type = (is_enumerable ? prop.PropertyType.GetGenericType() : prop.PropertyType);

                            if (fkey == null)
                            {
                                // If the property has no PrimaryKey we can't map it.
                                // If the property maps back to this entity we can't map it.
                                if (type.GetPrimaryKeys().Count() == 0
                                    || type.GetProperties().Any(p => p.PropertyType == this.EntityType))
                                    continue;
                            }

                            var gtype = typeof(Repository<>);
                            var rtype = gtype.MakeGenericType(new[] { type });
                            var rp = _context.Repositories.FirstOrDefault(p => p.PropertyType == rtype);
                            if (rp != null)
                            {
                                var repo = rp.GetValue(_context);

                                if (repo == null)
                                {
                                    repo = Activator.CreateInstance(rtype, _context);
                                    rp.SetValue(_context, repo);
                                }
                                // The repository was mapped in the DataContext.
                                deps.Add((Repository)repo);
                            }
                            else
                            {
                                // The repository is not mapped in the DataContext.
                                deps.Add((Repository)Activator.CreateInstance(rtype, _context));
                            }
                        }
                        _dependencies = deps.ToArray();
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
        #endregion

        #region Methods
        private string GenerateKeyWhere()
        {
            var pkeys = new List<string>();
            for (var i = 0; i < _primarykeys.Count(); i++)
            {
                var key = _primarykeys[i];
                if (Type.GetTypeCode(key.PropertyType) == TypeCode.Object)
                {
                    var fpkeys = key.GetForeignKeyColumnNames().ToArray();
                    for (var i2 = 0; i2 < fpkeys.Length; i2++)
                    {
                        var fkey = fpkeys[i2];
                        pkeys.Add($"[{fkey}]=@{fkey}");
                    }
                }
                else
                {
                    var name = key.GetColumnName();
                    pkeys.Add($"[{name}]=@{name}");
                }
            }
            return String.Join(" AND ", pkeys);
        }

        public TEntity Find(params object[] keys)
        {
            var param = new ExpandoObject();
            for (var i = 0; i < _primarykeys.Count(); i++)
            {
                var key = _primarykeys[i];
                if (Type.GetTypeCode(key.PropertyType) == TypeCode.Object)
                {
                    var fpkeys = key.GetForeignKeyColumnNames().ToArray();
                    for(var i2 = 0; i2 < fpkeys.Length; i2++)
                    {
                        param.TryAdd(fpkeys[i2], keys[i+=i2]);
                    }
                }
                else
                {
                    param.TryAdd(key.GetColumnName(), keys[i]);
                }
            }
            return _context.Connection.QuerySingleOrDefault<TEntity>($"SELECT * FROM [{this.GetScheme()}].[{this.GetTableName()}] WHERE {GenerateKeyWhere()}", param);
        }

        public IEnumerable<TEntity> Where(object conditions, IDbTransaction transaction = null)
        {
            var properties = conditions.GetType().GetProperties();
            var where = String.Join(" AND ", properties.Select(p => $"[{p.Name}]=@{p.Name}"));
            return _context.Connection.Query<TEntity>($"SELECT * FROM [{this.GetScheme()}].[{this.GetTableName()}] WHERE {where}", conditions, transaction: transaction);
        }

        public IEnumerable<TEntity> Where(string where, object param = null, IDbTransaction transaction = null)
        {
            return _context.Connection.Query<TEntity>($"SELECT * FROM [{this.GetScheme()}].[{this.GetTableName()}] WHERE {where}", param, transaction: transaction);
        }

        public IEnumerable<TEntity> All(IDbTransaction transaction = null)
        {
            return _context.Connection.Query<TEntity>($"SELECT * FROM [{this.GetScheme()}].[{this.GetTableName()}]", transaction: transaction);
        }

        public IEnumerable<TEntity> Query(string query, object param = null, IDbTransaction transaction = null)
        {
            return _context.Connection.Query<TEntity>(query, param, transaction: transaction);
        }

        public IEnumerable<TEntity> Query<TFirst, TSecond>(string query, Func<TFirst, TSecond, TEntity> map, string splitOn, object param = null, IDbTransaction transaction = null)
        {
            return _context.Connection.Query<TFirst, TSecond, TEntity>(query, map, param, splitOn: splitOn, transaction: transaction);
        }

        public IEnumerable<TEntity> Query<TFirst, TSecond, TThird>(string query, Func<TFirst, TSecond, TThird, TEntity> map, string splitOn, object param = null, IDbTransaction transaction = null)
        {
            return _context.Connection.Query<TFirst, TSecond, TThird, TEntity>(query, map, param, splitOn: splitOn, transaction: transaction);
        }

        public IEnumerable<TEntity> Query<TFirst, TSecond, TThird, TFourth>(string query, Func<TFirst, TSecond, TThird, TFourth, TEntity> map, string splitOn, object param = null, IDbTransaction transaction = null)
        {
            return _context.Connection.Query<TFirst, TSecond, TThird, TFourth, TEntity>(query, map, param, splitOn: splitOn, transaction: transaction);
        }

        private string GenerateComputedTempTable()
        {
            return $"DECLARE @Computed TABLE ( {String.Join(", ", _computed.Select(p => $"[{p.GetColumnName()}] {p.GetDataTypeForAction()}"))} ); \n";
        }

        private string GenerateComputedOutput()
        {
            return $"OUTPUT {String.Join(", ", _computed.Select(p => $"inserted.[{p.GetColumnName()}]"))} INTO @Computed \n";
        }

        private IEnumerable<PropertyInfo> GetMappedProperties()
        {
            return _properties.Where(p => p.GetComputed() == null).Union(_foreignkeys);
        }

        private IEnumerable<string> GetColumnNames(IEnumerable<PropertyInfo> properties)
        {
            var columns = new List<string>();

            foreach(var prop in properties)
            {
                var fkey = prop.GetForeignKey();

                if (fkey == null)
                {
                    columns.Add(prop.GetColumnName());
                }
                else if (fkey.Columns == null || fkey.Columns.Count() == 0)
                {
                    // These properties are not mapped in the model.
                    var fpkeys = prop.PropertyType.GetPrimaryKeys();
                    foreach (var fprop in fpkeys)
                    {
                        columns.Add($"{prop.PropertyType.GetTableName()}_{fprop.GetColumnName()}");
                    }
                }
                else
                {
                    // These properties are mapped.
                    columns.AddRange(fkey.Columns);
                }
            }
            return columns.Distinct().ToArray();
        }

        private object PrepareDataSet(TEntity entity)
        {
            // Create an object containing foreign keys.
            var result = new ExpandoObject();
            foreach (var prop in _properties)
            {
                result.TryAdd(prop.Name, prop.GetValue(entity));
            }

            foreach (var prop in _foreignkeys)
            {
                var fkey = prop.GetForeignKey();
                var fpkeys = prop.PropertyType.GetPrimaryKeys().ToArray();

                if (fkey.Columns != null && fkey.Columns.Length != 0)
                {
                    // These are mapped foreign keys.
                    for (var i = 0; i < fkey.Columns.Length; i++)
                    {
                        var fvalue = prop.GetValue(entity);
                        result.TryAdd(fkey.Columns[i], fvalue != null ? fpkeys[i].GetValue(fvalue) : null);
                    }
                }
                else
                {
                    // These are dynamically mapped foreign keys.
                    foreach (var fprop in fpkeys)
                    {
                        var fvalue = prop.GetValue(entity);
                        result.TryAdd($"{prop.PropertyType.GetTableName()}_{fprop.GetColumnName()}", fvalue != null ? fprop.GetValue(fvalue) : null);
                    }
                }
            }

            return result;
        }

        private object PrepareDataSet(IEnumerable<TEntity> entities)
        {
            var entity_objects = entities.ToArray();
            var result = new ExpandoObject();
            for (var i = 0; i < entities.Count(); i++)
            {
                foreach (var prop in _properties)
                {
                    result.TryAdd($"{prop.GetColumnName()}{i}", prop.GetValue(entity_objects[i]));
                }

                foreach (var prop in _foreignkeys)
                {
                    var fkey = prop.GetForeignKey();
                    var fpkeys = prop.PropertyType.GetPrimaryKeys().ToArray();

                    if (fkey.Columns != null && fkey.Columns.Length != 0)
                    {
                        // These are mapped foreign keys.
                        for (var i2 = 0; i2 < fkey.Columns.Length; i2++)
                        {
                            var fvalue = prop.GetValue(entity_objects[i]);
                            result.TryAdd($"{fkey.Columns[i2]}{i}", fvalue != null ? fpkeys[i2].GetValue(fvalue) : null);
                        }
                    }
                    else
                    {
                        // These are dynamically mapped foreign keys.
                        foreach (var fprop in fpkeys)
                        {
                            var fvalue = prop.GetValue(entity_objects[i]);
                            result.TryAdd($"{prop.PropertyType.GetTableName()}_{fprop.GetColumnName()}{i}", fvalue != null ? fprop.GetValue(fvalue) : null);
                        }
                    }
                }
            }

            return result;
        }

        private string GenerateEntityKey(TEntity entity)
        {
            var key = new StringBuilder();

            foreach (var prop in _primarykeys)
            {
                key.Append($"{prop.Name}={prop.GetValue(entity)};");
            }

            return key.ToString();
        }

        public void Add(TEntity entity, SqlTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var properties = GetMappedProperties();
            var columns = GetColumnNames(properties);

            var sql = new StringBuilder();

            if (_computed.Count() > 0)
            {
                sql.Append(GenerateComputedTempTable());
            }

            sql.Append($"INSERT INTO [{this.GetScheme()}].[{this.GetTableName()}] ({String.Join(",", columns.Select(c => $"[{c}]"))}) ");

            if (_computed.Count() > 0)
            {
                sql.Append(GenerateComputedOutput());
            }

            sql.Append($"VALUES ({String.Join(",", columns.Select(c => $"@{c}"))}); \n");

            if (_computed.Count() > 0)
            {
                sql.Append("SELECT * FROM @Computed;");
            }

            var data = PrepareDataSet(entity);
            var result = _context.Connection.Query<dynamic>(sql.ToString(), data, transaction: transaction).Single();

            if (transaction != null)
            {
                transaction.Commit();
            }

            Update(entity, result);

            _entities[GenerateEntityKey(entity)] = entity;
        }

        public int Add(IEnumerable<TEntity> entities, IDbTransaction transaction = null)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            if (entities.Count() == 0)
                return 0;
            
            var properties = GetMappedProperties();
            var columns = GetColumnNames(properties);

            var sql = new StringBuilder();

            if (_computed.Count() > 0)
            {
                sql.Append(GenerateComputedTempTable());
            }

            sql.Append($"INSERT INTO [{this.GetScheme()}].[{this.GetTableName()}] ({String.Join(",", columns.Select(c => $"[{c}]"))}) \n");

            if (_computed.Count() > 0)
            {
                sql.Append(GenerateComputedOutput());
            }

            sql.Append("VALUES ");

            var items = entities.ToArray();
            for (var i = 0; i < items.Length; i++)
            {
                sql.Append($"{(i > 0 ? "\n\t," : "")}({String.Join(",", columns.Select(c => $"@{c}{i}"))})");
            }

            if (_computed.Count() > 0)
            {
                sql.Append("; \nSELECT * FROM @Computed;");
            }

            var data = PrepareDataSet(items);
            var results = _context.Connection.Query<dynamic>(sql.ToString(), data, transaction).ToList();

            if (transaction != null)
            {
                transaction.Commit();
            }

            if (_computed.Count() > 0)
            {
                // Update the original entities with the database generated values.
                for (var i = 0; i < entities.Count(); i++)
                {
                    Update(items[i], results[i]);
                    _entities[GenerateEntityKey(items[i])] = items[i];
                }
            }

            return results.Count();
        }

        private void Update(TEntity entity, dynamic result)
        {
            foreach (var prop in _computed)
            {
                IDictionary<string, object> dresult = result;
                object value = dresult[prop.Name];
                prop.SetValue(entity, value);
            }
        }

        public void Update(TEntity entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var primaries = this.GetPrimaryKeys();
            var properties = this.GetEntityProperties().Where(p => p.GetComputed() == null);
            var rowversion = this.GetRowVersion();
            var sql = new StringBuilder();

            if (_computed.Count() > 0)
            {
                sql.Append(GenerateComputedTempTable());
            }

            sql.Append($@"UPDATE [{this.GetScheme()}].[{this.GetTableName()}] ");
            sql.Append($"SET {String.Join(", ", properties.Select(p => p.GetColumnName()).Select(p => $"[{p}]=@{p}"))} ");

            if (_computed.Count() > 0)
            {
                sql.Append(GenerateComputedOutput());
            }

            sql.Append($"WHERE {GenerateKeyWhere()}");

            if (rowversion != null)
            {
                var name = rowversion.GetColumnName();
                sql.Append($" AND [{name}]=@{name}");
            }

            if (_computed.Count() > 0)
            {
                sql.Append("; SELECT * FROM @Computed;");
            }

            var data = PrepareDataSet(entity);
            var result = _context.Connection.QuerySingleOrDefault<dynamic>(sql.ToString(), data, transaction: transaction);

            if (transaction != null)
            {
                transaction.Commit();
            }

            // If result is null it means it didn't update, now find out if the id matched.
            if (result == null)
            {
                if (!_context.Connection.QuerySingle<bool>($"SELECT CAST( CASE WHEN EXISTS( SELECT * FROM [{this.GetScheme()}].[{this.GetTableName()}] WHERE {GenerateKeyWhere()} ) THEN 1 ELSE 0 END AS BIT );", data))
                {
                    // Entity does not exist.
                    throw new NotFoundException(this);
                }
                else
                {
                    // Concurrency exception.
                    throw new ConcurrencyException(this);
                }
            }

            Update(entity, result);

            _entities[GenerateEntityKey(entity)] = entity;
        }
        
        public void Delete(TEntity entity, IDbTransaction transaction = null)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var primaries = this.GetPrimaryKeys();
            var rowversion = this.GetRowVersion();

            var sql = new StringBuilder($"DELETE FROM [{this.GetScheme()}].[{this.GetTableName()}] ");
            sql.Append($"WHERE {GenerateKeyWhere()}");
            if (rowversion != null)
            {
                var name = rowversion.GetColumnName();
                sql.Append($" AND [{name}]=@{name}");
            }

            var data = PrepareDataSet(entity);
            var result = _context.Connection.Execute(sql.ToString(), data, transaction: transaction);

            if (transaction != null)
            {
                transaction.Commit();
            }

            // If result == 0 it means it didn't delete, now find out if the id matched.
            if (result == 0)
            {
                if (!_context.Connection.QuerySingle<bool>($"SELECT CAST( CASE WHEN EXISTS( SELECT * FROM [{this.GetScheme()}].[{this.GetTableName()}] WHERE {GenerateKeyWhere()} ) THEN 1 ELSE 0 END AS BIT );", data))
                {
                    // Entity does not exist.
                    throw new NotFoundException(this);
                }
                else
                {
                    // Concurrency exception.
                    throw new ConcurrencyException(this);
                }
            }

            _entities.Remove(GenerateEntityKey(entity));
        }

        /// <summary>
        /// Create the repository in the datasource.
        /// </summary>
        public void Create()
        {
            var tname = this.GetTableName();
            var tscheme = this.GetScheme();

            var lines = new List<string>();
            // Add columns.
            foreach (var property in _properties)
            {
                var primary = property.GetPrimaryKey();
                var required = property.GetRequired();
                var iattr = property.GetIdentity();
                var identity = iattr != null ? $"IDENTITY ({iattr.Seed},{iattr.Increment})" : "";
                var nullable = required != null || primary != null ? "NOT NULL" : property.IsNullable() || property.PropertyType == typeof(string) ? "NULL" : "NOT NULL";
                var dattr = property.GetDefaultValue();
                var default_value = dattr != null ? $" DEFAULT {dattr.Value}" : "";
                lines.Add($"[{property.GetColumnName()}] {property.GetDataType()} {identity} {nullable}{default_value}");
            }

            // Add columns for unmapped foreign keys.
            foreach (var property in _foreignkeys.Concat(_unmappedKeys))
            {
                var primary = property.GetPrimaryKey();
                var fkey = property.GetForeignKey();
                var required = property.GetRequired();
                var nullable = primary == null && required == null ? "NULL" : "NOT NULL";
                var fpkeys = property.PropertyType.GetPrimaryKeys();
                var isEnumerable = property.PropertyType.IsEnumerable();
                // Add foreign keys.
                if (fkey?.Columns != null && fkey?.Columns.Length > 0)
                {
                    if (fpkeys.Count() != fkey.Columns.Count())
                        throw new InvalidOperationException($"The foreign key for property '{property.Name}' has only mapped {fkey.Columns.Count()} of {fpkeys.Count()} keys.");

                    // Map the primary object to the specified columns.
                    foreach (var cname in fkey.Columns)
                    {
                        if (!_properties.Any(p => p.Name == cname))
                        {
                            throw new InvalidOperationException($"The foreign key '{cname}' has been mapped to a property that does not exist.");
                        }
                    }
                }
                else if (isEnumerable)
                {
                    // This is either a many-to-many or an unmapped on-to-many.
                    // If the other entity has a collection of the same time or if it does not have a one-to-many map.
                    //var ptype = property.PropertyType.GetGenericType();
                    //var ptype_props = ptype.GetProperties();
                    //if (ptype_props.Any(p => p.PropertyType == property.PropertyType)
                    //    || !ptype_props.Any(p => p.PropertyType == this.EntityType))
                    //{
                    //    // Create a many-to-many table.
                    //    var ftname = ptype.GetTableName();
                    //    fpkeys = ptype.GetPrimaryKeys();
                    //    var many_to_many = new List<string>();
                    //    var okeys = new List<string>();
                    //    var keys = new List<string>();
                    //    foreach (var prop in _primarykeys)
                    //    {
                    //        var cname = prop.GetColumnName();
                    //        var name = $"{tname}_{cname}";
                    //        many_to_many.Add($"[{name}] {prop.GetDataType()} NOT NULL");
                    //        okeys.Add(cname);
                    //        keys.Add(name);
                    //    }
                    //    foreach (var prop in fpkeys)
                    //    {
                    //        var cname = prop.GetColumnName();
                    //        var name = $"{ftname}_{prop.GetColumnName()}";
                    //        many_to_many.Add($"[{name}] {prop.GetDataType()} NOT NULL");
                    //        okeys.Add(cname);
                    //        keys.Add(name);
                    //    }
                    //    many_to_many.Add($"CONSTRAINT [PK_{tname}_{ftname}_map] PRIMARY KEY ({String.Join(", ", keys)})");
                    //    many_to_many.Add($"CONSTRAINT [FK_{tname}_{ftname}_{tname}] FOREIGN KEY ({String.Join(", ", keys.GetRange(0, _primarykeys.Length))}) REFERENCES [{tscheme}].[{tname}] ({String.Join(", ", okeys.GetRange(0, _primarykeys.Length))}) ON DELETE CASCADE ON UPDATE CASCADE");
                    //    var i = _primarykeys.Length > 1 ? _primarykeys.Length - 1 : 1;
                    //    many_to_many.Add($"CONSTRAINT [FK_{tname}_{ftname}_{ftname}] FOREIGN KEY ({String.Join(", ", keys.GetRange(i, fpkeys.Count()))}) REFERENCES [{tscheme}].[{ftname}] ({String.Join(", ", okeys.GetRange(i, fpkeys.Count()))}) ON DELETE CASCADE ON UPDATE CASCADE");

                    //    _context.Execute($"CREATE TABLE [{tscheme}].[{tname}_{ftname}] ( \n{String.Join("\n, ", many_to_many)} );");
                    //}
                }
                else if (fpkeys?.Count() > 0)
                {
                    // Dynamically add the primary keys as foreign keys.
                    foreach (var fprop in fpkeys)
                    {
                        lines.Add($"[{property.PropertyType.GetTableName()}_{fprop.GetColumnName()}] {fprop.GetDataType()} {nullable}");
                    }
                }
            }

            // Add constraints.
            // Add primary key.
            if (_primarykeys.Count() > 0)
            {
                var primary_key = _primarykeys.First().GetPrimaryKey();
                var name = String.IsNullOrWhiteSpace(primary_key.Name) ? $"PK_{tname}" : primary_key.Name;
                var columns = new List<string>();
                foreach (var key in _primarykeys)
                {
                    var fkey = key.GetForeignKey();
                    if (fkey == null)
                    {
                        columns.Add(key.GetColumnName());
                    }
                    else
                    {
                        columns.AddRange(key.GetForeignKeyColumnNames());
                    }
                }
                lines.Add($"CONSTRAINT [{name}] PRIMARY KEY ({String.Join(",", columns.Select(n => $"[{n}]"))})");
            }

            // Add foreign keys
            foreach (var key in _foreignkeys)
            {
                var kattr = key.GetForeignKey();
                var fscheme = key.PropertyType.GetScheme();
                var fname = key.PropertyType.GetTableName();
                var name = String.IsNullOrWhiteSpace(kattr.Name) ? $"FK_{tname}_{fname}" : kattr.Name;
                var mapped = kattr.Columns != null && kattr.Columns.Count() > 0;
                var columns = key.PropertyType.GetPrimaryKeys().Select(p => p.GetColumnName());
                var lcolumns = String.Join(",", (mapped ? kattr.Columns : columns).Select(c => $"[{(mapped ? c : $"{fname}_{c}")}]"));
                var fcolumns = String.Join(",", columns.Select(c => $"[{c}]"));
                var ondelete = kattr.OnDelete.GetDescription();
                var onupdate = kattr.OnUpdate.GetDescription();
                lines.Add($"CONSTRAINT [{name}] FOREIGN KEY ({lcolumns}) REFERENCES [{fscheme}].[{fname}] ({fcolumns}) ON DELETE {ondelete} ON UPDATE {onupdate}");
            }

            // Add indexes.
            foreach (var group in _indexes)
            {
                var iattr = group.First().GetIndex();
                var name = String.IsNullOrWhiteSpace(iattr.Name) ? $"IX_{tname}" : iattr.Name;
                var clustered = iattr.IsClustered ? "CLUSTERED" : "NONCLUSTERED";
                var columns = String.Join(",", group.Select(p => $"[{p.GetColumnName()}]"));
                if (group.First().GetIndex().IsUnique)
                {
                    lines.Add($"CONSTRAINT [{name}] UNIQUE {clustered} ({columns})");
                }
                else
                {
                    lines.Add($"INDEX [{name}] {clustered} ON ({columns}");
                }
            }

            var sql = new StringBuilder($"CREATE TABLE [{tscheme}].[{tname}] (\n\t");
            sql.AppendJoin("\t\n, ", lines);
            sql.Append(");");

            _context.Execute(sql.ToString());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            foreach (var item in _entities)
            {
                yield return item.Value;
            }
        }
        #endregion
    }
}
