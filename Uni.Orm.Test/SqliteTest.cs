using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using Uni.Extensions;
using System.Dynamic;
using System.Diagnostics;

namespace Uni.Orm.Test
{
    [TestClass]
    public class SqliteTest
    {
        UniOrm northwindSqlite = new UniOrm("NorthwindSqlite", System.Data.SQLite.SQLiteFactory.Instance, new Options { FieldNameLower = true });
        //UniOrm northwindSqlite = new UniOrm(@"Data Source=.\Northwind.sqlite;Version=3;", DatabaseType.SQLite, System.Data.SQLite.SQLiteFactory.Instance, new Options { FieldNameLower = true });

        private Listener uniOrm_Callback = new Listener
        {
            OnCallback = (Callback f) =>
            {
                //decimal RETURN_VALUE = f.OutputParameters.RETURN_VALUE;
                Console.WriteLine(f.SqlQuery);
            }
        };

        public SqliteTest()
        {
            northwindSqlite.Options.EventListener = uniOrm_Callback;
        }

        [TestMethod]
        public void Exercise1()
        {
            var result1 = northwindSqlite.dyno.Count(Table: "Products", Columns: "ProductName");

            var result2 = northwindSqlite.dyno.Count(Table: "Products", Columns: "*");

            var result3 = northwindSqlite.dyno.Count(Table: "Products", Columns: "*", Where: "ProductID=@0", Args: 1);

            var result4 = northwindSqlite.dyno.Exists(Table: "Products", Where: "ProductID IN (@0,@1)", Args: new object[] { 1, 2 });

            var result5 = northwindSqlite.dyno.Sum(Table: "Products", Columns: "UnitPrice");

            var result6 = northwindSqlite.dyno.Max(Table: "Products", Columns: "UnitPrice");

            var result7 = northwindSqlite.dyno.Min(Table: "Products", Columns: "UnitPrice");

            var result8 = northwindSqlite.dyno.Avg(Table: "Products", Columns: "UnitPrice");

            var result9 = northwindSqlite.dyno.Query(Table: "Customers", Columns: "CompanyName", Where: "Region IS NULL");

            var result10 = northwindSqlite.dyno.Query(Table: "Customers", Columns: "CompanyName", Where: "Region IS NULL", City: "London");

            var result11 = northwindSqlite.dyno.Sum(Table: "Products", Columns: "UnitPrice", CategoryID: 1);

            var result12 = northwindSqlite.dyno.Sum(Table: "Products", Columns: "UnitPrice", Where: "CategoryID IN @CategoryID", CategoryID: new object[] { 1, 2 });

            IEnumerable<dynamic> result13 = northwindSqlite.dyno.Query(Table: "Products", Columns: "UnitPrice", Where: "CategoryID IN @CategoryID", CategoryID: new object[] { 1, 2 });

            result13 = result13.ToList();

            IEnumerable<dynamic> result14 = northwindSqlite.dyno.Query(Table: "Customers", Columns: "CompanyName", Where: "Region IS NULL", City: "London");

            result14 = result14.ToList();

            IEnumerable<Products> result15 = northwindSqlite.dyno.Query<Products>(Sql: "SELECT * FROM Products");

            result15 = result15.ToList();

            var result16 = northwindSqlite.dyno.Sum(Table: "Products", Columns: "UnitPrice", CategoryID: new object[] { 1, 2 });

            IEnumerable<dynamic> result17 = northwindSqlite.dyno.Query(Table: "Products", CategoryID: new object[] { 1, 2, 3, 4, 5, 6, 7 });

            result17 = result17.ToList();

            IEnumerable<Products> result18 = northwindSqlite.dyno.Query<Products>(Table: "Products", CategoryID: new object[] { 1, 2, 3, 4, 5, 6, 7 });

            result18 = result18.ToList();

            Products result19 = northwindSqlite.dyno.Query<Products>(Sql: "SELECT * FROM Products", Limit: 1);
        }

        [TestMethod]
        public void Exercise2()
        {
            //This code will return dynamic mapped result.
            IEnumerable<Person> result1 = northwindSqlite.dyno.Query<Person>(Table: "Products", Limit: 100);

            result1 = result1.ToList();

            var firstItem = result1.FirstOrDefault();

            var anonymousObj = new UniAnonymousObject();

            var personType = anonymousObj.GetDynamicType(firstItem, "Person");

            string personPoco = anonymousObj.GetPoco(firstItem, "Person");
        }

