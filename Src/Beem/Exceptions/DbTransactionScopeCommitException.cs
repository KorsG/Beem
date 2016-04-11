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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Beem.Exceptions
{
    [Serializable]
    public class DbTransactionScopeCommitException : DbTransactionScopeException
    {
        private static string _defaultMessage = $"Exception(s) occurred while attempting to Commit {nameof(DbTransactionScope)}";

        private static string _defaultMessageWithException = $"{_defaultMessage} - Check InnerException for details.";

        public DbTransactionScopeCommitException()
            : base(_defaultMessage)
        { }

        public DbTransactionScopeCommitException(string message)
            : base(message)
        { }

        public DbTransactionScopeCommitException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public DbTransactionScopeCommitException(Exception innerException)
            : base(innerException == null ? _defaultMessage : _defaultMessageWithException, innerException)
        { }

        protected DbTransactionScopeCommitException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
