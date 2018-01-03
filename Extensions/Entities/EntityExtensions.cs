using Fosol.Core.Extensions.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Fosol.Data.Extensions.PropertyInfos;
using Fosol.Data.Extensions.Types;
using System.Dynamic;

namespace Fosol.Data.Extensions.Entities
{
    public static class EntityExtensions
    {
        public static string GetTableName<TEntity>(this TEntity entity) where TEntity : class
        {
            return typeof(TEntity).GetTableName();
        }

        public static string GetScheme<TEntity>(this TEntity entity) where TEntity : class
        {
            return typeof(TEntity).GetScheme();
        }

        public static IEnumerable<PropertyInfo> GetPrimaryKeys<TEntity>(this TEntity entity) where TEntity : class
        {
            return typeof(TEntity).GetPrimaryKeys();
        }

        public static PropertyInfo GetIdentity<TEntity>(this TEntity entity) where TEntity : class
        {
            return typeof(TEntity).GetIdentity();
        }

        public static IEnumerable<PropertyInfo> GetProperties<TEntity>(this TEntity entity, bool scalarOnly = true) where TEntity : class
        {
            return typeof(TEntity).GetEntityProperties(scalarOnly);
        }

        public static IEnumerable<string> GetColumnNames<TEntity>(this TEntity entity, bool scalarOnly = true) where TEntity : class
        {
            return entity.GetProperties(scalarOnly).Select(p => p.GetColumnName());
        }

        public static IEnumerable<object> GetPropertyValues<TEntity>(this TEntity entity, bool scalarOnly = true) where TEntity : class
        {
            return entity.GetProperties(scalarOnly).Select(p => p.GetValue(entity));
        }

        public static object GetEntityKey<TEntity>(this TEntity entity) where TEntity : class
        {
            var key = new ExpandoObject();

            foreach (var prop in typeof(TEntity).GetPrimaryKeys())
            {
                key.TryAdd(prop.Name, prop.GetValue(entity));
            }
            return key;
        }
    }
}
