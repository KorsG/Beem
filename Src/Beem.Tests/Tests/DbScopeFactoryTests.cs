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
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Beem.Tests
{
    public class DbScopeFactoryTests : TestBase
    {
        private readonly DbScopeFactory _scopeFactory;

        public DbScopeFactoryTests()
        {
            _scopeFactory = new DbScopeFactory();
        }

        [Fact]
        public void CreateConnectionScope_with_transaction_will_create_nested()
        {
            using (var tx = _scopeFactory.CreateTransactionScope())
            using (var scope = _scopeFactory.CreateConnectionScope<TestDbConnectionScope>(tx))
            {
                Assert.False(scope._isRootScope);
            };
        }

        [Fact]
        public void Unnamed01()
        {
            using (var tx1 = _scopeFactory.CreateTransactionScope())
            using (var cs = _scopeFactory.CreateConnectionScope<TestDbConnectionScope>(tx1))
            using (var tx2 = cs.BeginTransactionScope())
            {
            }
        }
    }
}
