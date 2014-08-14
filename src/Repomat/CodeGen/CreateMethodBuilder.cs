using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class CreateMethodBuilder : MethodBuilder
    {
        private readonly string _statementSeparator;
        private readonly string _scopeIdentityFunction;
        private readonly string _scopeIdentityDatatype;

        internal CreateMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, string statementSeparator, string scopeIdentityFunction, string scopeIdentityDatatype, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
            _statementSeparator = statementSeparator;
            _scopeIdentityFunction = scopeIdentityFunction;
            _scopeIdentityDatatype = scopeIdentityDatatype;
        }

        public override void GenerateCode()
        {
            GenerateConnectionAndStatementHeader();

            CodeBuilder.Write("cmd.CommandText = @\"insert into [{0}] (", EntityDef.TableName);
            CodeBuilder.Write(string.Join(", ", EntityDef.NonPrimaryKeyColumns.Select(c => c.ColumnName)));
            CodeBuilder.Write(") values (");
            CodeBuilder.Write(string.Join(", ", EntityDef.NonPrimaryKeyColumns.Select(c => "@" + c.PropertyName)));
            CodeBuilder.WriteLine("){0} SELECT {1}\";", _statementSeparator, _scopeIdentityFunction);

            foreach (var column in EntityDef.NonPrimaryKeyColumns)
            {
                AddParameterToParameterList(column);
            }

            CodeBuilder.WriteLine("{0}.{1} = (int)({2})cmd.ExecuteScalar();", MethodDef.DtoParameterOrNull.Name, EntityDef.PrimaryKey[0].PropertyName, _scopeIdentityDatatype);

            if (MethodDef.ReturnsInt)
            {
                CodeBuilder.WriteLine("return {0}.{1};", MethodDef.DtoParameterOrNull.Name, EntityDef.PrimaryKey[0].PropertyName);
            }

            GenerateMethodFooter();
        }
    }
}
