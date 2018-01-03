using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RowVersionAttribute : ComputedAttribute
    {
        #region Properties
        #endregion

        #region Constructors
        public RowVersionAttribute()
        {
        }
        #endregion  
    }
}
