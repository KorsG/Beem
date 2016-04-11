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
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace Beem.Tests
{
    public class TestDbConnectionScope : DbConnectionScope
    {
        public TestDbConnectionScope()
            : this(new SqlConnection(TestConstants.TempDbConnectionString))
        {
        }
        public TestDbConnectionScope(string connectionString)
            : this(new SqlConnection(connectionString))
        {
        }

        public TestDbConnectionScope(DbConnection dbConnection) 
            : base(dbConnection)
        {
        }
    }
}