        [TestMethod]
        public void Exercise3()
        {
            IEnumerable<Products> result1 = northwindSqlite.dyno.Query<Products>(Sql: "SELECT * FROM Products");

            IEnumerable<dynamic> result2 = northwindSqlite.dyno.Query(Sql: "SELECT * FROM Products");
        }

        [TestMethod]
        public void Exercise4()
        {
            //IEnumerable<Products> result1 = northwindSqlite.dyno.Query<Products>(Sql: "SELECT * FROM Customers");

            IEnumerable<dynamic> result2 = northwindSqlite.dyno.Query(Sql: "SELECT * FROM Products");
        }

        public class Person
        {

            public long PersonID { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }
        }


        [TestMethod]
        public void InsertTest()
        {
            var result = northwindSqlite.dyno.Insert(Table: "Products",
                                                     PKField: "ProductID",
                                                     Args: new { ProductID = 0, ProductName = "Pencil" });
        }

        [TestMethod]
        public void BulkInsertTest()
        {
            var result = northwindSqlite.dyno.Insert(Table: "Products",
                                                     PKField: "ProductID",
                                                     Args: new object[]
                                                     {
                                                         new { ProductID = 0, ProductName = "Pen" },
                                                         new { ProductID = 0, ProductName = "Notebook" }
                                                     });
        }

        [TestMethod]
        public void UpdateTest()
        {
            var result1 = northwindSqlite.dyno.Update(Table: "Products",
                                                     Columns: "ProductName",
                                                     Args: new { ProductID = 97, ProductName = "Pen" });

            var result2 = northwindSqlite.dyno.Update(Table: "Products",
                                                     Columns: "ProductName",
                                                     Where: "ProductID=@ProductID",
                                                     Args: new { ProductID = 97, ProductName = "Eraser" });

            var result3 = northwindSqlite.dyno.Update(Table: "Products",
                                                     Columns: "ProductName",
                                                     Where: "ProductName=@ProductName2 AND SupplierID=@SupplierID",
                                                     Args: new { ProductName2 = "Eraser", SupplierID = 122, ProductName = "Faber Castel Eraser" });

            var result4 = northwindSqlite.dyno.Update(Table: "Products",
                                                     Columns: "ProductName",
                                                     Where: "ProductID=@ProductID",
                                                     Args: new { ProductID = 97, ProductName = "Pen", SupplierID = 122 });
        }

        [TestMethod]
        public void BulkUpdateTest()
        {
            var productList = new List<dynamic>
            {
                new { SupplierID = 122, CategoryID = 101, ProductName = "Kenan" },
                new { SupplierID = 122, CategoryID = 102, ProductName = "Sara" },
                new { SupplierID = 122, CategoryID = 103, ProductName = "Enes" }
            };

            //Generated Sql: UPDATE Products SET ProductName=@ProductName0  WHERE CategoryID=@CategoryID0;UPDATE Products SET ProductName=@ProductName1  WHERE CategoryID=@CategoryID1;UPDATE Products SET ProductName=@ProductName2  WHERE CategoryID=@CategoryID2
            var result1 = northwindSqlite.dyno.Update(Table: "Products",
                                                     Columns: "ProductName",
                                                     Args: productList);

            var result2 = northwindSqlite.dyno.Update(Table: "Products",
                                                     Columns: "ProductName",
                                                     Where: "CategoryID=@CategoryID OR SupplierID=@SupplierID",
                                                     Args: productList);
        }

        [TestMethod]
        public void DeleteTest()
        {
            //Generated Sql: DELETE FROM Products  WHERE ProductID=@ProductID
            int result1 = northwindSqlite.dyno.Delete(Table: "Products",
                                                     Where: "ProductID=@ProductID",
                                                     Args: new { ProductID = 96 });

            //Generated Sql: DELETE FROM Products  WHERE ProductName=@ProductName AND SupplierID=@SupplierID
            int result2 = northwindSqlite.dyno.Delete(Table: "Products",
                                                     Where: "ProductName=@ProductName AND SupplierID=@SupplierID",
                                                     Args: new { SupplierID = 122, ProductName = "JohnNNN____" });

            //Generated Sql: DELETE FROM Products  WHERE ProductID=@ProductID
            int result3 = northwindSqlite.dyno.Delete(Table: "Products",
                                                     PKField: "ProductID",
                                                     Args: 79);

            //Generated Sql: DELETE FROM Products  WHERE ProductID=@ProductID0;DELETE FROM Products  WHERE ProductID=@ProductID1
            int result4 = northwindSqlite.dyno.Delete(Table: "Products",
                                                     PKField: "ProductID",
                                                     Args: new object[] { 78, 79 });
        }

