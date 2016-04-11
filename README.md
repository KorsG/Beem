Beem
====================

Database connection and transaction framework for .NET

## Key features

- Pass transaction to service/repository/request methods etc.
- Start nested transaction scopes.
- Execute actions on Commit/Rollback/Dispose.
- Explicit alternative to the ambient System.Transactions.TransactionScope.

## Concepts

- DbConnectionScope
- DbTransactionScope
- DbScopeFactory
- DbScopeConfig

### DbConnectionScope

- Defines a database connection (abstract).
- Wraps a System.Data.DbConnection.
- Primarily used by DbTransactionScope to make sure that only 1 connection of this type is part of the transaction.
    - *Help's prevent unwanted escalation to DTC if not required.*

### DbTransactionScope

- Defines a database transaction.
- Can contain multiple database connections in the form of DbConnectionScope's.
- Add actions to execute on Commit/Rollback/Dispose.
- Start nested scopes.
- Get or create a DbConnectionScope type's.
    - *If the DbConnectionScope type is not already part of the DbTransactionScope then it will be created via a parameterless constructor or via a registered factory in DbScopeConfig and added.*

### DbScopeFactory

- Factory to create DbConnectionScope's and DbTransactionScope's.
- Makes it easier to create new or nested Scopes.

### DbScopeConfig

- Register custom factories for DbConnectionScope's.
- Set default Transaction Isolation Level.
- Static.

## Installation

Install via [NuGet](https://www.nuget.org/packages/Beem):

    Install-Package Beem

## Example

There are multiple ways to use Beem depending on your requirements/coding style, so the following example is just that... an example! :-)

### Step 1: Create DbConnectionScope type

- DbConnectionScope requires a System.Data.Common.DbConnectionScope in the base constructor.
- If you provide a parameterless contructor you do not need to register a factory in DbScopeConfig.

```csharp
namespace SomeApp
{
    using Beem;

    public class AppDbConnectionScope : DbConnectionScope
    {
        public AppDbConnectionScope()
            : base(new System.Data.SqlClient.SqlConnection(@"Server=(localdb)\mssqllocaldb; Database=tempdb; Trusted_Connection=true;"))
        {
        }
        
        public AppDbConnectionScope(string connectionString)
            : base(new System.Data.SqlClient.SqlConnection(connectionString))
        {
        }
            
        public AppDbConnectionScope(System.Data.Common.DbConnection dbConnection)
            : base(dbConnection)
        {
        }
    }
}
```

### Step 2: Create a factory that inherits DbScopeFactory to create your custom DbConnectionScope types

- Optional but recommended - Beem.DbScopeFactory is useable as-is.
- Also a good place to create e.g. EF DbContext's.
    - *EF requires a few simple configuration steps which is outlined below. (EF6)*

```csharp
namespace SomeApp
{
    using Beem;

    public class DbFactory : DbScopeFactory
    {
        public AppDbConnectionScope CreateAppConnectionScope(IDbTransactionScope transactionScope = null)
        {
            return base.CreateConnectionScope<AppDbConnectionScope>(transactionScope);
        }

        public AppDbContext CreateAppDbContext()
        {
            return new AppDbContext(CreateAppConnectionScope(), contextOwnsConnectionScope: true);
        }

        public AppDbContext CreateAppDbContext(IDbTransactionScope transactionScope, bool disposeWithTransaction = false)
        {
            if (transactionScope == null)
            {
                return CreateAppDbContext();
            }
            else
            {
                var ctx = new AppDbContext(CreateAppConnectionScope(transactionScope), contextOwnsDbConnectionScope: false);
                if (disposeWithTransaction)
                {
                    transactionScope.AddDisposable(ctx);
                }
                return ctx;
            }
        }
    }
        
    // EF DbContext "skeleton" setup.
    public class AppDbContext : System.Data.Entity.DbContext
    {
        private readonly DbConnectionScope _dbConnectionScope;
        private readonly bool _contextOwnsDbConnectionScope;

        public AppDbContext(DbConnectionScope dbConnectionScope, bool contextOwnsDbConnectionScope = false)
            : base(dbConnectionScope.Connection, contextOwnsConnection: false)
        {
            _dbConnectionScope = dbConnectionScope;
            _contextOwnsDbConnectionScope = contextOwnsDbConnectionScope;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _contextOwnsDbConnectionScope)
            {
                _dbConnectionScope?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
```

### Step 3: Start using Beem.

```csharp
namespace SomeApp
{
    using Beem;

    public class Program
    {
        // DbFactory from Step 2.
        private static readonly DbFactory _dbFactory = new DbFactory();
        
        static void Main(string[] args)
        {           
            using (var tx = _dbFactory.CreateTransactionScope())
            {
                tx.AddRollbackAction(() => Console.WriteLine("Transaction rolledback!"));
                
                // By providing tx it is now clear that the operations will be part of the transactionScope and supports rollback.
                CreateCustomer("Some customer", tx);
                CreateContact("Some contact", tx);
                
                // Create "outside" transactionScope.
                CreateContact("Another contact");
                    
            } // Dispose will execute rollback if not committed.
        }
        
        // E.g. service method.
        public static void CreateCustomer(string name, IDbTransactionScope transactionScope = null)
        {
            using (var cs = _dbFactory.CreateAppConnectionScope(transactionScope))
            {
                // Use cs.Connection to Create a Customer.
            }
        }
        
        // E.g. service method.
        public static void CreateContact(string name, IDbTransactionScope transactionScope = null)
        {
            // Perform the following operations in a transaction regardless if transactionScope is provided.
            using (var cs = _dbFactory.CreateAppConnectionScope(transactionScope))
            {
                // If transactionScope is provided the following will begin a "nested" transaction.
                using (var tx = cs.BeginTransactionScope())
                {
                    tx.AddRollbackAction(() => Console.WriteLine("Contact not created because transaction was rolledback!"));
                    tx.AddCommitAction(() => Console.WriteLine("Contact committed to database!"));
                
                    // Use cs.Connection to Create a Contact and e.g and Log entry in the database that must be part of the same transaction.
                        
                    tx.Commit();
                }
            }
        }                
    }   
}
```

