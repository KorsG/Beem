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

namespace Beem
{
    public static class DbScopeConfig
    {
        internal static readonly Dictionary<Type, Func<DbConnectionScope>> _connectionScopeFactoryRegistrations = new Dictionary<Type, Func<DbConnectionScope>>();

        /// <summary>
        ///     Get or set the default <see cref="System.Transactions.IsolationLevel"/>.
        ///     The initial default is ReadCommitted.
        /// </summary>
        public static System.Transactions.IsolationLevel DefaultTransactionIsolationLevel { get; set; } = System.Transactions.IsolationLevel.ReadCommitted;

        public static void RegisterConnectionFactory<TScope>(Func<TScope> factory) where TScope : DbConnectionScope
        {
            if (factory == null) { throw new ArgumentNullException(nameof(factory)); }
            var scopeType = typeof(TScope);
            _connectionScopeFactoryRegistrations[scopeType] = factory;
        }

        /// <summary>
        ///     Create new instance of <typeparamref name="TDbConnectionScope" /> using a registered factory or a parameterless constructor.
        /// </summary>
        /// <typeparam name="TDbConnectionScope"></typeparam>
        /// <exception cref="MissingMethodException">
        ///     If no factory have been registered for <typeparamref name="TDbConnectionScope"/> and it has no parameterless constructor.
        /// </exception>
        public static TDbConnectionScope CreateConnectionScope<TDbConnectionScope>() where TDbConnectionScope : DbConnectionScope
        {
            return (TDbConnectionScope)CreateConnectionScope(typeof(TDbConnectionScope));
        }

        /// <summary>
        ///     Create new instance of <paramref name="dbConnectionScopeType"/> using a registered factory or a parameterless constructor.
        /// </summary>
        /// <param name="dbConnectionScopeType"></param>
        /// <exception cref="ArgumentNullException">
        ///     If <paramref name="dbConnectionScopeType"/> is null.
        /// </exception>
        /// <exception cref="MissingMethodException">
        ///     If no factory have been registered for <paramref name="dbConnectionScopeType"/> and it has no parameterless constructor.
        /// </exception>
        public static DbConnectionScope CreateConnectionScope(Type dbConnectionScopeType)
        {
            if (dbConnectionScopeType == null) { throw new ArgumentNullException(nameof(dbConnectionScopeType)); }

            Func<DbConnectionScope> factory;
            if (_connectionScopeFactoryRegistrations.TryGetValue(dbConnectionScopeType, out factory))
            {
                return factory.Invoke();
            }
            else
            {
                try
                {
                    return Activator.CreateInstance(dbConnectionScopeType, nonPublic: true) as DbConnectionScope;
                }
                catch (MissingMethodException ex)
                {
                    throw new MissingMethodException(
                         $"The type: {dbConnectionScopeType.FullName}' does not have a parameterless constructor and no factory for this type is registered in {nameof(DbScopeConfig)}.", ex);
                }
            }
        }
    }
}