        [TestMethod]
        public void Listener_OnConvertingResultTest()
        {
            var options = new Options
            {
                EventListener = new Listener
                {
                    OnConvertingResult = (sqlEntity, result) =>
                        {
                            if (sqlEntity.Binder == "delete")
                                return result.To<bool>();

                            return result;
                        }
                }
            };

            //Generated Sql: DELETE FROM Products  WHERE ProductID=@ProductID
            bool result1 = northwindSqlite.dyno.Delete(Table: "Products",
                                                     PKField: "ProductID",
                                                     Options: options,
                                                     Args: 79);
        }

        [TestMethod]
        public void InsertUpdateDeleteMixTest()
        {
            var insertedProductID = northwindSqlite.dyno.Insert(Table: "Products",
                                                     PKField: "ProductID",
                                                     Args: new { ProductID = 0, ProductName = "Pencil", SupplierID = 122 });

            var result1 = northwindSqlite.dyno.Update(Table: "Products",
                                                     Columns: "ProductName",
                                                     Where: "ProductID=@ProductID",
                                                     Args: new { ProductID = insertedProductID, ProductName = "Faber Castel Eraser" });

            if (result1 == 1)
            {
                var result2 = northwindSqlite.dyno.Delete(Table: "Products",
                                                          Where: "ProductID=@ProductID",
                                                          Args: new { ProductID = insertedProductID });
            }
        }

        [TestMethod]
        public void TransactionTest()
        {
            var con = northwindSqlite.NewConnection();

            var options = new Options { Transaction = con.BeginTransaction() };

            var result1 = northwindSqlite.dyno.Insert(Table: "Products",
                                                     PKField: "ProductID",
                                                     Options: options,
                                                     Args: new { ProductID = 0, ProductName = "Pencil" });

            var result2 = northwindSqlite.dyno.Insert(Table: "Products",
                                                     PKField: "ProductID",
                                                     Options: options,
                                                     Args: new { ProductID = 0, ProductName = "Pencil" });

            var result3 = northwindSqlite.dyno.Insert(Table: "Products",
                                                                 PKField: "ProductID",
                                                                 Options: options,
                                                                 Args: new { ProductID = 0, ProductName = "Pencil" });

            options.Transaction.Commit();
        }

        [TestMethod]
        public void Query_SimpleUsageTest()
        {
            //Generated Sql: SELECT * FROM Products
            IEnumerable<dynamic> result1 = northwindSqlite.dyno.Query(Table: "Products");

            IEnumerable<dynamic> result2 = northwindSqlite.dyno.MultiQuery(Sql: "SELECT * FROM Products;SELECT * FROM Customers;");

            IEnumerable<dynamic> result3 = northwindSqlite.dyno.MultiQuery(Sql: "SELECT 10;SELECT 12;", MultiResultSet: true);
        }

