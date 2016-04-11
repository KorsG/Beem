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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beem.Exceptions;

namespace Beem
{
    public partial class DbTransactionScope : IDbTransactionScope
    {
        private readonly Dictionary<Type, DbConnectionScopeEnlistment> _dbConnectionScopeCache = new Dictionary<Type, DbConnectionScopeEnlistment>();

        private readonly List<NestedDbTransactionScope> _nestedScopes = new List<NestedDbTransactionScope>();

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        internal System.Transactions.CommittableTransaction _transaction { get; set; }

        private ICollection<Action> _commitActions { get; } = new List<Action>();

        private ICollection<Action> _rollbackActions { get; } = new List<Action>();

        public DbTransactionScope(DbConnectionScope dbConnectionScope, System.Transactions.IsolationLevel isolationLevel, int timeoutInSeconds = 30)
            : this(isolationLevel, timeoutInSeconds)
        {
            AddConnectionScopeEnlistment(dbConnectionScope, false);
        }

        public DbTransactionScope(System.Transactions.IsolationLevel isolationLevel, int timeoutInSeconds = 30)
        {
            _transaction = new System.Transactions.CommittableTransaction(
               new System.Transactions.TransactionOptions()
               {
                   IsolationLevel = isolationLevel,
                   Timeout = new TimeSpan(0, 0, timeoutInSeconds)
               });
        }

        public bool Completed { get; private set; }

        internal bool Aborted { get; private set; }

        private void AddConnectionScopeEnlistment(DbConnectionScope scope, bool createdByTransaction)
        {
            var connectionOpenedByTransaction = false;
            if (scope.Connection.State == System.Data.ConnectionState.Closed)
            {
                scope.Connection.Open();
                connectionOpenedByTransaction = true;
            }

            scope.Connection.EnlistTransaction(_transaction);

            var enlistment = new DbConnectionScopeEnlistment()
            {
                Scope = scope,
                ConnectionOpenedByTransaction = connectionOpenedByTransaction,
                CreatedByTransaction = createdByTransaction,
            };
            var type = scope.GetType();
            _dbConnectionScopeCache[type] = enlistment;
        }

        /// <inheritdoc />
        public IDbTransactionScope BeginNested()
        {
            ThrowIfHandled();
            var nestedScope = new NestedDbTransactionScope(this);
            _nestedScopes.Add(nestedScope);
            return nestedScope;
        }

        /// <inheritdoc />
        [DebuggerStepThrough()]
        public IDbTransactionScope BeginNested(System.Transactions.IsolationLevel isolationLevel)
        {
            ThrowIfHandled();
            // Validate isolationLevel
            if (isolationLevel != System.Transactions.IsolationLevel.Unspecified && isolationLevel != _transaction.IsolationLevel)
            {
                throw new DbTransactionScopeException(
                    $"Cannot start nested {nameof(DbTransactionScope)} because the parent was created with a different IsolationLevel ({_transaction.IsolationLevel.ToString()}) than the provided ({isolationLevel.ToString()}) which is not supported.");
            }
            return BeginNested();
        }

        public TScope GetConnectionScope<TScope>() where TScope : DbConnectionScope
        {
            ThrowIfHandled();

            var scopeType = typeof(TScope);
            return GetConnectionScope(scopeType) as TScope;
        }

        public DbConnectionScope GetConnectionScope(Type dbConnectionScopeType)
        {
            if (dbConnectionScopeType == null) { throw new ArgumentNullException(nameof(dbConnectionScopeType)); }
            ThrowIfHandled();

            if (!_dbConnectionScopeCache.ContainsKey(dbConnectionScopeType))
            {
                // First time we've been asked for this particular DbScope type, create one and cache it.
                var scope = DbScopeConfig.CreateConnectionScope(dbConnectionScopeType);
                scope.CurrentTransactionScope = this;
                AddConnectionScopeEnlistment(scope, true);
            }

            return _dbConnectionScopeCache[dbConnectionScopeType].Scope;
        }

        /// <inheritdoc />
        public IDbTransactionScope AddDisposable(IDisposable disposable)
        {
            if (disposable == null) { throw new ArgumentNullException(nameof(disposable)); }
            _disposables.Add(disposable);
            return this;
        }

        /// <inheritdoc />
        public IDbTransactionScope AddCommitAction(Action action)
        {
            ThrowIfHandled();
            if (action != null)
            {
                _commitActions.Add(action);
            }
            return this;
        }

        /// <inheritdoc />
        public IDbTransactionScope AddRollbackAction(Action action)
        {
            ThrowIfHandled();
            if (action != null)
            {
                _rollbackActions.Add(action);
            }
            return this;
        }

