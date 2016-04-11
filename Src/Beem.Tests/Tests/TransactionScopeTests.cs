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
using Beem.Exceptions;
using Xunit;

namespace Beem.Tests
{
    public partial class TransactionScopeTests : TestBase
    {
        private readonly DbScopeFactory _scopeFactory;

        public TransactionScopeTests()
        {
            _scopeFactory = new DbScopeFactory();
        }

        private DbTransactionScope CreateDefaultScope(System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted)
        {
            return new DbTransactionScope(isolationLevel);
        }

        [Fact]
        public void Can_commit()
        {
            using (var tx = CreateDefaultScope())
            {
                tx.Commit();
            }
        }

        [Fact]
        public void Can_dispose()
        {
            using (var tx = CreateDefaultScope())
            {
                tx.Dispose();
            }
        }

        [Fact]
        public void Can_begin_nested()
        {
            using (var tx1 = CreateDefaultScope())
            using (var tx2 = tx1.BeginNested())
            {
            }
        }

        [Fact]
        public void Catched_nested_exception_should_throw_if_outer_commit()
        {
            Assert.Throws<DbTransactionScopeAbortedException>(() =>
            {
                using (var tx1 = CreateDefaultScope())
                {
                    using (var tx2 = tx1.BeginNested())
                    {
                        try
                        {
                            using (var tx3 = tx1.BeginNested())
                            {
                                throw new Exception();
                            }
                        }
                        catch (Exception)
                        {
                        }
                        // This should throw because the nested transaction has failed. 
                        tx2.Commit();
                    }
                    tx1.Commit();
                }
            });
        }

        [Fact]
        public void Catched_nested_exception_should_throw_if_starting_new_transaction()
        {
            Assert.Throws<DbTransactionScopeAbortedException>(() =>
            {
                using (var tx1 = CreateDefaultScope())
                {
                    try
                    {
                        using (var tx2 = tx1.BeginNested())
                        {
                            throw new Exception();
                        }
                    }
                    catch (Exception) { }

                    // This should throw because the nested transaction has failed. 
                    using (var tx3 = tx1.BeginNested())
                    {
                    }
                }
            });
        }

        [Fact]
        public void Committing_outermost_transaction_before_nested_should_throw()
        {
            Assert.Throws<DbTransactionScopeCommitException>(() =>
            {
                using (var tx1 = CreateDefaultScope())
                {
                    using (var tx2 = tx1.BeginNested())
                    {
                        // This should throw because there are unfinished nested transactions. 
                        // It just seems wierd if the outermost transaction could be commited before any nested.
                        tx1.Commit();
                    }
                }
            });
        }

        [Fact]
        public void Committing_nested_transaction_out_of_order_should_not_throw()
        {
            using (var tx1 = CreateDefaultScope())
            {
                using (var tx2 = tx1.BeginNested())
                {
                    using (var tx3 = tx2.BeginNested())
                    {
                        // This should not throw - it is okay that nested transactions are committed out of order.
                        tx2.Commit();
                        tx3.Commit();
                    }
                }
                tx1.Commit();
            }
        }

        [Fact]
        public void Begin_nested_transaction_with_different_isolationLevel_should_throw()
        {
            Assert.Throws<DbTransactionScopeException>(() =>
            {
                using (var tx1 = CreateDefaultScope(System.Transactions.IsolationLevel.ReadCommitted))
                {
                    // This should throw because it is not allowed to change Isolation level.
                    using (var tx2 = tx1.BeginNested(System.Transactions.IsolationLevel.Serializable))
                    {
                    }
                }
            });

        }

        [Fact]
        public void Connections_opened_by_transaction_should_close_when_transaction_is_disposed()
        {
            using (var cs = _scopeFactory.CreateConnectionScope<TestDbConnectionScope>())
            {
                using (var tx = cs.BeginTransactionScope())
                {
                    Assert.True(cs.Connection.State == System.Data.ConnectionState.Open, "Connection was expected to be open.");
                }
                Assert.True(cs.Connection.State == System.Data.ConnectionState.Closed, "Connection was expected to be closed.");
            }
        }

        [Fact]
        public void Connections_not_opened_by_transaction_should_not_close_when_transaction_is_disposed()
        {
            using (var cs = _scopeFactory.CreateConnectionScope<TestDbConnectionScope>())
            {
                cs.Connection.Open();
                using (var tx = cs.BeginTransactionScope())
                {
                }
                Assert.True(cs.Connection.State == System.Data.ConnectionState.Open, "Connection was expected to be open.");
            }
        }

        [Fact]
        public void Connections_created_by_transaction_should_dispose_when_transaction_is_disposed()
        {
            TestDbConnectionScope cs = null;
            using (var tx = CreateDefaultScope())
            {
                cs = tx.GetConnectionScope<TestDbConnectionScope>();
            }
            Assert.True(cs._disposed);
        }
    }
}