        [TestMethod]
        public void Query_ForOneParameter_WithDifferentUsage_ButSameResultTest()
        {
            //Different usage but same result below 6 lines ( result1, result2, result3, result4, result5, result6)
            //Setting @0 parameter via Args parameter as Array

            //Generated Sql: SELECT * FROM Products WHERE ProductName=@0
            IEnumerable<dynamic> result1 = northwindSqlite.dyno.Query(Table: "Products", Where: "ProductName=@0", Args: "Aniseed Syrup");

            //Generated Sql: SELECT * FROM Products WHERE ProductName=@0
            IEnumerable<dynamic> result2 = northwindSqlite.dyno.Query(Table: "Products", Where: "ProductName=@0", Args: new object[] { "Aniseed Syrup" });

            //Generated Sql: SELECT * FROM Products WHERE ProductName=@0
            IEnumerable<dynamic> result3 = northwindSqlite.dyno.Query(Table: "Products", Where: "ProductName=@0", Args: new[] { "Aniseed Syrup" });

            //Setting @Name parameter via args as anonymous object
            //Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName
            IEnumerable<dynamic> result4 = northwindSqlite.dyno.Query(Table: "Products", Where: "ProductName=@ProductName", Args: new { ProductName = "Aniseed Syrup" });

            //Setting @Name parameter directly without using Args parameter
            //Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName
            IEnumerable<dynamic> result5 = northwindSqlite.dyno.Query(Table: "Products", Where: "ProductName=@ProductName", ProductName: "Aniseed Syrup");

            //Setting @Name parameter directly without using Args and Where parameter (Below code will create where clause)
            //Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName
            IEnumerable<dynamic> result6 = northwindSqlite.dyno.Query(Table: "Products", ProductName: "Aniseed Syrup");

            var argsDict = new Dictionary<string, object> { { "ProductName", "Aniseed Syrup" } };

            IEnumerable<dynamic> result7 = northwindSqlite.dyno.Query(Table: "Products", Args: argsDict);

            dynamic dynamicArgs = new ExpandoObject();
            dynamicArgs.ProductName = "Aniseed Syrup";

            IEnumerable<dynamic> result8 = northwindSqlite.dyno.Query(Table: "Products", Args: dynamicArgs);

            Assert.IsTrue((result1.Count() == result2.Count()) == (result3.Count() == result4.Count()) == (result5.Count() == result6.Count()));
        }

        [TestMethod]
        public void Query_ForSeveralParameters_WithDifferentUsage_ButSameResultTest()
        {
            //Different usage but same result below 4 lines ( result1, result2, result3, result4)
            //Setting @0 and @1 parameters via Args parameter

            //Generated Sql: SELECT * FROM Products WHERE ProductName=@1 AND UnitPrice=@0
            IEnumerable<dynamic> result1 = northwindSqlite.dyno.Query(Table: "Products", Where: "ProductName=@1 AND UnitPrice=@0", Args: new object[] { 10, "Aniseed Syrup" });

            //Setting @Color and @ListPrice parameters via Args parameter as anonymous object
            //Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName and UnitPrice=@UnitPrice
            IEnumerable<dynamic> result2 = northwindSqlite.dyno.Query(Table: "Products", Where: "ProductName=@ProductName AND UnitPrice=@UnitPrice", Args: new { ProductName = "Aniseed Syrup", UnitPrice = 10 });

            //Setting @Color and @ListPrice parameters directly without using Args parameter
            //Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName and UnitPrice=@UnitPrice
            IEnumerable<dynamic> result3 = northwindSqlite.dyno.Query(Table: "Products", Where: "ProductName=@ProductName AND UnitPrice=@UnitPrice", ProductName: "Aniseed Syrup", UnitPrice: 10);

            //Setting @Color and @ListPrice parameters directly without using Args and Where parameter (Below code will create where clause)
            //Generated Sql: SELECT * FROM Products WHERE ProductName=@ProductName and UnitPrice=@UnitPrice
            IEnumerable<dynamic> result4 = northwindSqlite.dyno.Query(Table: "Products", ProductName: "Aniseed Syrup", UnitPrice: 10);

            Assert.IsTrue((result1.Count() == result2.Count()) == (result3.Count() == result4.Count()));
        }

        [TestMethod]
        public void Query_ForSqlStatement_WithDifferentUsage_ButSameResultTest()
        {
            //Different usage but same result below 3 lines ( result1, result2, result3)

            IEnumerable<dynamic> result1 = northwindSqlite.dyno.Query(Sql: "SELECT * FROM Products WHERE UnitPrice=@0 and ProductName=@1", Args: new object[] { 10, "Aniseed Syrup" });

            IEnumerable<dynamic> result2 = northwindSqlite.dyno.Query(Sql: "SELECT * FROM Products WHERE UnitPrice=@UnitPrice and ProductName=@ProductName", Args: new { UnitPrice = 10, ProductName = "Aniseed Syrup" });

            IEnumerable<dynamic> result3 = northwindSqlite.dyno.Query(Sql: "SELECT * FROM Products WHERE UnitPrice=@UnitPrice and ProductName=@ProductName", UnitPrice: 10, ProductName: "Aniseed Syrup");
        }

