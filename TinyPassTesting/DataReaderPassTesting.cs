using System;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using Chiats.TinyPass;
using System.Diagnostics;

namespace TinyPassTesting
{
    [TestClass]
    public class DataReaderPassTesting
    {

        public SqlConnection GetDefaultConnection()
        {
            var ConnectionStringBuilder = new SqlConnectionStringBuilder();

            ConnectionStringBuilder.DataSource = ".";
            ConnectionStringBuilder.InitialCatalog = "Northwind";
            ConnectionStringBuilder.IntegratedSecurity = true;
            var cnn = new SqlConnection(ConnectionStringBuilder.ConnectionString);
            return cnn;
        }

        //SELECT TOP (1000) [CategoryID]      ,[CategoryName]      ,[Description]      ,[Picture]    FROM[Northwind].[dbo].[Categories]
        class CategoryEntry
        {
            public int CategoryID { get; set; }
            public string CategoryName { get; set; }
            public string Description { get; set; }
            public byte[] Picture { get; set; }
            public override string ToString()
            {
                return $"CategoriesEntry (CategoryID={CategoryID} CategoryName='{CategoryName}'  Description='{Description}'  Picture Size={Picture?.Length}";
            }
        }
        [TestMethod]
        public void Pass_001()
        {
            using (var Connection = GetDefaultConnection())
            {
                Connection.Open();
                SqlCommand cmd = new SqlCommand("select * from Categories", Connection);
                Stopwatch s = new Stopwatch();
                s.Start();
                var Categories = TinyPass<CategoryEntry>.QueryAll(cmd.ExecuteReader());
                s.Stop();
                Debug.Print($"ElapsedMilliseconds : {s.ElapsedMilliseconds:#,##0}ms\r\n");
                foreach (var category in Categories)
                {

                    Debug.Print($"{category}");

                }
            }
        }
        [TestMethod]
        public void Pass_002()
        {
            using (var Connection = GetDefaultConnection())
            {
                Connection.Open();
                SqlCommand cmd = new SqlCommand("select * from Categories", Connection);

                Stopwatch s = new Stopwatch();
                s.Start();

                var category = new { CategoryID = 0, CategoryName = "" };
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        dr.QueryFill(category);
                        Debug.Print($"{category}");
                    }
                    else
                        Debug.Print("No Data Found");
                }
                s.Stop();
                Debug.Print($"ElapsedMilliseconds : {s.ElapsedMilliseconds:#,##0}ms\r\n");

            }
        }
    }
}
