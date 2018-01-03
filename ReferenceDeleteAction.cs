using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Fosol.Data
{
    public enum ReferenceDeleteAction
    {
        [Description("NO ACTION")]
        NoAction = 0,
        [Description("CASCADE")]
        Cascade = 1,
        [Description("SET NULL")]
        SetNull = 2,
        [Description("SET DEFAULT")]
        SetDefault = 3
    }
}
