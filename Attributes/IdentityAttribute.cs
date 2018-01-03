using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class IdentityAttribute : ComputedAttribute
    {
        #region Properties
        public int Seed { get; }
        public int Increment { get; }
        #endregion

        #region Constructors
        public IdentityAttribute(int seed = 1, int increment = 1)
        {
            this.Seed = seed;
            this.Increment = increment;
        }
        #endregion  
    }
}
