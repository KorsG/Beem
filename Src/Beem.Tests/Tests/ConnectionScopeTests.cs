﻿/*
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
    public partial class ConnectionScopeTests : TestBase
    {
        private readonly DbScopeFactory _scopeFactory;

        public ConnectionScopeTests()
        {
            _scopeFactory = new DbScopeFactory();
        }

        [Fact]
        public void Can_create()
        {
            using (var cs = _scopeFactory.CreateConnectionScope<TestDbConnectionScope>())
            {
            }
        }

        [Fact]
        public void Can_begin_nested()
        {
            using (var cs1 = _scopeFactory.CreateConnectionScope<TestDbConnectionScope>())
            using (var cs2 = cs1.BeginNested() as DbConnectionScope)
            {
                Assert.False(cs2._isRootScope);
            }
        }
    }
}
