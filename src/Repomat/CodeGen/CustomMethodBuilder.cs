using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal class CustomMethodBuilder : MethodBuilder
    {
        internal CustomMethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
            : base(codeBuilder, repoDef, methodDef, newConnectionEveryTime, methodBuilderFactory)
        {
        }

        public override void GenerateCode()
        {
            if (MethodDef.ReturnType == typeof(void))
            {
                GenerateCodeForNonQueryMethod(MethodDef);
            }
            else
            {
                var getBuilder = _factory.Create(MethodDef, MethodType.Get);
                getBuilder.GenerateCode();
            }
        }

        private void GenerateCodeForNonQueryMethod(MethodDef method)
        {
            GenerateConnectionAndStatementHeader();

            CodeBuilder.WriteLine("cmd.CommandText = @\"{0}\";", method.CustomSqlOrNull.Replace("\"", "\"\""));

            if (method.CustomSqlIsStoredProcedure)
            {
                CodeBuilder.WriteLine("cmd.CommandType = System.Data.CommandType.StoredProcedure;");
            }

            foreach (var arg in method.Parameters)
            {
                if (arg.IsPrimitiveType)
                {
                    CodeBuilder.OpenBrace();
                    CodeBuilder.WriteLine("var parm = cmd.CreateParameter();");
                    CodeBuilder.WriteLine("parm.ParameterName = \"@{0}\";", arg.Name);
                    CodeBuilder.WriteLine("parm.Value = {0};", GetParmValue(arg.Name, arg.Type));
                    CodeBuilder.WriteLine("cmd.Parameters.Add(parm);");
                    CodeBuilder.CloseBrace();
                }
            }

            CodeBuilder.WriteLine("cmd.ExecuteNonQuery();");

            GenerateMethodFooter();
        }
    }
}
