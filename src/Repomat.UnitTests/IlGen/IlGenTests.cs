using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using Repomat.IlGen;
using System.Data;
using System.Data.SqlClient;
using Repomat.Schema;
using Dapper;

namespace Repomat.UnitTests.IlGen
{
    [TestFixture]
    public class IlGenTests
    {
        [Test]
        public void BuildClass()
        {
            var asmBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("MyAssembly"), AssemblyBuilderAccess.RunAndSave);
            var modBuilder = asmBuilder.DefineDynamicModule("MyModule");
            var typeBuilder = modBuilder.DefineType("MyClass");
            var myField = typeBuilder.DefineField("_this", typeof(int), FieldAttributes.Public);
            
            var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, new Type[0]);
                        
            var ilGen = ctorBuilder.GetILGenerator();
            
//            ilGen.Emit(OpCodes.Ldarg_0);
//            var objectCtor = typeof(object).GetConstructor(new Type[0]);
//            ilGen.Emit(OpCodes.Call, objectCtor);

            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldc_I4_S, (byte)25);
//            ilGen.Emit(OpCodes.Pop);
            ilGen.Emit(OpCodes.Stfld, myField);
            ilGen.Emit(OpCodes.Ret);

            var type = typeBuilder.CreateType();
            var obj = type.GetConstructor(new Type[0]).Invoke(new object[0]);

            var fieldInfo = obj.GetType().GetField("_this");
            Assert.AreEqual(25, fieldInfo.GetValue(obj));
        }

        [Test]
        public void BuildConnectionBasedRepo()
        {
            var repoDef = RepositoryDefBuilder.BuildRepositoryDef<INothing>(NamingConvention.NoOp, NamingConvention.NoOp);

            RepoSqlBuilder b = new RepoSqlBuilder(repoDef, false, RepoConnectionType.SingleConnection);
            Type t = b.CreateType();

            var ctor = t.GetConstructor(new [] { typeof(IDbConnection) });

            var conn = new SqlConnection();
            var repo = ctor.Invoke(new object[] { conn });

            Assert.AreSame(conn, repo.GetType().GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(repo));
        }

        [Test]
        public void BuildConnectionFactoryBasedRepo()
        {
            var repoDef = RepositoryDefBuilder.BuildRepositoryDef<INothing>(NamingConvention.NoOp, NamingConvention.NoOp);

            RepoSqlBuilder b = new RepoSqlBuilder(repoDef, false, RepoConnectionType.ConnectionFactory);
            Type t = b.CreateType();

            var ctor = t.GetConstructor(new[] { typeof(Func<IDbConnection>) });

            Func<IDbConnection> connFactory = () => new SqlConnection();
            var repo = ctor.Invoke(new object[] { connFactory });

            Assert.AreSame(connFactory, repo.GetType().GetField("_connectionFactory", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(repo));
        }

        [Test]
        public void Another()
        {
            var personEntityDef = new EntityDef(
                typeof(Person),
                "Person",
                new [] { new PropertyDef("Name", "Name", typeof(string)) },
                new PropertyDef[0],
                false,
                false);

            var repoDef = RepositoryDefBuilder.BuildRepositoryDef<ICreatesATable>(NamingConvention.NoOp, NamingConvention.NoOp);
//            repoDef.Methods.First(m => m.MethodName == "CreateTable").EntityDef = personEntityDef;
//            repoDef.Methods.First(m => m.MethodName == "DropTable").EntityDef = personEntityDef;

            RepoSqlBuilder b = new RepoSqlBuilder(repoDef, false, RepoConnectionType.SingleConnection);
            Type t = b.CreateType();

            var ctor = t.GetConstructor(new[] { typeof(IDbConnection) });

            IDbConnection conn = Connections.NewSqlConnection();
            var repo = (ICreatesATable)(ctor.Invoke(new object[] { conn }));

            Assert.AreSame(conn, repo.GetType().GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(repo));

            try { repo.DropTable(); } catch { }
            repo.CreateTable();

            var blah = repo.Get(1);

            repo.DropTable();
        }

        public interface INothing { }
        public interface ICreatesATable
        {
            void DropTable();
            void CreateTable();
            Person Get(int personId);
        }
    }

    class FooBar
    {
        public static int? DoSomeStuff(object o)
        {
            return (o == null || o == DBNull.Value) ? (int?)null : Convert.ToInt32(o);
        }
    }
}
