﻿using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat
{
    public class PropertyBuilder
    {
        private readonly PropertyDef _columnDef;

        internal PropertyBuilder(PropertyDef columnDef)
        {
            _columnDef = columnDef;
        }

        public PropertyBuilder SetColumnName(string newColumnName)
        {
            _columnDef.ColumnName = newColumnName;

            return this;
        }

        public PropertyBuilder SetWidth(int width)
        {
            _columnDef.StringWidthOrNull = width;

            return this;
        }
    }
}
