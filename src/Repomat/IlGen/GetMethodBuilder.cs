using Repomat.CodeGen;
using Repomat.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.IlGen
{
    internal class GetMethodBuilder : MethodBuilderBase
    {
        private readonly int _customQueryIdx;
        private readonly ILGenerator _ctorIlBuilder;
        private readonly SqlMethodBuilderFactory _methodBuilderFactory;

        private bool _useStrictTyping;

        internal GetMethodBuilder(TypeBuilder typeBuilder, FieldInfo connectionField, RepositoryDef repoDef, MethodDef methodDef, bool newConnectionEveryTime, int customQueryIdx, SqlMethodBuilderFactory methodBuilderFactory, bool useStrictTyping, ILGenerator ctorIlBuilder)
            : base(typeBuilder, connectionField, repoDef, methodDef, newConnectionEveryTime)
        {
            _customQueryIdx = customQueryIdx;
            _ctorIlBuilder = ctorIlBuilder;
            _useStrictTyping = useStrictTyping;
            _methodBuilderFactory = methodBuilderFactory;
        }

        protected override void GenerateMethodIl(LocalBuilder cmdVariable)
        {
            if (MethodDef.CustomSqlOrNull != null && !MethodDef.IsSimpleQuery)
            {
                // private _queryX_columnIndexesAssigned = false;
                var indexesAssignedField = DefineField<bool>(string.Format("_query{0}_columnIndexesAssigned", _customQueryIdx));
                _ctorIlBuilder.Emit(OpCodes.Ldc_I4, 0);
                _ctorIlBuilder.Emit(OpCodes.Stfld, indexesAssignedField);

                Dictionary<string, FieldBuilder> columnIndexFields = new Dictionary<string, FieldBuilder>();
                foreach (var col in RepositoryDefBuilder.GetAssignableColumnsForType(RepositoryDef.ColumnNamingConvention, MethodDef.ReturnType))
                {
                    // private _queryX_columnYIdx = 0;
                    var field = DefineField<int>(string.Format("_query{0}_column{1}Idx", _customQueryIdx, col.PropertyName));
                    columnIndexFields[col.PropertyName] = field;
                    _ctorIlBuilder.Emit(OpCodes.Ldc_I4, 0);
                    _ctorIlBuilder.Emit(OpCodes.Stfld, field);
                }
            }
            
            if (MethodDef.EntityDef != null && MethodDef.EntityDef.Type != typeof(void) && MethodDef.ReturnType.Equals(typeof(IEnumerable<>).MakeGenericType(MethodDef.EntityDef.Type)))
            {
                //GenerateCodeForEnumerableGetMethod();
            }
            else
            {
                //GenerateConnectionAndStatementHeader();
                GenerateGetMethodBody(_customQueryIdx);
                //GenerateMethodFooter();
            }

            IlGenerator.Emit(OpCodes.Ldnull);
            IlGenerator.Emit(OpCodes.Ret);
        }

        private void GenerateGetMethodBody(int queryIdx)
        {
            Type typeToGet = MethodDef.ReturnType.GetCoreType();
//            PropertyDef[] columnsToGet = DetermineColumnsToGet(typeToGet);

//            WriteSqlStatement(columnsToGet);

//            WriteParameterAssignments();

            if (MethodDef.IsSimpleQuery)
            {
                // var __result = cmd.ExecuteScalar();
                // return Convert.ToSomething(__result);
                ExecuteScalar();
                EmitScalarConversion(MethodDef.ReturnType);
                IlGenerator.Emit(OpCodes.Ret);
            }
            else
            {
                //CodeBuilder.WriteLine("using (var reader = cmd.ExecuteReader())");

                //CodeBuilder.OpenBrace();

                //if (MethodDef.IsSingleton)
                //{
                //    WriteSingletonResultRead(columnsToGet, queryIdx);
                //}
                //else if (MethodDef.ReturnType.GetCoreType().IsDatabaseType())
                //{
                //    WriteMultiRowSimpleTypeRead();
                //}
                //else
                //{
                //    WriteMultiRowResultRead(columnsToGet, queryIdx);
                //}
                //CodeBuilder.CloseBrace();
            }
        }

        private void EmitScalarConversion(Type convertToType)
        {
            PrimitiveTypeInfo.Get(convertToType).EmitConversion(IlGenerator);
        }

    }
}
