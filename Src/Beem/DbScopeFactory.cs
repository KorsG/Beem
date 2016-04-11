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
using System.Threading.Tasks;
using Beem.Exceptions;

namespace Beem
{
    public class DbScopeFactory : IDbScopeFactory
    {
        /// <summary>
        ///     Create new instance of <typeparamref name="TDbConnectionScope" /> using a registered factory or a parameterless constructor.
        ///     If <paramref name="transaction"/> is provided then a nested <typeparamref name="TDbConnectionScope" /> is returned.
        /// </summary>
        /// <typeparam name="TDbConnectionScope"></typeparam>
        /// <param name="transaction"></param>
        /// <exception cref="MissingMethodException">
        ///     If no factory have been registered for <typeparamref name="TDbConnectionScope"/> and it has no parameterless constructor
        /// </exception>
        public TDbConnectionScope CreateConnectionScope<TDbConnectionScope>(IDbTransactionScope transaction = null) where TDbConnectionScope : DbConnectionScope
        {
            return (TDbConnectionScope)CreateConnectionScope(typeof(TDbConnectionScope), transaction);
        }

        /// <summary>
        ///     Create new instance of <paramref name="dbConnectionScopeType"/> using a registered factory or a parameterless constructor.
        ///     If <paramref name="transaction"/> is provided then a nested <see cref="DbConnectionScope"/> is returned.
        /// </summary>
        /// <param name="dbConnectionScopeType"></param>
        /// <param name="transaction"></param>
        /// <exception cref="ArgumentNullException">
        ///     If <paramref name="dbConnectionScopeType"/> is null.
        /// </exception>
        /// <exception cref="MissingMethodException">
        ///     If no factory have been registered for <paramref name="dbConnectionScopeType"/> and it has no parameterless constructor.
        /// </exception>
        public DbConnectionScope CreateConnectionScope(Type dbConnectionScopeType, IDbTransactionScope transaction = null)
        {
            if (dbConnectionScopeType == null) { throw new ArgumentNullException(nameof(dbConnectionScopeType)); }

            if (transaction == null)
            {
                return DbScopeConfig.CreateConnectionScope(dbConnectionScopeType);
            }
            else
            {
                return transaction.GetConnectionScope(dbConnectionScopeType).BeginNested();
            }
        }

        /// <summary>
        ///     Create an empty <see cref="IDbTransactionScope"/> with <see cref="DbScopeConfig.DefaultTransactionIsolationLevel"/>.
        /// </summary>
        public IDbTransactionScope CreateTransactionScope()
        {
            return CreateTransactionScope(DbScopeConfig.DefaultTransactionIsolationLevel);
        }

        /// <summary>
        ///     Create an empty <see cref="IDbTransactionScope"/> with provided <paramref name="isolationLevel"/>. 
        /// </summary>
        /// <param name="isolationLevel"></param>
        public IDbTransactionScope CreateTransactionScope(System.Transactions.IsolationLevel isolationLevel)
        {
            return new DbTransactionScope(isolationLevel);
        }

        /// <summary>
        ///     Create an empty or nested <see cref="IDbTransactionScope"/> with <see cref="DbScopeConfig.DefaultTransactionIsolationLevel"/> when creating empty, 
        ///     otherwise for nested the provided <paramref name="transactionScope"/>'s IsolationLevel is inherited.
        /// </summary>
        /// <param name="transactionScope">
        ///     If null a new <see cref="IDbTransactionScope"/> is returned; otherwise nested.
        /// </param>
        public IDbTransactionScope CreateTransactionScope(IDbTransactionScope transactionScope)
        {
            if (transactionScope == null)
            {
                return new DbTransactionScope(DbScopeConfig.DefaultTransactionIsolationLevel);
            }
            else
            {
                return transactionScope.BeginNested();
            }
        }

        /// <summary>
        ///     Create an empty or nested <see cref="IDbTransactionScope"/> with provided <paramref name="isolationLevel"/>.
        /// </summary>
        /// <param name="transactionScope">If null a new <see cref="IDbTransactionScope"/> is returned; otherwise nested.</param>
        /// <param name="isolationLevel"></param>
        /// <exception cref="DbTransactionScopeException">
        ///     If the provided IsolationLevel is different than the provided <paramref name="transactionScope"/>'s IsolationLevel.
        /// </exception>
        public IDbTransactionScope CreateTransactionScope(IDbTransactionScope transactionScope, System.Transactions.IsolationLevel isolationLevel)
        {
            if (transactionScope == null)
            {
                return new DbTransactionScope(isolationLevel);
            }
            else
            {
                return transactionScope.BeginNested(isolationLevel);
            }
        }
    }
}
