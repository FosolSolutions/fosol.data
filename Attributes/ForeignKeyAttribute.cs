using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class ForeignKeyAttribute : Attribute
    {
        #region Properties
        public string Name { get; set; }

        public string[] Columns { get; set; }

        public ReferenceDeleteAction OnDelete { get; set; } = ReferenceDeleteAction.NoAction;

        public ReferenceUpdateAction OnUpdate { get; set; } = ReferenceUpdateAction.NoAction;

        public int Order { get; }
        #endregion

        #region Constructors
        public ForeignKeyAttribute(int order, params string[] columns)
        {
            this.Order = order;
            this.Columns = columns;
        }

        public ForeignKeyAttribute(string name, int order, params string[] columns)
        {
            this.Name = name;
            this.Order = order;
            this.Columns = columns;
        }

        public ForeignKeyAttribute(params string[] columns)
        {
            this.Columns = columns;
        }
        #endregion  
    }
}
