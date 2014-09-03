using System;
using System.Data;
using System.Threading;

namespace Repomat.UnitTests
{
    public class DeleteMe_UpsertWithCreateRepo_1
    {
        private IDbConnection _connection;
        public DeleteMe_UpsertWithCreateRepo_1(IDbConnection connection)
        {
            _connection = connection;
        }

        public Person Get(int num)
        {
            Person person;
            IDataReader dataReader = null;

            lock (this._connection)
            {
                IDbConnection connection = _connection;
                IDbCommand dbCommand = connection.CreateCommand();
                try
                {
                    dbCommand.CommandText = "select [Name], [Birthday], [Image] from Person  where [PersonId] = @personId";
                    IDbDataParameter dbDataParameter = dbCommand.CreateParameter();
                    dbDataParameter.ParameterName = "personId";
                    dbDataParameter.DbType = DbType.Int32;
                    dbDataParameter.Value = num;
                    dbCommand.Parameters.Add(dbDataParameter);
                    try
                    {
                        dataReader = dbCommand.ExecuteReader();
                        if (!dataReader.Read())
                        {
                            throw new RepomatException("No rows returned from singleton query", new object[0]);
                        }
                        person = new Person();
                        Person arg_AD_0 = person;
                        object value = dataReader.GetValue(0);
                        arg_AD_0.Name = ((value == null || value == DBNull.Value) ? null : Convert.ToString(value));
                        person.Birthday = Convert.ToDateTime(dataReader.GetValue(1));
                        Person arg_ED_0 = person;
                        object value2 = dataReader.GetValue(2);
                        arg_ED_0.Image = ((value2 == null || value2 == DBNull.Value) ? null : (byte[])value2);
                        person.PersonId = num;
                        if (dataReader.Read())
                        {
                            throw new RepomatException("More than one row returned from singleton query", new object[0]);
                        }
                    }
                    finally
                    {
                        if (dataReader != null)
                        dataReader.Dispose();
                    }
                }
                finally
                {
                    dbCommand.Dispose();
                }
            }
            return person;
        }
        public void Create(Person person)
        {
            bool flag = false;
            IDbConnection connection = _connection;
            try
            {
                Monitor.Enter(connection = this._connection, ref flag);
                IDbCommand dbCommand = connection.CreateCommand();
                try
                {
                    dbCommand.CommandText = "insert into [Person] (Name, Birthday, Image) values (@Name, @Birthday, @Image) SELECT SCOPE_IDENTITY()";
                    IDbDataParameter dbDataParameter = dbCommand.CreateParameter();
                    dbDataParameter.ParameterName = "Name";
                    dbDataParameter.DbType = DbType.String;
                    IDataParameter arg_62_0 = dbDataParameter;
                    object obj = person.Name;
                    if (obj == null)
                    {
                        obj = DBNull.Value;
                    }
                    arg_62_0.Value = obj;
                    dbCommand.Parameters.Add(dbDataParameter);
                    IDbDataParameter dbDataParameter2 = dbCommand.CreateParameter();
                    dbDataParameter2.ParameterName = "Birthday";
                    dbDataParameter2.DbType = DbType.DateTime;
                    dbDataParameter2.Value = person.Birthday;
                    dbCommand.Parameters.Add(dbDataParameter2);
                    IDbDataParameter dbDataParameter3 = dbCommand.CreateParameter();
                    dbDataParameter3.ParameterName = "Image";
                    dbDataParameter3.DbType = DbType.Binary;
                    IDataParameter arg_F9_0 = dbDataParameter3;
                    object obj2 = person.Image;
                    if (obj2 == null)
                    {
                        obj2 = DBNull.Value;
                    }
                    arg_F9_0.Value = obj2;
                    dbCommand.Parameters.Add(dbDataParameter3);
                    int personId = Convert.ToInt32((decimal)dbCommand.ExecuteScalar());
                    person.PersonId = personId;
                    return;
                }
                finally
                {
                    dbCommand.Dispose();
                }
            }
            finally
            {
                if (flag)
                {
                    Monitor.Exit(connection);
                    goto IL_149;
                }
                goto IL_149;
            IL_149: ;
            }
        }
        public void Update(Person person)
        {
            lock (this._connection)
            {
                IDbConnection connection = _connection;
                IDbCommand dbCommand = connection.CreateCommand();
                try
                {
                    dbCommand.CommandText = "update [Person] set [Name] = @Name, [Birthday] = @Birthday, [Image] = @Image WHERE [PersonId] = @PersonId";
                    IDbDataParameter dbDataParameter = dbCommand.CreateParameter();
                    dbDataParameter.ParameterName = "PersonId";
                    dbDataParameter.DbType = DbType.Int32;
                    dbDataParameter.Value = person.PersonId;
                    dbCommand.Parameters.Add(dbDataParameter);
                    IDbDataParameter dbDataParameter2 = dbCommand.CreateParameter();
                    dbDataParameter2.ParameterName = "Name";
                    dbDataParameter2.DbType = DbType.String;
                    IDataParameter arg_A7_0 = dbDataParameter2;
                    object obj = person.Name;
                    if (obj == null)
                    {
                        obj = DBNull.Value;
                    }
                    arg_A7_0.Value = obj;
                    dbCommand.Parameters.Add(dbDataParameter2);
                    IDbDataParameter dbDataParameter3 = dbCommand.CreateParameter();
                    dbDataParameter3.ParameterName = "Birthday";
                    dbDataParameter3.DbType = DbType.DateTime;
                    dbDataParameter3.Value = person.Birthday;
                    dbCommand.Parameters.Add(dbDataParameter3);
                    IDbDataParameter dbDataParameter4 = dbCommand.CreateParameter();
                    dbDataParameter4.ParameterName = "Image";
                    dbDataParameter4.DbType = DbType.Binary;
                    IDataParameter arg_13E_0 = dbDataParameter4;
                    object obj2 = person.Image;
                    if (obj2 == null)
                    {
                        obj2 = DBNull.Value;
                    }
                    arg_13E_0.Value = obj2;
                    dbCommand.Parameters.Add(dbDataParameter4);
                    dbCommand.ExecuteNonQuery();
                }
                finally
                {
                    dbCommand.Dispose();
                }
            }
        }
        public void Upsert(Person person)
        {
            lock (this._connection)
            {
                IDbConnection connection = _connection;
                IDbCommand dbCommand = connection.CreateCommand();
                try
                {
                    if (person.PersonId == 0)
                    {
                        dbCommand.CommandText = "insert into [Person] (Name, Birthday, Image) values (@Name, @Birthday, @Image) SELECT SCOPE_IDENTITY()";
                        IDbDataParameter dbDataParameter = dbCommand.CreateParameter();
                        dbDataParameter.ParameterName = "Name";
                        dbDataParameter.DbType = DbType.String;
                        IDataParameter arg_1B7_0 = dbDataParameter;
                        object obj = person.Name;
                        if (obj == null)
                        {
                            obj = DBNull.Value;
                        }
                        arg_1B7_0.Value = obj;
                        dbCommand.Parameters.Add(dbDataParameter);
                        IDbDataParameter dbDataParameter2 = dbCommand.CreateParameter();
                        dbDataParameter2.ParameterName = "Birthday";
                        dbDataParameter2.DbType = DbType.DateTime;
                        dbDataParameter2.Value = person.Birthday;
                        dbCommand.Parameters.Add(dbDataParameter2);
                        IDbDataParameter dbDataParameter3 = dbCommand.CreateParameter();
                        dbDataParameter3.ParameterName = "Image";
                        dbDataParameter3.DbType = DbType.Binary;
                        IDataParameter arg_24E_0 = dbDataParameter3;
                        object obj2 = person.Image;
                        if (obj2 == null)
                        {
                            obj2 = DBNull.Value;
                        }
                        arg_24E_0.Value = obj2;
                        dbCommand.Parameters.Add(dbDataParameter3);
                        int personId = Convert.ToInt32((decimal)dbCommand.ExecuteScalar());
                        person.PersonId = personId;
                        return;
                    }
                    dbCommand.CommandText = "update [Person] set [Name] = @Name, [Birthday] = @Birthday, [Image] = @Image WHERE [PersonId] = @PersonId";
                    IDbDataParameter dbDataParameter4 = dbCommand.CreateParameter();
                    dbDataParameter4.ParameterName = "PersonId";
                    dbDataParameter4.DbType = DbType.Int32;
                    dbDataParameter4.Value = person.PersonId;
                    dbCommand.Parameters.Add(dbDataParameter4);
                    IDbDataParameter dbDataParameter5 = dbCommand.CreateParameter();
                    dbDataParameter5.ParameterName = "Name";
                    dbDataParameter5.DbType = DbType.String;
                    IDataParameter arg_B7_0 = dbDataParameter5;
                    object obj3 = person.Name;
                    if (obj3 == null)
                    {
                        obj3 = DBNull.Value;
                    }
                    arg_B7_0.Value = obj3;
                    dbCommand.Parameters.Add(dbDataParameter5);
                    IDbDataParameter dbDataParameter6 = dbCommand.CreateParameter();
                    dbDataParameter6.ParameterName = "Birthday";
                    dbDataParameter6.DbType = DbType.DateTime;
                    dbDataParameter6.Value = person.Birthday;
                    dbCommand.Parameters.Add(dbDataParameter6);
                    IDbDataParameter dbDataParameter7 = dbCommand.CreateParameter();
                    dbDataParameter7.ParameterName = "Image";
                    dbDataParameter7.DbType = DbType.Binary;
                    IDataParameter arg_14E_0 = dbDataParameter7;
                    object obj4 = person.Image;
                    if (obj4 == null)
                    {
                        obj4 = DBNull.Value;
                    }
                    arg_14E_0.Value = obj4;
                    dbCommand.Parameters.Add(dbDataParameter7);
                    dbCommand.ExecuteNonQuery();
                }
                finally
                {
                    dbCommand.Dispose();
                }
            }
        }
        public void CreateTable()
        {
            lock (this._connection)
            {
                IDbConnection connection = _connection;
                IDbCommand dbCommand = connection.CreateCommand();
                try
                {
                    dbCommand.CommandText = "create table [Person] ([PersonId] INT IDENTITY , [Name] VARCHAR(MAX), [Birthday] DATETIME  NOT NULL, [Image] VARBINARY(MAX), CONSTRAINT [pk_Person] PRIMARY KEY ([PersonId]))";
                    dbCommand.ExecuteNonQuery();
                }
                finally
                {
                    dbCommand.Dispose();
                }
            }
        }
        public void DropTable()
        {
            IDbConnection connection = _connection;
            lock (this._connection)
            {
                IDbCommand dbCommand = connection.CreateCommand();
                try
                {
                    dbCommand.CommandText = "drop table [Person]";
                    dbCommand.ExecuteNonQuery();
                }
                finally
                {
                    dbCommand.Dispose();
                }
            }
        }
        public bool TableExists()
        {
            bool result;
            lock (this._connection)
            {
                IDbConnection connection = _connection;
                IDbCommand dbCommand = connection.CreateCommand();
                try
                {
                    dbCommand.CommandText = "if exists (select 1 from information_schema.tables where table_type='BASE TABLE' and table_name='Person') SELECT 1 ELSE SELECT 0";
                    result = Convert.ToBoolean(dbCommand.ExecuteScalar());
                }
                finally
                {
                    dbCommand.Dispose();
                }
            }
            return result;
        }
    }
}
