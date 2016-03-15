`Uni.Orm` is the continuation of `Uni` project. Project name is just changed.

`Uni.Orm` is a simple, fast and lightweight micro ORM. It has been developed as a compact single class library enabling to do the job with minimal effort just by using a few basic methods.

##Performance
![orm_comp](https://cloud.githubusercontent.com/assets/1851856/11440560/3e3438d6-950c-11e5-8a67-fa479f75a839.jpg)

##How To Install It?
Drop `UniOrm.cs` and `UniExtensions.cs` C#.NET code files into your project and change it as you wish or install from `NuGet Galery`.

If you want to install from `Nuget`, you should write Package Manager Console below code and `Uni.ORM` will be installed automatically as shown below.
```
Install-Package Uni.ORM
```
![uniormpkmanager](https://cloud.githubusercontent.com/assets/1851856/10602062/d7680ef8-771e-11e5-8f88-2945bc3f74ea.PNG)

By the way, you can also reach `Uni.ORM` `NuGet` package from https://www.nuget.org/packages/Uni.ORM/ address as shown below.

![uniormnuget](https://cloud.githubusercontent.com/assets/1851856/10601767/88efab66-771c-11e5-9981-291a23ff43e6.PNG)

##How Do You Use It?
Suppose you installed database connectors in your machine.
Your project does not need any DLL in references. `Uni.ORM` will find DLL which is necessary from the GAC.

###First Step
Let's have a look at web.config or app.config file for ConnectionStrings which will be used by `Uni.ORM`.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <connectionStrings>
    <add name="AdventureWorks" connectionString="Data Source=localhost;Initial Catalog=AdventureWorks2012;Integrated Security=True" providerName="System.Data.SqlClient"/>
    
    <add name="HR" connectionString="DATA SOURCE=localhost;PASSWORD=1;PERSIST SECURITY INFO=True;USER ID=HR" providerName="Oracle.DataAccess.Client"/>
    
    <add name="Sakila" connectionString="server=localhost;Uid=root;Pwd=1;database=sakila;Allow User Variables=true;" providerName="MySql.Data.MySqlClient"/>
    
    <add name="NorthwindSqlite" connectionString="Data Source=.\Northwind.sqlite;Version=3;" providerName="System.Data.Sqlite"/>
    
    <add name="NorthwindPostgre" connectionString="HOST=localhost;PORT=5432;DATABASE=Northwind;USER ID=postgres;PASSWORD=123456;" providerName="NpgSql"/>
  </connectionStrings>
</configuration>
```
`Uni.ORM` has the ability to understand the database you want to use with providerName attribute in connectionString. So, don't forget the providerName.
Suppose we want to use Oracle database. The providerName must be set as "Oracle.DataAccess.Client" as shown in the below config code.

###Second Step
Create UniOrm object now.
```csharp
var aw = new UniOrm("AdventureWorks");//Microsoft SQL Server

var hr = new UniOrm("HR");//Oracle

var sakila = new UniOrm("Sakila");//MySQL

var northwindPostgre = new UniOrm("NorthwindPostgre");//PostgreSQL

var northwindSqlite = new UniOrm("NorthwindSqlite");//SQLite
```

Create UniOrm object with connectionString directly in case you don't want to use config file.
```csharp
var aw = new UniOrm(@"Data Source=localhost;Initial Catalog=AdventureWorks2012;Integrated Security=True", DatabaseType.SQLServer);//Microsoft SQL Server

var hr = new UniOrm(@"DATA SOURCE=localhost;PASSWORD=1;PERSIST SECURITY INFO=True;USER ID=HR", DatabaseType.Oracle);//Oracle

var sakila = new UniOrm(@"server=localhost;Uid=root;Pwd=1;database=sakila;Allow User Variables=true;", DatabaseType.MySQL);//MySQL
```

Suppose you want to use SQLite database. Add SQLite DLL files in your project references as you don't have DLLs in your GAC. 

```csharp
var northwindSqlite = new UniOrm(@"Data Source=.\Northwind.sqlite;Version=3;", DatabaseType.SQLite, System.Data.SQLite.SQLiteFactory.Instance);//SQLite
```

##How To Execute a Query?
Suppose we want to Query "Product" table. All you need is to instantiate it inline.
```csharp
//returns all the products
var result = aw.dyno.Query(Schema: "Production", Table: "Product");
```
Actually, after you write "aw.dyno" and click the point button, you will not see intellisense. Because, methods and arguments after "aw.dyno" code are on the fly. But, `Uni.ORM` is smart and dynamic. So, it will generate and execute query according to your method and parameters.

```csharp
//if you want to use dynamic advantages, you should use dynamic. 
//But, if you use like that, you will lose intellisense.
dynamic aw = new UniOrm("AdventureWorks");

var result = aw.Query(Schema: "Production", Table: "Product");

//if you use as bellow, you just use dyno property without extra code
var aw = new UniOrm("AdventureWorks");

var result = aw.dyno.Query(Schema: "Production", Table: "Product");

//you can use intellisense here
var result = aw.Count(commandType: System.Data.CommandType.TableDirect, schema: "Production", commandText: "Product");
```

You can also run classic queries.
```csharp
IEnumerable<dynamic> result = aw.dyno.Query(Sql: "SELECT * FROM [Production].[Product]");

IEnumerable<dynamic> result = aw.dyno.Query(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@0 and Name=@1", Args: new object[] { 0, "Adjustable Race" });

IEnumerable<dynamic> result = aw.dyno.Query(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@ListPrice and Name=@Name", Args: new { ListPrice = 0, Name = "Adjustable Race" });

IEnumerable<dynamic> result = aw.dyno.Query(Sql: "SELECT * FROM Production.Product WHERE ListPrice=@ListPrice and Name=@Name", ListPrice: 0, Name: "Adjustable Race");

//Generated Sql: SELECT * FROM Production.Product WHERE ListPrice=@ListPrice AND Name=@Name
IEnumerable<dynamic> result = aw.dyno.Query(Sql: "SELECT * FROM Production.Product", ListPrice: 0, Name: "Adjustable Race");
```

##Dynamic object and strongly typed result
Suppose you want to use `POCO` model, you can set your `POCO` type as generic in method. So, `Uni.ORM` will return strongly typed result.

```csharp
public class customer
{
    public int customer_id { get; set; }
    public int store_id { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email { get; set; }
    public int address_id { get; set; }
    public bool active { get; set; }
    public DateTime create_date { get; set; }
    public DateTime last_update { get; set; }
}

//Execute and return strongly typed result
IEnumerable<customer> result = sakila.dyno.Query<customer>(Table: "customer");

//Execute and return dynamic object result
IEnumerable<dynamic> result = sakila.dyno.Query(Table: "customer");
```

##Generating POCO Model
Below is an easy way how to generate POCO Model.

```csharp
//This code will return dynamic mapped result.
var result1 = oracle.dyno.Query(Table: "PRODUCTS", Limit: 1);

var anonymousObj = new UniAnonymousObject();

var productType = anonymousObj.GetDynamicType(result1, "PRODUCTS");

string productPoco = anonymousObj.GetPoco(result1, "PRODUCTS");
```

Generated POCO class

```csharp
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
```

Let's use generated POCO type.
```csharp
IEnumerable<PRODUCTS> result2 = oracle.dyno.Query<PRODUCTS>(Table: "PRODUCTS");
```

##IN Statement
You can use In statement with `Uni.ORM` simply as below;
```csharp
//This query is created by Uni.ORM
//SELECT ProductID,Name,ProductNumber FROM [Production].[Product] WHERE Color in (@Color0,@Color1,@Color2) and Size in (@Size0,@Size1,@Size2)
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "Color in @Color and Size in @Size", Args: new { Color = new[] { "Black", "Yellow", "Red" }, Size = new[] { "38", "40", "42" } });
```

##LIMIT AND ORDERBY
Let's say we need first row of our data. We need to set "Limit" argument as 1 (Limit:1) and "OrderBy" argument as "ASC"
```csharp
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "ListPrice=@ListPrice and Color in @Color", OrderBy: "ProductID", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });
```
We can also take last row in same way. Just change "OrderBy" from "ASC" to "DESC" like below
```csharp
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "ListPrice=@ListPrice and Color in @Color", OrderBy: "ProductID DESC", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });
```

##PAGING
Let's say you need paging in your application. Just use query method with extra arguments which are "PageSize" and "PageNo" as below;
```csharp
//First page 10 record
var sakilaResult4 = sakila.dyno.Query(Table: "customer", OrderBy: "address_id", PageSize: 10, PageNo: 1);
//Secodn page 10 record
var sakilaResult5 = sakila.dyno.Query(Table: "customer", OrderBy: "address_id", PageSize: 10, PageNo: 2);
```

Use RowNumberColumn for Microsoft SQL Server
```csharp
//Generated Sql: SELECT TOP 50 * FROM (SELECT ROW_NUMBER() OVER (ORDER BY BusinessEntityID) AS RowNumber, * FROM (SELECT * FROM [Person].[Person]) as PagedTable) as PagedRecords WHERE RowNumber > 0
var result1 = adventureWorks.dyno.Query(Schema: "Person", Table: "Person", RowNumberColumn: "BusinessEntityID", PageSize: 50, PageNo: 1);

//Generated Sql: SELECT TOP 50 * FROM (SELECT ROW_NUMBER() OVER (ORDER BY BusinessEntityID) AS RowNumber, * FROM (SELECT * FROM [Person].[Person]) as PagedTable) as PagedRecords WHERE RowNumber > 50
var result2 = adventureWorks.dyno.Query(Schema: "Person", Table: "Person", RowNumberColumn: "BusinessEntityID", PageSize: 50, PageNo: 2);
```

SQLite
```csharp
//Generated Sql: SELECT * FROM Products LIMIT 0,50
IEnumerable<dynamic> result1 = northwindSqlite.dyno.Query(Table: "Products", PageSize: 50, PageNo: 1);

//Generated Sql: SELECT * FROM Products LIMIT 50,50
IEnumerable<dynamic> result2 = northwindSqlite.dyno.Query(Table: "Products", PageSize: 50, PageNo: 2);
```

##Aggregate operations
You can also use aggregates. Actually, logic is the same. Just change Method name and `Uni.ORM` will do this job.
Do not forget to set "Columns" argument for "Sum", "Max", "Min", "Avg" aggregates.
```csharp
//Exists and In Usage
var result = aw.dyno.Exists(Schema: "Production", Table: "Product", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

//Count and In Usage
var result = aw.dyno.Count(Schema: "Production", Table: "Product", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

//Sum and In Usage
var result = aw.dyno.Sum(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

//Max and In Usage
var result = aw.dyno.Max(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

//Min and In Usage
var result = aw.dyno.Min(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });

//Avg and In Usage
var result = aw.dyno.Avg(Schema: "Production", Table: "Product", Columns: "ListPrice", Where: "Color in @Color", Color: new[] { "Black", "Yellow" });
```

##Some Cast Operations
Uni.Orm has some direct cast operations which are `GetBool`, `GetInt`, `GetLong`, `GetDecimal`, `GetDouble`, `GetFloat`, `GetDateTime`, `GetValue`

```csharp
decimal result1 = adventureWorks.dyno.GetDecimal(Sql: "SELECT AVG(ListPrice) ListPrice FROM Production.Product");

OR

decimal result2 = adventureWorks.dyno.Query<decimal>(Sql: "SELECT AVG(ListPrice) ListPrice FROM Production.Product WHERE Name=@Name", Limit: 1, Name: "Sport-100 Helmet, Red");

bool result3 = adventureWorks.dyno.GetBool(Sql: "SELECT CASE WHEN EXISTS(SELECT * FROM Production.Product) THEN 1 ELSE 0 END as RESULT", Limit: 1);

```

##Stored Procedure and Function
Let's say you want to execute your stored procedure or function. Just use "SP" or "FN" arguments.
If your stored procedure or function returns back as an output parameter, you should also use "Listener" argument so that after method is executed, you can retrieve output parameters and sql statement which is generated by `Uni.ORM`

If you need some complex stored procedure usage, `Uni.ORM` presents Listener. You can listen some events which are `OnCallback` and `OnParameterCreating`. There will be other events in the future.

`OnCallback` occurs when the result is generated.

`OnParameterCreating` occurs before the parameter is created. So, you can change parameter direction, dataType etc.

```csharp
//Simple usage of Stored procedure
var result = aw.dyno.Query(Schema: "Person", Sp: "GetPersonList");

//Stored Procedure and CallBack(You can take output parameter of SP)

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
```

```csharp
//Simple usage of Function
var result = crm.dyno.Query(Schema: "User", FN: "fn_GetUserByUserID", Args: new object[] { 64, 1 });
```
##Insert, Update, Delete, BulkInsert and BulkUpdate Operations
`Uni.ORM` will generate Insert SQL query according to your object.

```csharp
//Insert one record
var newID = sakila.dyno.Insert(
    Table: "customer",
    PKField: "customer_id",
    Args: new { first_name = "kenan", last_name = "hancer", email = "kenanhancer@hotmail.com", active = true, store_id = 1, address_id = 5, create_date = DateTime.Now }
);

`Uni.ORM` will generate Delete SQL query according to parameters.
//Delete record which is inserted
var result = sakila.dyno.Delete(Table: "customer", PKField: "customer_id", Args: newID);


/////////////////////////////////////////////////////////////////////////////////////////////////////
//Insert more than one record (BulkInsert)
var insertResult = sakila.dyno.Insert(
    Table: "customer",
    PKField: "customer_id",
    Args: new object[] { 
                new { first_name = "kenan", last_name = "hancer", email = "kenanhancer@hotmail.com", active = true, store_id = 1, address_id = 5, create_date=DateTime.Now },
                new { first_name = "sinan", last_name = "hancer", email = "kenanhancer@hotmail.com", active = true, store_id = 1, address_id = 5, create_date=DateTime.Now },
                new { first_name = "kemal", last_name = "hancer", email = "kenanhancer@hotmail.com", active = true, store_id = 1, address_id = 5, create_date=DateTime.Now }
            }
);

//Delete more than one record
var deleteResult = sakila.dyno.Delete(Table: "customer", PKField: "customer_id", Args: insertResult);
/////////////////////////////////////////////////////////////////////////////////////////////////////

//Update one record
var updateResult = sakila.dyno.Update(
    Table: "customer",
    Columns: "active",
    Args: new { customer_id = 1, active = false }
);

//Update more than one record
var updateResult = sakila.dyno.Update(
    Table: "customer",
    Columns: "active",
    Args: new object[] {
                new { customer_id = 2, active = false },
                new { customer_id = 3, active = false }
            }
);


//BulkInsert
//Below code retriews first 5 rows and updates and then insert again database.
IEnumerable<dynamic> result = sakila.dyno.Query(Table: "customer", Limit: 5, OrderBy: "customer_id"));
result = result.ToList();
result.ForEach(f => f.active = false);

var bulkInsertResult = sakila.dyno.Insert(
    Table: "customer",
    PKField: "customer_id",
    Args: result
);

//BulkUpdate
IEnumerable<dynamic> result = sakila.dyno.Query(Table: "customer", Limit: 5, OrderBy: "customer_id DESC");
result = result.ToList();
result.ForEach(f => f.active = true);

var bulkUpdateResult = sakila.dyno.Update(
    Table: "customer",
    Columns: "active",
    Where: "address_id=@address_id AND store_id=@store_id",
    Args: result
);

var deleteResult = sakila.dyno.Delete(
    Table: "customer",
    PKField: "customer_id",
    Args: result
);
```

##Listeners(Events)
`Uni.ORM` supports some special events as `OnCallback`, `OnCommandPreExecuting`, `OnConvertingResult`, `OnParameterCreating`, `OnPreGeneratingSql`

Let’s say you want to convert Delete result from int to bool. Just use `OnConvertingResult` event. After executing query `OnCallback`, it wıll give you some details such as SqlQuery that is genetated by `Uni.Orm`

```csharp
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
```

You can also set OnConvertingResult and OnCallback parameters directly.
```csharp
Func<SqlEntity, object, object> onConvertingResult = (sqlEntity, result) => result.To<bool>();

Action<Callback> onCallback = callback => Console.WriteLine("SqlQuery is {0}", callback.SqlQuery);

bool result5 = oracle.dyno.Delete(Table: "PRODUCTS",
                                  PKField: "PRODUCTID",
                                  OnConvertingResult: onConvertingResult,
                                  OnCallback: onCallback,
                                  Args: new object[] { 88, 89 });
```


##Transaction
`Uni.ORM` supports transaction based operations. You just need to set Options parameter as below.

```csharp
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
```

You can also set Trasaction parameter directly.

```csharp
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
```

##Config Based Query
Let’s say you need to make a query. But, this `Uni.Orm` query parameters also should be set as a json data. So, you can make query dynamicly. :)

```csharp

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

var options = new Options();
options.EventListener = new Listener
{
    OnCallback = (Callback f) =>
    {
        Console.WriteLine(f.SqlQuery);
    },
    OnParameterCreating = (DbParameter f) =>
    {
    
    }
};
criteria.Options = options;

IEnumerable<dynamic> result = northwindSqlite.dyno.Execute(criteria);

result = result.ToList();
```

##Simple Join, GroupBy and Having
Let’s say you want to use simple join queries. Actually, you can use the capabilities of `Uni.ORM`. 
First of all, we must ask ourselves what join is. It is the equality of specific table columns. 
So, we can set “Table” argument for tables we want to join so later we can set “Where” argument for columns which are equal as the following codes. 
If we use aggregate functions such as “SUM, MAX, MIN, AVG, COUNT” and normal column together such as “sum(amount), first_name” we should set “GroupBy” argument such as “GroupBy: first_name”. 
That is all, this is just an idea and ,it can be developed in different variations.

```csharp
//Three tables are joined. customer and payment tables are joined with customer_id column. Later, payment and staff tables are joined with staff_id column
var result = sakila.dyno.Query(
        Table: "payment as p,customer as c,staff as s", 
        Columns: "p.*, CONCAT(s.first_name, ' ', s.last_name) as Staff_FullName, CONCAT(c.first_name, ' ', c.last_name) as Customer_FullName", 
        Where: "p.customer_id=c.customer_id and s.staff_id=p.staff_id and p.customer_id=?customer_id", 
        customer_id: 1);

//Get Total payment amounts which are bigger than 100 and payment counts according to customers
var result = sakila.dyno.Query(
        Table: "payment p,customer c", 
        Columns: "CONCAT(c.first_name, ' ', c.last_name) as Customer_FullName,SUM(p.amount) TotalPayment,COUNT(p.customer_id) PaymentCount",
        Where: "p.customer_id=c.customer_id",
        GroupBy: "Customer_FullName",
        Having: "SUM(p.amount)>100");
```

##Some example codes
```csharp
//Actually, you can use Uni.ORM in several ways. below four lines of code will return same result.
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: "Adjustable Race");

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@0", Args: new object[] { "Adjustable Race" });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@Name", Args: new { Name = "Adjustable Race" });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Name=@Name", Name: "Adjustable Race");

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Name: "Adjustable Race");


