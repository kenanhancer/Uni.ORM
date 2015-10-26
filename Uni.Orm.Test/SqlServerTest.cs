using System;
using System.Dynamic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using Uni.Extensions;
using System.Data.SqlClient;

namespace Uni.Orm.Test
{
    [TestClass]
    public class SqlServerTest
    {
        private UniOrm adventureWorks = new UniOrm("AdventureWorks");

        private Listener uniOrm_Callback = new Listener
        {
            OnCallback = (Callback f) =>
            {
                Console.WriteLine(f.SqlQuery);
            }
        };

        public SqlServerTest()
        {
            adventureWorks.Options.EventListener = uniOrm_Callback;
        }

        [TestMethod]
        public void Query_StoredProcedure_With_Output_and_ReturnValue_ParameterTest()
        {
            dynamic args = new ExpandoObject();
            args.RETURN_VALUE = (int)0;
            args.ErrorLogID = (int)0;

            var listener = new Listener
            {
                OnCallback = (Callback f) =>
                {
                    //Callback returns back some variables.(Sql statement and Stored procedure output parameters)
                    Console.WriteLine(f.SqlQuery);

                    args.RETURN_VALUE = f.OutputParameters.RETURN_VALUE;
                },
                OnParameterCreating = (DbParameter f) =>
                {
                    if (f.ParameterName == "ErrorLogID")
                        f.Direction = ParameterDirection.Output;
                    else if (f.ParameterName == "RETURN_VALUE")
                        f.Direction = ParameterDirection.ReturnValue;
                }
            };

            adventureWorks.dyno.NonQuery(Sp: "uspLogError", Options: new Options { EventListener = listener }, Args: args);
        }

        [TestMethod]
        public void Query_StoredProcedure_With_ResultSet_Test()
        {
            dynamic args = new ExpandoObject();
            args.RETURN_VALUE = (int)0;
            args.BusinessEntityID = (int)10;

            var listener = new Listener
            {
                OnCallback = (Callback f) =>
                {
                    Console.WriteLine(f.SqlQuery);

                    args.RETURN_VALUE = f.OutputParameters.RETURN_VALUE;
                },
                OnParameterCreating = (DbParameter f) =>
                {
                    if (f.ParameterName == "RETURN_VALUE")
                        f.Direction = ParameterDirection.ReturnValue;
                }
            };

            var result = adventureWorks.dyno.Query(Sp: "uspGetEmployeeManagers", Options: new Options { EventListener = listener }, Args: args);
        }

        [TestMethod]
        public void Query_SimpleUsageTest()
        {
            //Generated Sql: SELECT * FROM [Production].[Product]
            IEnumerable<dynamic> result1 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product");

            IEnumerable<dynamic> result2 = adventureWorks.dyno.Query(Sql: "SELECT * FROM Production.Product;SELECT * FROM Person.Address", MultiResultSet: true);

            IEnumerable<dynamic> result3 = adventureWorks.dyno.MultiQuery(Sql: "SELECT * FROM Production.Product;SELECT * FROM Person.Address");
        }