        [TestMethod]
        public void Exists_Test()
        {
            //Different usage but same result below 7 lines ( result1, result2, result3, result4, result5, result6, result7)
            //Below lines will return bool value for Sql query result. (If row count of query result is bigger than 0 than true. Otherwise false)

            //bool result1 = northwindSqlite.dyno.Exists(Sql: "SELECT * FROM Products WHERE UnitPrice=@0 and ProductName=@1", Args: new object[] { 10, "Aniseed Syrup" });

            //bool result2 = northwindSqlite.dyno.Exists(Sql: "SELECT * FROM Products WHERE UnitPrice=@UnitPrice and ProductName=@ProductName", Args: new { UnitPrice = 10, ProductName = "Aniseed Syrup" });

            //bool result3 = northwindSqlite.dyno.Exists(Sql: "SELECT * FROM Products WHERE UnitPrice=@UnitPrice and ProductName=@ProductName", UnitPrice: 10, ProductName: "Aniseed Syrup");

            bool result4 = northwindSqlite.dyno.Exists(Table: "Products", Where: "UnitPrice=@0 and ProductName=@1", Args: new object[] { 10, "Aniseed Syrup" });

            bool result5 = northwindSqlite.dyno.Exists(Table: "Products", Where: "UnitPrice=@UnitPrice and ProductName=@ProductName", Args: new { ProductName = "Aniseed Syrup", UnitPrice = 10 });

            bool result6 = northwindSqlite.dyno.Exists(Table: "Products", Where: "UnitPrice=@UnitPrice and ProductName=@ProductName", UnitPrice: 10, ProductName: "Aniseed Syrup");

            bool result7 = northwindSqlite.dyno.Exists(Table: "Products", UnitPrice: 10, ProductName: "Aniseed Syrup");
        }

        [TestMethod]
        public void Limit_And_OrderBy_Test()
        {
            //Different usage but same result below 4 lines ( result1, result2, result3, result4)
            //UniOrm will not append limit parameter to sql statement when Sql and Limit parameters are used together.

            //Generated Sql: SELECT ProductID,ProductName,SupplierID FROM Products WHERE SupplierID in (@SupplierID0,@SupplierID1) ORDER BY ProductID LIMIT 1
            var result1 = northwindSqlite.dyno.Query(Table: "Products", Columns: "ProductID,ProductName,SupplierID", Where: "SupplierID in @SupplierID", OrderBy: "ProductID", Limit: 1, SupplierID: new[] { 1, 2 });

            //Generated Sql: SELECT ProductID,ProductName,SupplierID FROM Products WHERE SupplierID in (@SupplierID0,@SupplierID1) ORDER BY ProductID LIMIT 1
            var result2 = northwindSqlite.dyno.Query(Table: "Products", Columns: "ProductID,ProductName,SupplierID", OrderBy: "ProductID", Limit: 1, SupplierID: new[] { 1, 2 });

            //Generated Sql: SELECT * FROM (SELECT ProductID,ProductName,SupplierID FROM Products LIMIT 1) t WHERE SupplierID in (@SupplierID0,@SupplierID1) ORDER BY ProductID
            var result3 = northwindSqlite.dyno.Query(Sql: "SELECT ProductID,ProductName,SupplierID FROM Products LIMIT 1", Where: "SupplierID in @SupplierID", OrderBy: "ProductID", Limit: 1, SupplierID: new[] { 1, 2 });

            //Generated Sql: SELECT * FROM (SELECT ProductID,ProductName,SupplierID FROM Products LIMIT 1) t WHERE SupplierID in (@SupplierID0,@SupplierID1) ORDER BY ProductID
            var result4 = northwindSqlite.dyno.Query(Sql: "SELECT ProductID,ProductName,SupplierID FROM Products LIMIT 1", OrderBy: "ProductID", Limit: 1, SupplierID: new[] { 1, 2 });
        }

        [TestMethod]
        public void DynamicObject_And_StronglyType_Mapping_Test()
        {
            //Below two lines of codes(result1, result2) return Strongly-Type mapped result.

            IEnumerable<Products> result1 = northwindSqlite.dyno.Query<Products>(Sql: "SELECT * FROM Products");

            result1 = result1.ToList();

            IEnumerable<Products> result2 = northwindSqlite.dyno.Query<Products>(Table: "Products");

            result2 = result2.ToList();

            //This code will return dynamic mapped result.
            var result3 = northwindSqlite.dyno.Query(Table: "Products", Limit: 1);

            var anonymousObj = new UniAnonymousObject();

            var personType = anonymousObj.GetDynamicType(result3, "Products");

            string personPoco = anonymousObj.GetPoco(result3, "Products");
        }

