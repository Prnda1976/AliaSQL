using System.IO;
using AliaSQL.Console;
using AliaSQL.Core;
using AliaSQL.Core.Model;
using AliaSQL.Core.Services.Impl;
using NUnit.Framework;

namespace AliaSQL.IntegrationTests
{
    [TestFixture]
    public class ChangedEverytimeScriptShouldRunTester
    {
        [Test]
        public void Update_Database_Runs_Changed_Everytime_Script()
        {
            //arrange
            string scriptsDirectory = Path.Combine("Scripts", GetType().Name.Replace("Tester", ""));

            var settings = new ConnectionSettings(".\\sqlexpress", "aliasqltest", true, null, null);
            new ConsoleAliaSQL().UpdateDatabase(settings, scriptsDirectory, RequestedDatabaseAction.Drop);

            //act
            //run once 
            Assert.True(new ConsoleAliaSQL().UpdateDatabase(settings, scriptsDirectory, RequestedDatabaseAction.Update));
            Assert.AreEqual(1, new QueryExecutor().ExecuteScalarInteger(settings, "select 1 from dbo.sysobjects where name = 'TestTable' and type='U'"));

            //change contents of script
            File.WriteAllText(Path.Combine(scriptsDirectory, "Everytime", "TestScript.sql"), "CREATE TABLE [dbo].[TestTable2]([Id] [int] IDENTITY(1,1) NOT NULL, [FullName] [nvarchar](50) NULL)");

            Assert.True(new ConsoleAliaSQL().UpdateDatabase(settings, scriptsDirectory, RequestedDatabaseAction.Update));

            //assert
            Assert.AreEqual(1, new QueryExecutor().ExecuteScalarInteger(settings, "select 1 from dbo.sysobjects where name = 'TestTable2' and type='U'"));

            //change contents of script back in case you run again without rebuilding
            File.WriteAllText(Path.Combine(scriptsDirectory, "Everytime", "TestScript.sql"), "CREATE TABLE [dbo].[TestTable]([Id] [int] IDENTITY(1,1) NOT NULL, [FullName] [nvarchar](50) NULL)");
        }
    }
}