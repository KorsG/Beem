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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Beem;
using System.Diagnostics;
using ConsoleApp.DomainModels;
using System.Data.SqlClient;

namespace ConsoleApp.DomainServices
{
    public interface ICustomerService
    {
        IEnumerable<Customer> GetAll(IDbTransactionScope transactionScope = null);

        Customer GetByKey(int customerID, IDbTransactionScope transactionScope = null);

        object GetStats(int customerID);

        int Create(Customer customer, IDbTransactionScope transactionScope = null);

        void DeleteAll(IDbTransactionScope transactionScope = null);
    }

    public class CustomerService : ICustomerService
    {
        private readonly DbFactory _dbFactory;

        public CustomerService(DbFactory dbFactory)
        {
            Debug.WriteLine($"{nameof(CustomerService)} - Constructor({nameof(dbFactory)})");
            _dbFactory = dbFactory;
        }

        public IEnumerable<Customer> GetAll(IDbTransactionScope transactionScope = null)
        {
            using (var db = _dbFactory.CreateDomainEFContext(transactionScope))
            {
                return db.Customer.AsNoTracking().ToList();
            }
        }

        public Customer GetByKey(int customerID, IDbTransactionScope transactionScope = null)
        {
            using (var db = _dbFactory.CreateDomainEFContext(transactionScope))
            {
                return db.Customer.AsNoTracking().SingleOrDefault(x => x.CustomerID == customerID);
            }
        }

        public int Create(Customer customer, IDbTransactionScope transactionScope = null)
        {
            // The following should be in a transaction regardless if the consumer provides a DbTransactionScope.
            using (var tx = _dbFactory.CreateTransactionScope(transactionScope))
            using (var db = _dbFactory.CreateDomainEFContext(tx))
            {
                db.Customer.Add(customer);
                db.SaveChanges();

                db.Database.ExecuteSqlCommand("INSERT INTO dbo.Log (Type, Value) VALUES (@p0, @p1)", "CustomerCreated", $"CustomerID: {customer.CustomerID}");
                tx.Commit();
            }

            // Do something outside the provided transaction. (just as an example)
            using (var cs = _dbFactory.CreateDomainConnectionScope())
            {
                //cs.Connection.Execute("");
            }

            return customer.CustomerID;
        }

        public object GetStats(int customerID)
        {
            // Some api call or the like that is not transactable.
            // The good thing here is that the consumer of the method knows that it is not transactionable since it does not accept a DbTransactionScope parameter.
            return new { SomeData = Guid.NewGuid().ToString() };
        }

        public void DeleteAll(IDbTransactionScope transactionScope = null)
        {
            using (var db = _dbFactory.CreateDomainConnectionScope(transactionScope))
            {
                db.Connection.Execute("DELETE FROM dbo.Customer");
            }
        }
    }
}
