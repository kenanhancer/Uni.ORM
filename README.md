`Uni.Orm` is the continuation of `Uni` project. Project name is just changed.

##How To Install It?
Drop `UniOrm.cs` and `UniExtensions.cs` C#.NET code files into your project and change it as you wish or you can install from `NuGet Galery`;

If you want to install from `Nuget`, you should write Package Manager Console below code and `Uni.ORM` will be installed automatically.
```
Install-Package Uni.ORM
```
By the way, you can also reach `Uni.ORM` `NuGet` package from https://www.nuget.org/packages/Uni.ORM/ address.

##How Do You Use It?
Let's say that you installed database connectors in your machine.
Your project doesn't need any DLL in references. `Uni.ORM` will find DLL which is necessary from the GAC.

Let's have a look at config file for ConnectionStrings which will be used by `Uni.ORM`.
```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <connectionStrings>
    <add name="AdventureWorks" connectionString="Data Source=localhost;Initial Catalog=AdventureWorks2012;Integrated Security=True" providerName="System.Data.SqlClient"/>
    <add name="HR" connectionString="DATA SOURCE=localhost;PASSWORD=1;PERSIST SECURITY INFO=True;USER ID=HR" providerName="Oracle.DataAccess.Client"/>
    <add name="Sakila" connectionString="server=localhost;Uid=root;Pwd=1;database=sakila;Allow User Variables=true;" providerName="MySql.Data.MySqlClient"/>
  </connectionStrings>
</configuration>
```
`Uni.ORM` can understand database that you want to use with providerName attribute in connectionString.
Let's say we want to use Oracle database. We should set providerName as "Oracle.DataAccess.Client" as shown in config code.

We can create UniOrm object now.
```csharp
var aw = new UniOrm("AdventureWorks");//Microsoft SQL Server
var hr = new UniOrm("HR");//Oracle
var sakila = new UniOrm("Sakila");//MySQL
```

If you don't want to use config file, you can create UniOrm object with connectionString directly.
```csharp
var aw = new UniOrm(@"Data Source=localhost;Initial Catalog=AdventureWorks2012;Integrated Security=True", DatabaseType.SQLServer);//Microsoft SQL Server
var hr = new UniOrm(@"DATA SOURCE=localhost;PASSWORD=1;PERSIST SECURITY INFO=True;USER ID=HR", DatabaseType.Oracle);//Oracle
var sakila = new UniOrm(@"server=localhost;Uid=root;Pwd=1;database=sakila;Allow User Variables=true;", DatabaseType.MySQL);//MySQL
```

##How To Execute a Query?
Let's say we want to Query "Product" table. So, You just need to instantiate it inline.
```csharp
//returns all the products
var result = aw.dyno.Query(Schema: "Production", Table: "Product");
```
Actually, after you write "aw.dyno" and click the point button, you will not see intellisense. Because, Methods and arguments after "aw.dyno" code
are on the fly. But, `Uni.ORM` is smart and dynamic. So, it will generate and execute query according to your method and parameters.

```csharp
//if you want to use dynamic advantages, you should use dynamic. 
//But, if you use like that, you will lose intellisense. So, you will not use other static methods of Uni.ORM.
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
var result = aw.dyno.Query(Sql: "SELECT * FROM [Production].[Product]");
```

##Dynamic object and strongly typed result
Let's say you want to use `POCO` model, you can set your `POCO` type as generic in method. So, `Uni.ORM` will return strongly typed result.
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
var result = sakila.dyno.Query<customer>(Table: "customer");

//Execute and return dynamic object result
var result = sakila.dyno.Query(Table: "customer");
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
we can also take last row in same way. Just change "OrderBy" from "ASC" to "DESC" like below
```csharp
var result = aw.dyno.Query(Schema: "Production", Table: "Product", Columns: "ProductID,Name,ProductNumber", Where: "ListPrice=@ListPrice and Color in @Color", OrderBy: "ProductID DESC", Limit: 1, ListPrice: 0, Color: new[] { "Red", "Black" });
```

##PAGING
Let's say you need paging in you application. You just use query method with extra arguments which are "PageSize" and "PageNo" like below;
```csharp
//First page 10 record
var sakilaResult4 = sakila.dyno.Query(Table: "customer", OrderBy: "address_id", PageSize: 10, PageNo: 1);
//Secodn page 10 record
var sakilaResult5 = sakila.dyno.Query(Table: "customer", OrderBy: "address_id", PageSize: 10, PageNo: 2);
```

