using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data.Extensions
{
    public static class DataContextExtensions
    {
        public static IServiceCollection AddDataContext(this IServiceCollection services, Action<DataSourceOptions> options)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            services.Configure(options);
            return services.AddSingleton<IDataContext, DataContext>();
        }
    }
}
