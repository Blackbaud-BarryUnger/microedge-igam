using System;
using System.Data;
using System.Threading;
using System.Transactions;
using MicroEdge.Igam.Providers.Dal;

namespace MicroEdge.Igam.Data
{
	/// <summary>
	/// Represents a local (non-distributed) transaction.
	/// </summary>
	/// <remarks>
	///  Author:    LDF
	///  Created:   05/27/2007
	/// </remarks>
	public sealed class TransactionScope : ITransactionScope
	{
		#region Fields

		// The connection string used to access the database in the transaction.
		private string _connectionString;

		// The database connection participating in the transaction.
		private IDbConnection _connection;

		// The TransactionScope object participating in the transaction. This is used for all but Access.
		private System.Transactions.TransactionScope _scope;

		//The IDbTransaction object participating in the transaction. This is used only for Access.
		private IDbTransaction _transaction;

		//Value indicating if the current transaction is nested.
		private bool _isNested;

		#endregion Fields

		#region Constructors

	    /// <summary>
	    /// Initialize a new instance of the TransactionScope class with the specified isolation level and 
	    /// connection string.
	    /// </summary>
	    /// <param name="option">
	    /// TransactionScopeOption to use with this TransactionScope. Note this is ignored for Access databases since transactions are not accomplished
	    /// in Access using TransactionScope.
	    /// </param>
	    /// <param name="data">
	    /// The MicroEdge.Igam.Data object that indicates the database on which to start the transaction.
	    /// </param>
	    /// <param name="isolationLevel"></param>
	    public TransactionScope(Data data, TransactionScopeOption option, int? isolationLevel)
		{
			//Only use TransactionScope if not Access.
			if (isolationLevel == null)
				_scope = new System.Transactions.TransactionScope(option);
			else
			{
				_scope = new System.Transactions.TransactionScope(option, new TransactionOptions { IsolationLevel = Tools.ToEnum<System.Transactions.IsolationLevel>(isolationLevel) });
			}

			if (Current == null || Current.ConnectionString != data.ConnectionString)
			{
				//Either this is the first connection or the default connection has changed. Create a new connection and store it in global thread storage.
				_connection = data.GetConnection();
				_connection.Open();
				_connectionString = data.ConnectionString;
				Thread.SetData(Thread.GetNamedDataSlot("TransactionScope"), this);
			}
			else
			{
				_connection = Current.Connection;
				_isNested = true;
			}
		}
		public TransactionScope(Data data, TransactionScopeOption option)
			: this(data, option, null)
		{ }
		public TransactionScope(TransactionScopeOption option) 
			: this(Data.Current, option)
		{ }
		public TransactionScope(Data data) 
			: this(data, TransactionScopeOption.Required)
		{ }
		public TransactionScope() 
			: this(Data.Current, TransactionScopeOption.Required)
		{ }

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Gets the IDbConnection used by this transaction.
		/// </summary>
		public IDbConnection Connection
		{
			get { return _connection; }
		}

		/// <summary>
		/// Gets the connection string used to access the database in the transaction.
		/// </summary>
		public string ConnectionString
		{
			get { return _connectionString; }
		}

		/// <summary>
		/// The underlying System TransactionScope object. This is only defined if not Access.
		/// </summary>
		public System.Transactions.TransactionScope Scope
		{
			get { return _scope; }
		}

		/// <summary>
		/// The underlying IDbTransaction object. This is only defined if Access.
		/// </summary>
		public IDbTransaction Transaction
		{
			get { return _transaction; }
		}

		/// <summary>
		/// Gets the current transaction associated with the current thread of execution.
		/// </summary>
		public static TransactionScope Current
		{
			get { return (TransactionScope)Thread.GetData(Thread.GetNamedDataSlot("TransactionScope")); }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Commits the transaction.
		/// </summary>
		public void Complete()
		{
			_transaction.Commit();
			_transaction.Dispose();
			_transaction = null;
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the transaction is reclaimed by garbage collection.
		/// </summary>
		~TransactionScope()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases all resources used by the transaction.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases all resources used by this transaction. Don't release the connection unless this is a
		/// root transaction.
		/// </summary>
		/// <param name="disposing">
		/// True to release both managed and unmanaged resources; 
		/// False to release only unmanaged resources.
		/// </param>
		private void Dispose(bool disposing)
		{
		    if (!disposing)
                return;

		    _scope.Dispose();

		    if (_isNested)
                return;

		    _connection.Dispose();
		    Thread.SetData(Thread.GetNamedDataSlot("TransactionScope"), null);
		}

		#endregion Methods
	}
}