        [TestMethod]
        public void Query_ForOneParameter_WithDifferentUsage_ButSameResultTest()
        {
            //Different usage but same result below 6 lines ( result1, result2, result3, result4, result5, result6)
            //Setting @0 parameter via Args parameter as Array

            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Name=@0
            IEnumerable<dynamic> result1 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: "Adjustable Race");

            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Name=@0
            IEnumerable<dynamic> result2 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: new object[] { "Adjustable Race" });

            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Name=@0
            IEnumerable<dynamic> result3 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: new[] { "Adjustable Race" });

            //Setting @Name parameter via args as anonymous object
            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Name=@Name
            IEnumerable<dynamic> result4 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@Name", Args: new { Name = "Adjustable Race" });

            //Setting @Name parameter directly without using Args parameter
            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Name=@Name
            IEnumerable<dynamic> result5 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@Name", Name: "Adjustable Race");

            //Setting @Name parameter directly without using Args and Where parameter (Below code will create where clause)
            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Name=@Name
            IEnumerable<dynamic> result6 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Name: "Adjustable Race");

            Assert.IsTrue((result1.Count() == result2.Count()) == (result3.Count() == result4.Count()) == (result5.Count() == result6.Count()));
        }

        [TestMethod]
        public void Query_ForSeveralParameters_WithDifferentUsage_ButSameResultTest()
        {
            //Different usage but same result below 4 lines ( result1, result2, result3, result4)
            //Setting @0 and @1 parameters via Args parameter

            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Color=@1 AND ListPrice=@0
            IEnumerable<dynamic> result1 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@1 AND ListPrice=@0", Args: new object[] { 0, "Black" });

            //Setting @Color and @ListPrice parameters via Args parameter as anonymous object
            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Color=@Color AND ListPrice=@ListPrice
            IEnumerable<dynamic> result2 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@Color AND ListPrice=@ListPrice", Args: new { Color = "Black", ListPrice = 0 });

            //Setting @Color and @ListPrice parameters directly without using Args parameter
            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Color=@Color AND ListPrice=@ListPrice
            IEnumerable<dynamic> result3 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@Color AND ListPrice=@ListPrice", Color: "Black", ListPrice: 0);

            //Setting @Color and @ListPrice parameters directly without using Args and Where parameter (Below code will create where clause)
            //Generated Sql: SELECT * FROM [Production].[Product] WHERE Color=@Color AND ListPrice=@ListPrice
            IEnumerable<dynamic> result4 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Color: "Black", ListPrice: 0);

            Assert.IsTrue((result1.Count() == result2.Count()) == (result3.Count() == result4.Count()));
        }

        [TestMethod]
        public void Query_ForSqlStatement_WithDifferentUsage_ButSameResultTest()
        {
            //Different usage but same result below 3 lines ( result1, result2, result3)

            IEnumerable<dynamic> result1 = adventureWorks.dyno.Query(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Name=@1", Args: new object[] { 0, "Adjustable Race" });

            IEnumerable<dynamic> result2 = adventureWorks.dyno.Query(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@ListPrice and Name=@Name", Args: new { ListPrice = 0, Name = "Adjustable Race" });

            IEnumerable<dynamic> result3 = adventureWorks.dyno.Query(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@ListPrice and Name=@Name", ListPrice: 0, Name: "Adjustable Race");

            //Generated Sql: SELECT * FROM Production.Product WHERE ListPrice=@ListPrice AND Name=@Name
            IEnumerable<dynamic> result4 = adventureWorks.dyno.Query(Sql: "SELECT * FROM Production.Product", ListPrice: 0, Name: "Adjustable Race");

            Assert.IsTrue((result1.Count() == result2.Count()) == (result3.Count() == result4.Count()));
        }

        [TestMethod]
        public void Exists_Test()
        {
            //Different usage but same result below 7 lines ( result1, result2, result3, result4, result5, result6, result7)
            //Below lines will return bool value for Sql query result. (If row count of query result is bigger than 0 than true. Otherwise false)

            bool result1 = adventureWorks.dyno.Exists(Schema: "Production", Table: "Product");

            bool result2 = adventureWorks.dyno.Exists(Schema: "Production", Table: "Product", ListPrice: 0, Name: "Adjustable Race");

            bool result3 = adventureWorks.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@ListPrice AND Name=@Name", ListPrice: 0, Name: "Adjustable Race");

            bool result4 = adventureWorks.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@0 AND Name=@1", Args: new object[] { 0, "Adjustable Race" });

            bool result5 = adventureWorks.dyno.Exists(Schema: "Production", Table: "Product", Where: "ListPrice=@ListPrice AND Name=@Name", Args: new { Name = "Adjustable Race", ListPrice = 0 });

            bool result7 = adventureWorks.dyno.Query<bool>(Sql: "SELECT CASE WHEN EXISTS(SELECT * FROM Production.Product) THEN 1 ELSE 0 END as RESULT", Limit: 1);

            int result8 = adventureWorks.dyno.Query(Sql: "SELECT CASE WHEN EXISTS(SELECT * FROM Production.Product) THEN 1 ELSE 0 END as RESULT", Limit: 1);

            bool result9 = adventureWorks.dyno.GetBool(Sql: "SELECT CASE WHEN EXISTS(SELECT * FROM Production.Product) THEN 1 ELSE 0 END as RESULT", Limit: 1);
        }

        [TestMethod]
        public void Count_Test()
        {
            //Below line will return the number of rows for Product table
            //Generated SQL: SELECT COUNT(*) COUNT FROM Production.Product
            int result1 = adventureWorks.dyno.Count(Schema: "Production", Table: "Product");

            //Different usage but same result below 7 lines ( result2, result3, result4, result5, result6, result7, result8)
            int result2 = adventureWorks.dyno.Count(Schema: "Production", Table: "Product", ListPrice: 0, Color: "Black");

            int result3 = adventureWorks.dyno.Count(Schema: "Production", Table: "Product", Where: "ListPrice=@ListPrice and Color=@Color", ListPrice: 0, Color: "Black");

            int result4 = adventureWorks.dyno.Count(Schema: "Production", Table: "Product", Where: "ListPrice=@0 and Color=@1", Args: new object[] { 0, "Black" });

            int result5 = adventureWorks.dyno.Count(Schema: "Production", Table: "Product", Where: "ListPrice=@ListPrice and Color=@Color", Args: new { ListPrice = 0, Color = "Black" });

            int result6 = adventureWorks.dyno.Query(Sql: "SELECT COUNT(*) FROM Production.Product WHERE ListPrice=@0 and Color=@1", Limit: 1, Args: new object[] { 0, "Black" });

            int result7 = adventureWorks.dyno.Query(Sql: "SELECT COUNT(*) FROM Production.Product WHERE ListPrice=@ListPrice and Color=@Color", Limit: 1, Args: new { ListPrice = 0, Color = "Black" });

            int result8 = adventureWorks.dyno.Query(Sql: "SELECT COUNT(*) FROM Production.Product WHERE ListPrice=@ListPrice and Color=@Color", Limit: 1, ListPrice: 0, Color: "Black");
        }

        [TestMethod]
        public void Sum_Test()
        {
            //Below line will return the total sum of a numeric column
            decimal result1 = adventureWorks.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice");

            decimal result2 = adventureWorks.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice", Args: new { Color = "Black" });

            decimal result3 = adventureWorks.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice", Color: "Black");

            decimal result4 = adventureWorks.dyno.Query(Sql: "SELECT SUM(ListPrice) FROM Production.Product", Limit: 1);

            //Different usage but same result below 5 lines ( result5, result6, result7, result8, result9)
            decimal result5 = adventureWorks.dyno.Query(Sql: "SELECT SUM(ListPrice) FROM Production.Product WHERE Color=@Color", Limit: 1, Args: new { Color = "Black" });

            decimal result6 = adventureWorks.dyno.Query(Sql: "SELECT SUM(ListPrice) FROM Production.Product WHERE Color=@0", Limit: 1, Args: new object[] { "Black" });

            decimal result7 = adventureWorks.dyno.Query(Sql: "SELECT SUM(ListPrice) FROM Production.Product WHERE Color=@0", Limit: 1, Args: new List<string> { "Black" });

            decimal result8 = adventureWorks.dyno.Query(Sql: "SELECT SUM(ListPrice) FROM Production.Product WHERE Color=@0", Limit: 1, Args: "Black");

            decimal result9 = adventureWorks.dyno.Query(Sql: "SELECT SUM(ListPrice) FROM Production.Product WHERE Color=@Color", Limit: 1, Color: "Black");
        }

        [TestMethod]
        public void Max_Test()
        {
            var result1 = adventureWorks.dyno.Max(Schema: "Production", Table: "Product", Columns: "ListPrice");

            var result2 = adventureWorks.dyno.Max(Schema: "Production", Table: "Product", Columns: "ListPrice", ListPrice: 34.99, Name: "Sport-100 Helmet, Red");

            var result3 = adventureWorks.dyno.Max(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@ListPrice AND Name=@Name", ListPrice: 34.99, Name: "Sport-100 Helmet, Red");

            var result4 = adventureWorks.dyno.Max(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@0 AND Name=@1", Args: new object[] { 34.99, "Sport-100 Helmet, Red" });

            var result5 = adventureWorks.dyno.Max(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@ListPrice AND Name=@Name", Args: new { Name = "Sport-100 Helmet, Red", ListPrice = 34.99 });

            var result6 = adventureWorks.dyno.Max(Sql: "SELECT MAX(ListPrice) MaxListPrice FROM Production.Product WHERE Name=@Name", Args: new { Name = "Sport-100 Helmet, Red" });

            var result7 = adventureWorks.dyno.Max(Sql: "SELECT MAX(ListPrice) MaxListPrice FROM Production.Product WHERE Name=@0", Args: new object[] { "Sport-100 Helmet, Red" });

            var result8 = adventureWorks.dyno.Max(Sql: "SELECT MAX(ListPrice) MaxListPrice FROM Production.Product WHERE Name=@Name", Name: "Sport-100 Helmet, Red");

            var result9 = adventureWorks.dyno.GetDecimal(Sql: "SELECT MAX(ListPrice) MaxListPrice FROM Production.Product", Limit: 1);

            var result10 = adventureWorks.dyno.Query<decimal>(Sql: "SELECT MAX(ListPrice) MaxListPrice FROM Production.Product WHERE Name=@Name", Limit: 1, Name: "Sport-100 Helmet, Red");

            var result11 = adventureWorks.dyno.Query<decimal>(Sql: "SELECT MAX(ListPrice) MaxListPrice FROM Production.Product WHERE Name=@Name", Name: "Sport-100 Helmet, Red");

            var result12 = adventureWorks.dyno.Query(Sql: "SELECT MAX(ListPrice) MaxListPrice, Name FROM Production.Product GROUP BY Name", Limit: 1);

            var result13 = adventureWorks.dyno.Query(Sql: "SELECT MAX(ListPrice) MaxListPrice FROM Production.Product", Limit: 1);
        }

        [TestMethod]
        public void Min_Test()
        {
            var result1 = adventureWorks.dyno.Min(Schema: "Production", Table: "Product", Columns: "ListPrice");

            var result2 = adventureWorks.dyno.Min(Schema: "Production", Table: "Product", Columns: "ListPrice", ListPrice: 34.99, Name: "Sport-100 Helmet, Red");

            var result3 = adventureWorks.dyno.Min(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@ListPrice AND Name=@Name", ListPrice: 34.99, Name: "Sport-100 Helmet, Red");

            var result4 = adventureWorks.dyno.Min(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@0 AND Name=@1", Args: new object[] { 34.99, "Sport-100 Helmet, Red" });

            var result5 = adventureWorks.dyno.Min(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@ListPrice AND Name=@Name", Args: new { Name = "Sport-100 Helmet, Red", ListPrice = 34.99 });

            var result6 = adventureWorks.dyno.Min(Sql: "SELECT MIN(ListPrice) ListPrice FROM Production.Product WHERE Name=@Name", Args: new { Name = "Sport-100 Helmet, Red" });

            var result7 = adventureWorks.dyno.Min(Sql: "SELECT MIN(ListPrice) ListPrice FROM Production.Product WHERE Name=@0", Args: new object[] { "Sport-100 Helmet, Red" });

            var result8 = adventureWorks.dyno.Min(Sql: "SELECT MIN(ListPrice) ListPrice FROM Production.Product WHERE Name=@Name", Name: "Sport-100 Helmet, Red");

            var result9 = adventureWorks.dyno.GetDecimal(Sql: "SELECT MIN(ListPrice) ListPrice FROM Production.Product", Limit: 1);

            var result10 = adventureWorks.dyno.Query<decimal>(Sql: "SELECT MIN(ListPrice) ListPrice FROM Production.Product WHERE Name=@Name", Limit: 1, Name: "Sport-100 Helmet, Red");
        }

        [TestMethod]
        public void Avg_Test()
        {
            var result1 = adventureWorks.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice");

            var result2 = adventureWorks.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice", ListPrice: 34.99, Name: "Sport-100 Helmet, Red");

            var result3 = adventureWorks.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@ListPrice AND Name=@Name", ListPrice: 34.99, Name: "Sport-100 Helmet, Red");

            var result4 = adventureWorks.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@0 AND Name=@1", Args: new object[] { 34.99, "Sport-100 Helmet, Red" });

            var result5 = adventureWorks.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "ListPrice=@ListPrice AND Name=@Name", Args: new { Name = "Sport-100 Helmet, Red", ListPrice = 34.99 });

            var result6 = adventureWorks.dyno.Avg(Sql: "SELECT AVG(ListPrice) ListPrice FROM Production.Product WHERE Name=@Name", Args: new { Name = "Sport-100 Helmet, Red" });

            var result7 = adventureWorks.dyno.Avg(Sql: "SELECT AVG(ListPrice) ListPrice FROM Production.Product WHERE Name=@0", Args: new object[] { "Sport-100 Helmet, Red" });

            var result8 = adventureWorks.dyno.Avg(Sql: "SELECT AVG(ListPrice) ListPrice FROM Production.Product WHERE Name=@Name", Name: "Sport-100 Helmet, Red");

            var result9 = adventureWorks.dyno.GetDecimal(Sql: "SELECT AVG(ListPrice) ListPrice FROM Production.Product");

            var result10 = adventureWorks.dyno.Query<decimal>(Sql: "SELECT AVG(ListPrice) ListPrice FROM Production.Product WHERE Name=@Name", Limit: 1, Name: "Sport-100 Helmet, Red");
        }

        [TestMethod]
        public void In_Test()
        {
            var colorArray = new[] { "Black", "Yellow", "Red" };
            var sizeArray = new[] { "38", "40", "42" };

            //Generated Sql: SELECT ProductID,Name,ProductNumber FROM [Production].[Product] WHERE Color in (@Color0,@Color1,@Color2) and Size in (@Size0,@Size1,@Size2)
            var result1 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "Color in @Color and Size in @Size", Args: new { Color = colorArray, Size = sizeArray });

            //Generated Sql: SELECT ProductID,Name,ProductNumber FROM [Production].[Product] WHERE Color in (@Color0,@Color1,@Color2) and Size in (@Size0,@Size1,@Size2)
            var result2 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "Color in @Color and Size in @Size", Color: colorArray, Size: sizeArray);
        }

        [TestMethod]
        public void AggregateFunctions_And_In_TogetherUsage_Test()
        {
            //Exists and In Usage
            //Generated Sql: SELECT CASE WHEN EXISTS(SELECT * FROM [Production].[Product] WHERE Color in (@Color0,@Color1)) THEN 1 ELSE 0 END as RESULT 
            var result1 = adventureWorks.dyno.Exists(Schema: "Production", Table: "Product", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

            //Count and In Usage
            //Generated Sql: SELECT COUNT(ListPrice) FROM [Production].[Product] WHERE Color in (@Color0,@Color1)
            var result2 = adventureWorks.dyno.Count(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

            //Sum and In Usage
            //Generated Sql: SELECT SUM(ListPrice) FROM [Production].[Product] WHERE Color in (@Color0,@Color1)
            var result3 = adventureWorks.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

            //Max and In Usage
            //Generated Sql: SELECT MAX(ListPrice) FROM [Production].[Product] WHERE Color in (@Color0,@Color1)
            var result4 = adventureWorks.dyno.Max(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

            //Min and In Usage
            //Generated Sql: SELECT MIN(ListPrice) FROM [Production].[Product] WHERE Color in (@Color0,@Color1)
            var result5 = adventureWorks.dyno.Min(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

            //Avg and In Usage
            //Generated Sql: SELECT AVG(ListPrice) AVG FROM Production.Product WHERE Color in (@Color0,@Color1)
            var result6 = adventureWorks.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });
        }

        [TestMethod]
        public void Limit_And_OrderBy_Test()
        {
            //Different usage but same result below 4 lines ( result1, result2, result3, result4)
            //UniOrm will not append limit parameter to sql statement when Sql and Limit parameters are used together.

            //Generated Sql: SELECT TOP 1 ProductID,Name,ProductNumber FROM [Production].[Product] WHERE ListPrice=@ListPrice and Color in (@Color0,@Color1) ORDER BY ProductID
            var result1 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "ListPrice=@ListPrice and Color in @Color", OrderBy: "ProductID", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });

            //Generated Sql: SELECT TOP 1 ProductID,Name,ProductNumber FROM [Production].[Product] WHERE Color in (@Color0,@Color1) AND ListPrice=@ListPrice ORDER BY ProductID
            var result2 = adventureWorks.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", OrderBy: "ProductID", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });

            //Generated Sql: SELECT TOP 1 ProductID,Name,ProductNumber FROM [Production].[Product] WHERE Color in (@Color0,@Color1) AND ListPrice=@ListPrice ORDER BY ProductID
            var result3 = adventureWorks.dyno.Query(Sql: "SELECT TOP 1 ProductID,Name,ProductNumber FROM [Production].[Product]", Where: "ListPrice=@ListPrice and Color in @Color", OrderBy: "ProductID", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });

            //Generated Sql: SELECT TOP 1 ProductID,Name,ProductNumber FROM [Production].[Product] WHERE Color in (@Color0,@Color1) AND ListPrice=@ListPrice ORDER BY ProductID
            var result4 = adventureWorks.dyno.Query(Sql: "SELECT TOP 1 ProductID,Name,ProductNumber FROM [Production].[Product]", OrderBy: "ProductID", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });

            var result5 = adventureWorks.dyno.Query(Sql: "SELECT TOP 1 ProductID FROM [Production].[Product]", OrderBy: "ProductID", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });
        }

        [TestMethod]
        public void DynamicObject_And_StronglyType_Mapping_Test()
        {
            //Below two lines of codes(result1, result2) return Strongly-Type mapped result.

            IEnumerable<Person> result1 = adventureWorks.dyno.Query<Person>(Sql: "SELECT * FROM Person.Person");

            IEnumerable<Person> result2 = adventureWorks.dyno.Query<Person>(Schema: "Person", Table: "Person");

            //This code will return dynamic mapped result.
            var result3 = adventureWorks.dyno.Query(Schema: "Person", Table: "Person", Limit: 1);

            var anonymousObj = new UniAnonymousObject();

            var personType = anonymousObj.GetDynamicType(result3, "Person");

            var personPoco = anonymousObj.GetPoco(result3, "Person");
        }

        [TestMethod]
        public void Paging_Test()
        {
            //(Use RowNumberColumn for Microsoft SQL Server)

            //Generated Sql: SELECT TOP 50 * FROM (SELECT ROW_NUMBER() OVER (ORDER BY BusinessEntityID) AS RowNumber, * FROM (SELECT * FROM [Person].[Person]) as PagedTable) as PagedRecords WHERE RowNumber > 0
            IEnumerable<dynamic> result1 = adventureWorks.dyno.Query(Schema: "Person", Table: "Person", RowNumberColumn: "BusinessEntityID", PageSize: 50, PageNo: 1);

            result1 = result1.ToList();

            //Generated Sql: SELECT TOP 50 * FROM (SELECT ROW_NUMBER() OVER (ORDER BY BusinessEntityID) AS RowNumber, * FROM (SELECT * FROM [Person].[Person]) as PagedTable) as PagedRecords WHERE RowNumber > 50
            IEnumerable<dynamic> result2 = adventureWorks.dyno.Query(Schema: "Person", Table: "Person", RowNumberColumn: "BusinessEntityID", PageSize: 50, PageNo: 2);

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
                    Console.WriteLine(f.SqlQuery);
                }
            };

            var criteria = new
            {
                Operation = "Query",
                Schema = "Production",
                Table = "Product",
                Columns = "ListPrice",
                Where = "Color in @Color",
                Color = new[] { "Black", "Yellow" },
                Options = options
            };

            //Generated Sql: SELECT ListPrice FROM [Production].[Product] WHERE Color in (@Color0,@Color1)
            IEnumerable<dynamic> result = adventureWorks.dyno.Execute(criteria);

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
                    Console.WriteLine(f.SqlQuery);
                }
            };

            string json = @"{
              'Operation': 'Query',
              'Schema': 'Production',
              'Table': 'Product',
              'Where': 'Color in @Color',
              'Color': [
                'Black',
                'Yellow'
              ]
            }";

            dynamic criteria = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(json);
            criteria.Options = options;

            //Generated Sql: SELECT ListPrice FROM [Production].[Product] WHERE Color in (@Color0,@Color1)
            IEnumerable<dynamic> result = adventureWorks.dyno.Execute(criteria);

            result = result.ToList();
        }

        [TestMethod]
        public void InsertTest()
        {
            var result = adventureWorks.dyno.Insert(
                                                    Schema: "Production",
                                                    Table: "Location",
                                                    PKField: "LocationID",
                                                    Args: new { LocationID = 0, Name = "TestProduct_" + Guid.NewGuid().ToString("N"), CostRate = 0, Availability = 3, ModifiedDate = DateTime.Now });
        }

        [TestMethod]
        public void BulkInsertTest()
        {
            var result = adventureWorks.dyno.Insert(
                                                    Schema: "Production",
                                                    Table: "Location",
                                                    PKField: "LocationID",
                                                    Args: new object[]
                                                    {
                                                        new { LocationID = 0, Name = "TestProduct_" + Guid.NewGuid().ToString("N"), CostRate = 0, Availability = 3, ModifiedDate = DateTime.Now },
                                                        new { LocationID = 0, Name = "TestProduct_" + Guid.NewGuid().ToString("N"), CostRate = 0, Availability = 5, ModifiedDate = DateTime.Now },
                                                        new { LocationID = 0, Name = "TestProduct_" + Guid.NewGuid().ToString("N"), CostRate = 0, Availability = 7, ModifiedDate = DateTime.Now }
                                                    });
        }
    }

    public class Person
    {
        public int BusinessEntityID { get; set; }

        public string PersonType { get; set; }

        public bool NameStyle { get; set; }

        public object Title { get; set; }

        public string FirstName { get; set; }

        public string MiddleName { get; set; }

        public string LastName { get; set; }

        public object Suffix { get; set; }

        public int EmailPromotion { get; set; }

        public object AdditionalContactInfo { get; set; }

        public string Demographics { get; set; }

        public System.Guid rowguid { get; set; }

        public System.DateTime ModifiedDate { get; set; }
    }
}