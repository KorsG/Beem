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
    public class TransactionScopeActionTests : TestBase
    {
        private readonly DbScopeFactory _scopeFactory;

        public TransactionScopeActionTests()
        {
            _scopeFactory = new DbScopeFactory();
        }

        private DbTransactionScope CreateDefaultScope(System.Transactions.IsolationLevel isolationLevel = System.Transactions.IsolationLevel.ReadCommitted)
        {
            return new DbTransactionScope(isolationLevel);
        }

        [Fact]
        public void Exception_in_commit_action_does_not_break_other_actions_and_throws_commit_exception_with_aggregate_inner()
        {
            var tx1Commited = false;
            var ex = Assert.Throws<DbTransactionScopeCommitException>(() =>
            {
                using (var tx1 = CreateDefaultScope())
                {
                    using (var tx2 = tx1.BeginNested())
                    {
                        tx2.AddCommitAction(() =>
                        {
                            throw new Exception("Should not break other actions.");
                        });
                        tx2.Commit();
                    }

                    tx1.AddCommitAction(() =>
                    {
                        tx1Commited = true;
                    });
                    tx1.Commit();
                }
            });
            Assert.True(tx1Commited);
            Assert.NotNull(ex.InnerException);
            Assert.IsType<AggregateException>(ex.InnerException);
        }

        [Fact]
        public void Exception_in_rollback_action_does_not_break_other_actions_and_throws_rollback_exception_with_aggregate_inner()
        {
            var tx1Rollbacked = false;
            var ex = Assert.Throws<DbTransactionScopeRollbackException>(() =>
            {
                using (var tx1 = CreateDefaultScope())
                {
                    tx1.AddRollbackAction(() =>
                    {
                        tx1Rollbacked = true;
                    });
                    using (var tx2 = tx1.BeginNested())
                    {
                        tx2.AddRollbackAction(() =>
                        {
                            throw new Exception("Should not break other actions.");
                        });
                    }
                }
            });
            Assert.True(tx1Rollbacked);
            Assert.NotNull(ex.InnerException);
            Assert.IsType<AggregateException>(ex.InnerException);
        }

        [Fact]
        public void Commit_with_uncompleted_nested_transactions_will_execute_rollback_and_actions()
        {
            var rollbackActionProcessed = false;
            Assert.Throws<DbTransactionScopeCommitException>(() =>
            {
                using (var tx1 = CreateDefaultScope())
                using (var tx2 = tx1.BeginNested())
                {
                    tx1.AddRollbackAction(() => rollbackActionProcessed = true);
                    tx1.Commit();
                    Assert.True(tx1.Aborted);
                }
            });
            Assert.True(rollbackActionProcessed);
        }

        [Fact]
        public void Commit_sql_exception_will_execute_rollback_actions()
        {
            var rollbackActionProcessed = false;
            Assert.Throws<DbTransactionScopeCommitException>(() =>
            {
                using (var tx1 = CreateDefaultScope())
                using (var cs = tx1.GetConnectionScope<TestDbConnectionScope>())
                {
                    tx1.AddRollbackAction(() => rollbackActionProcessed = true);
                    using (var cmd = cs.Connection.CreateCommand())
                    {
                        cmd.CommandText = "ROLLBACK TRAN;";
                        cmd.ExecuteNonQuery();
                    }
                    tx1.Commit();
                }
            });
            Assert.True(rollbackActionProcessed);
        }

        [Fact]
        public void Disposables_will_be_disposed_on_dispose()
        {
            var disposable1 = new GenericDisposable();
            var disposable2 = new GenericDisposable();
            try
            {
                using (var tx1 = CreateDefaultScope())
                {
                    tx1.AddDisposable(disposable1);
                    tx1.AddDisposable(disposable2);
                }
                Assert.True(disposable1.Disposed);
                Assert.True(disposable2.Disposed);
            }
            finally
            {
                disposable1?.Dispose();
                disposable2?.Dispose();
            }
        }

        private class GenericDisposable : IDisposable
        {
            public bool Disposed = false;

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