        [TestMethod]
        public void Paging_Test()
        {
            //Generated Sql: SELECT * FROM Products LIMIT 0,50
            IEnumerable<dynamic> result1 = northwindSqlite.dyno.Query(Table: "Products", PageSize: 50, PageNo: 1);

            result1 = result1.ToList();

            //Generated Sql: SELECT * FROM Products LIMIT 50,50
            IEnumerable<dynamic> result2 = northwindSqlite.dyno.Query(Table: "Products", PageSize: 50, PageNo: 2);

            result2 = result2.ToList();
        }

        [TestMethod]
        public void ConfigBasedQuery_Test()
        {
            //UniOrm can execute config based query as below.

            var options = new Options();
            options.EventListener = new Listener
            {
                OnCallback = (Callback f) =>
                {
                    //decimal RETURN_VALUE = f.OutputParameters.RETURN_VALUE;
                    Console.WriteLine(f.SqlQuery);
                },
                OnParameterCreating = (DbParameter f) =>
                {
                    //if (f.ParameterName == "ErrorLogID")
                    //    f.Direction = ParameterDirection.Output;
                }
            };

            var criteria = new
            {
                Operation = "Query",
                Table = "Products",
                Where = "ProductName in @ProductName",
                ProductName = new[] { "Chai", "Chang", "Aniseed Syrup", "Mishi Kobe Niku", "Ikura" },
                Options = options
            };

            //Generated Sql: SELECT * FROM Products WHERE ProductName in (@ProductName0,@ProductName1,@ProductName2,@ProductName3,@ProductName4)
            IEnumerable<dynamic> result = northwindSqlite.dyno.Execute(criteria);

            result = result.ToList();
        }

        [TestMethod]
        public void ConfigBasedQuery_WithJson_Test()
        {
            var options = new Options();
            options.EventListener = new Listener
            {
                OnCallback = (Callback f) =>
                {
                    //decimal RETURN_VALUE = f.OutputParameters.RETURN_VALUE;
                    Console.WriteLine(f.SqlQuery);
                },
                OnParameterCreating = (DbParameter f) =>
                {
                    //if (f.ParameterName == "ErrorLogID")
                    //    f.Direction = ParameterDirection.Output;
                }
            };

            string json = @"{
              'Operation': 'Query',
              'Table': 'Products',
              'Where': 'ProductName in @ProductName',
              'ProductName': [
                'Chai',
                'Chang',
                'Aniseed Syrup',
                'Mishi Kobe Niku',
                'Ikura'
              ]
            }";

            dynamic criteria = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(json);
            criteria.Options = options;

            //Generated Sql: SELECT * FROM Products WHERE ProductName in (@ProductName0,@ProductName1,@ProductName2,@ProductName3,@ProductName4)
            IEnumerable<dynamic> result = northwindSqlite.dyno.Execute(criteria);

            result = result.ToList();
        }
    }

    public class Products
    {
        //public long ProductID { get; set; }

        //public string ProductName { get; set; }

        //public long SupplierID { get; set; }

        //public long CategoryID { get; set; }

        //public string QuantityPerUnit { get; set; }

        //public decimal UnitPrice { get; set; }

        //public long UnitsInStock { get; set; }

        //public long UnitsOnOrder { get; set; }

        //public long ReorderLevel { get; set; }

        //public string Discontinued { get; set; }

        public long productid { get; set; }

        public string productname { get; set; }

        public long supplierid { get; set; }

        public long categoryid { get; set; }

        public string quantityperunit { get; set; }

        public decimal unitprice { get; set; }

        public long unitsinstock { get; set; }

        public long UnitsOnOrder { get; set; }

        public long ReorderLevel { get; set; }

        public string Discontinued { get; set; }
    }

    //public class Products
    //{

    //    public long productid { get; set; }

    //    public string productname { get; set; }

    //    public long supplierid { get; set; }

    //    public long categoryid { get; set; }

    //    public string quantityperunit { get; set; }

    //    public decimal unitprice { get; set; }

    //    public long unitsinstock { get; set; }

    //    public long unitsonorder { get; set; }

    //    public long reorderlevel { get; set; }

    //    public string discontinued { get; set; }
    //}
}