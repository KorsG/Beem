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
    public class DbScopeConfigTests : TestBase
    {
        private readonly DbScopeFactory _scopeFactory;

        public DbScopeConfigTests()
        {
            _scopeFactory = new DbScopeFactory();
        }

        [Fact]
        public void Can_register_connectionScopeFactory()
        {
            var scopeType = typeof(TestDbConnectionScope);
            DbScopeConfig.RegisterConnectionFactory(() => new TestDbConnectionScope());
            Assert.True(DbScopeConfig._connectionScopeFactoryRegistrations.ContainsKey(scopeType));
        }

        [Fact]
        public void CreateConnectionScope_with_no_factory_throws_if_no_parameterless_ctor()
        {
            Assert.Throws<MissingMethodException>(() =>
            {
                using (var scope = _scopeFactory.CreateConnectionScope<ConnectionScope_No_Parameterless_Constructor>()) { };
            });
        }

        [Fact]
        public void CreateConnectionScope_with_no_factory_can_create_via_public_parameterless_ctor()
        {
            using (var scope = _scopeFactory.CreateConnectionScope<ConnectionScope_Public_Parameterless_Constructor>()) { };
        }

        [Fact]
        public void CreateConnectionScope_with_no_factory_can_create_via_private_parameterless_ctor()
        {
            using (var scope = _scopeFactory.CreateConnectionScope<ConnectionScope_Private_Parameterless_Constructor>()) { };
        }

        public class ConnectionScope_No_Parameterless_Constructor : DbConnectionScope
        {
            public ConnectionScope_No_Parameterless_Constructor(string value)
               : base(new SqlConnection(TestConstants.TempDbConnectionString))
            {
            }
        }

        public class ConnectionScope_Public_Parameterless_Constructor : DbConnectionScope
        {
            public ConnectionScope_Public_Parameterless_Constructor()
               : base(new SqlConnection(TestConstants.TempDbConnectionString))
            {
            }
        }

        public class ConnectionScope_Private_Parameterless_Constructor : DbConnectionScope
        {
            public ConnectionScope_Private_Parameterless_Constructor()
               : base(new SqlConnection(TestConstants.TempDbConnectionString))
            {
            }
        }
    }
}
