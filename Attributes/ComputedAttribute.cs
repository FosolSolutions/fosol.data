using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ComputedAttribute : Attribute
    {
        #region Properties
        #endregion

        #region Constructors
        public ComputedAttribute()
        {
        }
        #endregion  
    }
}
