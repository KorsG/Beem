/*
 * Copyright 2016 Morten Korsgaard <morten@korsg.dk>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

namespace ConsoleApp
{
    class Program
    {
        public static readonly string DatabaseName = "Beem_ConsoleApp";
        public static readonly string ConnectionString = $@"Server=(localdb)\mssqllocaldb; Database={DatabaseName}; Trusted_Connection=true;";
        private static readonly DbFactory _dbFactory = new DbFactory();

        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            InitializeDatabase();

            WriteLine();
            WriteLine("--- Example01");
            Example01();

            WriteLine();
            WriteLine("--- Example02");
            Example02().Wait();

            WriteLine();
            WriteLine("------------------------");
            WriteLine("Press any key to exit...");
            WriteLine();
            ReadKey();
        }

        public static void Example01()
        {
            var customerService = new DomainServices.CustomerService(_dbFactory);
            using (var customerQueries = new DomainQueries.CustomerQueries(_dbFactory))
            using (var cs1 = _dbFactory.CreateDomainConnectionScope())
            {
                WriteLine("Create customer outside transaction.");
                customerService.Create(new DomainModels.Customer() { Name = Guid.NewGuid().ToString() });

                using (var tx1 = cs1.BeginTransactionScope())
                {
                    tx1.AddRollbackAction(() => WriteLine("tx1 - rollback action"));

                    WriteLine("Create customer in transaction.");
                    customerService.Create(new DomainModels.Customer() { Name = Guid.NewGuid().ToString() }, tx1);

                    WriteLine("Get all customers in transaction.");
                    customerQueries.Query(tx1).ToList().Dump();

                    WriteLine("Get all customers with seperate transaction with ReadUncommitted IsolationLevel.");
                    using (var cs2 = _dbFactory.CreateDomainConnectionScope())
                    using (var tx2 = cs2.BeginTransactionScope(isolationLevel: System.Transactions.IsolationLevel.ReadUncommitted))
                    {
                        customerQueries.Query(tx2).ToList().Dump();
                    }
                    // Commit is not called which will trigger a Rollback.
                }

                WriteLine("Get all customers outside transaction.");
                customerQueries.Query().ToList().Dump();
            }
        }

        public static async Task Example02()
        {
            using (var db = _dbFactory.CreateDomainEFContext())
            {
                using (var tx1 = db.BeginTransactionScope())
                {
                    var newCustomer = new DomainModels.Customer() { Name = Guid.NewGuid().ToString() };
                    db.Customer.Add(newCustomer);
                    await db.SaveChangesAsync();

                    WriteLine("Get all customers inside transaction");
                    (await db.Customer.ToListAsync()).Dump();
                    // Commit is not called which will trigger a Rollback.
                }

                WriteLine("Get all customers outside transaction.");
                (await db.Customer.ToListAsync()).Dump();
            }
        }

        private static void InitializeDatabase()
        {
            System.Data.Entity.Database.SetInitializer<EF.DomainEFContext>(null);
            // DropCreate database.
            var masterConnectionString = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" }.ToString();
            using (var conn = new SqlConnection(masterConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"
IF DB_ID('{DatabaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{DatabaseName}];
END;

CREATE DATABASE [{DatabaseName}];
ALTER DATABASE [{DatabaseName}] SET READ_COMMITTED_SNAPSHOT ON;

EXEC sp_executesql N'
USE [{DatabaseName}];

CREATE TABLE [dbo].[Customer] (
    [CustomerID] [int] NOT NULL IDENTITY,
    [Name] [nvarchar](255),
    CONSTRAINT [PK_dbo.Customer] PRIMARY KEY ([CustomerID])
);
CREATE TABLE [dbo].[Log] (
    [LogID] [int] NOT NULL IDENTITY,
    [Type] [nvarchar](255),
    [Value] [nvarchar](255),
    CONSTRAINT [PK_dbo.Log] PRIMARY KEY ([LogID])
);
';
";
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static class ObjectExtensions
    {
        public static void Dump(this object obj)
        {
            WriteLine("--------");
            WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented));
            WriteLine("--------");
        }
    }
}
