using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace GuiBinConverter
{
    class DataProvInfo
    {
        public string Name { get; private set; }
        public string TickPath { get; private set; }
        public string BarPath { get; private set; }

        public DataProvInfo(string name, string tickPath, string barPath)
        {
            Name = name;
            TickPath = tickPath;
            BarPath = barPath;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
