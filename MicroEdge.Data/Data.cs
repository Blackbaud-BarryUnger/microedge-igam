using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Transactions;

namespace MicroEdge.Igam.Data
{
	#region Enumerated Constants

	public enum CommandFunction
	{
		ExecuteNonQuery,
		ExecuteReader,
		ExecuteScalar
	}

	//Standard return values.
	public enum ReturnStatus
	{
		Success = 0,
		NotFound = 1,
	}

	public enum SqlFunction
	{
		Year = 0,
		Quarter = 1,
		Month = 2,
		Average = 3,
		Max = 4,
		Min = 5,
		Sum = 6,
		Concat = 7,
		ReplaceNull = 8,
		Count = 9,
		If = 10,
		FiscalYear = 11,
		ToDecimal = 12
	}

	#endregion Enumerated Constants

	/// <summary>
	/// This is a helper class to facilitate access to a single database connection.
	/// </summary>
	/// <remarks>
	///  Author:    LDF
	///  Created:   08/02/2006
	/// </remarks>
	[Serializable]
	public class Data
	{
		#region Constants

		public const string ResizeSql = "ALTER TABLE {0} MODIFY {1}";
		public const string DropColumnSql = "ALTER TABLE {0} DROP COLUMN {1}";

		#endregion Constants


		#region Fields

		//Data connection access parameters.
	    // ReSharper disable InconsistentNaming
		protected string _connectionString = "";
		protected string _userId = "";
        // ReSharper restore InconsistentNaming

        //Key used to hold the current data connection in thread storage.
        private const string ThreadDataKey = "LocalMicroEdgeData";

		//Key used to hold the Foundation Power connection in thread storage.
		private const string ThreadFpDataKey = "LocalFoundationPowerData";


		#endregion Fields

		#region Constructors

		/// <summary>
		/// Constructor to initialize DB type and connection string using parts of the connection string.
		/// </summary>
		/// <param name="provider">
		/// Database connection provider to use.  If empty/null; default for the db type will be used
		/// </param>
		/// <param name="dataSource">
		/// The data source of the connect string.
		/// </param>
		/// <param name="userId">
		/// The user ID of the connect string.
		/// </param>
		/// <param name="password">
		/// The user password of the connect string.
		/// </param>
		/// <param name="databaseName">
		/// The name of the database to connect to for a MS SQL server.
		/// </param>
		/// <param name="commandTimeout">
		/// Timeout to use for database commands created by this instance
		/// </param>
		public Data(string provider, string dataSource, string userId, string password, string databaseName, int commandTimeout)
		{
			Initialize(provider, dataSource, userId, password, databaseName, commandTimeout);
		}

