using System;
using System.Collections.Generic;
using System.Text;

namespace Fosol.Data
{
    public class DataSourceOptions
    {
        public string ConnectionString { get; set; }

        public bool Drop { get; set; } = false;

        public bool Create { get; set; } = false;

        public bool Clear { get; set; } = false;

        public bool Initialize { get; set; } = false;

        public bool Seed { get; set; } = false;
    }
}