        public void Commit()
        {
            ThrowIfHandled();
            Completed = true;
            if (_nestedScopes.Any(x => x.Completed != true))
            {
                throw new DbTransactionScopeCommitException("TransactionScope has nested scopes that have not been completed.");
            }

            try
            {
                _transaction.Commit();
                try
                {
                    _transaction.Dispose();
                }
                catch (Exception ex)
                {
                    // Swallow any Dispose exception.
                    Debug.WriteLine($"Exception occured while trying to dispose the transaction after Commit:{Environment.NewLine}{ex.ToString()}");
                }
                _transaction = null;
            }
            catch (Exception ex)
            {
                // NOTE: Shouldn't RollbackActions be processed immediately ?
                // But keep in mind how a consumer would like to catch exception types...
                // Exception type should probably be different if its a commit error or a RollbackAction error.
                // Or perhaps there should just be 1 exception type (TransactionScopeException).
                // Otherwise combine commit exception with RollbackAction exceptions in an AggregateException ?
                throw new DbTransactionScopeCommitException(ex);
            }

            // Handle CommitActions. Process all even if any exceptions occur.
            var commitActionExceptions = new List<Exception>();
            foreach (var action in _commitActions)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    commitActionExceptions.Add(ex);
                }
            }
            if (commitActionExceptions.Any())
            {
                var aggregate = new AggregateException("1 or more Commit actions threw an exception.", commitActionExceptions);
                throw new DbTransactionScopeCommitException("1 or more Commit actions threw an exception - Check InnerException for details.", aggregate);
            }
        }

        public void Rollback()
        {
            ThrowIfHandled();
            InternalRollback();
        }

        private void InternalRollback()
        {
            Completed = true;
            Aborted = true;

            var rollbackExceptions = new List<Exception>();

            try
            {
                _transaction.Rollback();
            }
            catch (Exception ex)
            {
                rollbackExceptions.Add(ex);
            }
            finally
            {
                try
                {
                    _transaction.Dispose();
                }
                catch (Exception ex)
                {
                    // Swallow any Dispose exception.
                    Debug.WriteLine($"Exception occured while trying to dispose the transaction after Rollback:{Environment.NewLine}{ex.ToString()}");
                }
                _transaction = null;
            }

            // TODO: Should rollback actions be processed in parallel ?
            foreach (var action in _rollbackActions)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    rollbackExceptions.Add(ex);
                }
            }

            if (rollbackExceptions.Any())
            {
                var aggregate = new AggregateException("Exception(s) occured while attempting to Rollback", rollbackExceptions);
                throw new DbTransactionScopeRollbackException("Exception(s) occured while attempting to Rollback - Check InnerException for details", aggregate);
            }
        }

        [DebuggerStepThrough]
        private void ThrowIfHandled()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(DbTransactionScope));
            }
            if (Aborted)
            {
                throw new DbTransactionScopeAbortedException();
            }
            // TODO: Optimize the nested scope check - maybe have a NestedAborted property on the parent, that the Nested needs to set ?
            if (_nestedScopes.Any(x => x.Aborted == true))
            {
                throw new DbTransactionScopeAbortedException();
            }
            if (Completed)
            {
                throw new DbTransactionScopeCompletedException();
            }
        }

        private class DbConnectionScopeEnlistment
        {
            public DbConnectionScope Scope { get; set; }

            public bool ConnectionOpenedByTransaction { get; set; }

            public bool CreatedByTransaction { get; set; }
        }

        #region IDisposable Implementation

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_transaction != null)
                    {
                        try
                        {
                            // Do not catch any exceptions thrown by Rollback.
                            // Even though it is not "best practice" to throw exceptions from dispose,
                            // this is an exception because Dispose in this case also means "Rollback".
                            InternalRollback();
                        }
                        finally
                        {
                            Completed = true;
                            _transaction = null;
                        }
                    }

                    foreach (var enlistment in _dbConnectionScopeCache.Values)
                    {
                        try
                        {
                            if (enlistment.CreatedByTransaction)
                            {
                                if (enlistment.Scope != null)
                                {
                                    // Set CurrentTransactionScope(this) to null before disposing to prevent dispose loop.
                                    enlistment.Scope.CurrentTransactionScope = null;
                                    enlistment.Scope.Dispose();
                                }
                            }
                            else if (enlistment.ConnectionOpenedByTransaction)
                            {
                                if (enlistment.Scope?.Connection?.State != System.Data.ConnectionState.Closed)
                                {
                                    enlistment.Scope.Connection.Close();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }

                    foreach (var disposable in _disposables)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
