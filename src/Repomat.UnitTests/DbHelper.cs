using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repomat.UnitTests
{
    public abstract class DbHelper
    {
        private readonly IDbConnection _connection;
        protected DbHelper(IDbConnection conn)
        {
            _connection = conn;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public T ExecuteScalar<T>(string query, object arguments=null)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;
                AddParametersToCommand(cmd, arguments);

                return (T)Convert.ChangeType(cmd.ExecuteScalar(), typeof(T));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void ExecuteNonQuery(string query, object arguments = null)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;
                AddParametersToCommand(cmd, arguments);

                cmd.ExecuteNonQuery();
            }
        }

        public abstract bool TableExists(string tableName);

        private void AddParametersToCommand(IDbCommand command, object arguments)
        {
            if (arguments != null)
            {
                foreach (var property in arguments.GetType().GetProperties())
                {
                    var parm = command.CreateParameter();
                    parm.ParameterName = property.Name;
                    parm.Value = property.GetGetMethod().Invoke(arguments, new object[0]);
                    command.Parameters.Add(parm);
                }
            }
        }
    }
}
