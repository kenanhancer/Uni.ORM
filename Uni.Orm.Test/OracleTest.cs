using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uni.Extensions;

namespace Uni.Orm.Test
{
    [TestClass]
    public class OracleTest
    {
        UniOrm oracle = new UniOrm("Oracle");

        Listener uniOrm_Callback = new Listener
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

        public OracleTest()
        {
            oracle.Options.FieldNameLower = true;
            oracle.Options.EventListener = uniOrm_Callback;
        }

        [TestMethod]
        public void Exercise1()
        {
            var result1 = oracle.dyno.Count(Table: "PRODUCTS");

            var result2 = oracle.dyno.Count(Table: "PRODUCTS", Where: "PRODUCTID IN :PRODUCTID", Args: new { PRODUCTID = new int[] { 1, 2, 3, 4, 5 } });

            var result3 = oracle.dyno.Count(Table: "PRODUCTS", Where: "PRODUCTID=:0", Args: 1);

            var result4 = oracle.dyno.Exists(Table: "PRODUCTS", Where: "PRODUCTID IN (:0,:1)", Args: new object[] { 1, 2 });

            var result5 = oracle.dyno.Sum(Table: "PRODUCTS", Columns: "UNITPRICE");

            var result6 = oracle.dyno.Max(Table: "PRODUCTS", Columns: "UNITPRICE");

            var result7 = oracle.dyno.Min(Table: "PRODUCTS", Columns: "UNITPRICE");

            //var result8 = oracle.dyno.Avg(Table: "PRODUCTS", Columns: "UNITPRICE");

            var result9 = oracle.dyno.Query(Table: "CUSTOMERS", Columns: "COMPANYNAME", Where: "REGION IS NULL");

            var result10 = oracle.dyno.Query(Table: "CUSTOMERS", Columns: "COMPANYNAME", Where: "REGION IS NULL", CITY: "London");

            var result11 = oracle.dyno.Sum(Table: "PRODUCTS", Columns: "UNITPRICE", CATEGORYID: 1);

            var result12 = oracle.dyno.Sum(Table: "PRODUCTS", Columns: "UNITPRICE", Where: "CATEGORYID IN :CategoryID", CategoryID: new object[] { 1, 2 });

            IEnumerable<dynamic> result13 = oracle.dyno.Query(Table: "PRODUCTS", Columns: "UNITPRICE", Where: "CATEGORYID IN :CATEGORYID", CATEGORYID: new object[] { 1, 2 });

            result13 = result13.ToList();

            IEnumerable<dynamic> result14 = oracle.dyno.Query(Table: "CUSTOMERS", Columns: "COMPANYNAME", Where: "REGION IS NULL", CITY: "London");

            result14 = result14.ToList();

            IEnumerable<PRODUCTS> result15 = oracle.dyno.Query<PRODUCTS>(Sql: "SELECT * FROM PRODUCTS");

            result15 = result15.ToList();

            var result16 = oracle.dyno.Sum(Table: "PRODUCTS", Columns: "UNITPRICE", CATEGORYID: new object[] { 1, 2 });

            IEnumerable<dynamic> result17 = oracle.dyno.Query(Table: "PRODUCTS", CATEGORYID: new object[] { 1, 2, 3, 4, 5, 6, 7 });

            result17 = result17.ToList();

            IEnumerable<PRODUCTS> result18 = oracle.dyno.Query<PRODUCTS>(Table: "PRODUCTS", CATEGORYID: new object[] { 1, 2, 3, 4, 5, 6, 7 });

            result18 = result18.ToList();

            PRODUCTS result19 = oracle.dyno.Query<PRODUCTS>(Sql: "SELECT * FROM PRODUCTS", Limit: 1);
        }

        [TestMethod]
        public void DynamicObject_And_StronglyType_Mapping_Test()
        {
            //Below two lines of codes(result1, result2) return Strongly-Type mapped result.

            IEnumerable<PRODUCTS> result1 = oracle.dyno.Query<PRODUCTS>(Sql: "SELECT * FROM PRODUCTS");

            result1 = result1.ToList();

            IEnumerable<PRODUCTS> result2 = oracle.dyno.Query<PRODUCTS>(Table: "PRODUCTS");

            result2 = result2.ToList();

            //This code will return dynamic mapped result.
            var result3 = oracle.dyno.Query(Table: "PRODUCTS", Limit: 1);

            var anonymousObj = new UniAnonymousObject();

            var productType = anonymousObj.GetDynamicType(result3, "PRODUCTS");

            string productPoco = anonymousObj.GetPoco(result3, "PRODUCTS");
        }

