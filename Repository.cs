using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Fosol.Data
{
    public abstract class Repository
    {
        #region Variables
        #endregion

        #region Properties
        public abstract bool HasDependency { get; }

        public abstract Repository[] Dependencies { get; }
        #endregion

        #region Constructors
        #endregion
    }
}
