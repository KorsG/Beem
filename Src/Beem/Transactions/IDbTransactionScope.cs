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
    public interface IDbTransactionScope : IDisposable
    {
        bool Completed { get; }

        /// <summary>
        ///     Begin nested <see cref="IDbTransactionScope"/>.
        /// </summary>
        IDbTransactionScope BeginNested();

        /// <summary>
        ///     Begin nested <see cref="IDbTransactionScope"/> with provided <paramref name="isolationLevel"/>.
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <exception cref="DbTransactionScopeException">
        ///     If the provided IsolationLevel is different than the parent <see cref="IDbTransactionScope"/>'s IsolationLevel
        /// </exception>
        IDbTransactionScope BeginNested(System.Transactions.IsolationLevel isolationLevel);

        TDbConnectionScope GetConnectionScope<TDbConnectionScope>() where TDbConnectionScope : DbConnectionScope;

        DbConnectionScope GetConnectionScope(Type dbConnectionScopeType);

        /// <summary>
        ///     Add a <paramref name="disposable"/> that will be disposed at TransactionScope disposal.
        /// </summary>
        /// <param name="disposable"></param>
        /// <exception cref="ArgumentNullException"></exception>
        IDbTransactionScope AddDisposable(IDisposable disposable);

        /// <summary>
        ///     Add an <see cref="Action"/> to be executed after the database transaction has been committed successfully.
        /// </summary>
        /// <param name="action"></param>
        IDbTransactionScope AddCommitAction(Action action);

        /// <summary>
        ///     Add an <see cref="Action"/> to be executed after the database transaction has been rolledback.
        /// </summary>
        /// <param name="action"></param>
        IDbTransactionScope AddRollbackAction(Action action);

        void Commit();

        void Rollback();
    }
}
