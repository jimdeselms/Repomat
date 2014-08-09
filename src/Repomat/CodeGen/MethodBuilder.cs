using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.CodeGen
{
    internal abstract class MethodBuilder
    {
        protected MethodBuilderFactory _factory { get { return _methodBuilderFactory; } }

        private readonly CodeBuilder _codeBuilder;
        private readonly RepositoryDef _repoDef;
        private readonly MethodDef _methodDef;
        private readonly bool _newConnectionEveryTime;
        private readonly MethodBuilderFactory _methodBuilderFactory;

        protected CodeBuilder CodeBuilder { get { return _codeBuilder; } }
        protected RepositoryDef RepoDef { get { return _repoDef; } }
        protected MethodDef MethodDef { get { return _methodDef; } }
        protected bool NewConnectionEveryTime { get { return _newConnectionEveryTime; } }

        internal MethodBuilder(CodeBuilder codeBuilder, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, MethodBuilderFactory methodBuilderFactory)
        {
            _codeBuilder = codeBuilder;
            _repoDef = repoDef;
            _methodDef = methodDef;
            _newConnectionEveryTime = newConnectionEveryTime;
            _methodBuilderFactory = methodBuilderFactory;
        }

        public abstract void GenerateCode();

        protected void GenerateConnectionAndStatementHeader()
        {
            string connectionExpr;

            bool makeNewConnection = MethodDef.ConnectionOrTransactionOrNull == null && _newConnectionEveryTime;

            if (makeNewConnection)
            {
                connectionExpr = "conn";
            }
            else if (MethodDef.ConnectionOrTransactionOrNull == null)
            {
                connectionExpr = "_connection";
            }
            else if (MethodDef.ConnectionOrTransactionOrNull.IsConnection)
            {
                connectionExpr = MethodDef.ConnectionOrTransactionOrNull.Name;
            }
            else
            {
                connectionExpr = MethodDef.ConnectionOrTransactionOrNull.Name + ".Connection";
            }

            CodeBuilder.WriteLine(MethodDef.ToString());
            CodeBuilder.OpenBrace();
            if (makeNewConnection)
            {
                CodeBuilder.WriteLine("using (var conn = _connectionFactory())");
            }
            else
            {
                CodeBuilder.WriteLine("lock ({0})", connectionExpr);
            }
            CodeBuilder.OpenBrace();
            if (makeNewConnection)
            {
                CodeBuilder.WriteLine("conn.Open();");
            }
            CodeBuilder.WriteLine("using (var cmd = {0}.CreateCommand())", connectionExpr);
            CodeBuilder.OpenBrace();
            if (MethodDef.ConnectionOrTransactionOrNull != null && MethodDef.ConnectionOrTransactionOrNull.IsTransaction)
            {
                CodeBuilder.WriteLine("cmd.Transaction = {0};", MethodDef.ConnectionOrTransactionOrNull.Name);
            }
        }

        protected void GenerateCodeForSql(string sql)
        {
            var method = MethodDef.CloneToCustomQuery(sql);

            var customMethodBuilder = _factory.Create(method);
            customMethodBuilder.GenerateCode();
        }

        protected void GenerateMethodFooter()
        {
            CodeBuilder.CloseBrace();
            CodeBuilder.CloseBrace();
            CodeBuilder.CloseBrace();
        }

        protected void AddParameterToParameterList(PropertyDef column)
        {
            string parmValue = GetParmValue(string.Format("{0}.{1}", MethodDef.DtoParameterOrNull.Name, column.PropertyName), column.Type);
            CodeBuilder.OpenBrace();
            CodeBuilder.WriteLine("var parm = cmd.CreateParameter();");
            CodeBuilder.WriteLine("parm.ParameterName = \"@{0}\";", column.PropertyName);
            CodeBuilder.WriteLine("parm.Value = {0};", parmValue);
            if (column.Type == typeof(byte[]))
            {
                CodeBuilder.WriteLine("if (({0}) == System.DBNull.Value)", parmValue);
                CodeBuilder.OpenBrace();
                CodeBuilder.WriteLine("parm.Size = -1;");
                CodeBuilder.WriteLine("parm.DbType = System.Data.DbType.Binary;");
                CodeBuilder.CloseBrace();
            }
            CodeBuilder.WriteLine("cmd.Parameters.Add(parm);");
            CodeBuilder.CloseBrace();
        }

        protected string GetParmValue(string text, Type type)
        {
            // If the thing can be null, then convert it to DBNull.
            if (type.IsValueType && !type.IsNullable())
            {
                return text;
            }
            else
            {
                return string.Format("{0} == null ? (object)System.DBNull.Value : (object){0}", text);
            }
        }
    }
}
