using Fosol.Core.Extensions.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Fosol.Data.Extensions.PropertyInfos;

namespace Fosol.Data.Extensions.Types
{
    public static class TypeExtensions
    {
        public static DataSourceAttribute GetDataSource(this Type type)
        {
            return type.GetCustomAttribute<DataSourceAttribute>();
        }

        public static string GetTableName(this Type type)
        {
            return type.GetTable()?.Name ?? type.Name;
        }

        public static string GetScheme(this Type type)
        {
            return type.GetTable()?.Scheme ?? "dbo";
        }

        public static TableAttribute GetTable(this Type type)
        {
            return type.GetCustomAttribute<TableAttribute>();
        }

        public static PropertyInfo GetRowVersion(this Type type)
        {
            return type.GetProperties().FirstOrDefault(p => p.GetCustomAttribute<RowVersionAttribute>() != null);
        }

        public static IEnumerable<PropertyInfo> GetPrimaryKeys(this Type type)
        {
            return type.GetProperties().Where(p => p.GetPrimaryKey() != null).OrderBy(p => p.GetPrimaryKey().Order).OrderBy(p => p.GetPrimaryKey().Name);
        }

        public static IEnumerable<PropertyInfo> GetForeignKeys(this Type type)
        {
            return type.GetProperties().Where(p => p.GetForeignKey() != null);
        }

        public static IEnumerable<PropertyInfo> GetUnmappedKeys(this Type type)
        {
            var mapped = type.GetEntityProperties();
            return type.GetProperties().Where(p => Type.GetTypeCode(p.PropertyType) == TypeCode.Object && p.GetForeignKey() == null).Except(mapped);
        }

        public static IEnumerable<IGrouping<string, PropertyInfo>> GetIndexes(this Type type)
        {
            return type.GetProperties().Where(p => p.GetIndex() != null).OrderBy(p => p.GetIndex().Name).OrderBy(p => p.GetIndex().Order).GroupBy(p => p.GetIndex().Name);
        }

        public static IEnumerable<PropertyInfo> GetIdentityKeys(this Type type)
        {
            return type.GetProperties().Where(p => p.GetIdentity() != null);
        }

        public static PropertyInfo GetIdentityKey(this Type type)
        {
            return type.GetProperties().FirstOrDefault(p => p.GetIdentity() != null && p.GetPrimaryKey() != null);
        }

        public static IEnumerable<PropertyInfo> GetComputed(this Type type)
        {
            return type.GetProperties().Where(p => p.GetComputed() != null);
        }

        public static IEnumerable<PropertyInfo> GetEntityProperties(this Type type, bool scalarOnly = true)
        {
            return type.GetProperties().Where(p => 
                p.GetIgnore() == null
                && ((Type.GetTypeCode(p.PropertyType) == TypeCode.Object
                    && p.GetForeignKey() == null
                    && p.PropertyType.In(typeof(Guid), typeof(byte[])))
                || (scalarOnly && Type.GetTypeCode(p.PropertyType).In(
                    TypeCode.Boolean
                    , TypeCode.Char
                    , TypeCode.DateTime
                    , TypeCode.Decimal
                    , TypeCode.Double
                    , TypeCode.Int16
                    , TypeCode.Int32
                    , TypeCode.Int64
                    , TypeCode.Single
                    , TypeCode.String
                    , TypeCode.Byte
                    , TypeCode.SByte
                    , TypeCode.UInt16
                    , TypeCode.UInt32
                    , TypeCode.UInt64))));
        }

        public static IEnumerable<string> GetColumnNames(this Type type, bool scalarOnly = true)
        {
            return type.GetEntityProperties(scalarOnly).Select(p => p.GetColumnName());
        }

        public static IEnumerable<object> GetPropertyValues<T>(this Type type, T entity, bool scalarOnly = true) where T : class
        {
            return type.GetEntityProperties(scalarOnly).Select(p => p.GetValue(entity));
        }
    }
}
