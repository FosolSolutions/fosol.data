using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class IndexAttribute : Attribute
    {
        #region Properties
        public string Name { get; }

        public bool IsUnique { get; set; }

        public bool IsClustered { get; set; } = false;

        public int Order { get; set; }
        #endregion

        #region Constructors
        public IndexAttribute(string name, bool isUnique = true, int order = 0)
        {
            this.Name = name;
            this.IsUnique = isUnique;
            this.Order = order;
        }
        #endregion  
    }
}
