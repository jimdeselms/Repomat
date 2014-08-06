using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat
{
    public enum MethodType
    {
        Get,
        Insert,
        Delete,
        Update,
        Create,
        Upsert,
        CreateTable,
        DropTable,
        TableExists,
        GetCount,
        Exists,
        Custom,
    }
}
