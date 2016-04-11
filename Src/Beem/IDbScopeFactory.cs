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
    public interface IDbScopeFactory
    {
        /// <summary>
        ///     Create new instance of <typeparamref name="TDbConnectionScope" /> using a registered factory or a parameterless constructor.
        ///     If <paramref name="transaction"/> is provided then a nested <typeparamref name="TDbConnectionScope" /> is returned.
        /// </summary>
        /// <typeparam name="TDbConnectionScope"></typeparam>
        /// <param name="transaction"></param>
        /// <exception cref="InvalidOperationException">If no factory have been registered for <typeparamref name="TDbConnectionScope"/> and it has no parameterless constructor</exception>
        TDbConnectionScope CreateConnectionScope<TDbConnectionScope>(IDbTransactionScope transaction = null) where TDbConnectionScope :  DbConnectionScope;

        /// <summary>
        ///     Create new instance of <paramref name="dbConnectionScopeType"/> using a registered factory or a parameterless constructor.
        ///     If <paramref name="transaction"/> is provided then a nested <see cref="DbConnectionScope"/> is returned.
        /// </summary>
        /// <param name="dbConnectionScopeType"></param>
        /// <param name="transaction"></param>
        /// <exception cref="InvalidOperationException">If no factory have been registered for <paramref name="dbConnectionScopeType"/> and it has no parameterless constructor</exception>
        DbConnectionScope CreateConnectionScope(Type dbConnectionScopeType, IDbTransactionScope transaction = null);

        /// <summary>
        ///     Create an empty <see cref="IDbTransactionScope"/> with <see cref="DbScopeConfig.DefaultTransactionIsolationLevel"/>.
        /// </summary>
        IDbTransactionScope CreateTransactionScope();

        /// <summary>
        ///     Create an empty <see cref="IDbTransactionScope"/> with provided <paramref name="isolationLevel"/>. 
        /// </summary>
        /// <param name="isolationLevel"></param>
        IDbTransactionScope CreateTransactionScope(System.Transactions.IsolationLevel isolationLevel);

        /// <summary>
        ///     Create an empty or nested <see cref="IDbTransactionScope"/> with <see cref="DbScopeConfig.DefaultTransactionIsolationLevel"/> when creating empty, 
        ///     otherwise for nested the provided <paramref name="transactionScope"/>'s IsolationLevel is inherited.
        /// </summary>
        /// <param name="transactionScope">If null a new <see cref="IDbTransactionScope"/> is returned; otherwise nested.</param>
        IDbTransactionScope CreateTransactionScope(IDbTransactionScope transactionScope);

        /// <summary>
        ///     Create an empty or nested <see cref="IDbTransactionScope"/> with provided <paramref name="isolationLevel"/>.
        /// </summary>
        /// <param name="transactionScope">If null a new <see cref="IDbTransactionScope"/> is returned; otherwise nested.</param>
        /// <param name="isolationLevel"></param>
        /// <exception cref="DbTransactionScopeException">If the provided IsolationLevel is different than the provided <paramref name="transactionScope"/>'s IsolationLevel</exception>
        IDbTransactionScope CreateTransactionScope(IDbTransactionScope transactionScope, System.Transactions.IsolationLevel isolationLevel);
    }
}
