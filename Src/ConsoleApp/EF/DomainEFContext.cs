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
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beem;

namespace ConsoleApp.EF
{
    public class DomainEFContext : DbContext
    {
        private const string ClassName = nameof(DomainEFContext);
        private readonly DbConnectionScope _dbConnectionScope;
        private readonly bool _contextOwnsConnectionScope;

        public DomainEFContext(DbConnectionScope dbConnectionScope, bool contextOwnsConnectionScope = false)
            : base(dbConnectionScope.Connection, contextOwnsConnection: false)
        {
            Debug.WriteLine($"{ClassName} - Constructor({nameof(dbConnectionScope)}, {nameof(contextOwnsConnectionScope)}: {contextOwnsConnectionScope})");
            _dbConnectionScope = dbConnectionScope;
            _contextOwnsConnectionScope = contextOwnsConnectionScope;
        }

        public DbSet<DomainModels.Customer> Customer { get; set; }

        public DbSet<DomainModels.Log> Log { get; set; }

        public IDbTransactionScope BeginTransactionScope()
        {
            return _dbConnectionScope.BeginTransactionScope();
        }

        public IDbTransactionScope BeginTransactionScope(System.Transactions.IsolationLevel isolationLevel)
        {
            return _dbConnectionScope.BeginTransactionScope(isolationLevel);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Debug.WriteLine($"{ClassName} - {nameof(OnModelCreating)}({nameof(modelBuilder)})");
            base.OnModelCreating(modelBuilder);

            // Use singular table names
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        protected override void Dispose(bool disposing)
        {
            Debug.WriteLine($"{ClassName} - {nameof(Dispose)}({nameof(disposing)}: {disposing})");
            if (disposing)
            {
                if (_contextOwnsConnectionScope)
                {
                    _dbConnectionScope?.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