		/// <summary>
		/// Constructor to initialize DB type and connection string passing the entire connection string.
		/// </summary>
		/// <param name="connectionString">
		/// The connection string used to access the database.
		/// </param>
		/// <param name="commandTimeout">
		/// Timeout to use for database commands created by this instance
		/// </param>
		public Data(string connectionString, int commandTimeout)
		{
			Initialize(connectionString, commandTimeout);
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Connection parameters.
		/// </summary>
		public int CommandTimeout { get; private set; }
		public string ConnectionString { get { return _connectionString; } }
		public string UserId { get { return _userId; } }

		/// <summary>
		/// Gets the current MicroEdge.Igam.Data object associated with the current thread of execution.
		/// </summary>
		public static Data Current
		{
			get { return Thread.GetData(Thread.GetNamedDataSlot(ThreadDataKey)) as Data; }
			set { Thread.SetData(Thread.GetNamedDataSlot(ThreadDataKey), value); }
		}

		/// <summary>
		/// Gets the current FoundationPwer.Data object associated with the current thread of execution.
		/// </summary>
		public static Data FoundationPowerCurrent
		{
			get { return Thread.GetData(Thread.GetNamedDataSlot(ThreadFpDataKey)) as Data; }
			set { Thread.SetData(Thread.GetNamedDataSlot(ThreadFpDataKey), value); }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Adds a column to an existing table
		/// </summary>
		/// <param name="tableName">
		/// Name of the table
		/// </param>
		/// <param name="columnDefinition">
		/// Information about the column to add
		/// </param>
		/// <param name="checkForExistence">
		/// Whether or not we want to check for an existing column with the same name and, if 
		/// one is found, skip creation
		/// </param>
		public void CreateColumn(string tableName, ColumnDefinition columnDefinition, bool checkForExistence)
		{
			if (!checkForExistence || !SqlTools.ColumnExists(tableName, columnDefinition.Name))
			{
				Current.ExecuteCommand(string.Format("ALTER TABLE {0} ADD {1}", tableName,
					SqlTools.GetColumnSql(columnDefinition)));
			}
		}
		/// <summary>
		/// Adds a column to an existing table
		/// </summary>
		/// <param name="tableName">
		/// Name of the table
		/// </param>
		/// <param name="columnName">
		/// Name of the column to add
		/// </param>
		/// <param name="type">
		/// System.Type of the column to add (will be converted to OleDbType)
		/// </param>
		/// <param name="size">
		/// Size of the column (needed for text/memo fields)
		/// </param>
		/// <param name="checkForExistence">
		/// Whether or not we want to check for an existing column with the same name and, if 
		/// one is found, skip creation
		/// </param>
		public void CreateColumn(string tableName, string columnName, Type type, int? size, bool checkForExistence)
		{ CreateColumn(tableName, new ColumnDefinition(columnName, type, size), checkForExistence); }
		public void CreateColumn(string tableName, string columnName, Type type, int? size)
		{ CreateColumn(tableName, columnName, type, size, true); }

		/// <summary>
		/// Centralized logic for creating a table regardless of backend.  Will
		/// check for the table's existence before trying to create
		/// </summary>
		/// <param name="tableName"></param>
		/// <param name="columns"></param>
		public void CreateTable(string tableName, IEnumerable<ColumnDefinition> columns)
		{
			//If the table already exists, nothing to do
			if (!SqlTools.TableExists(tableName))
				SqlTools.CreateTable(tableName, columns);
		}

		/// <summary>
		/// Tests for existence before trying to create create a primary
		/// and/or unique index on a single column in a table.  
		/// </summary>
		public void CreateIndex(string tableName, string columnName, bool isPrimary, bool isUnique)
		{
			if (string.IsNullOrEmpty(columnName))
				throw new ArgumentNullException(nameof(columnName), "Column name is required.");

			CreateIndex(tableName, new[] { columnName }, isPrimary, isUnique);
		}

		/// <summary>
		/// Tests for existence before trying to create create a primary
		/// and/or unique index in a table.  
		/// </summary>
		/// <param name="tableName">
		/// Name of the table
		/// </param>
		/// <param name="columnNames">
		/// Names of the columns involved in the index
		/// </param>
		/// <param name="isPrimary">
		/// Is the index a primary key?
		/// </param>
		/// <param name="isUnique">
		/// Is the index unique
		/// </param>
		public void CreateIndex(string tableName, IEnumerable<string> columnNames, bool isPrimary, bool isUnique)
		{
			if (string.IsNullOrEmpty(tableName))
				throw new ArgumentNullException(nameof(tableName), "Table name is required.");

			if (columnNames == null)
				throw new ArgumentNullException(nameof(columnNames), "At least one column name is required.");

			IList<string> nameList = columnNames as IList<string> ?? columnNames.ToList();
			if (!nameList.Any())
				throw new ArgumentException("At least one column name is required.");

			string indexName = string.Join("_", nameList);

			//Don't try to create if it already exists.
			if (!SqlTools.IndexExists(tableName, indexName))
				SqlTools.CreateIndex(tableName, SqlTools.EscapeName(indexName), nameList.Select(SqlTools.EscapeName), isUnique, isPrimary);
		}

		/// <summary>
		/// Initialize the connection for the database.
		/// </summary>
		/// <param name="connectionString">
		/// The connection string to use to access the database to which to initialize.
		/// </param>
		/// <param name="commandTimeout">
		/// Timeout to use for database commands created by this instance
		/// </param>
		private void Initialize(string connectionString, int commandTimeout)
		{
			try
			{
				_connectionString = connectionString;
				CommandTimeout = commandTimeout;
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while initializing database connection.", ex);
			}
		}

		/// <summary>
		/// Initialize the connection for the database.
		/// </summary>
		/// <param name="provider">
		/// Name of the provider to use
		/// </param>
		/// <param name="dataSource">
		/// The data source of the connect string.
		/// </param>
		/// <param name="userId">
		/// The user ID of the connect string.
		/// </param>
		/// <param name="password">
		/// The user password of the connect string.
		/// </param>
		/// <param name="databaseName">
		/// The name of the database to connect to for a MS SQL server.
		/// </param>
		/// <param name="commandTimeout">
		/// Timeout to use for database commands created by this instance
		/// </param>
		private void Initialize(string provider, string dataSource, string userId, string password, string databaseName, int commandTimeout)
		{
			try
			{
				_userId = userId;
				CommandTimeout = commandTimeout;

				//Calculate the appropriate connection string.
				_connectionString = SqlTools.GetConnectionString(provider, dataSource, userId, password, databaseName);
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while initializing database connection.", ex);
			}
		}

		/// <summary>
		/// Get a new connection using the connection parameters specified when this class was created.
		/// </summary>
		/// <returns>
		/// A new connection.
		/// </returns>
		public IDbConnection GetConnection()
		{
			try
			{
				return SqlTools.GetConnection(ConnectionString);
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while getting a new connection.", ex);
			}
		}

		/// <summary>
		/// Create a new data reader object for the indicated SQL statement on the indicated connection.
		/// </summary>
		/// <param name="sql">
		/// The SQL string to use to select the data.
		/// </param>
		/// <param name="connection">
		/// The connection to use to get the reader. If no connection is specified, a either a new 
		/// connection will be created or the current transction connection will be used.
		/// </param>
		/// <param name="commandBehavior">
		/// The command behavior to use when an existing connection is already open.  If the connection state
		/// is open then the command behavior parameter is ignored.
		/// </param>
		/// <returns>
		/// The new DataReader.
		/// </returns>
		public IDataReader GetReader(string sql, IDbConnection connection, CommandBehavior commandBehavior)
		{
			return GetReader(SqlTools.GetCommand(sql), connection, commandBehavior);
		}
		public IDataReader GetReader(string sql, IDbConnection connection)
		{
			return GetReader(SqlTools.GetCommand(sql), connection, CommandBehavior.Default);
		}
		public IDataReader GetReader(string sql)
		{
			return GetReader(SqlTools.GetCommand(sql), null, CommandBehavior.Default);
		}

		/// <summary>
		/// Create a new data reader object for the indicated command using either a new connection or the
		/// current transaction connection. The caller can specify the command behavior, which will be utilized if 
		/// the connection used is not already open.
		/// </summary>
		/// <param name="command">
		/// The command object with which to create the data reader.
		/// </param>
		/// <param name="connection">
		/// The connection on which the data reader should be created. If no connection is specified, a either a 
		/// new connection will be created or the current transaction connection will be used.
		/// </param>
		/// <param name="commandBehavior">
		/// The command behavior to use when an existing connection is already open.  If the connection state
		/// is open then the command behavior parameter is ignored.
		/// </param>
		/// <returns>
		/// The new DataReader.
		/// </returns>
		public IDataReader GetReader(IDbCommand command, IDbConnection connection, CommandBehavior commandBehavior)
		{
			//Save the command text now because in some cases, the Command object doesn't report the text after an error.
			string sql = command.CommandText;

			try
			{
				IDbTransaction transaction = null;
				if (connection == null)
				{
					if (TransactionScope.Current != null)
					{
						//Only use the transaction connection if it uses the same connection string as this connection.
						if (TransactionScope.Current.ConnectionString == ConnectionString)
						{
							connection = TransactionScope.Current.Connection;
							transaction = TransactionScope.Current.Transaction;
						}
					}
				}

				if (connection == null)
				{
					connection = GetConnection();
				}

				//If the connection is not yet open, open it now and indicate that it should be closed when
				//the data reader is closed.
				if (connection.State != ConnectionState.Open)
				{
					commandBehavior |= CommandBehavior.CloseConnection;
					if (TransactionScope.Current != null)
					{
						//If there is a current transaction, this new connection is not the same as the transaction's connection so we not include
						//it in the transaction
						using (new TransactionScope(TransactionScopeOption.Suppress))
						{
							connection.Open();
						}
					}
					else
					{
						connection.Open();
					}
				}

				//This allows us to get the schema information along with the data.
				commandBehavior |= CommandBehavior.KeyInfo;

				command.Connection = connection;

				using (command)
				{
					command.Transaction = transaction;
					return ExeccuteReader(command, commandBehavior);
				}
			}
			catch (System.Exception e)
			{
				//Make sure that the connection closed if we opened it here; in the case of an exception due
				//to bad sql, we can't rely on the commandbehavior to have done the trick
				if (connection != null && connection.State == ConnectionState.Open && (commandBehavior & CommandBehavior.CloseConnection) == CommandBehavior.CloseConnection)
					connection.Close();

				throw new SystemException("Error occurred while executing a command to create a Data Reader: " + sql, e);
			}
		}

		public IDataReader GetReader(IDbCommand command, IDbConnection connection)
		{
			return GetReader(command, connection, CommandBehavior.Default);
		}
		public IDataReader GetReader(IDbCommand command, CommandBehavior commandBehavior)
		{
			return GetReader(command, null, commandBehavior);
		}
		public IDataReader GetReader(IDbCommand command)
		{
			return GetReader(command, null, CommandBehavior.Default);
		}

		/// <summary>
		/// Put into separate method for performance testing.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="commandBehavior"></param>
		/// <returns></returns>
		private static IDataReader ExeccuteReader(IDbCommand command, CommandBehavior commandBehavior)
		{
			return command.ExecuteReader(commandBehavior);
		}

		public DataSet GetDataSet(IDbCommand command, IDbConnection connection, CommandBehavior commandBehavior)
		{
			//Save the command text now because in some cases, the Command object doesn't report the text after an error.
			string sql = command.CommandText;
			
			try
			{
				IDbTransaction transaction = null;
				if (connection == null)
				{
					if (TransactionScope.Current != null)
					{
						//Only use the transaction connection if it uses the same connection string as this connection.
						if (TransactionScope.Current.ConnectionString == ConnectionString)
						{
							connection = TransactionScope.Current.Connection;
							transaction = TransactionScope.Current.Transaction;
						}
					}
				}

				if (connection == null)
					connection = GetConnection();

				////If the connection is not yet open, open it now and indicate that it should be closed when
				////the data reader is closed.
				//if (connection.State != ConnectionState.Open)
				//{
				//    commandBehavior |= CommandBehavior.CloseConnection;
				//    if (TransactionScope.Current != null)
				//    {
				//        //If there is a current transaction, this new connection is not the same as the transaction's connection so we not include
				//        //it in the transaction
				//        using (new TransactionScope(System.Transactions.TransactionScopeOption.Suppress))
				//            connection.Open();
				//    }
				//    else
				//        connection.Open();
				//}

				////This allows us to get the schema information along with the data.
				//commandBehavior |= CommandBehavior.KeyInfo;

				command.Connection = connection;
				command.Transaction = transaction;

				DataSet dataSet = new DataSet();
				IDataAdapter adapter = SqlTools.GetDataAdapter(command);

				//CFL 12/4/11 - Don't open the connection; let the adapter do that when you call Fill and it will automatically
				//close the connection for you when it's done.  If the connection is open when Fill is called, then the adapter
				//won't close the connection, even on disposal of the dataset, resulting in a vestigial db connection
				//until the application exits entirely (which will, at a minimum, screw up unit testing)
				adapter.Fill(dataSet);

				return dataSet;
			}
			catch (System.Exception e)
			{
				throw new SystemException("Error occurred while executing a command to create a DataSet: " + sql, e);
			}
		}
		public DataSet GetDataSet(IDbCommand command, IDbConnection connection)
		{
			return GetDataSet(command, connection, CommandBehavior.Default);
		}
		public DataSet GetDataSet(IDbCommand command)
		{
			return GetDataSet(command, null, CommandBehavior.Default);
		}
		public DataSet GetDataSet(string sql, IDbConnection connection, CommandBehavior commandBehavior)
		{
			return GetDataSet(SqlTools.GetCommand(sql), connection, commandBehavior);
		}
		public DataSet GetDataSet(string sql, IDbConnection connection)
		{
			return GetDataSet(SqlTools.GetCommand(sql), connection, CommandBehavior.Default);
		}
		public DataSet GetDataSet(string sql)
		{
			return GetDataSet(SqlTools.GetCommand(sql));
		}

		/// <summary>
		/// Execute the indicated non-query sql command on the indicated connection.
		/// </summary>
		/// <param name="sql">
		/// The SQL to execute.
		/// </param>
		/// <param name="connection">
		/// The connection on which to execute the command. If no connection is specified, a either a new 
		/// connection will be created or the current transction connection will be used.
		/// </param>
		/// <returns>
		/// Integer representing the number of rows affected. 
		/// </returns>
		public int ExecuteCommand(string sql, IDbConnection connection)
		{
			return ExecuteCommand(SqlTools.GetCommand(sql), connection);
		}
		public int ExecuteCommand(string sql)
		{
			return ExecuteCommand(SqlTools.GetCommand(sql), null);
		}

		/// <summary>
		/// Execute the indicated command on the indicated connection.
		/// </summary>
		/// <param name="command">
		/// The command to execute.
		/// </param>
		/// <param name="connection">
		/// The connection on which to execute the command. If no connection is specified, a either a new 
		/// connection will be created or the current transction connection will be used.
		/// </param>
		/// <returns>
		/// Integer representing the number of rows affected. 
		/// </returns>
		public int ExecuteCommand(IDbCommand command, IDbConnection connection)
		{
			int rowsAffected;
			
			//Save the command text now because in some cases, the Command object doesn't report the text after an error.
			string sql = command.CommandText;

			try
			{
				IDbTransaction transaction = null;
				if (connection == null)
				{
					if (TransactionScope.Current != null)
					{
						//Only use the transaction connection if it uses the same connection string as this connection.
						if (TransactionScope.Current.ConnectionString == ConnectionString)
						{
							connection = TransactionScope.Current.Connection;
							transaction = TransactionScope.Current.Transaction;
						}
					}
				}

				if (connection == null)
				{
					using (connection = GetConnection())
					{
						if (TransactionScope.Current != null)
						{
							//If there is a current transaction, this new connection is not the same as the transaction's connection so we not include
							//it in the transaction
							using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Suppress))
							{
								connection.Open();
								using (command)
								{
									command.Connection = connection;
									command.Transaction = scope.Transaction;
									rowsAffected = command.ExecuteNonQuery();
								}
							}
						}
						else
						{
							connection.Open();
							using (command)
							{
								command.Connection = connection;
								rowsAffected = command.ExecuteNonQuery();
							}
						}
					}
				}
				else
				{
					command.Connection = connection;
					command.Transaction = transaction;
					using (command)
						rowsAffected = command.ExecuteNonQuery();
				}
			}
			catch (System.Exception e)
			{
				throw new SystemException("Error occurred while executing SQL statement: " + sql, e);
			}
			return rowsAffected;
		}
		public int ExecuteCommand(IDbCommand command)
		{
			return ExecuteCommand(command, null);
		}

