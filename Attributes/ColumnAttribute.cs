using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ColumnAttribute : Attribute
    {
        #region Properties
        public string Name { get; }

        public string DataType { get; set; }

        public int Order { get; }
        #endregion

        #region Constructors
        public ColumnAttribute(string name = null, int order = 0)
        {
            this.Name = name;
            this.Order = order;
        }
        #endregion  
    }
}
