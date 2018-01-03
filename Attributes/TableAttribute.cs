using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        #region Properties
        public string Name { get; }

        public string Scheme { get; }
        #endregion

        #region Constructors
        public TableAttribute(string name, string scheme = "dbo")
        {
            this.Name = name;
            this.Scheme = scheme;
        }
        #endregion  
    }
}
