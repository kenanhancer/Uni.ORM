using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using Uni.Extensions;
namespace Uni.Orm
{
    #region Entities
    public enum DatabaseType { SQLServer, SqlServerCE, MySQL, Oracle, SQLite, PostgreSQL, OleDB }
    /// <summary>
    /// Events of UniOrm
    /// </summary>
    public class Listener
    {
        /// <summary>
        /// Occurs before the parameter is created.
        /// </summary>
        public Action<DbParameter> OnParameterCreating { get; set; }
        public Action<DbCommand> OnCommandPreExecuting { get; set; }
        public Action<SqlEntity> OnPreGeneratingSql { get; set; }
        public Func<SqlEntity, object, object> OnConvertingResult { get; set; }
        /// <summary>
        /// Occurs when the result is generated.
        /// </summary>
        public Action<Callback> OnCallback { get; set; }
    }
    public class Options
    {
        public bool FieldNameLower { get; set; }
        public Listener EventListener { get; set; }
        public DbTransaction Transaction { get; set; }
        public Options()
        {
            EventListener = new Listener();
        }
    }
    public class Callback
    {
        public string SqlQuery { get; set; }
        public dynamic OutputParameters { get; set; }
    }
    public class SqlEntity
    {
        public string Binder { get; set; }
        public string Package { get; set; }
        public string Schema { get; set; }
        public string Table { get; set; }
        public string Columns { get; set; }
        public string Where { get; set; }
        public string OrderBy { get; set; }
        public string GroupBy { get; set; }
        public string Having { get; set; }
        public string Fn { get; set; }
        public string Sp { get; set; }
        public string Sql { get; set; }
        public string PKField { get; set; }
        public string Sequence { get; set; }
        public string RowNumberColumn { get; set; }
        //public string GeneratedSql { get; set; }
        public long Limit { get; set; }
        public long PageSize { get; set; }
        public long PageNo { get; set; }
        public List<ArgumentEntity> NamedArguments { get; set; }
        public List<object> ArrayArguments { get; set; }
        public SqlEntity()
        {
            Binder = "";
            Package = "";
            Schema = "";
            Table = "";
            Columns = "*";
            Where = "";
            OrderBy = "";
            GroupBy = "";
            Having = "";
            Fn = "";
            Sp = "";
            Sql = "";
            PKField = "";
            RowNumberColumn = "";
            NamedArguments = new List<ArgumentEntity>();
            ArrayArguments = new List<object>();
        }
    }
    public class ArgumentEntity
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }
    #endregion Entities
    public static class UniOrmExtensions
    {
        public static string ToParameterString(this object obj, string parameterPrefix, string PKField = "", string parameterSuffix = "")
        {
            return string.Join(",", obj.ToParameters(parameterPrefix, PKField, parameterSuffix).Keys.Where(f => f.TrimStart(parameterPrefix.ToCharArray()) != string.Format("{0}{1}", PKField, parameterSuffix)));
        }
        public static string ToColumnString(this object obj, string PKField = "")
        {
            return string.Join(",", obj.ToDictionary<Attribute>().Keys.Where(f => f != PKField));
        }
        public static string ToColumnParameterString(this object obj, string parameterPrefix, string PKField = "", string parameterSuffix = "", string seperator = ",", string[] exceptFields = null)
        {
            if (obj == null) return null;
            var columnParameterList = new List<string>();
            var objDict = obj.ToDictionary<Attribute>();
            exceptFields = exceptFields == null ? new string[] { } : exceptFields;
            if (objDict == null && !string.IsNullOrEmpty(PKField))
                columnParameterList.Add(string.Format("{0}={1}{0}{2}", PKField, parameterPrefix, parameterSuffix));
            else if (objDict != null && objDict.Count > 0)
                foreach (var item in objDict.Keys.Where(f => !exceptFields.Contains(f)))
                    columnParameterList.Add(string.Format("{0}={1}{0}{2}", item, parameterPrefix, parameterSuffix));
            else
            {
                var objCollection = obj as ICollection;
                if (objCollection != null)
                {
                    var objEnumerable = objCollection.Cast<object>();
                    for (int i = 0; i < objCollection.Count; i++)
                        columnParameterList.Add(string.Format("{0}={1}{0}{2}", objEnumerable.ElementAtOrDefault(i), parameterPrefix, i));
                }
            }
            return string.Join(seperator, columnParameterList);
        }
        public static Dictionary<string, object> ToParameters(this object obj, string parameterPrefix, string PKField = "", string parameterSuffix = "")
        {
            var retValue = new Dictionary<string, object>();
            if (obj == null) return retValue;
            Type objType = obj.GetType();

            bool isObjExpando = false;
            IEnumerator objEnumerator;
            IEnumerator valueEnumerator;
            ICollection objCol = obj as ICollection;
            IDictionary<string, object> objDict = null;
            if (objCol == null)
            {
                objDict = obj.ToExpando<Attribute>() as IDictionary<string, object>;
                objCol = objDict.ToArray();
                isObjExpando = (objDict is ExpandoObject);
            }
            objEnumerator = objCol.GetEnumerator();
            if (objType.IsPrimitive || objType == UniExtensions.stringType)
                retValue.Add(string.Format("{0}{1}", parameterPrefix, "0"), obj);
            else if (objCol != null && objCol.Count > 0)
            {
                Type firstItemType = null;
                for (int x = 0; x < objCol.Count; x++)
                {
                    objEnumerator.MoveNext();
                    object objValue = objEnumerator.Current;
                    if (x == 0)
                        firstItemType = objValue.GetType();
                    if (firstItemType.IsPrimitive || firstItemType == UniExtensions.stringType)
                        retValue.Add(string.Format("{0}{1}{2}", parameterPrefix, PKField, (!string.IsNullOrEmpty(PKField) && objCol.Count == 1) ? "" : x.ToString()), objValue);
                    else
                    {
                        ICollection valueCol = objValue as ICollection;
                        IDictionary<string, object> valueDict = null;
                        if (valueCol == null)
                        {
                            valueDict = objValue.ToExpando<Attribute>() as IDictionary<string, object>;
                            valueCol = valueDict.ToArray();
                        }


                        if (isObjExpando || valueDict.ContainsKey("Key"))
                        {
                            var valueDictItemKey = valueDict.ElementAt(0).Value.ToString();
                            var valueDictItemValue = valueDict.ElementAt(1).Value;
                            var valueItemAsCol = valueDictItemValue as ICollection;
                            if (valueItemAsCol != null)
                                foreach (var item in valueDictItemValue.ToParameters(parameterPrefix + valueDictItemKey, ""))
                                    retValue.Add(item.Key, item.Value);
                            else
                            {
                                if (valueDictItemKey == PKField) continue;
                                retValue.Add(string.Format("{0}{1}{2}", parameterPrefix, valueDictItemKey, parameterSuffix), valueDictItemValue);
                            }
                        }
                        else
                        {
                            valueEnumerator = valueCol.GetEnumerator();
                            bool isValueDict = (valueDict != null);
                            for (int y = 0; y < valueCol.Count; y++)
                            {
                                if (isValueDict)
                                {
                                    var valueDictItem = valueDict.ElementAt(y);
                                    if (valueDictItem.Key == PKField) continue;
                                    retValue.Add(string.Format("{0}{1}{2}", parameterPrefix, valueDictItem.Key, x), valueDictItem.Value);
                                }
                                else
                                {
                                    if (!valueEnumerator.MoveNext() || valueEnumerator.Current == PKField) continue;
                                    retValue.Add(string.Format("{0}{1}{2}", parameterPrefix, valueEnumerator.Current, x), valueEnumerator.Current);
                                }
                            }
                        }
                    }
                }
            }

            return retValue;
        }
        public static List<T> ToDbParameters<T>(this object obj, Func<T, T> onParameterCreating = null) where T : DbParameter
        {
            List<T> retValue = new List<T>();
            IDictionary<string, object> objDict = obj.ToDictionary<Attribute>();
            foreach (var item in objDict)
            {
                T parameter = New<T>.Instance();
                parameter.ParameterName = item.Key;
                parameter.Value = (item.Value ?? DBNull.Value);

                if (onParameterCreating != null)
                    parameter = onParameterCreating(parameter);
                retValue.Add(parameter);
            }
            return retValue;
        }
    }
    public class UniOrm : DynamicObject
    {
        #region Field Members
        Type csharpBinder;
        Type uniOrmType = typeof(UniOrm);
        PropertyInfo csharpBinderTypeArguments;
        DbProviderFactory _dbProviderFactory;
        ConnectionStringSettings conStrSettings;
        DatabaseType dbType;
        string parameterPrefix = "";
        string parameterFormat = "{0}{1}";
        string commandFormat = "{0}.{1}";
        string defaultSchema = "";
        string password = "";
        #endregion Field Members
        #region Properties
        public DatabaseType DbType { get { return dbType; } }
        public string ParameterPrefix { get { return parameterPrefix; } }
        public dynamic dyno { get { return this; } }
        public Options Options { get; private set; }
        #endregion Properties
        #region Constructors
        public UniOrm(string connectionStringName, DbProviderFactory dbProviderFactory = null, Options options = null)
        {
            SetBaseProperties(connectionStringName: connectionStringName, dbProviderFactory: dbProviderFactory, options: options);
        }
        public UniOrm(string connectionString, DatabaseType dbType, DbProviderFactory dbProviderFactory = null, Options options = null)
        {
            SetBaseProperties(connectionString: connectionString, dbType: dbType, dbProviderFactory: dbProviderFactory, options: options);
        }
        #endregion Constructors
        #region ORM Operations
        public static DatabaseType? GetDatabaseType(string connectionStringName = "")
        {
            DatabaseType? retValue = null;
            if (!string.IsNullOrEmpty(connectionStringName))
            {
                var conStrSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
                if (conStrSettings == null || string.IsNullOrEmpty(conStrSettings.ConnectionString)) return retValue;
                string providerNameLower = conStrSettings.ProviderName.ToLower();
                if (providerNameLower.Contains("sqlclient"))
                    retValue = DatabaseType.SQLServer;
                else if (providerNameLower.Contains("mysql"))
                    retValue = DatabaseType.MySQL;
                else if (providerNameLower.Contains("oracle"))
                    retValue = DatabaseType.Oracle;
                else if (providerNameLower.Contains("sqlite"))
                    retValue = DatabaseType.SQLite;
                else if (providerNameLower.Contains("npgsql"))
                    retValue = DatabaseType.PostgreSQL;
                else if (providerNameLower.Contains("sqlserverce"))
                    retValue = DatabaseType.SqlServerCE;
                else if (providerNameLower.Contains("oledb"))
                    retValue = DatabaseType.OleDB;
            }
            return retValue;
        }
        private void SetBaseProperties(string connectionStringName = "", string connectionString = "", DatabaseType? dbType = null, DbProviderFactory dbProviderFactory = null, Options options = null)
        {
            var databaseType = dbType ?? GetDatabaseType(connectionStringName);
            if (databaseType.HasValue)
                this.dbType = databaseType.Value;
            else
                throw new Exception("ConnectionString does not exists");
            string providerName = null;
            if (!string.IsNullOrEmpty(connectionStringName))
                providerName = ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName;
            if (this.dbType == DatabaseType.SQLServer)
            {
                providerName = providerName ?? "System.Data.SqlClient";
                parameterPrefix = "@";
                defaultSchema = "dbo";
                //commandFormat = "[{0}].[{1}]";
            }
            else if (this.dbType == DatabaseType.MySQL)
            {
                providerName = providerName ?? "MySql.Data.MySqlClient";
                parameterPrefix = "?";
            }
            else if (this.dbType == DatabaseType.Oracle)
            {
                providerName = providerName ?? "Oracle.DataAccess.Client";
                parameterPrefix = ":";
            }
            else if (this.dbType == DatabaseType.SQLite)
            {
                providerName = providerName ?? "System.Data.Sqlite";
                parameterPrefix = "@";
                defaultSchema = "dbo";
            }
            else if (this.dbType == DatabaseType.PostgreSQL)
            {
                providerName = providerName ?? "Npgsql";
                parameterPrefix = ":";
                //commandFormat = "\"{0}\".\"{1}\"";
            }
            else if (this.dbType == DatabaseType.SqlServerCE)
            {
                providerName = providerName ?? "System.Data.SqlServerCe.4.0";
            }
            if (string.IsNullOrEmpty(connectionStringName))
                conStrSettings = new ConnectionStringSettings(name: "NewConnection", connectionString: connectionString, providerName: providerName);
            else
                conStrSettings = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (dbProviderFactory == null)
                _dbProviderFactory = DbProviderFactories.GetFactory(providerName);
            else
                _dbProviderFactory = dbProviderFactory;
            Options = options == null ? new Options() : options;
        }
        public virtual DbConnection NewConnection()
        {
            var con = _dbProviderFactory.CreateConnection();
            con.ConnectionString = conStrSettings.ConnectionString;
            con.Open();
            return con;
        }
        public virtual DbCommand NewCommand(CommandType commandType, string schema, string package, string commandText, DbConnection con, Options options, params object[] args)
        {
            options = options ?? Options;
            var com = _dbProviderFactory.CreateCommand();
            com.Connection = con;
            com.CommandType = commandType;
            com.CommandTimeout = 180;
            if (commandType == CommandType.TableDirect)
            {
                var tableName = string.Format(commandFormat, schema, commandText).Trim('.');
                com.CommandText = string.Format("SELECT * FROM {0}", tableName);
            }
            else
            {
                if (commandType == CommandType.StoredProcedure)
                    com.CommandText = string.Format("{0}.{1}.{2}", schema, package, commandText).Trim('.');
                else if (commandType == CommandType.Text)
                    com.CommandText = commandText;
                if (args != null && args.Length == 1)
                {
                    var argDict = new Dictionary<string, object>();
                    var arg = args[0];
                    var argType = arg.GetType();
                    if (argType.IsPrimitive)
                        argDict = args.ToParameters(parameterPrefix);
                    else if (argType != typeof(Dictionary<string, object>))
                        argDict = arg.ToParameters(parameterPrefix);
                    else
                        argDict = arg as Dictionary<string, object>;
                    if (argDict != null && argDict.Count > 0)
                    {
                        string[] commandTextParameterArray = null;
                        if (commandType == CommandType.Text && !string.IsNullOrEmpty(commandText))
                        {
                            Regex parameterRegex = new Regex(string.Format("(?<parameter>{0}[^,;) ]+)", parameterPrefix == "?" ? @"\?" : parameterPrefix));
                            MatchCollection parameterMatchCollection = parameterRegex.Matches(commandText);
                            commandTextParameterArray = parameterMatchCollection.Cast<Match>().Select(f => f.Groups["parameter"].Value).Distinct().ToArray();
                            if (commandTextParameterArray.Length > 0)
                                foreach (var prm in commandTextParameterArray)
                                {
                                    object prmValue;
                                    if (argDict.TryGetValue(prm, out prmValue))
                                        com.Parameters.Add(NewParameter(com, prm, prmValue, options: options));
                                }
                        }
                        if (commandTextParameterArray == null || commandTextParameterArray.Length == 0)
                            if (argDict.Count > 0)
                                foreach (var item in argDict)
                                    com.Parameters.Add(NewParameter(com, item.Key, item.Value, options: options));
                            else if (args.Length > 0)
                                for (int i = 0; i < args.Length; i++)
                                    com.Parameters.Add(NewParameter(com, string.Format(parameterFormat, parameterPrefix, i), args[i], options: options));
                    }
                }
            }
            if (options.EventListener.OnCommandPreExecuting != null)
                options.EventListener.OnCommandPreExecuting(com);
            return com;
        }
        public virtual DbParameter NewParameter(DbCommand com, string parameterName, object value, DbType dbType = System.Data.DbType.Object, ParameterDirection parameterDirection = ParameterDirection.Input, Options options = null)
        {
            DbParameter parameter = com.CreateParameter();
            parameter.ParameterName = parameterName.Replace(parameterPrefix, "");
            parameter.Value = (value ?? DBNull.Value);
            parameter.Direction = parameterDirection;
            if (parameterDirection == ParameterDirection.Output)//It will be also tested for input type
                parameter.DbType = dbType;
            options = options ?? Options;
            if (options.EventListener.OnParameterCreating != null)
                options.EventListener.OnParameterCreating(parameter);
            return parameter;
        }
        public virtual dynamic NewExpando(string commandText, string schema = "")
        {
            IDictionary<string, object> result = new ExpandoObject();
            var columns = GetColumns(commandText, schema);
            foreach (dynamic column in columns)
                result.Add(column.COLUMN_NAME, null);
            return result;
        }
        public virtual DbCommand CloneCommand(DbCommand com)
        {
            var newCom = _dbProviderFactory.CreateCommand();
            newCom.CommandType = com.CommandType;
            newCom.CommandTimeout = com.CommandTimeout;
            newCom.CommandText = com.CommandText;
            foreach (DbParameter prm in com.Parameters)
                newCom.Parameters.Add(NewParameter(com: com, parameterName: prm.ParameterName, value: prm.Value, parameterDirection: prm.Direction));
            return newCom;
        }
        public virtual dynamic GetOutputParameters(DbCommand com)
        {
            dynamic retValue = new ExpandoObject();
            var retValueDict = retValue as IDictionary<string, object>;
            foreach (DbParameter parameter in com.Parameters)
                if (parameter.Direction == ParameterDirection.Output || parameter.Direction == ParameterDirection.InputOutput || parameter.Direction == ParameterDirection.ReturnValue)
                    retValueDict[parameter.ParameterName] = parameter.Value;
            return retValue;
        }
        #endregion ORM Operations
        #region Database Helper Operations
        public virtual IEnumerable<DbParameter> GetCommandParameters(DbCommand com, Options options, params object[] args)
        {
            var argDict = args[0] as Dictionary<string, object>;
            if (argDict == null)
                argDict = args[0].ToParameters(parameterPrefix);
            if (argDict != null)
                foreach (var item in argDict)
                    yield return NewParameter(com: com, parameterName: item.Key, value: item.Value, options: options);
        }
        public virtual IEnumerable<dynamic> GetCommandParameters(string commandText, string schema = "", string package = "")
        {
            IEnumerable<dynamic> retValue = null;
            using (var con = NewConnection())
            {
                var sql = "";
                if (this.dbType == DatabaseType.SQLServer || this.dbType == DatabaseType.MySQL)
                {
                    schema = (schema ?? defaultSchema);
                    sql = string.Format("SELECT PARAMETER_MODE,PARAMETER_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH DATA_LENGTH FROM INFORMATION_SCHEMA.PARAMETERS WHERE {0}", string.IsNullOrEmpty(schema) ? string.Format("SPECIFIC_NAME={0}0", parameterPrefix) : string.Format("SPECIFIC_NAME={0}0 AND SPECIFIC_SCHEMA={0}1", parameterPrefix));
                }
                else if (this.dbType == DatabaseType.Oracle)
                {
                    var criteria = string.IsNullOrEmpty(schema) ? "PROCEDURE_NAME=:0" : "PROCEDURE_NAME=:0 AND OWNER=:1";
                    sql = string.Format("SELECT A.ARGUMENT_NAME PARAMETER_NAME, A.IN_OUT PARAMETER_MODE, A.DATA_TYPE, A.DATA_LENGTH FROM SYS.ALL_ARGUMENTS A, (SELECT * FROM (SELECT * FROM SYS.ALL_PROCEDURES WHERE {0} ORDER BY SUBPROGRAM_ID DESC) WHERE ROWNUM=1) P WHERE P.OBJECT_ID=A.OBJECT_ID AND P.SUBPROGRAM_ID=A.SUBPROGRAM_ID {1}", criteria, string.IsNullOrEmpty(package) ? "" : "AND A.PACKAGE_NAME=:2");
                }
                //else if (this.dbType == DatabaseType.SQLite)
                //else if (this.dbType == DatabaseType.PostgreSQL)
                if (string.IsNullOrEmpty(schema))
                    retValue = Query(commandText: sql, args: new object[] { commandText.ToUpperInvariant() }.ToParameters(parameterPrefix));
                else
                    retValue = Query(commandText: sql, args: new object[] { commandText.ToUpperInvariant(), schema.ToUpperInvariant(), package.ToUpperInvariant() }.ToParameters(parameterPrefix));
            }
            return retValue;
        }
        public virtual IEnumerable<dynamic> GetTables()
        {
            IEnumerable<dynamic> retValue = null;
            using (var con = NewConnection())
            {
                if (this.dbType == DatabaseType.SQLServer)
                    retValue = Query(commandText: "SELECT * FROM [sys].[all_objects] WHERE type_desc LIKE '%table%'");
                else if (this.dbType == DatabaseType.MySQL)
                    retValue = Query(commandText: "SELECT * FROM INFORMATION_SCHEMA.TABLES");
                else if (this.dbType == DatabaseType.Oracle)
                    retValue = Query(commandText: "SELECT * FROM ALL_TABLES");
                else if (this.dbType == DatabaseType.SQLite)
                    retValue = Query(commandText: "SELECT * FROM sqlite_master WHERE type='table'");
                //else if (this.dbType == DatabaseType.PostgreSQL)
            }
            return retValue;
        }
        public virtual IEnumerable<dynamic> GetColumns(string commandText, string schema = "")
        {
            IEnumerable<dynamic> retValue = null;
            using (var con = NewConnection())
            {
                var sql = "";
                if (this.dbType == DatabaseType.SQLServer)
                {
                    sql = string.Format("SELECT r2.name COLUMN_NAME FROM [sys].[all_objects] r1, [sys].[all_columns] r2 WHERE r1.object_id=r2.object_id AND schema_name(r1.schema_id) like {0}0 AND r1.name={0}1", parameterPrefix);
                    schema = (schema ?? defaultSchema);
                    retValue = Query(commandText: sql, args: new object[] { schema, commandText }.ToParameters(parameterPrefix));
                }
                else if (this.dbType == DatabaseType.MySQL)
                {
                    sql = string.Format("SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = {0}0", parameterPrefix);
                    retValue = Query(commandText: sql, args: new object[] { commandText }.ToParameters(parameterPrefix));
                }
                else if (this.dbType == DatabaseType.Oracle)
                {
                    sql = string.Format("SELECT * FROM ALL_TAB_COLUMNS WHERE TABLE_NAME = UPPER({0}0)", parameterPrefix);
                    retValue = Query(commandText: sql, args: new object[] { commandText }.ToParameters(parameterPrefix));
                }
                else if (this.dbType == DatabaseType.SQLite)
                    retValue = Query(commandText: string.Format("PRAGMA table_info( {0} )", commandText));
                //else if (this.dbType == DatabaseType.PostgreSQL)
            }
            return retValue;
        }
        public virtual void ChangePassword(string password)
        {
            this.password = password;
            using (dynamic con = NewConnection())
                if (dbType == DatabaseType.SQLite && !string.IsNullOrEmpty(password))
                    con.ChangePassword(password);
        }
        #endregion Database Helper Operations
        #region Database Operations
        private object CallQueryReflection(Type type, CommandType commandType = CommandType.Text, string schema = "", string package = "", string commandText = "", Options options = null, params object[] args)
        {
            MethodInfo queryMethodInfo = uniOrmType.MakeGenericMethod("Query", type);
            return queryMethodInfo.Invoke(this, new object[] { commandType, schema, package, commandText, options, args });
        }
        public virtual IEnumerable<dynamic> MultipleQuery(CommandType commandType = CommandType.Text, string schema = "", string package = "", string commandText = "", Options options = null, params object[] args)
        {
            options = options ?? Options;
            var retValue = new List<dynamic>();
            using (var con = NewConnection())
            {
                var com = NewCommand(commandType: commandType, schema: schema, package: package, commandText: commandText, con: con, options: options, args: args);
                var reader = com.ExecuteReader();
                int i = 0;
                do
                {
                    retValue.Add(ResultSet(i++, com));
                } while (reader.NextResult());
                if (options.EventListener.OnCallback != null)
                    options.EventListener.OnCallback(new Callback { SqlQuery = com.CommandText, OutputParameters = GetOutputParameters(com) });
            }
            if (retValue.Count == 1)
                return retValue[0];
            else
                return retValue;
        }
        public virtual IEnumerable<dynamic> Query(CommandType commandType = CommandType.Text, string schema = "", string package = "", string commandText = "", Options options = null, params object[] args)
        {
            options = options ?? Options;
            List<dynamic> result = new List<dynamic>();
            using (var con = NewConnection())
            {
                var com = NewCommand(commandType: commandType, schema: schema, package: package, commandText: commandText, con: con, options: options, args: args);
                var reader = com.ExecuteReader();
                //dynamic dynamicRow = new ExpandoObject();
                //string propName;
                //object value = null;
                //while (reader.Read())
                //{
                //    dynamicRow = new ExpandoObject();
                //    var dynamicRowDict = dynamicRow as IDictionary<string, object>;
                //    for (int x = 0; x < reader.FieldCount; x++)
                //    {
                //        propName = reader.GetName(x);
                //        value = reader[x];
                //        dynamicRowDict.Add(propName, DBNull.Value.Equals(value) ? null : value);
                //    }

                //    result.Add(dynamicRow);
                //    //yield return reader.ToExpando<Attribute>();
                //}

                var expandoObjectMap = reader.ToExpandoObjectMapMethod();
                while (reader.Read())
                    result.Add(expandoObjectMap(reader));
                    //yield return expandoObjectMap(reader);

                if (options.EventListener.OnCallback != null)
                    options.EventListener.OnCallback(new Callback { SqlQuery = com.CommandText, OutputParameters = GetOutputParameters(com) });
            }
            return result;
        }
        public virtual IEnumerable<T> Query<T>(CommandType commandType = CommandType.Text, string schema = "", string package = "", string commandText = "", Options options = null, params object[] args) where T : new()
        {
            options = options ?? Options;
            List<T> retVal = new List<T>();
            DbCommand com = null;
            DbDataReader reader = null;
            using (var con = NewConnection())
            {
                com = NewCommand(commandType: commandType, schema: schema, package: package, commandText: commandText, con: con, options: options, args: args);
                reader = com.ExecuteReader();
                //First Way
                var entityMap = reader.ToEntityMapMethod<T>();
                while (reader.Read())
                {
                    var a1 = reader[0];
                    //yield return entityMap(reader);
                    retVal.Add(entityMap(reader));
                }
                //Second Way
                //retVal = reader.ToEntityListMapMethod<T>()(reader);
                if (options.EventListener.OnCallback != null)
                    options.EventListener.OnCallback.Invoke(new Callback { SqlQuery = com.CommandText, OutputParameters = GetOutputParameters(com) });
            }
            return retVal;
        }
        private IEnumerable<dynamic> ResultSet(int index, DbCommand com)
        {
            //List<dynamic> result = new List<dynamic>();
            using (var con = NewConnection())
            {
                var newCom = CloneCommand(com);
                newCom.Connection = con;
                var reader = newCom.ExecuteReader();
                int i = 0;
                do
                {
                    if (i++ == index)
                        while (reader.Read())
                        {
                            //result.Add(reader.ToExpando<Attribute>());
                            yield return reader.ToExpando<Attribute>();
                        }
                } while (reader.NextResult());
            }
            //return result;
        }
        public virtual object ExecuteScalar(CommandType commandType = CommandType.Text, string schema = "", string package = "", string commandText = "", Options options = null, params object[] args)
        {
            options = options ?? Options;
            var retValue = default(object);
            //using (var con = NewConnection())
            var con = options.Transaction != null ? options.Transaction.Connection : NewConnection();
            {
                var com = NewCommand(commandType: commandType, schema: schema, package: package, commandText: commandText, options: options, con: con, args: args);
                if (options.Transaction != null)
                    com.Transaction = options.Transaction;
                retValue = com.ExecuteScalar();
                if (options.EventListener.OnCallback != null)
                    options.EventListener.OnCallback(new Callback { SqlQuery = com.CommandText, OutputParameters = GetOutputParameters(com) });
            }
            return retValue;
        }
        public virtual T ExecuteScalar<T>(CommandType commandType = CommandType.Text, string schema = "", string package = "", string commandText = "", Options options = null, params object[] args)
        {
            options = options ?? Options;
            var retValue = ExecuteScalar(commandType: commandType, schema: schema, package: package, commandText: commandText, options: options, args: args);
            return retValue.To<T, Attribute>(fieldNameLower: options.FieldNameLower);
        }
        public virtual object[] ExecuteScalar(Options options = null, params DbCommand[] commands)
        {
            var retValue = new List<object>();
            //using (var con = NewConnection())
            var con = options.Transaction != null ? options.Transaction.Connection : NewConnection();
            {
                //using (var dbTransaction = con.BeginTransaction())
                var dbTransaction = options.Transaction ?? con.BeginTransaction();
                {
                    foreach (var cmd in commands)
                    {
                        cmd.Connection = con;
                        cmd.Transaction = dbTransaction;
                        retValue.Add(cmd.ExecuteScalar());
                    }
                    if (options.Transaction == null)
                        dbTransaction.Commit();
                }
            }
            return retValue.ToArray();
        }
        public virtual object ExecuteScalar(DbCommand command, Options options = null)
        {
            var retValue = ExecuteScalar(options, new DbCommand[] { command });
            return retValue != null ? retValue[0] : null;
        }
        public virtual int ExecuteNonQuery(CommandType commandType = CommandType.Text, string schema = "", string package = "", Options options = null, string commandText = "", params object[] args)
        {
            var retValue = 0;
            //using (var con = NewConnection())
            //{
            var com = NewCommand(commandType: commandType, schema: schema, package: package, commandText: commandText, options: options, con: null, args: args);
            retValue = ExecuteNonQuery(options, com);
            //}
            return retValue;
        }
        public virtual int ExecuteNonQuery(Options options = null, params DbCommand[] commands)
        {
            options = options ?? Options;
            var retValue = 0;
            //using (var con = options.Transaction != null ? options.Transaction.Connection : NewConnection())
            var con = options.Transaction != null ? options.Transaction.Connection : NewConnection();
            {
                //using (var dbTransaction = options.Transaction ?? con.BeginTransaction())
                var dbTransaction = options.Transaction ?? con.BeginTransaction();
                {
                    foreach (var com in commands)
                    {
                        com.Connection = con;
                        com.Transaction = dbTransaction;
                        retValue += com.ExecuteNonQuery();
                        if (options.EventListener.OnCallback != null)
                            options.EventListener.OnCallback(new Callback { SqlQuery = com.CommandText, OutputParameters = GetOutputParameters(com) });
                    }
                    if (options.Transaction == null)
                        dbTransaction.Commit();
                }
            }
            return retValue;
        }
        #endregion Database Operations
        #region DynamicObject Overrides
        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            #region Base
            if (csharpBinder == null)
                csharpBinder = binder.GetType().GetInterface("Microsoft.CSharp.RuntimeBinder.ICSharpInvokeOrInvokeMemberBinder");
            if (csharpBinder != null && csharpBinderTypeArguments == null)
                csharpBinderTypeArguments = csharpBinder.GetProperty("TypeArguments");
            var genericTypeArgs = csharpBinderTypeArguments.GetValue(binder, null) as IList<Type>;
            Type convertType = null;
            TypeCode convertTypeCode = TypeCode.Empty;
            if (genericTypeArgs != null && genericTypeArgs.Count > 0)
            {
                convertType = genericTypeArgs[0];
                convertTypeCode = Type.GetTypeCode(convertType);
            }
            var binderNameLower = binder.Name.ToLower(CultureInfo.GetCultureInfo("en-US"));
            var binderNameUpper = binder.Name.ToUpper(CultureInfo.GetCultureInfo("en-US"));
            var argumentNames = binder.CallInfo.ArgumentNames.ToArray();
            if (binderNameLower == "execute")
            {
                var argsExecuteDict = args[0].ToDictionary<Attribute>();
                var operation = argsExecuteDict.FirstOrDefault(f => f.Key.ToLower(CultureInfo.GetCultureInfo("en-US")) == "operation");
                binderNameLower = operation.Value.ToString().ToLower(CultureInfo.GetCultureInfo("en-US"));
                binderNameUpper = binderNameLower.ToUpper(CultureInfo.GetCultureInfo("en-US"));
                argsExecuteDict = argsExecuteDict.Where(f => f.Key != operation.Key).ToDictionary(f => f.Key, f => f.Value);//.Except(new List<KeyValuePair<string, object>> { operation }).ToDictionary();
                argumentNames = argsExecuteDict.Keys.ToArray();
                args = argsExecuteDict.Values.ToArray();
            }
            bool multiResultSet = false;
            object argsParameterValue = null;
            ICollection<object> argsParameterValueAsCollection = null;
            Type argsParameterValueType;
            bool argsParameterValueIsCollection = false;
            bool argsParameterValueIsPrimitive;
            Options options = null;
            dynamic onconvertingresult = null;
            dynamic onCallback = null;
            DbTransaction transaction = null;
            string sqlStatement;
            CommandType commandType = CommandType.Text;
            string argsName;
            dynamic argsValue;
            SqlEntity sqlEntity = new SqlEntity { Binder = binderNameLower };
            for (int i = 0; i < args.Length; i++)
            {
                argsName = argumentNames[i].ToLower(CultureInfo.GetCultureInfo("en-US"));
                argsValue = args[i];
                if (argsName == "schema")
                    sqlEntity.Schema = argsValue.ToString();
                else if (argsName == "table")
                    sqlEntity.Table = argsValue.ToString();
                else if (argsName == "fn")
                    sqlEntity.Fn = argsValue.ToString();
                else if (argsName == "sp")
                    sqlEntity.Sp = argsValue.ToString();
                else if (argsName == "where")
                    sqlEntity.Where = argsValue.ToString();
                else if (argsName == "orderby")
                    sqlEntity.OrderBy = argsValue.ToString();
                else if (argsName == "groupby")
                    sqlEntity.GroupBy = argsValue.ToString();
                else if (argsName == "having")
                    sqlEntity.Having = argsValue.ToString();
                else if (argsName == "columns")
                    sqlEntity.Columns = argsValue.ToString();
                else if (argsName == "limit")
                    sqlEntity.Limit = argsValue;
                else if (argsName == "pagesize")
                    sqlEntity.PageSize = argsValue;
                else if (argsName == "pageno")
                    sqlEntity.PageNo = argsValue;
                else if (argsName == "sql")
                    sqlEntity.Sql = argsValue.ToString();
                else if (argsName == "pkfield")
                    sqlEntity.PKField = argsValue;
                else if (argsName == "rownumbercolumn")
                    sqlEntity.RowNumberColumn = argsValue;
                else if (argsName == "package")
                    sqlEntity.Package = argsValue;
                else if (argsName == "sequence")
                    sqlEntity.Sequence = argsValue;
                else if (argsName == "options")
                    options = argsValue;
                else if (argsName == "onconvertingresult")
                    onconvertingresult = argsValue;
                else if (argsName == "oncallback")
                    onCallback = argsValue;
                else if (argsName == "resulttype")
                    convertType = Type.GetType(argsValue);
                else if (argsName == "multiresultset")
                    multiResultSet = argsValue;
                else if (argsName == "args")
                {
                    if (argsValue == null) continue;
                    argsParameterValue = argsValue;
                    argsParameterValueType = argsValue.GetType();
                    argsParameterValueAsCollection = argsParameterValue as ICollection<object>;
                    argsParameterValueIsCollection = argsParameterValueAsCollection != null;
                    argsParameterValueIsPrimitive = argsParameterValueType.IsPrimitive || argsParameterValueType == UniExtensions.stringType;
                    if (argsParameterValueIsPrimitive)
                        sqlEntity.ArrayArguments.Add(argsValue);
                    else if (argsParameterValueIsCollection)
                        sqlEntity.ArrayArguments.AddRange(argsValue);
                    else
                        foreach (var item in argsParameterValue.ToDictionary<Attribute>())
                            sqlEntity.NamedArguments.Add(new ArgumentEntity { Name = item.Key, Value = item.Value });
                }
                else
                    sqlEntity.NamedArguments.Add(new ArgumentEntity { Name = argumentNames[i], Value = argsValue });
            }
            sqlStatement = sqlEntity.Sql;
            options = options ?? Options;
            if (transaction != null)
                options.Transaction = transaction;
            if (onconvertingresult != null)
                options.EventListener.OnConvertingResult = onconvertingresult;
            if (onCallback != null)
                options.EventListener.OnCallback = onCallback;
            if (options.EventListener.OnPreGeneratingSql != null)
                options.EventListener.OnPreGeneratingSql(sqlEntity);
            Dictionary<string, object> namedArgumentsAsDict = sqlEntity.NamedArguments.ToDictionary(f => f.Name, f => f.Value);
            Dictionary<string, object> namedArgumentsAsParameterDict = namedArgumentsAsDict.ToParameters(parameterPrefix: parameterPrefix);
            Dictionary<string, object> arrayArgementsAsParameterDict = sqlEntity.ArrayArguments.ToArray().ToParameters(parameterPrefix, sqlEntity.PKField);
            Dictionary<string, object> commandParametersDict = namedArgumentsAsParameterDict.Concat(arrayArgementsAsParameterDict).ToDictionary(f => f.Key, f => f.Value);
            #endregion Base
            #region SqlStatement
            #region DbObjectName
            string dbObjectName = null;
            if (!string.IsNullOrEmpty(sqlEntity.Table))
                dbObjectName = string.Format(commandFormat, sqlEntity.Schema, sqlEntity.Table).Trim('.');
            else if (!string.IsNullOrEmpty(sqlEntity.Fn))
                dbObjectName = string.Format(string.Concat(commandFormat, " ({2})"), sqlEntity.Schema, sqlEntity.Fn, commandParametersDict.ToParameterString("")).Trim('.');
            else if (!string.IsNullOrEmpty(sqlEntity.Sp))
            {
                commandType = CommandType.StoredProcedure;
                sqlStatement = string.Format("{0}.{1}.{2}", sqlEntity.Schema, sqlEntity.Package, sqlEntity.Sp).Trim('.');
            }
            #endregion DbObjectName
            #region Sql
            if (commandType != CommandType.StoredProcedure && (binderNameLower == "query" || binderNameLower == "multiquery" || multiResultSet || binderNameLower == "exists" ||
        binderNameLower == "count" || binderNameLower == "sum" || binderNameLower == "max" || binderNameLower == "min" || binderNameLower == "avg"))
            {
                List<string> whereCriterias = new List<string>();
                foreach (var item in namedArgumentsAsDict)
                {
                    var itemValueType = item.Value.GetType();
                    var namedArgKey = string.Format(parameterFormat, parameterPrefix, item.Key);
                    if (!sqlEntity.Sql.Contains(namedArgKey) && !sqlEntity.Where.Contains(namedArgKey) && (itemValueType.IsPrimitive || item.Value is string))
                        whereCriterias.Add(string.Format("{0}={1}", item.Key, namedArgKey));
                    else
                    {
                        var namedArgValue = item.Value as IList;
                        if (namedArgValue != null)
                        {
                            var prms = string.Join(",", Enumerable.Repeat(namedArgKey, namedArgValue.Count).Select((f, i) => f + i));
                            //iyilestirme icin kontrol edilecek.
                            sqlEntity.NamedArguments.Remove(sqlEntity.NamedArguments.FirstOrDefault(f => f.Name == item.Key));
                            if (sqlEntity.Where.Contains(namedArgKey))
                                sqlEntity.Where = sqlEntity.Where.Replace(namedArgKey, string.Format("({0})", prms));
                            else
                                whereCriterias.Add(string.Format("{0} IN ({1})", item.Key, prms));
                        }
                    }
                }
                if (!string.IsNullOrEmpty(sqlEntity.Where))
                    whereCriterias.Add(sqlEntity.Where);
                sqlEntity.Where = string.Join(" AND ", whereCriterias);
                string whereStatement = string.IsNullOrEmpty(sqlEntity.Where) ? "" : string.Format(" WHERE {0}", sqlEntity.Where);
                string orderByStatement = string.IsNullOrEmpty(sqlEntity.OrderBy) ? "" : string.Format(" ORDER BY {0}", sqlEntity.OrderBy);
                string groupByStatement = string.IsNullOrEmpty(sqlEntity.GroupBy) ? "" : string.Format(" GROUP BY {0}", sqlEntity.GroupBy);
                string havingStatement = string.IsNullOrEmpty(sqlEntity.Having) ? "" : string.Format(" HAVING {0}", sqlEntity.Having);
                if (string.IsNullOrEmpty(sqlEntity.Sql))
                {
                    string columnStatement = sqlEntity.Columns;
                    string limitStatement = null;
                    if (string.IsNullOrEmpty(columnStatement))
                        throw new ArgumentNullException("Column statement cannot be null.");
                    else
                    {
                        string[] columnArray = columnStatement.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(f => f.Trim()).ToArray();
                        if (binderNameLower == "count" || binderNameLower == "sum" || binderNameLower == "max" || binderNameLower == "min" || binderNameLower == "avg")
                            if (columnArray.Length > 1)
                                throw new Exception(string.Format("Columns cannot be more than one for {0}", binderNameLower));
                            else if (columnStatement == "*" && (binderNameLower == "sum" || binderNameLower == "max" || binderNameLower == "min" || binderNameLower == "avg"))
                                throw new Exception(string.Format("Columns cannot be * for {0}", binderNameLower));
                            else
                                columnStatement = string.Format("{0}({1}) {2}", binderNameUpper, columnStatement, binderNameUpper);
                    }
                    if (string.IsNullOrEmpty(dbObjectName))
                        throw new ArgumentNullException("Database object name cannot be null.");
                    if (sqlEntity.Limit > 0)
                    {
                        columnStatement = this.dbType == DatabaseType.SQLServer ? string.Format("TOP {0} {1}", sqlEntity.Limit, sqlEntity.Columns) : sqlEntity.Columns;
                        if (this.dbType == DatabaseType.MySQL || this.dbType == DatabaseType.PostgreSQL || this.dbType == DatabaseType.SQLite)
                            limitStatement = string.Format(" LIMIT {0}", sqlEntity.Limit);
                    }
                    sqlStatement = string.Format("SELECT {0} FROM {1}{2}{3}{4}{5}{6}", columnStatement, dbObjectName, whereStatement, groupByStatement, havingStatement, orderByStatement, limitStatement);
                    if (sqlEntity.Limit > 0 && this.dbType == DatabaseType.Oracle)
                        sqlStatement = string.Format("SELECT * FROM ({0}) WHERE ROWNUM <= {1}", sqlStatement, sqlEntity.Limit);
                    if (binderNameLower == "exists")
                        sqlStatement = string.Format("SELECT CASE WHEN EXISTS({0}) THEN 1 ELSE 0 END as RESULT {1}", sqlStatement, this.dbType == DatabaseType.Oracle ? "FROM DUAL" : null);
                }
                else
                {
                    string sql = sqlEntity.Sql;
                    if (dbType == DatabaseType.SQLite && sqlEntity.Limit > 0)
                        sql = string.Format("SELECT * FROM ({0})", sqlEntity.Sql);
                    //Kontrol edilecek
                    sqlStatement = string.Format("{0}{1}{2}{3}{4}", sql, whereStatement, groupByStatement, havingStatement, orderByStatement);
                }
                if (string.IsNullOrEmpty(sqlStatement))
                    throw new ArgumentNullException("Sql statement cannot be null.");

                if (sqlEntity.PageSize > 0 || sqlEntity.PageNo > 0)
                {
                    if (sqlEntity.PageNo <= 0) sqlEntity.PageNo = 1;
                    if (sqlEntity.PageSize <= 0) sqlEntity.PageSize = 10;
                    var pageStart = (sqlEntity.PageNo - 1) * sqlEntity.PageSize;
                    if (this.dbType == DatabaseType.SQLServer)
                        sqlStatement = string.Format("SELECT TOP {3} * FROM (SELECT ROW_NUMBER() OVER (ORDER BY {0}) AS RowNumber, * FROM ({1}) as PagedTable) as PagedRecords WHERE RowNumber > {2}", sqlEntity.RowNumberColumn, sqlStatement, pageStart, sqlEntity.PageSize);
                    else if (this.dbType == DatabaseType.Oracle)
                        sqlStatement = string.Format("SELECT * FROM (SELECT T1.*,ROWNUM ROWNUMBER FROM ({0}) T1 WHERE ROWNUM <= {2}) WHERE ROWNUMBER > {1}", sqlStatement, pageStart, (pageStart + sqlEntity.PageSize));
                    else if (this.dbType == DatabaseType.MySQL || this.dbType == DatabaseType.SQLite)
                        sqlStatement = string.Format("{0} LIMIT {1},{2}", sqlStatement, pageStart, sqlEntity.PageSize);
                }
            }
            #endregion Sql
            #endregion SqlStatement
            #region Execute
            bool isKnownMethod = false;
            if (binderNameLower == "query" && !multiResultSet)
            {
                isKnownMethod = true;
                if (convertType != null)
                    result = CallQueryReflection(type: convertType, commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
                else
                    result = Query(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
                if (sqlEntity.Limit == 1 || (convertType != null && convertTypeCode != TypeCode.Object && Enum.IsDefined(typeof(TypeCode), convertTypeCode)))
                {
                    var resultEnumerable = result as IEnumerable;
                    if (resultEnumerable != null)
                    {
                        IEnumerator resultEnumerator = resultEnumerable.GetEnumerator();
                        if (resultEnumerator.MoveNext())
                        {
                            object limit1 = resultEnumerator.Current;
                            if (limit1 is ExpandoObject)
                            {
                                var limit1Dict = limit1 as IDictionary<string, object>;
                                result = limit1Dict.Count == 1 ? limit1Dict.ElementAtOrDefault(0).Value : limit1;
                            }
                            else
                            {
                                IEnumerable limit1AsEnumerable = limit1 as IEnumerable;
                                if (limit1AsEnumerable != null)
                                {
                                    IEnumerator limit1Enumerator = limit1AsEnumerable.GetEnumerator();
                                    if (limit1Enumerator.MoveNext())
                                        limit1 = limit1Enumerator.Current;
                                }
                                result = limit1;
                                //TypeCode objTypeCode = Type.GetTypeCode(limit1.GetType());
                                //if (objTypeCode != TypeCode.Object && Enum.IsDefined(typeof(TypeCode), objTypeCode))
                                //    result = limit1;
                                //else
                                //{
                                //    var limit1Dict = limit1.ToDictionary<Attribute>();
                                //    if (limit1Dict != null && limit1Dict.Count > 0)
                                //        result = limit1Dict.ElementAtOrDefault(1).Value;
                                //    else
                                //        result = limit1;
                                //}
                            }
                        }
                    }
                }
            }
            else if (binderNameLower == "multiquery" || multiResultSet)
            {
                isKnownMethod = true;
                result = MultipleQuery(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "getbool" || binderNameLower == "exists")
            {
                isKnownMethod = true;
                result = ExecuteScalar<bool>(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "getlong")
            {
                isKnownMethod = true;
                result = ExecuteScalar<long>(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "getdouble")
            {
                isKnownMethod = true;
                result = ExecuteScalar<double>(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "getfloat")
            {
                isKnownMethod = true;
                result = ExecuteScalar<float>(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "getdatetime")
            {
                isKnownMethod = true;
                result = ExecuteScalar<DateTime>(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "getint" || binderNameLower == "count")
            {
                isKnownMethod = true;
                result = ExecuteScalar<int>(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "getdecimal" || binderNameLower == "sum" || binderNameLower == "max" || binderNameLower == "min" || binderNameLower == "avg")
            {
                isKnownMethod = true;
                result = ExecuteScalar<decimal>(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "getvalue")
            {
                isKnownMethod = true;
                result = ExecuteScalar(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "nonquery")
            {
                isKnownMethod = true;
                result = ExecuteNonQuery(commandType: commandType, commandText: sqlStatement, options: options, args: commandParametersDict);
            }
            else if (binderNameLower == "insert")
            {
                isKnownMethod = true;
                #region INSERT
                string pkField = sqlEntity.PKField;
                if (string.IsNullOrEmpty(pkField))
                    throw new ArgumentNullException("PKField cannot be null.");
                string pkFieldWithPrefix = string.Format(parameterFormat, parameterPrefix, pkField);
                string insertValuesAsPrm;
                string insertValues;
                string suffix;
                string replaceStr = null;
                if (argsParameterValueIsCollection)
                {
                    var sqlList = new List<string>();
                    var parameterList = new Dictionary<string, object>();
                    for (int i = 0; i < argsParameterValueAsCollection.Count; i++)
                    {
                        suffix = i.ToString();
                        var item = argsParameterValueAsCollection.ElementAt(i);
                        if (dbType == DatabaseType.Oracle && !string.IsNullOrEmpty(sqlEntity.Sequence))
                        {
                            pkField = "";
                            replaceStr = string.Format("{0}.NEXTVAL", sqlEntity.Sequence);
                        }
                        else if (dbType == DatabaseType.PostgreSQL && !string.IsNullOrEmpty(sqlEntity.Sequence))
                        {
                            pkField = "";
                            replaceStr = string.Format("NEXTVAL('{0}')", sqlEntity.Sequence);
                        }
                        insertValuesAsPrm = item.ToColumnString(pkField);
                        insertValues = item.ToParameterString(parameterPrefix, pkField, suffix);
                        if (!string.IsNullOrEmpty(replaceStr))
                            insertValues = insertValues.Replace(string.Format(parameterFormat, pkFieldWithPrefix, suffix), replaceStr);
                        sqlList.Add(string.Format("INSERT INTO {0} ({1}) VALUES ({2})", dbObjectName, insertValuesAsPrm, insertValues));
                    }
                    if (dbType == DatabaseType.Oracle)
                        sqlStatement = string.Format("BEGIN {0} RETURNING 1 INTO {1};END;", string.Join(";", sqlList), pkFieldWithPrefix);
                    else
                        sqlStatement = string.Join(";", sqlList);
                    if (string.IsNullOrEmpty(sqlStatement))
                        throw new ArgumentNullException("Sql statement cannot be null.");
                    if (argsParameterValueIsCollection && dbType == DatabaseType.Oracle)
                    {
                        var newCom = NewCommand(commandType: CommandType.Text, schema: "", package: "", commandText: sqlStatement, options: options, con: null, args: commandParametersDict);
                        var outputPrm = NewParameter(newCom, sqlEntity.PKField, null, System.Data.DbType.Decimal, ParameterDirection.Output, options);
                        newCom.Parameters.Add(outputPrm);
                        ExecuteNonQuery(options, newCom);
                        result = outputPrm.Value.To<int, Attribute>(defaultValue: 0);
                    }
                    else
                        result = ExecuteNonQuery(commandType: CommandType.Text, commandText: sqlStatement, options: options, args: argsParameterValueAsCollection.ToParameters(parameterPrefix, sqlEntity.PKField));
                }
                else
                {
                    if (string.IsNullOrEmpty(sqlStatement))
                    {
                        if (dbType == DatabaseType.Oracle && !string.IsNullOrEmpty(sqlEntity.Sequence))
                        {
                            pkField = "";
                            replaceStr = string.Format("{0}.NEXTVAL", sqlEntity.Sequence);
                        }
                        else if (dbType == DatabaseType.PostgreSQL && !string.IsNullOrEmpty(sqlEntity.Sequence))
                        {
                            pkField = "";
                            replaceStr = string.Format("NEXTVAL('{0}')", sqlEntity.Sequence);
                        }
                        insertValuesAsPrm = argsParameterValue.ToColumnString(pkField);
                        insertValues = argsParameterValue.ToParameterString(parameterPrefix, pkField);
                        if (!string.IsNullOrEmpty(replaceStr))
                            insertValues = insertValues.Replace(pkFieldWithPrefix, replaceStr);
                        string newIDSql = "";
                        if (this.dbType == DatabaseType.SQLServer)
                            newIDSql = ";SELECT SCOPE_IDENTITY()";
                        else if (dbType == DatabaseType.MySQL)
                            newIDSql = ";SELECT LAST_INSERT_ID()";
                        else if (dbType == DatabaseType.SQLite)
                            newIDSql = ";SELECT LAST_INSERT_ROWID();";
                        else if (dbType == DatabaseType.Oracle)
                            newIDSql = string.Format(" RETURNING {0} INTO {1}", sqlEntity.PKField, pkFieldWithPrefix);
                        else if (dbType == DatabaseType.PostgreSQL)
                            newIDSql = string.Format(" RETURNING {0}", sqlEntity.PKField);
                        sqlStatement = string.Format("INSERT INTO {0} ({1}) VALUES ({2}){3}", dbObjectName, insertValuesAsPrm, insertValues, newIDSql);
                    }
                    if (string.IsNullOrEmpty(sqlStatement))
                        throw new ArgumentNullException("Sql statement cannot be null.");
                    commandParametersDict = commandParametersDict.Where(f => f.Key != pkFieldWithPrefix).ToDictionary(f => f.Key, f => f.Value);
                    if (dbType == DatabaseType.Oracle)
                    {
                        var newCom = NewCommand(commandType: CommandType.Text, schema: "", package: "", commandText: sqlStatement, options: options, con: null, args: commandParametersDict);
                        var outputPrm = NewParameter(newCom, sqlEntity.PKField, null, System.Data.DbType.Decimal, ParameterDirection.Output, options);
                        newCom.Parameters.Add(outputPrm);
                        ExecuteNonQuery(options, newCom);
                        result = outputPrm.Value.To<int, Attribute>(defaultValue: 0);
                    }
                    else
                        result = ExecuteScalar<int>(commandType: CommandType.Text, commandText: sqlStatement, options: options, args: commandParametersDict);
                }
                #endregion INSERT
            }
            else if (binderNameLower == "update")
            {
                isKnownMethod = true;
                #region UPDATE
                if (string.IsNullOrEmpty(sqlEntity.Columns))
                    throw new ArgumentNullException("Column statement cannot be null.");
                string pkField = sqlEntity.PKField;
                string outputPrmName = string.Format(parameterFormat, parameterPrefix, "outputPrm");
                string whereStatement = string.IsNullOrEmpty(sqlEntity.Where) ? "" : string.Format(" WHERE {0}", sqlEntity.Where);
                string[] columnArray = sqlEntity.Columns.Split(',');
                if (argsParameterValueIsCollection)
                {
                    var sqlList = new List<string>();
                    var parameterList = new Dictionary<string, object>();
                    object item = null;
                    string columnPrmString = null;
                    Regex regex = new Regex(string.Format("(?<parameter>{0}[^{0},;) ]+)", parameterPrefix));
                    MatchCollection parameterMatchCollection = regex.Matches(sqlEntity.Where);
                    var whereParameterArray = parameterMatchCollection.Cast<Match>().Select(f => f.Groups["parameter"].Value).Distinct().ToArray();
                    for (int i = 0; i < argsParameterValueAsCollection.Count; i++)
                    {
                        item = argsParameterValueAsCollection.ElementAt(i);
                        if (!string.IsNullOrEmpty(sqlEntity.Where))
                        {
                            whereStatement = sqlEntity.Where;
                            foreach (var whereItem in whereParameterArray)
                                whereStatement = whereStatement.Replace(whereItem, whereItem + i);
                        }
                        else
                            whereStatement = item.ToColumnParameterString(parameterPrefix, pkField, i.ToString(), " AND ", columnArray);
                        whereStatement = string.IsNullOrEmpty(whereStatement) ? "" : string.Format(" WHERE {0}", whereStatement);
                        columnPrmString = columnArray.ToDictionary(f => f).ToColumnParameterString(parameterPrefix, pkField, i.ToString());
                        sqlList.Add(string.Format("UPDATE {0} SET {1} {2}", dbObjectName, columnPrmString, whereStatement));
                    }
                    if (dbType == DatabaseType.Oracle)
                        sqlStatement = string.Format("BEGIN {0} RETURNING 1 INTO {1};END;", string.Join(";", sqlList), outputPrmName);
                    else
                        sqlStatement = string.Join(";", sqlList);
                }
                else
                {
                    if (string.IsNullOrEmpty(whereStatement))
                    {
                        whereStatement = argsParameterValue.ToColumnParameterString(parameterPrefix, pkField, "", " AND ", columnArray);
                        whereStatement = string.IsNullOrEmpty(whereStatement) ? "" : string.Format(" WHERE {0}", whereStatement);
                    }
                    if (string.IsNullOrEmpty(sqlStatement))
                        sqlStatement = string.Format("UPDATE {0} SET {1} {2}", dbObjectName, columnArray.ToDictionary(f => f).ToColumnParameterString(parameterPrefix), whereStatement);
                }
                if (string.IsNullOrEmpty(whereStatement))
                    throw new ArgumentNullException("Where statement cannot be null.");
                if (string.IsNullOrEmpty(sqlStatement))
                    throw new ArgumentNullException("Sql statement cannot be null.");
                if (argsParameterValueIsCollection && dbType == DatabaseType.Oracle)
                {
                    var newCom = NewCommand(commandType: CommandType.Text, schema: "", package: "", commandText: sqlStatement, options: options, con: null, args: commandParametersDict);
                    var outputPrm = NewParameter(newCom, outputPrmName, null, System.Data.DbType.Decimal, ParameterDirection.Output, options);
                    newCom.Parameters.Add(outputPrm);
                    ExecuteNonQuery(options, newCom);
                    result = outputPrm.Value.To<int, Attribute>(defaultValue: 0);
                }
                else
                    result = ExecuteNonQuery(commandType: CommandType.Text, commandText: sqlStatement, options: options, args: commandParametersDict);
                #endregion UPDATE
            }
            else if (binderNameLower == "delete")
            {
                isKnownMethod = true;
                #region DELETE
                string whereStatement = string.IsNullOrEmpty(sqlEntity.Where) ? "" : string.Format(" WHERE {0}", sqlEntity.Where);
                string outputPrmName = string.Format(parameterFormat, parameterPrefix, "outputPrm");
                if (argsParameterValueIsCollection)
                {
                    var sqlList = new List<string>();
                    var parameterList = new Dictionary<string, object>();
                    object item = null;
                    Regex regex = new Regex(string.Format("(?<parameter>{0}[^{0},;) ]+)", parameterPrefix));
                    MatchCollection parameterMatchCollection = regex.Matches(sqlEntity.Where);
                    var whereParameterArray = parameterMatchCollection.Cast<Match>().Select(f => f.Groups["parameter"].Value).Distinct().ToArray();
                    for (int i = 0; i < argsParameterValueAsCollection.Count; i++)
                    {
                        item = argsParameterValueAsCollection.ElementAt(i);
                        if (!string.IsNullOrEmpty(sqlEntity.Where))
                        {
                            whereStatement = sqlEntity.Where;
                            foreach (var whereItem in whereParameterArray)
                                whereStatement = whereStatement.Replace(whereItem, whereItem + i);
                        }
                        else
                            whereStatement = item.ToColumnParameterString(parameterPrefix, sqlEntity.PKField, i.ToString(), " AND ");
                        whereStatement = string.IsNullOrEmpty(whereStatement) ? "" : string.Format(" WHERE {0}", whereStatement);
                        sqlList.Add(string.Format("DELETE FROM {0} {1}", dbObjectName, whereStatement));
                    }
                    if (dbType == DatabaseType.Oracle)
                        sqlStatement = string.Format("BEGIN {0} RETURNING 1 INTO {1};END;", string.Join(";", sqlList), outputPrmName);
                    else
                        sqlStatement = string.Join(";", sqlList);
                }
                else
                {
                    if (string.IsNullOrEmpty(whereStatement))
                    {
                        whereStatement = argsParameterValue.ToColumnParameterString(parameterPrefix, sqlEntity.PKField, "", " AND ");
                        whereStatement = string.IsNullOrEmpty(whereStatement) ? "" : string.Format(" WHERE {0}", whereStatement);
                    }
                    if (string.IsNullOrEmpty(sqlStatement))
                        sqlStatement = string.Format("DELETE FROM {0} {1}", dbObjectName, whereStatement);
                }
                if (string.IsNullOrEmpty(whereStatement))
                    throw new ArgumentNullException("Where statement cannot be null.");
                if (string.IsNullOrEmpty(sqlStatement))
                    throw new ArgumentNullException("Sql statement cannot be null.");
                if (argsParameterValueIsCollection && dbType == DatabaseType.Oracle)
                {
                    var newCom = NewCommand(commandType: CommandType.Text, schema: "", package: "", commandText: sqlStatement, options: options, con: null, args: commandParametersDict);
                    var outputPrm = NewParameter(newCom, outputPrmName, null, System.Data.DbType.Decimal, ParameterDirection.Output, options);
                    newCom.Parameters.Add(outputPrm);
                    ExecuteNonQuery(options, newCom);
                    result = outputPrm.Value.To<int, Attribute>(defaultValue: 0);
                }
                else
                    result = ExecuteNonQuery(commandType: CommandType.Text, schema: "", package: "", commandText: sqlStatement, options: options, args: commandParametersDict);
                #endregion DELETE
            }
            #endregion Execute
            if (isKnownMethod)
            {
                if (options.EventListener.OnConvertingResult != null)
                    result = options.EventListener.OnConvertingResult(sqlEntity, result);
                return true;
            }
            return false;
        }
        public virtual decimal Avg(string schema = "", string table = "", string where = "", string columns = "", Options options = null, params object[] args)
        {
            return this.dyno.Avg(Schema: schema, Table: table, Where: where, Columns: columns, Options: options, Args: args);
        }
        public virtual decimal Max(string schema = "", string table = "", string where = "", string columns = "", Options options = null, params object[] args)
        {
            return this.dyno.Max(Schema: schema, Table: table, Where: where, Columns: columns, Options: options, Args: args);
        }
        public virtual decimal Min(string schema = "", string table = "", string where = "", string columns = "", Options options = null, params object[] args)
        {
            return this.dyno.Min(Schema: schema, Table: table, Where: where, Columns: columns, Options: options, Args: args);
        }
        public virtual decimal Sum(string schema = "", string table = "", string where = "", string columns = "", Options options = null, params object[] args)
        {
            return this.dyno.Sum(Schema: schema, Table: table, Where: where, Columns: columns, Options: options, Args: args);
        }
        public virtual bool Exists(string schema = "", string table = "", string where = "", Options options = null, params object[] args)
        {
            return this.dyno.Exists(Schema: schema, Table: table, Where: where, Options: options, Args: args);
        }
        public virtual int Count(string schema = "", string table = "", string where = "", Options options = null, params object[] args)
        {
            return this.dyno.Count(Schema: schema, Table: table, Where: where, Options: options, Args: args);
        }
        public virtual int Insert(string schema = "", string table = "", string pkField = "", Options options = null, params object[] args)
        {
            return this.dyno.Insert(Schema: schema, Table: table, PKField: pkField, Options: options, Args: args);
        }
        public virtual int Update(string schema = "", string table = "", string columns = "", Options options = null, params object[] args)
        {
            return this.dyno.Update(Schema: schema, Table: table, Columns: columns, Options: options, Args: args);
        }
        public virtual int Delete(string schema = "", string table = "", Options options = null, params object[] args)
        {
            return this.dyno.Delete(Schema: schema, Table: table, Options: options, Args: args);
        }
        #endregion DynamicObject Overrides
    }
}