        [TestMethod]
        public void In_Test()
        {
            IEnumerable<dynamic> result = oracle.dyno.Query(Table: "CUSTOMERS",
                                                           Where: "CUSTOMERID IN :CUSTOMERID",
                                                           CUSTOMERID: new string[] { "PERIC", "PRINI", "QUEEN", "RANCH", "REGGC", "ROMEY" });

            result = result.ToList();
        }

        [TestMethod]
        public void InsertTest()
        {
            var result = oracle.dyno.Insert(Table: "PRODUCTS",
                                            PKField: "PRODUCTID",
                                            Sequence: "SC_PRODUCT",
                                            Args: new
                                            {
                                                PRODUCTID = 0,
                                                PRODUCTNAME = "Test Product",
                                                SUPPLIERID = 12,
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                            }
                                            );
        }

        [TestMethod]
        public void InsertAutoIncrementTest()
        {
            var result = oracle.dyno.Insert(Table: "PERSON",
                                            PKField: "ID",
                                            Args: new
                                                {
                                                    ID = 0,
                                                    FIRSTNAME = "Test",
                                                    LASTNAME = "test"
                                                }
                                            );
        }

        [TestMethod]
        public void BulkInsertAutoIncrementTest()
        {
            var result = oracle.dyno.Insert(Table: "PERSON",
                                            PKField: "ID",
                                            Args: new object[]
                                                {
                                                    new
                                                    {
                                                        ID = 0,
                                                        FIRSTNAME = "Test1",
                                                        LASTNAME = "test1"
                                                    },
                                                    new
                                                    {
                                                        ID = 0,
                                                        FIRSTNAME = "Test2",
                                                        LASTNAME = "test2"
                                                    },
                                                    new
                                                    {
                                                        ID = 0,
                                                        FIRSTNAME = "Test3",
                                                        LASTNAME = "test3"
                                                    }
                                                }
                                            );
        }

        [TestMethod]
        public void BulkInsertTest()
        {
            var result = oracle.dyno.Insert(Table: "PRODUCTS",
                                            PKField: "PRODUCTID",
                                            Sequence: "SC_PRODUCT",
                                            Args: new object[]
                                            {
                                                new { PRODUCTID = 0,
                                                PRODUCTNAME = "Notebook", 
                                                SUPPLIERID = 12, 
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                                },
                                                new { PRODUCTID = 0,
                                                PRODUCTNAME = "Pen", 
                                                SUPPLIERID = 12, 
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                                }
                                            });
        }

        [TestMethod]
        public void DeleteTest()
        {
            int result1 = oracle.dyno.Delete(Table: "PRODUCTS",
                                             Where: "PRODUCTID=:PRODUCTID",
                                             Args: new { PRODUCTID = 80 });

            int result2 = oracle.dyno.Delete(Table: "PRODUCTS",
                                             Where: "PRODUCTNAME=:PRODUCTNAME AND SUPPLIERID=:SUPPLIERID",
                                             Args: new { SUPPLIERID = 12, PRODUCTNAME = "Pen" });

            int result3 = oracle.dyno.Delete(Table: "PRODUCTS",
                                             PKField: "PRODUCTID",
                                             Args: 86);

            int result4 = oracle.dyno.Delete(Table: "PRODUCTS",
                                             PKField: "PRODUCTID",
                                             Args: new object[] { 81, 82 });
        }

        [TestMethod]
        public void TransactionTest1()
        {
            var options = new Options { Transaction = oracle.NewConnection().BeginTransaction() };

            var result1 = oracle.dyno.Insert(Table: "PRODUCTS",
                                            PKField: "PRODUCTID",
                                            Sequence: "SC_PRODUCT",
                                            Options: options,
                                            Args: new
                                            {
                                                PRODUCTID = 0,
                                                PRODUCTNAME = "Test Product 1",
                                                SUPPLIERID = 12,
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                            });

            var result2 = oracle.dyno.Insert(Table: "PRODUCTS",
                                            PKField: "PRODUCTID",
                                            Sequence: "SC_PRODUCT",
                                            Options: options,
                                            Args: new
                                            {
                                                PRODUCTID = 0,
                                                PRODUCTNAME = "Test Product 2",
                                                SUPPLIERID = 12,
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                            });

            var result3 = oracle.dyno.Insert(Table: "PRODUCTS",
                                            PKField: "PRODUCTID",
                                            Sequence: "SC_PRODUCT",
                                            Options: options,
                                            Args: new
                                            {
                                                PRODUCTID = 0,
                                                PRODUCTNAME = "Test Product 3",
                                                SUPPLIERID = 12,
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                            });

            options.Transaction.Commit();
        }