##Aggregate operations
You can also use aggregates. Actually, logic is same. So, you just change Method name and `Uni.ORM` will do his job.
You should't forget to set "Columns" argument for "Sum", "Max", "Min", "Avg" aggregates.
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

##Stored Procedure and Function
Let's say that you want to execute your stored procedure or function. Just use "SP" or "FN" arguments.
If your stored procedure or function returns back output parameter, you should also use "Listener" argument so that after method is executed, you can retrieve output parameters and sql statement which is generated by `Uni.ORM`

If you need some complex stored procedure usage. `Uni.ORM` presents Listener. You can listen some events which are `OnCallback` and `OnParameterCreating`. There will be other events in the fiture.

`OnCallback` occurs when the result is generated.

`OnParameterCreating` occurs before the parameter is created. So, you can change parameter direction, dataType etc.

```csharp
//Simple usage of Stored procedure
var result = aw.dyno.Query(Schema: "Person", Sp: "GetPersonList");

//Stored Procedure and CallBack(You can take output parameter of SP)

var args = new
{
    UserID = (decimal)0,
    UserName = "Kenan",
    Password = 188231,
    Created = DateTime.Now,
    Updated = DateTime.Now,
    IsDeleted = false
};

var listener = new Listener
{
    OnCallback = (Callback f) =>
      {
          decimal UserID = f.OutputParameters[0].UserID;
          string sql = f.SqlQuery;
      },
    OnParameterCreating = (DbParameter f) =>
     {
         if (f.ParameterName == "UserID")
             f.Direction = ParameterDirection.InputOutput;

         return f;
     }
};

hr.dyno.NonQuery(Schema: "User", Package: "User_Package", Sp: "UserSP", Listeners: listener, Args: args);
```

```csharp
//Simple usage of Function
var result = crm.dyno.Query(Schema: "User", FN: "fn_GetUserByUserID", Args: new object[] { 64, 1 });
```
##Insert, Update, Delete, BulkInsert and BulkUpdate Operations

```csharp
//Insert one record
var newID = sakila.dyno.Insert(
    Table: "customer",
    PKField: "customer_id",
    Args: new { first_name = "kenan", last_name = "hancer", email = "kenanhancer@hotmail.com", active = true, store_id = 1, address_id = 5, create_date = DateTime.Now }
);

//Delete record which is inserted
var result = sakila.dyno.Delete(Table: "customer", PKField: "customer_id", Args: newID);


/////////////////////////////////////////////////////////////////////////////////////////////////////
//Insert more than one record
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
    PKField: "customer_id",
    Args: new { customer_id = 1, active = false }
);

//Update more than one record
var updateResult = sakila.dyno.Update(
    Table: "customer",
    PKField: "customer_id",
    Args: new object[] {
                new { customer_id = 2, active = false },
                new { customer_id = 3, active = false }
            }
);


//BulkInsert
//Below code retriews first 5 rows and updates and then insert again database.
var result = ((IEnumerable<dynamic>)sakila.dyno.Query(Table: "customer", Limit: 5, OrderBy: "customer_id")).ToList();
result.ForEach(f => f.active = false);

var bulkInsertResult = sakila.dyno.BulkInsert(
    Table: "customer",
    PKField: "customer_id",
    Args: result.ToArray()
);

//BulkUpdate
var result = ((IEnumerable<dynamic>)sakila.dyno.Query(Table: "customer", Limit: 5, OrderBy: "customer_id DESC")).ToList();
result.ForEach(f => f.active = true);

var bulkUpdateResult = sakila.dyno.BulkUpdate(
    Table: "customer",
    PKField: "customer_id",
    Args: result.ToArray()
);

var deleteResult = sakila.dyno.Delete(
    Table: "customer",
    PKField: "customer_id",
    Args: result.ToArray()
);
```

##Simple Join, GroupBy and Having
Let’s say you want to use simple join queries. Actually, you can use capabilities of `Uni.ORM`. 
First of all, we must ask ourselves that what is join. it is equality of specific table columns. 
So, we can set “Table” argument as tables which we want to join and later we can set “Where” argument as columns which are equal such as following code. 
If we use aggregate functions such as “SUM, MAX, MIN, AVG, COUNT” and normal column together such as “sum(amount), first_name” so, we should set “GroupBy” argument such as “GroupBy: first_name”. 
That is all, this is just idea and you can develop different variations.

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
```
