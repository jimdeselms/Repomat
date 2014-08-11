using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repomat.Databases;
using NUnit.Framework;
using Dapper;

namespace Repomat.UnitTests
{
    [TestFixture]
    public class PerformanceTests
    {
        /// <summary>
        /// Test the time it takes to get a fill result set.
        /// </summary>
        [Test]
        public void SelectToArrayTest()
        {
            const int count = 1000;
            Console.WriteLine();
            Console.WriteLine("### Testing Select {0} rows to dataset", count);
            Stopwatch dapper = new Stopwatch();
            Stopwatch Repomat = new Stopwatch();

            IDbConnection conn = Connections.NewSqlConnection();

            // Just do the first pass so that any cost associated with the first run can be ignored.
            dapper.Start();
            var firstDiscounts = conn.Query<Discount>("select DiscountId, DiscountTypeId, Description, Created, CreatedBy, OperationLogId, PricingConfigurationId from PerfTestTable").ToArray();
            dapper.Stop();

            Repomat.Start();
            var repo = CreateRepo(conn);
            var firstList = repo.GetAll().ToArray();
            Repomat.Stop();

            Console.WriteLine("Dapper first:         {0}", dapper.Elapsed.TotalMilliseconds);
            Console.WriteLine("Repomat first:      {0}", Repomat.Elapsed.TotalMilliseconds);
            Console.WriteLine("Repomat vs. Dapper: {0}%", (Repomat.Elapsed.TotalMilliseconds / dapper.Elapsed.TotalMilliseconds) * 100.0);

            dapper.Reset();
            Repomat.Reset();

            InsertRows(count);

            Discount[] dapperDiscounts = null;
            Discount[] spededbDiscounts = null;

            const int iterations = 100;

            for (int i = 0; i < 100; i++)
            {
                dapper.Start();
                dapperDiscounts = conn.Query<Discount>("select DiscountId, DiscountTypeId, Description, Created, CreatedBy, OperationLogId, PricingConfigurationId from PerfTestTable").ToArray();
                dapper.Stop();

                Repomat.Start();
                spededbDiscounts = repo.GetAll().ToArray();
                Repomat.Stop();
            }

            Assert.AreEqual(count, spededbDiscounts.Length);

            CollectionAssert.AreEqual(dapperDiscounts, spededbDiscounts);

            Console.WriteLine("-----");
            Console.WriteLine("Dapper {0} times:     {1}", iterations, dapper.Elapsed.TotalMilliseconds);
            Console.WriteLine("Repomat {0} times:  {1}", iterations, Repomat.Elapsed.TotalMilliseconds);
            Console.WriteLine("Repomat vs. Dapper: {0}%", (Repomat.Elapsed.TotalMilliseconds / dapper.Elapsed.TotalMilliseconds) * 100.0);
        }

        /// <summary>
        /// Test the time it takes to get the 
        /// </summary>
        [Test]
        public void InsertTest()
        {
            Console.WriteLine();
            Console.WriteLine("### Testing Insert");
            Stopwatch dapper = new Stopwatch();
            Stopwatch Repomat = new Stopwatch();

            IDbConnection conn = Connections.NewSqlConnection();

            int count = 100;

            dapper.Start();
            conn.Execute(
                "insert into PerfTestTable (DiscountID, DiscountTypeId, Description, Created, CreatedBy, OperationLogId, PricingConfigurationId) values (@discountId, @discountTypeId, @description, @created, @createdBy, @operationLogId, @pricingConfigurationId)",
                new
                {
                    discountId = -1,
                    discountTypeId = 0,
                    description = "Hello",
                    created = DateTime.Now,
                    createdBy = "Jim",
                    operationLogId = 2938,
                    pricingConfigurationId = 2934
                });

            dapper.Stop();

            Repomat.Start();
            var repo = CreateRepo(conn);
            Discount d = new Discount()
            {
                DiscountId = -2,
                DiscountTypeId = 0,
                Description = "Hello",
                Created = DateTime.Now,
                CreatedBy = "Jim",
                OperationLogId = 23928,
                PricingConfigurationId = 23942
            };
            repo.Insert(d);
            Repomat.Stop();

            Console.WriteLine("Dapper first:         {0}", dapper.Elapsed.TotalMilliseconds);
            Console.WriteLine("Repomat first:      {0}", Repomat.Elapsed.TotalMilliseconds);
            Console.WriteLine("Repomat vs. Dapper: {0}%", (Repomat.Elapsed.TotalMilliseconds / dapper.Elapsed.TotalMilliseconds) * 100.0);

            dapper.Reset();
            Repomat.Reset();

            for (int i = 0; i < count; i++)
            {
                dapper.Start();
                conn.Execute(
                    "insert into PerfTestTable (DiscountID, DiscountTypeId, Description, Created, CreatedBy, OperationLogId, PricingConfigurationId) values (@discountId, @discountTypeId, @description, @created, @createdBy, @operationLogId, @pricingConfigurationId)",
                    new
                        {
                            discountId = i,
                            discountTypeId = i % 50,
                            description = "Hello",
                            created = DateTime.Now,
                            createdBy = "Jim",
                            operationLogId = 2938,
                            pricingConfigurationId = 2934
                        });

                dapper.Stop();

                Repomat.Start();

                d = new Discount
                    {
                        DiscountId = i + count + 1,
                        DiscountTypeId = (short)(i % 50),
                        Description = "Hello",
                        Created = DateTime.Now,
                        CreatedBy = "Jim",
                        OperationLogId = 23928,
                        PricingConfigurationId = 23942
                    };
                repo.Insert(d);
                Repomat.Stop();
            }

            Console.WriteLine("-----");
            Console.WriteLine("Dapper {0} times:     {1}", count, dapper.Elapsed.TotalMilliseconds);
            Console.WriteLine("Repomat {0} times:  {1}", count, Repomat.Elapsed.TotalMilliseconds);
            Console.WriteLine("Repomat vs. Dapper: {0}%", count, (Repomat.Elapsed.TotalMilliseconds / dapper.Elapsed.TotalMilliseconds) * 100.0);
        }