        [TestMethod]
        public void TransactionTest2()
        {
            DbTransaction transaction = oracle.NewConnection().BeginTransaction();

            var result1 = oracle.dyno.Insert(Table: "PRODUCTS",
                                            PKField: "PRODUCTID",
                                            Sequence: "SC_PRODUCT",
                                            Transaction: transaction,
                                            Args: new
                                            {
                                                PRODUCTID = 0,
                                                PRODUCTNAME = "Test Product 1",
                                                SUPPLIERID = 12,
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                            });

            var result2 = oracle.dyno.Insert(Table: "PRODUCTS",
                                            PKField: "PRODUCTID",
                                            Sequence: "SC_PRODUCT",
                                            Transaction: transaction,
                                            Args: new
                                            {
                                                PRODUCTID = 0,
                                                PRODUCTNAME = "Test Product 2",
                                                SUPPLIERID = 12,
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                            });

            var result3 = oracle.dyno.Insert(Table: "PRODUCTS",
                                            PKField: "PRODUCTID",
                                            Sequence: "SC_PRODUCT",
                                            Transaction: transaction,
                                            Args: new
                                            {
                                                PRODUCTID = 0,
                                                PRODUCTNAME = "Test Product 3",
                                                SUPPLIERID = 12,
                                                CATEGORYID = 2,
                                                QUANTITYPERUNIT = "12 boxes",
                                                UNITPRICE = 10,
                                                UNITSINSTOCK = 50,
                                                DISCONTINUED = 0
                                            });

            transaction.Commit();
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

            bool result1 = oracle.dyno.Delete(Table: "PRODUCTS",
                                              PKField: "PRODUCTID",
                                              Options: options,
                                              Args: 79);
        }

        [TestMethod]
        public void InLineEvents()
        {
            Func<SqlEntity, object, object> onConvertingResult = (sqlEntity, result) => result.To<bool>();

            Action<Callback> onCallback = callback => Console.WriteLine("SqlQuery is {0}", callback.SqlQuery);

            bool result5 = oracle.dyno.Delete(Table: "PRODUCTS",
                                              PKField: "PRODUCTID",
                                              OnConvertingResult: onConvertingResult,
                                              OnCallback: onCallback,
                                              Args: new object[] { 88, 89 });
        }

        [TestMethod]
        public void Paging_Test()
        {
            IEnumerable<dynamic> result1 = oracle.dyno.Query(Table: "PRODUCTS", PageSize: 50, PageNo: 1);

            result1 = result1.ToList();

            IEnumerable<dynamic> result2 = oracle.dyno.Query(Table: "PRODUCTS", PageSize: 50, PageNo: 2);

            result2 = result2.ToList();
        }

        [TestMethod]
        public void Limit_And_OrderBy_Test()
        {
            //Different usage but same result below 4 lines ( result1, result2, result3, result4)
            //UniOrm will not append limit parameter to sql statement when Sql and Limit parameters are used together.

            var result1 = oracle.dyno.Query(Table: "PRODUCTS", Columns: "PRODUCTID,PRODUCTNAME,SUPPLIERID", Where: "SUPPLIERID IN :SUPPLIERID", OrderBy: "PRODUCTID", Limit: 1, SUPPLIERID: new[] { 1, 2 });

            var result2 = oracle.dyno.Query(Table: "PRODUCTS", Columns: "PRODUCTID,PRODUCTNAME,SUPPLIERID", OrderBy: "PRODUCTID", Limit: 1, SUPPLIERID: new[] { 1, 2 });

            var result3 = oracle.dyno.Query(Sql: "SELECT PRODUCTID,PRODUCTNAME,SUPPLIERID FROM PRODUCTS", Where: "SUPPLIERID IN :SUPPLIERID", OrderBy: "PRODUCTID", Limit: 1, SUPPLIERID: new[] { 1, 2 });

            var result4 = oracle.dyno.Query(Sql: "SELECT PRODUCTID,PRODUCTNAME,SUPPLIERID FROM PRODUCTS", OrderBy: "PRODUCTID", Limit: 1, SUPPLIERID: new[] { 1, 2 });
        }
    }

    public class PRODUCTS
    {
        public decimal PRODUCTID { get; set; }
        public string PRODUCTNAME { get; set; }
        public decimal SUPPLIERID { get; set; }
        public decimal CATEGORYID { get; set; }
        public string QUANTITYPERUNIT { get; set; }
        public decimal UNITPRICE { get; set; }
        public decimal UNITSINSTOCK { get; set; }
        public decimal UNITSONORDER { get; set; }
        public decimal REORDERLEVEL { get; set; }
        public decimal DISCONTINUED { get; set; }
    }
}