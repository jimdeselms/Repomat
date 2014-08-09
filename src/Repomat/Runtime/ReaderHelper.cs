using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.Runtime
{
    public static class ReaderHelper
    {
        public static void VerifyFieldsAreUnique(IDataReader reader)
        {
            HashSet<string> names = new HashSet<string>();
            int count = reader.FieldCount;
            for (int i = 0; i < count; i++)
            {
                string col = GetColumnName(reader, i);
                if (names.Contains(col))
                {
                    throw new RepomatException(string.Format("Column {0} exists more than once in result set", col));
                }

                names.Add(col);
            }
        }


        public static int GetIndexForColumn(IDataReader reader, string columnName)
        {
            int count = reader.FieldCount;
            for (int i = 0; i < count; i++)
            {
                if (GetColumnName(reader, i) == columnName.ToLower())
                {
                    return i;
                }
            }

            throw new RepomatException(string.Format("Column {0} not found in result set", columnName));
        }

        public static byte[] ConvertToByteArray(IDataReader reader)
        {
            return null;
        }

        private static string GetColumnName(IDataReader reader, int i)
        {
            string columnNameFromReader = reader.GetName(i);
            int dotIdx = columnNameFromReader.LastIndexOf('.');
            if (dotIdx == -1)
            {
                return columnNameFromReader.ToLower();
            }
            else
            {
                return columnNameFromReader.Substring(dotIdx + 1).ToLower();
            }
        }
    }
}
