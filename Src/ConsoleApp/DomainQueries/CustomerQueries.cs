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
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp.DomainModels;
using Beem;
using System.Diagnostics;

namespace ConsoleApp.DomainQueries
{
    public interface ICustomerQueries : IDisposable
    {
        IQueryable<Customer> Query(IDbTransactionScope transaction = null);

        IQueryable<Customer> Query(Expression<Func<Customer, bool>> wherePredicate, IDbTransactionScope transaction = null);

        IQueryable<Customer> QueryByKey(int customerID, IDbTransactionScope transaction = null);

        bool Exists(int customerID, IDbTransactionScope transaction = null);
    }

    public class CustomerQueries : ICustomerQueries
    {
        private readonly DbFactory _dbFactory;
        private readonly EF.DomainEFContext _efContext;

        public CustomerQueries(DbFactory dbFactory)
        {
            Debug.WriteLine($"{nameof(CustomerQueries)} - Constructor({nameof(dbFactory)})");
            _dbFactory = dbFactory;
            _efContext = dbFactory.CreateDomainEFContext();
        }

        public IQueryable<Customer> Query(IDbTransactionScope transaction = null)
        {
            var db = transaction == null ? _efContext : _dbFactory.CreateDomainEFContext(transaction, disposeWithTransaction: true);

            // Alternative to one-liner:
            // var db = _efContext;
            // if (transaction != null)
            // {
            //    db = _dbFactory.CreateDomainEFContext(transaction, disposeWithTransaction: true);
            // }

            return db.Customer.AsNoTracking();
        }

        public IQueryable<Customer> Query(Expression<Func<Customer, bool>> wherePredicate, IDbTransactionScope transaction = null)
        {
            return Query(transaction).Where(wherePredicate);
        }

        public IQueryable<Customer> QueryByKey(int customerID, IDbTransactionScope transaction = null)
        {
            return Query(transaction).Where(x => x.CustomerID == customerID);
        }

        public bool Exists(int customerID, IDbTransactionScope transaction = null)
        {
            return Query(transaction).Where(x => x.CustomerID == customerID).Select(x => true).FirstOrDefault();
        }

        public void Dispose()
        {
            _efContext?.Dispose();
        }
    }
}