		/// <summary>
		/// Executes multiple commands on a single connection
		/// </summary>
		/// <param name="commands">
		/// set of tuples, each consisting of the CommandFunction to exeucte and the command to do it with
		/// </param>
		/// <returns>
		/// A list of the results of each function (rows affected for an ExecuteNonQuery, a 
		/// DataReader for an ExecuteReader or an object for an ExecuteScalar)
		/// </returns>
		public List<object> ExecuteCommands(IEnumerable<Tuple<CommandFunction, IDbCommand>> commands)
		{
			List<object> results = new List<object>();

			string sql = "";
			try
			{
				IDbTransaction transaction = null;
				IDbConnection connection = null;
				if (TransactionScope.Current != null)
				{
					//Only use the transaction connection if it uses the same connection string as this connection.
					if (TransactionScope.Current.ConnectionString == ConnectionString)
					{
						connection = TransactionScope.Current.Connection;
						transaction = TransactionScope.Current.Transaction;
					}
				}

				if (connection == null)
				{
					using (connection = GetConnection())
					{
						if (TransactionScope.Current != null)
						{
							//If there is a current transaction, this new connection is not the same as the transaction's connection so we not include
							//it in the transaction
							using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Suppress))
							{
								connection.Open();
								foreach (Tuple<CommandFunction, IDbCommand> command in commands)
								{
									sql = command.Item2.CommandText;
									command.Item2.Connection = connection;
									command.Item2.Transaction = scope.Transaction;

									if (command.Item1 == CommandFunction.ExecuteNonQuery)
										results.Add(command.Item2.ExecuteNonQuery());
									else if (command.Item1 == CommandFunction.ExecuteReader)
										results.Add(command.Item2.ExecuteReader());
									else
										results.Add(command.Item2.ExecuteScalar());
								}
							}
						}
						else
						{
							connection.Open();
							foreach (Tuple<CommandFunction, IDbCommand> command in commands)
							{
								sql = command.Item2.CommandText;
								command.Item2.Connection = connection;

								if (command.Item1 == CommandFunction.ExecuteNonQuery)
									results.Add(command.Item2.ExecuteNonQuery());
								else if(command.Item1 == CommandFunction.ExecuteReader)
									results.Add(command.Item2.ExecuteReader());
								else
									results.Add(command.Item2.ExecuteScalar());
							}
						}
					}
				}
				else
				{
					foreach (Tuple<CommandFunction, IDbCommand> command in commands)
					{
						sql = command.Item2.CommandText;
						command.Item2.Connection = connection;
						if (transaction != null)
							command.Item2.Transaction = transaction;

						if (command.Item1 == CommandFunction.ExecuteNonQuery)
							results.Add(command.Item2.ExecuteNonQuery());
						else if (command.Item1 == CommandFunction.ExecuteReader)
							results.Add(command.Item2.ExecuteReader());
						else
							results.Add(command.Item2.ExecuteScalar());
					}
				}
			}
			catch (System.Exception e)
			{
				throw new SystemException(string.Format("Error occurred while executing SQL statement: {0}", sql), e);
			}

