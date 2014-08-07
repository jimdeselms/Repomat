using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class CreateTableMethodBuilder : MethodBuilder
    {
        private readonly Func<PropertyDef, bool, string> _sqlPropertyMapFunc;

        internal CreateTableMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, Func<PropertyDef, bool, string> sqlPropertyMapFunc, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
            _sqlPropertyMapFunc = sqlPropertyMapFunc;
        }

        public override void GenerateCode()
        {
            GenerateConnectionAndStatementHeader();

            CodeBuilder.Write("cmd.CommandText = @\"create table {0} (", RepoDef.TableName);

            List<string> columns = new List<string>();
            foreach (var property in RepoDef.Properties)
            {
                bool isIdentity = RepoDef.HasIdentity && RepoDef.PrimaryKey[0].ColumnName == property.ColumnName;
                columns.Add(string.Format("{0} {1}", property.ColumnName, _sqlPropertyMapFunc(property, isIdentity)));
            }
            CodeBuilder.Write(string.Join(", ", columns));

            if (RepoDef.PrimaryKey.Count > 0)
            {
                CodeBuilder.Write(", CONSTRAINT pk_{0} PRIMARY KEY ({1})", RepoDef.TableName, string.Join(", ", RepoDef.PrimaryKey.Select(pk => pk.ColumnName)));
            }

            CodeBuilder.WriteLine(")\";");
            CodeBuilder.WriteLine("cmd.ExecuteNonQuery();");

            GenerateMethodFooter();
        }
    }
}
