using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using System.Collections.Generic;
using System.Linq;

namespace Uni.Orm.Test
{
    [TestClass]
    public class PostgresqlTest
    {
        private UniOrm northwindPostgre = new UniOrm("NorthwindPostgre", Npgsql.NpgsqlFactory.Instance);
        private Listener uniOrm_Callback = new Listener
        {
            OnCallback = (Callback f) =>
            {
                //decimal UserID = f.OutputParameters[0].UserID;
                string sql = f.SqlQuery;
            },
            OnParameterCreating = (DbParameter f) =>
            {
                //if (f.ParameterName == "ErrorLogID")
                //    f.Direction = ParameterDirection.Output;
            },
            OnCommandPreExecuting = (DbCommand com) =>
            {
                com.CommandText = com.CommandText.Replace("#", "\"");
            },
            OnPreGeneratingSql = (SqlEntity sqlEntity) =>
            {

            }
        };

        public PostgresqlTest()
        {
            northwindPostgre.Options.EventListener = uniOrm_Callback;
        }

        [TestMethod]
        public void Query_SimpleUsageTest()
        {
            //Generated Sql: SELECT * FROM Products
            IEnumerable<dynamic> result1 = northwindPostgre.dyno.Query(Table: "products");

            IEnumerable<dynamic> result2 = northwindPostgre.dyno.MultiQuery(Table: "products");

            IEnumerable<dynamic> result3 = northwindPostgre.dyno.Query(Table: "products", MultiResultSet: true);

            IEnumerable<dynamic> result4 = northwindPostgre.dyno.Query(Sql: "SELECT #ProductID# FROM #products#");

            var a1 = result1.ToList();
        }

        [TestMethod]
        public void Query_ForOneParameter_WithDifferentUsage_ButSameResultTest()
        {
            //Different usage but same result below 6 lines ( result1, result2, result3, result4, result5, result6)
            //Setting @0 parameter via Args parameter as Array

            ////Generated Sql: SELECT * FROM Products WHERE ProductName=@0
            IEnumerable<dynamic> result1 = northwindPostgre.dyno.Query(Table: "products", Where: "#ProductName#=@0", Args: "Aniseed Syrup");

            result1 = result1.ToList();

            //var a1 = result1.ToList();

            ////Generated Sql: SELECT * FROM Products WHERE ProductName=@0
            //IEnumerable<dynamic> result2 = northwindPostgre.dyno.Query(Table: "products", Where: "#ProductName#=@0", Args: new object[] { "Aniseed Syrup" });

            ////Generated Sql: SELECT * FROM Products WHERE ProductName=@0
            //IEnumerable<dynamic> result3 = northwindPostgre.dyno.Query(Table: "products", Where: "#ProductName#=@0", Args: new[] { "Aniseed Syrup" });

            ////Setting @Name parameter via args as anonymous object
            ////Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName
            //IEnumerable<dynamic> result4 = northwindPostgre.dyno.Query(Table: "products", Args: new { ProductName = "Aniseed Syrup", UnitPrice = 10 });

            ////Setting @Name parameter directly without using Args parameter
            ////Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName
            //IEnumerable<dynamic> result5 = northwindPostgre.dyno.Query(Table: "products", Where: "#ProductName#=@ProductName", ProductName: "Aniseed Syrup");

            //Setting @Name parameter directly without using Args and Where parameter (Below code will create where clause)
            //Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName
            //IEnumerable<dynamic> result6 = northwindPostgre.dyno.Query(Table: "products", ProductName: "Aniseed Syrup");

            //Assert.IsTrue((result1.Count() == result2.Count()) == (result3.Count() == result4.Count()) == (result5.Count() == result6.Count()));
        }
    }
}