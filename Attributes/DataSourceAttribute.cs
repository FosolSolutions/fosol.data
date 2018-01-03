using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DataSourceAttribute : Attribute
    {
        #region Properties
        public string Name { get; }

        public string Scheme { get; }
        #endregion

        #region Constructors
        public DataSourceAttribute(string name, string scheme = "dbo")
        {
            this.Name = name;
            this.Scheme = scheme;
        }
        #endregion  
    }
}
