using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Data.Common;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;

namespace Uni.Orm.Test
{
    [TestClass]
    public class SqliteClassicTest
    {
        [TestMethod]
        public void ClassicTest()
        {
            var con = new SQLiteConnection(@"Data Source=.\Northwind.sqlite;Version=3;");
            var com = new SQLiteCommand("SELECT * FROM Products", con);

            List<Products> productList = new List<Products>();

            con.Open();

            DbDataReader reader = com.ExecuteReader();

            productList = ReaderMapToEntity<Products>(reader);

            con.Close();
        }

        public struct Field
        {
            public int Ordinal { get; set; }
            public string Name { get; set; }
            public MethodInfo PropertyFieldSetMethod { get; set; }
            public MethodInfo DataReaderGetValueMethod { get; set; }
        }

        public static List<T> ReaderMapToEntity<T>(DbDataReader reader) where T : new()
        {
            List<T> result = new List<T>();

            T newItem;
            PropertyInfo[] properties = typeof(T).GetProperties();
            int counter = 0;
            int ordinal;
            MethodInfo setMethod = null;
            MethodInfo readerGetValueMethod = null;
            List<Field> fieldList = new List<Field>();
            while (reader.Read())
            {
                newItem = new T();
                if (counter == 0)
                {
                    foreach (PropertyInfo pi in properties)
                    {
                        ordinal = reader.GetOrdinal(pi.Name);
                        setMethod = pi.GetSetMethod();
                        readerGetValueMethod = typeof(DbDataReader).GetMethod(string.Format("Get{0}", reader.GetFieldType(ordinal).Name));
                        fieldList.Add(new Field { Ordinal = ordinal, Name = pi.Name, PropertyFieldSetMethod = setMethod, DataReaderGetValueMethod = readerGetValueMethod });
                    }
                }

                for (int i = 0; i < fieldList.Count; i++)
                {
                    ordinal = fieldList[i].Ordinal;
                    if (!reader.IsDBNull(ordinal))
                    {
                        var value = fieldList[i].DataReaderGetValueMethod.Invoke(reader, new object[] { ordinal });
                        fieldList[i].PropertyFieldSetMethod.Invoke(newItem, new object[] { value });
                    }
                }

                result.Add(newItem);

                counter++;
            }

            return result;
        }
    }
}