        [Test]
        public void CreateLotsOfRepos()
        {
            const int count = 10;

            var conn = Connections.NewSqlConnection();

            SqlServerDataLayerBuilder db = new SqlServerDataLayerBuilder(conn);

            Stopwatch s = new Stopwatch();
            s.Start();

            var builder = db.SetupRepo<Discount, IDiscountProvider>();

            builder
              .SetupMethod("InsertStuffIntoTable")
              .SetTableName("PerfTestTable")
              .ExecutesSql("insert into PerfTestTable values (@discountId, @discountTypeId, @description, '2014-01-02, 'jim', 50, 90)");

            for (int i = 0; i < count; i++)
            {
                builder.CreateRepo();
            }
            s.Stop();

            Console.WriteLine("Time to create {0} repos: {1}", count, s.Elapsed);
        }

        [TestFixtureSetUp]
        public void SetUp()
        {
            if (Db.SqlServer.TableExists("PerfTestTable"))
            {
                Db.SqlServer.ExecuteNonQuery("drop table PerfTestTable");
            }

            Db.SqlServer.ExecuteNonQuery("create table PerfTestTable (DiscountId int primary key, DiscountTypeID smallint, Description varchar(100), Created datetime, CreatedBy varchar(50), OperationLogId int, PricingConfigurationId int)");
        }

        [SetUp]
        public void Setup()
        {
            Db.SqlServer.ExecuteNonQuery("truncate table PerfTestTable");
        }

        private void InsertRows(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Db.SqlServer.ExecuteNonQuery(
                    "insert into PerfTestTable values (@discountId, @type, @description, '2014-01-01', 'jim', 50, 90)",
                    new {discountId = i, type = i % 50, description = "Row " + i.ToString()});
            }
        }

        private IDiscountProvider CreateRepo(IDbConnection conn)
        {
            SqlServerDataLayerBuilder db = new SqlServerDataLayerBuilder(conn);
            var builder = db.SetupRepo<Discount, IDiscountProvider>();

            builder.SetupMethod("InsertStuffIntoTable")
              .SetTableName("PerfTestTable")
              .ExecutesSql("insert into PerfTestTable values (@discountId, @discountTypeId, @description, '2014-01-02, 'jim', 50, 90)");

            return builder.CreateRepo();
        }

    }


    public class Discount
    {
        public int DiscountId { get; set; }
        public short DiscountTypeId { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public int? OperationLogId { get; set; }
        public int? PricingConfigurationId { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as Discount;
            return other.Created == this.Created
                   && other.CreatedBy == this.CreatedBy
                   && other.Description == this.Description
                   && other.DiscountId == this.DiscountId
                   && other.DiscountTypeId == this.DiscountTypeId
                   && other.OperationLogId == this.OperationLogId
                   && other.PricingConfigurationId == this.PricingConfigurationId;
        }

        public override int GetHashCode()
        {
            return DiscountId.GetHashCode()
                ^ DiscountTypeId.GetHashCode()
                ^ Description.GetHashCode() 
                ^ Created.GetHashCode() 
                ^ OperationLogId.GetHashCode() 
                ^ PricingConfigurationId.GetHashCode();
        }
    }

    public interface IDiscountProvider
    {
        List<Discount> GetAll();
        void InsertStuffIntoTable(int discountId, short discountTypeId, string description);
        void Insert(Discount discount);
        void Delete(Discount discount);
    }


}