//below three lines of code will return same result.
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@1 and ListPrice=@0", Args: new object[] { 0, "Black" });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@Color and ListPrice=@ListPrice", Args: new { ListPrice = 0, Color = "Black" });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Where: "Color=@Color and ListPrice=@ListPrice", ListPrice: 0, Color: "Black");

var result = aw.dyno.Query(Schema: "Production", Table: "Product", ListPrice: 0, Color: "Black");

//After this method runs, generated query will be below line. So, Uni.ORM have some standart arguments. But, others will be criteria.
//Let's look at below SQL query "Color" and "ListPrice" arguments added as criteria.
//SELECT ProductID,Name,ProductNumber FROM [Production].[Product] WHERE ListPrice=@ListPrice AND Color=@Color ORDER BY ProductID DESC
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", OrderBy: "ProductID DESC", ListPrice: 0, Color: "Black");

//Named Argument Query Syntax
var result = sakila.dyno.Query(Table: "customer", Active: true);
var result = sakila.dyno.Query(Table: "customer", Where: "Active=?Active", Active: true);

//Get total payment amount of customer who has value 1 of customer_id
var result = sakila.dyno.Sum(Table: "payment", Columns: "amount", customer_id: 1);

//Get FullName of customers
var result = sakila.dyno.Query<string>(Table: "customer", Columns: "CONCAT(first_name, ' ', last_name) as FullName");

//Get tables of database
var tables = sakila.GetTables();

//Get columns of table
var tableColumns = sakila.GetColumns("customer");

//Below 5 rows generate same result
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "Color in @Color and Size in @Size", Args: new { Color = new[] { "Black", "Yellow", "Red" }, Size = new[] { "38", "40", "42" } });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "Color in @Color", Args: new { Color = new[] { "Black", "Yellow", "Red" }, Size = new[] { "38", "40", "42" } });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Args: new { Color = new[] { "Black", "Yellow", "Red" }, Size = new[] { "38", "40", "42" } });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "Color in @Color and Size in @Size", Color = new[] { "Black", "Yellow", "Red" }, Args: new { Size = new[] { "38", "40", "42" } });

var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "Size in @Size", Color = new[] { "Black", "Yellow", "Red" }, Args: new { Size = new[] { "38", "40", "42" } });

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
```
