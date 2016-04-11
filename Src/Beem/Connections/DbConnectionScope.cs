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
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Beem
{
    public abstract class DbConnectionScope : IDisposable
    {
        private readonly DbConnection _connection;
        private IDbTransactionScope _currentTransactionScope;
        private readonly System.Transactions.IsolationLevel _defaultIsolationLevel;

        private DbConnectionScope _rootScope { get; set; }

        internal bool _isRootScope { get; set; }

        public DbConnection Connection => _rootScope._connection;

        internal IDbTransactionScope CurrentTransactionScope { get { return _rootScope._currentTransactionScope; } set { _rootScope._currentTransactionScope = value; } }

        private DbConnectionScope() { }

        public DbConnectionScope(DbConnection dbConnection)
        {
            if (dbConnection == null) { throw new ArgumentNullException(nameof(dbConnection)); }
            _rootScope = this;
            _isRootScope = true;
            _connection = dbConnection;
        }

        private void OpenConnection()
        {
            if (Connection.State != ConnectionState.Open)
            {
                Connection.Open();
            }
        }

        private void CloseConnection()
        {
            if (Connection.State != ConnectionState.Closed)
            {
                Connection.Close();
            }
        }

        public DbConnectionScope BeginNested()
        {
            var scope = DbScopeConfig.CreateConnectionScope(GetType()) as DbConnectionScope;
            scope._rootScope = this;
            scope._isRootScope = false;
            return scope;
        }

        /// <inheritdoc />
        public IDbTransactionScope BeginTransactionScope(int timeoutInSeconds = 30)
        {
            return BeginTransactionScope(DbScopeConfig.DefaultTransactionIsolationLevel, timeoutInSeconds);
        }

        /// <inheritdoc />
        public IDbTransactionScope BeginTransactionScope(System.Transactions.IsolationLevel isolationLevel, int timeoutInSeconds = 30)
        {
            if (CurrentTransactionScope == null || CurrentTransactionScope.Completed)
            {
                return CurrentTransactionScope = new DbTransactionScope(this, isolationLevel, timeoutInSeconds);
            }
            else
            {
                return CurrentTransactionScope.BeginNested(isolationLevel);
            }
        }

        #region IDisposable Implementation

        internal bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Only dispose if current scope is "root".
                    if (_isRootScope == true)
                    {
                        if (CurrentTransactionScope != null)
                        {
                            CurrentTransactionScope.Dispose();
                            CurrentTransactionScope = null;
                        }
                        if (Connection != null)
                        {
                            CloseConnection();
                            Connection?.Dispose();
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
