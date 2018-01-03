using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
        #region Properties
        public string Name { get; }

        public int Order { get; set; }
        #endregion

        #region Constructors
        public PrimaryKeyAttribute(string name = null, int order = 0)
        {
            this.Name = name;
            this.Order = order;
        }
        #endregion  
    }
}