			return results;
		}

		/// <summary>
		/// Fill the dataset with a schema on the indicated connection given the sql statement.
		/// </summary>
		/// <param name="dataSet">
		/// The dataset to populate.
		/// </param>
		/// <param name="sql">
		/// The sql statement to use to populate the dataset.
		/// </param>
		/// <param name="connection">
		/// Connection from which to fill the schema. If no connection is specified, a either a new 
		/// connection will be created or the current transction connection will be used.
		/// </param>
		public void FillSchema(ref DataSet dataSet, string sql, IDbConnection connection)
		{
			try
			{
				if (dataSet == null) dataSet = new DataSet();

				IDbTransaction transaction = null;

				if (connection == null)
				{
					if (TransactionScope.Current != null)
					{
						//Only use the transaction connection if it uses the same connection string as this connection.
						if (TransactionScope.Current.ConnectionString == ConnectionString)
						{
							connection = TransactionScope.Current.Connection;
							transaction = TransactionScope.Current.Transaction;
						}
					}
				}

				if (connection == null)
					if (TransactionScope.Current != null)
					{
						using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Suppress))
						{
							using (connection = GetConnection())
							{
								using (IDbCommand command = SqlTools.GetCommand(sql, connection))
								{
									command.Transaction = scope.Transaction;
									IDbDataAdapter adapter = SqlTools.GetDataAdapter(command);
									adapter.FillSchema(dataSet, SchemaType.Source);
								}
							}
						}
					}
					else
					{
						using (connection = GetConnection())
						{
							using (IDbCommand command = SqlTools.GetCommand(sql, connection))
							{
								IDbDataAdapter adapter = SqlTools.GetDataAdapter(command);
								adapter.FillSchema(dataSet, SchemaType.Source);
							}
						}
					}
				else
				{
					using (IDbCommand command = SqlTools.GetCommand(sql, connection))
					{
						command.Transaction = transaction;
						IDbDataAdapter adapter = SqlTools.GetDataAdapter(command);
						adapter.FillSchema(dataSet, SchemaType.Source);
					}
				}
			}
			catch (System.Exception e)
			{
				throw new SystemException("Error occurred while filling schema for sql: " + sql, e);
			}
		}
		public void FillSchema(ref DataSet dataSet, string sql)
		{
			FillSchema(ref dataSet, sql, null);
		}

		/// <summary>
		/// Read a scalar value from the database.
		/// </summary>
		/// <param name="sql">
		/// A complete SQL string used to read the scalar value.
		/// </param>
		/// <param name="returnStatus">
		/// ReturnStatus.NotFound if record no found. ReturnStatus.Success otherwise.
		/// </param>
		/// <param name="connection">
		/// The connection on which to read the scalar. If a connection is not specified either a new connection
		/// or the current transaction connection will be used.
		/// </param>
		/// <returns>
		/// The value read as an object.
		/// </returns>
		public object ReadScalar(string sql, ref ReturnStatus returnStatus, IDbConnection connection)
		{
			return ReadScalar(SqlTools.GetCommand(sql), ref returnStatus, connection);
		}
		public object ReadScalar(string sql, ref ReturnStatus returnStatus)
		{
			return ReadScalar(sql, ref returnStatus, null);
		}
		public object ReadScalar(string sql)
		{
			ReturnStatus returnStatus = ReturnStatus.Success;
			return ReadScalar(sql, ref returnStatus, null);
		}

		/// <summary>
		/// Read a scalar value from the database using the indicated connection.
		/// </summary>
		/// <param name="command">
		/// The command to execute to read the scalar value.
		/// </param>
		/// <param name="returnStatus">
		/// ReturnStatus.NotFound if record no found. ReturnStatus.Success otherwise.
		/// </param>
		/// <param name="connection">
		/// The connection on which to read the scalar. If a connection is not specified either a new connection
		/// or the current transaction connection will be used.
		/// </param>
		/// <returns>
		/// The value read as an object.
		/// </returns>
		public object ReadScalar(IDbCommand command, ref ReturnStatus returnStatus, IDbConnection connection)
		{
			//Save the command text now because in some cases, the Command object doesn't report the text after an error.
			string sql = command.CommandText;

			try
			{
				//Set data to null. If it is still null after the read, we will know that no record was found.
				object data;
				IDbTransaction transaction = null;

				if (connection == null)
				{
					if (TransactionScope.Current != null)
					{
						//Only use the transaction connection if it uses the same connection string as this connection.
						if (TransactionScope.Current.ConnectionString == ConnectionString)
						{
							connection = TransactionScope.Current.Connection;
							transaction = TransactionScope.Current.Transaction;
						}
					}
				}

				if (connection == null)
					using (connection = GetConnection())
					{
						command.Connection = connection;
						if (TransactionScope.Current != null)
						{
							//If there is a current transaction, this new connection is not the same as the transaction's connection so we not include
							//it in the transaction
							using (TransactionScope scope = new TransactionScope(TransactionScopeOption.Suppress))
							{
								connection.Open();
								command.Transaction = scope.Transaction;
								using (command)
									data = command.ExecuteScalar();
							}
						}
						else
						{
							connection.Open();
							using (command)
								data = command.ExecuteScalar();
						}
					}
				else
				{
					command.Connection = connection;
					command.Transaction = transaction;
					using (command)
						data = command.ExecuteScalar();
				}

				//Set returnStatus.
				returnStatus = data != null ? ReturnStatus.Success : ReturnStatus.NotFound;

				return data;
			}
			catch (System.Exception ex)
			{
				throw new SystemException(String.Format("Error occurred while reading data using: {0}", sql), ex);
			}
		}
		public object ReadScalar(IDbCommand command, ref ReturnStatus returnStatus)
		{
			return ReadScalar(command, ref returnStatus, null);
		}
		public object ReadScalar(IDbCommand command, IDbConnection connection)
		{
			ReturnStatus returnStatus = ReturnStatus.Success;
			return ReadScalar(command, ref returnStatus, connection);
		}
		public object ReadScalar(IDbCommand command)
		{
			ReturnStatus returnStatus = ReturnStatus.Success;
			return ReadScalar(command, ref returnStatus, null);
		}

		/// <summary>
		/// Loads up a DataTable with information about the columns in a specific
		/// table in the database on the current connection
		/// </summary>
		/// <param name="tableName">
		/// Name of the table for which we want the schema
		/// </param>
		/// <param name="connection">
		/// DB connection to use for this action (if null, we'll use the current one)
		/// </param>
		public DataTable ReadSchema(string tableName, IDbConnection connection)
		{
			DataSet dataSet = null;
			FillSchema(ref dataSet, String.Concat("Select * from ", SqlTools.EscapeName(tableName)), connection);
			if (dataSet != null)
				return dataSet.Tables[0];

			return new DataTable();
		}

		/// <summary>
		/// Loads up a DataTable with information about the columns in a specific
		/// table in the database on the current connection
		/// </summary>
		public DataTable ReadSchema(string tableName) 
		{
			return ReadSchema(tableName, null);
		}

		/// <summary>
		/// Create a command object using the indicated sql and command parameters.
		/// </summary>
		/// <param name="sql">
		/// The sql statement to use in the command.
		/// </param>
		/// <param name="parameters">
		/// An array of all the command parameter to add to the command.
		/// </param>
		/// <returns>
		/// The command object.
		/// </returns>
		public IDbCommand GetCommand(string sql, params object[] parameters)
		{
			return GetCommand(sql, parameters.ToList());
		}

		public IDbCommand GetCommand(string sql, IEnumerable<object> parameters)
		{
			try
			{
				object[] paramArray = parameters.ToArray();
				int numberOfParameters = paramArray.Length;

				//Create an array of sql parameter names and use String.Format to place these into sql, replacing
				//the {N} placeholders.
				object[] parameterNames = new object[numberOfParameters];
				for (int i = 0; i < numberOfParameters; i++)
					parameterNames[i] = SqlTools.GetSqlParamName("A" + i);

				// TT 2494 - We may have brackets in the search expression, which will only occur when parameters.Count() == 0
				if (parameterNames.Any())
					sql = String.Format(sql, parameterNames);

				//Use this to create a command object.
				IDbCommand command = SqlTools.GetCommand(sql);

				//Now add each of the indicated parameters to the command object.
				for (int i = 0; i < numberOfParameters; i++)
					command.Parameters.Add(SqlTools.GetDataParameter("A" + i, paramArray[i].GetType(), paramArray[i]));

				return command;
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while creating a command object.", ex);
			}
		}

		/// <summary>
		/// Add a sql statement to a command object using the indicated sql and command parameters.
		/// </summary>
		/// <param name="command">
		/// The command to which to add the sql statement.
		/// </param>
		/// <param name="sql">
		/// The sql statement to use in the command.
		/// </param>
		/// <param name="parameters">
		/// An array of all the command parameter to add to the command.
		/// </param>
		public void AddToCommand(IDbCommand command, string sql, params object[] parameters)
		{
			try
			{
				//Figure how many statements already exist in this command. Use this to distinguish these
				//parameters from previous parameters. The first statement's parameters will start with A, 
				//the second B, etc.
				string text = command.CommandText;
				char prefix = Convert.ToChar(Convert.ToInt16('A') + text.Split(';').Length);

				//Create an array of sql parameter names and use String.Format to place these into sql, replacing
				//the {N} placeholders.
				object[] parameterNames = new object[parameters.Length];
				for (int i = 0; i < parameters.Length; i++)
					parameterNames[i] = SqlTools.GetSqlParamName(prefix + i.ToString());

				sql = String.Format(sql, parameterNames);

				//Update the command object with this sql.
				command.CommandText += ";" + sql;

				//Now add each of the indicated parameters to the command object.
				for (int i = 0; i < parameters.Length; i++)
					command.Parameters.Add(SqlTools.GetDataParameter(prefix + i.ToString(), parameters[i].GetType(), parameters[i]));
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while adding to an existing command object.", ex);
			}
		}
	
		#endregion Methods
	}
}
