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
    public partial class DbTransactionScope
    {
        private class NestedDbTransactionScope : IDbTransactionScope
        {
            private readonly DbTransactionScope _parentScope;

            public NestedDbTransactionScope(DbTransactionScope parentScope)
            {
                if (parentScope == null) { throw new ArgumentNullException(nameof(parentScope)); }
                _parentScope = parentScope;
            }

            public bool Completed { get; private set; }

            internal bool Aborted { get; private set; }

            public IDbTransactionScope BeginNested()
            {
                ThrowIfHandled();
                return _parentScope.BeginNested();
            }

            public IDbTransactionScope BeginNested(System.Transactions.IsolationLevel isolationLevel)
            {
                ThrowIfHandled();
                return _parentScope.BeginNested(isolationLevel);
            }

            TScope IDbTransactionScope.GetConnectionScope<TScope>()
            {
                ThrowIfHandled();
                return _parentScope.GetConnectionScope<TScope>();
            }

            public DbConnectionScope GetConnectionScope(Type dbConnectionScopeType)
            {
                ThrowIfHandled();
                return _parentScope.GetConnectionScope(dbConnectionScopeType);
            }

            public IDbTransactionScope AddDisposable(IDisposable disposable)
            {
                ThrowIfHandled();
                return _parentScope.AddDisposable(disposable);
            }

            public IDbTransactionScope AddCommitAction(Action action)
            {
                ThrowIfHandled();
                _parentScope.AddCommitAction(action);
                return this;
            }

            public IDbTransactionScope AddRollbackAction(Action action)
            {
                ThrowIfHandled();
                _parentScope.AddRollbackAction(action);
                return this;
            }

            public void Commit()
            {
                ThrowIfHandled();
                Completed = true;
            }

            public void Rollback()
            {
                ThrowIfHandled();
                Completed = true;
                Aborted = true;
            }

            [DebuggerStepThrough]
            private void ThrowIfHandled()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(NestedDbTransactionScope));
                }
                if (Aborted)
                {
                    throw new DbTransactionScopeAbortedException();
                }
                if (Completed)
                {
                    throw new DbTransactionScopeCompletedException();
                }
            }

            #region IDisposable Implementation

            private bool _disposed = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    if (disposing)
                    {
                        if (!Completed)
                        {
                            Completed = true;
                            Aborted = true;
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
}
