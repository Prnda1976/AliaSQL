using System;
using System.Data.SqlClient;
using System.IO;
using AliaSQL.Console;
using AliaSQL.Core;
using AliaSQL.Core.Model;
using AliaSQL.Core.Services.Impl;
using NUnit.Framework;

namespace AliaSQL.IntegrationTests
{
    [TestFixture]
    public class OldRunAlwaysScriptShouldRunTester
    {
        [Test]
        public void Update_Database_ShouldRun_Old_RunAlways_Script()
        {
            //arrange
            string scriptsDirectory = Path.Combine("Scripts", GetType().Name.Replace("Tester", ""));

            var settings = new ConnectionSettings(".\\sqlexpress", "aliasqltest", true, null, null);
            new ConsoleAliaSQL().UpdateDatabase(settings, scriptsDirectory, RequestedDatabaseAction.Drop);

            //act
            //run once 
            Assert.True(new ConsoleAliaSQL().UpdateDatabase(settings, scriptsDirectory, RequestedDatabaseAction.Update));
            Assert.AreEqual(1, new QueryExecutor().ExecuteScalarInteger(settings, "select 1 from dbo.sysobjects where name = 'TestTable' and type='U'"));
            var dateApplied = DateTime.MinValue;
            QueryUsdAppliedDatabaseScriptTable(settings, reader =>
            {
                while (reader.Read())
                {
                    Assert.AreEqual("TestScript.sql", reader["ScriptFile"]);
                    dateApplied = (DateTime)reader["DateApplied"];
                }
            });
            Assert.Greater(dateApplied, DateTime.MinValue);

            //delete TestTable to ensure script doesn't run again
            new QueryExecutor().ExecuteNonQuery(settings, "drop table TestTable", true);

            //run again
            bool success = new ConsoleAliaSQL().UpdateDatabase(settings, scriptsDirectory, RequestedDatabaseAction.Update);

            //assert
            Assert.AreEqual(1, new QueryExecutor().ExecuteScalarInteger(settings, "select 1 from dbo.sysobjects where name = 'TestTable' and type='U'"));

            DateTime dateAppliedUpdated = DateTime.MinValue; ;
            int records = 0;
            QueryUsdAppliedDatabaseScriptTable(settings, reader =>
            {
                while (reader.Read())
                {
                    records++;
                    Assert.AreEqual("TestScript.sql", reader["ScriptFile"]);
                    dateAppliedUpdated = (DateTime) reader["DateApplied"];
                }
               
            });

            Assert.True(success);
            Assert.AreEqual(1, records);
            Assert.Greater(dateAppliedUpdated, dateApplied);
        }


        private void QueryUsdAppliedDatabaseScriptTable(ConnectionSettings settings, Action<SqlDataReader> action)
        {
            string connectionString = new ConnectionStringGenerator().GetConnectionString(settings, true);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText =
                        "SELECT  [ScriptFile],[DateApplied],[Version],[hash] FROM [dbo].[usd_AppliedDatabaseScript]";
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        action(reader);
                    }
                }
            }
        }

    }
}