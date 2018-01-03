using Fosol.Core.Extensions.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Fosol.Data.Extensions.PropertyInfos;
using Fosol.Data.Extensions.Types;

namespace Fosol.Data.Extensions.Repositories
{
    public static class RepositoryExtensions
    {
        public static string GetTableName(this Repository repo)
        {
            return repo.GetType().GetGenericArguments()[0].GetTableName();
        }

        public static string GetTableName<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetTableName();
        }

        public static string GetScheme<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetScheme();
        }

        public static IEnumerable<PropertyInfo> GetPrimaryKeys<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetPrimaryKeys();
        }

        public static PropertyInfo GetIdentityKey<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetIdentityKey();
        }

        public static IEnumerable<PropertyInfo> GetIdentityKeys<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetIdentityKeys();
        }

        public static IEnumerable<PropertyInfo> GetForeignKeys<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetForeignKeys();
        }

        public static IEnumerable<IGrouping<string, PropertyInfo>> GetIndexes<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetIndexes();
        }

        public static IEnumerable<PropertyInfo> GetComputed<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetProperties().Where(p => p.GetCustomAttributes().Any(a => typeof(ComputedAttribute).IsAssignableFrom(a.GetType())));
        }

        public static PropertyInfo GetRowVersion<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.EntityType.GetRowVersion();
        }

        public static IEnumerable<PropertyInfo> GetEntityProperties<TEntity>(this Repository<TEntity> repo, bool scalarOnly = true) where TEntity : class
        {
            return repo.EntityType.GetEntityProperties();
        }

        public static IEnumerable<string> GetEntityColumnNames<TEntity>(this Repository<TEntity> repo, bool scalarOnly = true) where TEntity : class
        {
            return repo.GetEntityProperties(scalarOnly).Select(p => p.GetColumnName());
        }

        public static IEnumerable<object> GetEntityPropertyValues<TEntity>(this Repository<TEntity> repo, TEntity entity, bool scalarOnly = true) where TEntity : class
        {
            return repo.GetEntityProperties(scalarOnly).Select(p => p.GetValue(entity));
        }

        public static IEnumerable<PropertyInfo> GetEntityConstraints<TEntity>(this Repository<TEntity> repo) where TEntity : class
        {
            return repo.GetEntityProperties(false).Where(p => p.GetPrimaryKey() != null || p.GetIndex() != null || p.GetForeignKey() != null);
        }
    }
}
