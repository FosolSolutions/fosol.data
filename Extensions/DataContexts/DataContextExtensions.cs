using Fosol.Data.Extensions.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Fosol.Data.Extensions.DataContexts
{
    public static class DataContextExtensions
    {
        public static DataSourceAttribute GetDataSource(this DataContext context)
        {
            return context.GetType().GetDataSource();
        }

        public static string GetDataSourceName(this DataContext context)
        {
            return context.GetDataSource()?.Name ?? context.GetType().Name;
        }

        public static IEnumerable<PropertyInfo> GetRepositories(this DataContext repo)
        {
            return repo.GetType().GetProperties().Where(p => p.PropertyType.BaseType == typeof(Repository));
        }
    }
}
