using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using MicroEdge.Igam.Providers.Cache;

namespace MicroEdge.Igam.Data
{
	/// <summary>
	/// This class facilitates data access to a single row of data in a database.
	/// </summary>
	/// <remarks>
	///  Author:    LDF
	///  Created:   08/02/2006
	/// </remarks>
	[Serializable]
	public class Row
	{
        #region Fields

	    private DataTable _schema;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Constructor to initialize database connection and table name from which to read.
        /// </summary>
        /// <param name="data">
        /// A MicroEdge.Igam.Data object to indicate the connection the data will be read from.
        /// </param>
        /// <param name="tableName">
        /// The name of the table from which the row of data will be read.
        /// </param>
        public Row(Data data, string tableName)
		{
		    IsNew = true;
			Data = data;
			TableName = tableName;
		}


		/// <summary>
		/// Constructor to initialize database connection and table name from which to read and intialize the values of 
		/// the row with the data from a particular datareader row.
		/// </summary>
		/// <param name="data">
		/// A MicroEdge.Igam.Data object to indicate the connection the data will be read from.
		/// </param>
		/// <param name="tableName">
		/// The name of the table from which the row of data will be read.
		/// </param>
		/// <param name="dr">
		/// The datareader from which to initalize the values of the row.
		/// </param>
		public Row(Data data, string tableName, IDataReader dr)
			: this(data, tableName)
		{
			SetColumnValues(dr);
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Can be used by derived class to control if the schema for the table will be cached.
		/// Used when we have tables in different database with the same name used by the same 
		/// app at the same time.
		/// </summary>
		protected virtual bool CacheSchema => true;

	    protected HybridDictionary Current { get; set; }

		/// <summary>
		/// The MicroEdge.Igam.Data connection used to read this row of data.
		/// </summary>
		public virtual Data Data { get; protected set; }

		/// <summary>
		/// The name of the table of this row.
		/// </summary>
		public string TableName { get; protected set; }

		/// <summary>
		/// Whether this is a new row.
		/// </summary>
		public bool IsNew { get; protected set; }

        /// <summary>
        /// Whether this row is flagged for deletion.
        /// </summary>
        public bool IsDeleted { get; protected set; }

        /// <summary>
        /// Whether this row has been changed since being read from the database.
        /// </summary>
        public virtual bool IsDirty
		{
			get
			{
				//Check each field to see if it has changed.
				foreach (DictionaryEntry de in Current)
				{
					if (!Tools.Tools.Equals(de.Value, Original[de.Key]))
					{
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Name of the field in the table that provides the key for this row.
		/// Default is 'Id', but may be overridden by deriving classes
		/// </summary>
		protected virtual string KeyColumnName => "Id";

	    /// <summary>
		/// The schema for the table this row is associated with presented as a DataTable object.
		/// </summary>
		protected DataTable Schema
		{
			get
			{
				if (_schema == null)
					LoadSchema();

				return _schema;
			}
		}
        protected HybridDictionary Original { get; set; }

        /// <summary>
        /// Indexer to get the value of a column given the name of the column.
        /// </summary>
        /// <param name="columnName">
        /// The name of the column whose value to return.
        /// </param>
        /// <returns>
        /// The value of the column with the indicated name.
        /// </returns>
        protected object this[string columnName]
		{
			get
			{
				return Current[columnName];
			}
			set
			{
				Current[columnName] = value;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Returns the name of the column at the indicated position in the 
		/// given reader.  This is used by SetColumnValues and may be overridden
		/// for cases where we need to handle certain column names in a special
		/// fashion (such as when translating from one database type to another)
		/// </summary>
		protected virtual string GetColumnName(IDataReader reader, int index)
		{
			return reader.GetName(index);
		}

		/// <summary>
		/// Initialize this row as a new row.
		/// </summary>
		public virtual void NewRow()
		{
			try
			{
				IsNew = true;
				IsDeleted = false;

				//Create an entry in the dictionary for each column in the schema.
				Current = new HybridDictionary(Schema.Columns.Count, true);
				Original = new HybridDictionary(Schema.Columns.Count, true);

				foreach (DataColumn column in Schema.Columns)
				{
					Current.Add(column.ColumnName, DBNull.Value);
					Original.Add(column.ColumnName, DBNull.Value);
				}
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while initializing a new row for the {0} table.", TableName), e);
			}
		}

		/// <summary>
		/// Initialize this row as a new row. This is the same as NewRow but uses a method name that is friendly for use with a DAL object that inherits 
		/// from Row.
		/// </summary>
		public virtual void New()
		{
			NewRow();
		}

		/// <summary>
		/// Read a row of data from the associated table given the indicated where clause.
		/// </summary>
		/// <param name="where">
		/// A SQL Where clause to indicate the row to read.
		/// </param>
		/// <returns>
		/// ReturnStatus.NotFound if record no found. ReturnStatus.Success otherwise.
		/// </returns>
		public virtual ReturnStatus ReadWhere(string where)
		{
			//Insure the where clause starts with Where.
			if (where != "" && !where.ToLower().StartsWith("where"))
				where = "Where " + where;

			string sql = string.Format("Select * From {0} {1}", SqlTools.EscapeName(TableName), where);

			return Read(sql);
		}

		/// <summary>
		/// Read columns of a row of data from the associated table given the indicated where clause and columns 
		/// to read.
		/// </summary>
		/// <param name="where">
		/// A SQL Where clause to indicate the row to read.
		/// </param>
		/// <param name="columns">
		/// The columns to read.
		/// </param>
		/// <returns>
		/// ReturnStatus.NotFound if record no found. ReturnStatus.Success otherwise.
		/// </returns>
		public virtual ReturnStatus ReadWhere(string where, params string[] columns)
		{
			StringBuilder sqlColumns = new StringBuilder();

			//Build the columns list for the sql statement.
			sqlColumns.Append(SqlTools.EscapeName(columns[0]));

			for (int i = 1; i < columns.Length; i++)
			{
				sqlColumns.Append(", ");
				sqlColumns.Append(SqlTools.EscapeName(columns[i]));
			}

			//Insure the where clause starts with Where.
			if (where != "" && !where.ToLower().StartsWith("where "))
				where = "Where " + where;

			string sql = string.Format("Select {0} From {1} {2}", sqlColumns, SqlTools.EscapeName(TableName), where);

			return Read(sql);
		}


		/// <summary>
		/// This will read the row from the database using the indicated sql and update the class level 
		/// original and current column values.
		/// </summary>
		/// <param name="sql">
		/// A complete sql string used to read the row of data.
		/// </param>
		/// <returns>
		/// ReturnStatus.NotFound if record not found. ReturnStatus.Success otherwise.
		/// </returns>
		protected ReturnStatus Read(string sql)
		{

			return Read(SqlTools.GetCommand(sql));
		}


		/// <summary>
		/// This will read the row from the database using the indicated command and update the class level 
		/// original and current column values.
		/// </summary>
		/// <param name="command">
		/// The command containing the sql statement used to read the row of data.
		/// </param>
		/// <returns>
		/// ReturnStatus.NotFound if record not found. ReturnStatus.Success otherwise.
		/// </returns>
		protected ReturnStatus Read(IDbCommand command)
		{
			try
			{
				//Ensure we have the schema for the table before we read from the table.
				LoadSchema();

				ReturnStatus returnStatus = ReturnStatus.Success;

				using (IDataReader reader = Data.GetReader(command))
				{
					IsDeleted = false;
					if (reader.Read())
					{
						SetColumnValues(reader);
						reader.Close();
						IsNew = false;
					}
					else
					{
						//Not found. Initialize this as a new row of the table.
						IsNew = true;
						reader.Close();
						NewRow();
						returnStatus = ReturnStatus.NotFound;
					}
				}

				return returnStatus;
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while reading a row from the {0} table.", TableName), e);
			}
		}

		/// <summary>
		/// Initialize the current and original values of the columns based on the current row in the reader.
		/// </summary>
		/// <param name="reader">
		/// The data reader from which to reader the field values.
		/// </param>
		public virtual void SetColumnValues(IDataReader reader)
		{
			bool currentRow = true;

			//Determine if we have a current row by trying to read the first column.
		    // ReSharper disable once UnusedVariable
			try { object value = reader[0]; }
			catch { currentRow = false; }

			Current = new HybridDictionary(1, true);
			Original = new HybridDictionary(1, true);

			//Move all the fields for the current table from the reader into the current and original rows.
			for (int i = 0; i < reader.FieldCount; i++)
			{
				string columnName = GetColumnName(reader, i);

				//If the columnName has a . in it, it may include the table name; 
				//check if that's true and trim it down to just the actual column name
				if (columnName.Contains("."))
				{
					string[] nameParts = columnName.Split('.');
					if (nameParts[0].Equals(TableName, StringComparison.OrdinalIgnoreCase))
						columnName = nameParts[1];
				}

				//Make sure the column of this value is actually one for this table before adding it.
				//Also make sure we haven't already added a value for this column name (some db's, like MSSQL, have
				//a habit of including additional columns from other tables which may have the same name as those
				//from this table, depending on your joins/where criteria)
				if (Schema.Columns.Contains(columnName) && !Current.Contains(columnName))
				{
					object value = currentRow ? reader[i] : DBNull.Value;
					Current.Add(columnName, value);
					Original.Add(columnName, value);
				}
			}

			//If there is no current row, consider this a new row.
			if (currentRow)
			{
				//This won't be a new row or deleted because it's just come from the db.
				IsNew = false;
				IsDeleted = false;
			}
			else
			{
				//If there is no current row, consider this a new row.
				IsNew = true;
				IsDeleted = false;
			}
		}

		/// <summary>
		/// Update the changes made to the row of data.
		/// </summary>
		public virtual void Update()
		{
			if (IsNew)
				InsertRow();
			else if (IsDeleted)
				DeleteRow();
			else if (IsDirty)
				UpdateRow();
		}


		/// <summary>
		/// Insert this row as a new row in the database.
		/// </summary>
		protected virtual void InsertRow()
		{
			try
			{
				//No need to insert if it's already been deleted.
				if (!IsDeleted)
				{
					List<string> cols = new List<string>();
					List<string> vals = new List<string>();

					DataColumnCollection dataColumns = Schema.Columns;
					DataColumn[] keyColumns = GetKeyColumns();
					IDbCommand command = SqlTools.GetCommand("");

					IDataParameter newKey = null;
					for (int i = 0; i < dataColumns.Count; i++)
					{
						Type dataType = dataColumns[i].DataType;
						string column = dataColumns[i].ColumnName;
						object value = Current[column];
						int maxLength = dataColumns[i].MaxLength;

						//Only include the column in the insert if it is NOT an autoincrement column 
						if (!dataColumns[i].AutoIncrement)
						{
							//All boolean values will be stored as False if null.
							if (dataType == typeof(bool) && (value == null || value == DBNull.Value))
								value = false;

							//All string values will be stored as null if empty.
							if (dataType == typeof(string) && value != null && value.Equals(""))
								value = null;

							//Add command parameter for value.
							IDataParameter dataParameter = SqlTools.GetDataParameter(column, dataType, value, maxLength);
							//If the parameter comes back null, don't include this column in the insert. If
							//the parameter we get has a true null value (rather than DBNull), that indicates 
							//we're not using a parameter, but some db function instead
							if (dataParameter != null)
							{
								if (dataParameter.Value == null)
									vals.Add(dataParameter.ParameterName);
								else
								{
									command.Parameters.Add(dataParameter);
									vals.Add(SqlTools.GetSqlParamName(column));
								}

								cols.Add(SqlTools.EscapeName(column));
							}
						}
						else if (keyColumns.Length == 1 && column == keyColumns[0].ColumnName)
						{
							//If this autoincrement column is the primary key, we will read its value from the 
							//database after inserting.  Can only include in a single select for sql server
							newKey = SqlTools.GetDataParameter("NewKey", dataType, ParameterDirection.Output);
							command.Parameters.Add(newKey);
						}
					}

					//Put it all together
					command.CommandText = string.Format("Insert Into {0} ({1}) VALUES ({2})",
						SqlTools.EscapeName(TableName), string.Join(", ", cols),
						string.Join(", ", vals));

					//If we need to read the value of an autoincremented key after an insert, 
					//we will read the new key after the update (note that Access requires this
					//to be done with two separate commands on the same connection; no multiple
					//statements or out parameters allowed
					if (newKey != null)
					{
						command.CommandText += ";Select @NewKey = SCOPE_IDENTITY()";
						Data.ExecuteCommand(command);
						this[keyColumns[0].ColumnName] = newKey.Value;
					}
					else
						Data.ExecuteCommand(command);

					//It's no longer new.
					IsNew = false;

					//AcceptChanges so this row won't be seen as dirty.
					AcceptChanges();
				}
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while inserting a row into the {0} table.", TableName), e);
			}
		}


		/// <summary>
		/// Delete this row from the database.
		/// </summary>
		protected virtual void DeleteRow()
		{
			try
			{
				StringBuilder sb = new StringBuilder(string.Format("Delete From {0} Where", SqlTools.EscapeName(TableName)));

				IDbCommand command = SqlTools.GetCommand("");
				DataColumn[] keyColumns = GetKeyColumns();

				foreach (DataColumn keyColumn in keyColumns)
				{
					string column = keyColumn.ColumnName;
					sb.Append(" ");
					sb.Append(SqlTools.EscapeName(column));
					sb.Append(" = ");
					sb.Append(SqlTools.GetSqlParamName(column));

					command.Parameters.Add(SqlTools.GetDataParameter(column, keyColumn.DataType, Original[column], keyColumn.MaxLength));

					sb.Append(" And ");
				}
				//Remove trailing 'And'.
				string sql = sb.ToString();
				sql = sql.Substring(0, sql.Length - 5);

				command.CommandText = sql;
				Data.ExecuteCommand(command);

				//It's now new since it doesn't exist in the backend.
				IsNew = true;
				IsDeleted = false;
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while deleting a row from the {0} table.", TableName), e);
			}
		}


		/// <summary>
		/// Update the changes in this row to the database.
		/// </summary>
		protected virtual void UpdateRow()
		{
			try
			{
				DataColumnCollection dataColumns = Schema.Columns;
				IDbCommand command = SqlTools.GetCommand("");
				StringBuilder sb = new StringBuilder();

				//Create an update statement.
				for (int i = 0; i < dataColumns.Count; i++)
				{
					string column = dataColumns[i].ColumnName;

					//Insure this column was previously read.
					if (Current.Contains(column))
					{
						object oldValue = Original[column];
						object newValue = Current[column];
						Type dataType = dataColumns[i].DataType;

						//All boolean values will be stored as False if null.
						if (dataType == typeof(bool) && (newValue == null || newValue == DBNull.Value))
							newValue = false;

						//All string values will be stored as null if empty.
						if (dataType == typeof(string) && newValue != null && newValue.Equals(""))
							newValue = DBNull.Value;

						//Only update the column if the data has changed.
					    if (Tools.Tools.Equals(oldValue, newValue))
                            continue;

					    sb.Append(SqlTools.EscapeName(column));
					    sb.Append(" = ");

					    IDbDataParameter dataParameter = SqlTools.GetDataParameter(column, dataType, newValue, dataColumns[i].MaxLength);

					    //If the parameter comes back null, don't include this column in the update. 
					    //If the parameter we get has a true null value (rather than DBNull), that indicates 
					    //we're not using a parameter, but some db function instead, returned as the name
					    //of the parameter.
					    if (dataParameter != null)
					    {
					        if (dataParameter.Value == null)
					            sb.Append(dataParameter.ParameterName);
					        else
					        {
					            command.Parameters.Add(dataParameter);
					            sb.Append(SqlTools.GetSqlParamName(column));
					        }
					        sb.Append(", ");
					    }
					}
				}

				if (sb.Length > 0)
				{
					string sql = sb.ToString();

					DataColumn[] keyColumns = GetKeyColumns();

					//Add where clause for key columns.
					sql = sql.Remove(sql.Length - 2, 2); //Remove trailing comma.
					sql = string.Format("Update {0} Set {1} Where ", SqlTools.EscapeName(TableName), sql);
					foreach (DataColumn keyColumn in keyColumns)
					{
						string column = keyColumn.ColumnName;
						sql += SqlTools.EscapeName(column) + " = " + SqlTools.GetSqlParamName(column);

						command.Parameters.Add(SqlTools.GetDataParameter(column, keyColumn.DataType, Original[column], keyColumn.MaxLength));

						sql += " And ";
					}

					DataColumn[] updateConcurrencyColumns = GetUpdateConcurrencyColumns();

					// Add where clause for any additional concurrency columns
					foreach (DataColumn updateColumn in updateConcurrencyColumns)
					{
						string column = updateColumn.ColumnName;
						string paramColumn = "ORIG" + updateColumn.ColumnName;

						sql += SqlTools.EscapeName(column) + " = " + SqlTools.GetSqlParamName(paramColumn);

						command.Parameters.Add(SqlTools.GetDataParameter(paramColumn, updateColumn.DataType, Original[column], updateColumn.MaxLength));

						sql += " And ";
					}

					//Remove trailing 'And'.
					sql = sql.Substring(0, sql.Length - 5);

					command.CommandText = sql;
					if (Data.ExecuteCommand(command) == 0)
					{
						// DBConcurrencyException is normally thrown by the DataAdapter when no rows
						// are affected, so this is a suitable exception. 
						throw new DBConcurrencyException();
					}

					//Accept the row changes so that we will no longer see a change.
					AcceptChanges();
				}
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while updating a row in the {0} table.", TableName), e);
			}
		}


		/// <summary>
		/// Get the key columns for this table from the schema.
		/// </summary>
		/// <returns></returns>
		protected DataColumn[] GetKeyColumns()
		{
			DataColumn[] keyColumns = null;

			if (!string.IsNullOrEmpty(KeyColumnName) && Schema.Columns.Contains(KeyColumnName))
				keyColumns = new[] { Schema.Columns[KeyColumnName] };

			if (keyColumns == null || keyColumns.Length == 0)
				keyColumns = Schema.PrimaryKey;

			if (keyColumns.Length == 0)
			{
				//Default the key columns to the first column.
				keyColumns = new DataColumn[1];
				keyColumns[0] = Schema.Columns[0];
			}

			return keyColumns;
		}

		/// <summary>
		/// Gets the column that should be checked for concurrency purposes, if any. 
		/// </summary>
		/// <remarks>
		/// This method returns an array of DataColumns that should also be checked in the 
		/// where clause along with the key columns when updating. 
		/// 
		/// These additional columns can provide additional checks to ensure that the record
		/// has not changed since retrieval. By default, no additional columns are checked. 
		/// </remarks>
		/// <returns>
		/// An array of data columns that will be included within the where clause - checking
		/// to make sure the value in the row matches the original value. 
		/// </returns>
		protected virtual DataColumn[] GetUpdateConcurrencyColumns()
		{
			return new DataColumn[0];
		}

		/// <summary>
		/// Delete the current row.
		/// </summary>
		public virtual void Delete()
		{
			//Flag as deleted and update. Note nothing to delete if it's new.
			if (!IsNew)
			{
				MarkDeleted();
				Update();
			}
		}


		/// <summary>
		/// Set the IsDeleted flag for this row.
		/// </summary>
		public virtual void MarkDeleted()
		{
			IsDeleted = true;
		}


		/// <summary>
		/// Set the IsNew flag for this row to true.
		/// </summary>
		public virtual void MarkNew()
		{
			IsNew = true;
		}


		/// <summary>
		/// Set the IsNew flag for this row to false.
		/// </summary>
		public virtual void MarkOld()
		{
			IsNew = false;
		}

		/// <summary>
		/// Copy the current column values to the original column values.
		/// </summary>
		public virtual void AcceptChanges()
		{
			foreach (DictionaryEntry de in Current)
				Original[de.Key] = de.Value;
		}

		/// <summary>
		/// Load the _schema field with the schema for the current table.
		/// </summary>
		private void LoadSchema()
		{
			if (CacheSchema)
			{
				//Try and load it from cache 
				_schema = CacheManager.GetCacheObject<DataTable>(string.Concat(TableName, "Schema"));
				if (_schema == null)
				{
					//First time for this table; read it and cache what we get (if we get anything)
					ReadSchema();
					if (_schema != null)
						CacheManager.SetCacheObject(_schema, string.Concat(TableName, "Schema"));
				}
			}
			else
			{
				// Read it every time.
				ReadSchema();
			}
		}

		/// <summary>
		/// Read the schema for the table.
		/// </summary>
		private void ReadSchema()
		{
			try
			{
				_schema = Data.ReadSchema(TableName);
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while reading schema for {0} table.", TableName), e);
			}

			//Throw an error if the schema could not be obtained.
			if (_schema == null)
				throw new SystemException(string.Format("Schema for {0} table could not be read.", TableName));
		}


		/// <summary>
		/// Get the original value of the column with the indicated name.
		/// </summary>
		/// <param name="columnName">
		/// The column whose original value to return.
		/// </param>
		/// <returns>
		/// The original value of the column with the indicated name.
		/// </returns>
		public virtual object GetOriginal(string columnName)
		{
			return Original[columnName];
		}

		/// <summary>
		/// Get the key value.
		/// </summary>
		/// <returns>
		/// Key value.
		/// </returns>
		public object GetKeyValue()
		{
			return this[GetKeyColumns()[0].ColumnName];
		}

		#endregion Methods
	}


	/// <summary>
	/// This class inherits from the Row class in order to add type specific Read and Delete methods where the 
	/// type of the key is indicated using a generic class type. This class supports tables with a single key.
	/// </summary>
	/// <remarks>
	///  Author:    LDF
	///  Created:   08/04/2006
	/// </remarks>
	[Serializable]
	public class Row<TK> : Row
	{
		#region Constructors

		public Row(Data data, string tableName)
			: base(data, tableName)
		{ }
		public Row(Data data, string tableName, IDataReader dr)
			: base(data, tableName, dr)
		{ }

		#endregion Constructors

		#region Methods

		/// <summary>
		/// Read a row from the associated table using the single key of the table.
		/// </summary>
		/// <param name="key">
		/// This is the single key of the row to read.
		/// </param>
		/// <returns>
		/// ReturnStatus.Success or ReturnStatus.NotFound.
		/// </returns>
		public virtual ReturnStatus Read(TK key)
		{
			try
			{
				//Create a command to read the row using the primary key.
				DataColumn keyColumn = GetKeyColumns()[0];

				string sql = string.Format("Select * from {0} Where {1} = {2}", SqlTools.EscapeName(TableName),
					SqlTools.EscapeName(keyColumn.ColumnName), SqlTools.GetSqlParamName(keyColumn.ColumnName));

				IDbCommand command = SqlTools.GetCommand(sql);
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumn.ColumnName, keyColumn.DataType, key, keyColumn.MaxLength));

				return Read(command);
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while reading row {0} [{1}].", TableName, key.ToString()), e);
			}
		}

		/// <summary>
		/// Read a row from the associated table using the single key of the table.
		/// </summary>
		/// <param name="key">
		/// This is the single key of the row to read.
		/// </param>
		/// <param name="columns">
		/// The columns to read.
		/// </param>
		/// <returns>
		/// ReturnStatus.Success or ReturnStatus.NotFound.
		/// </returns>
		public virtual ReturnStatus Read(TK key, params string[] columns)
		{
			if (columns == null || columns.Length == 0)
				return Read(key);

			try
			{
				//Create a command to read the row using the primary key.
				DataColumn keyColumn = GetKeyColumns()[0];

				StringBuilder sb = new StringBuilder("Select ");

				//Build the columns list for the sql statement.
				sb.Append(SqlTools.EscapeName(columns[0]));

				for (int i = 1; i < columns.Length; i++)
				{
					sb.Append(", ").Append(SqlTools.EscapeName(columns[i]));
				}

				string sql = string.Format("{0} from {1} Where {2} = {3}", sb, SqlTools.EscapeName(TableName),
					SqlTools.EscapeName(keyColumn.ColumnName), SqlTools.GetSqlParamName(keyColumn.ColumnName));

				IDbCommand command = SqlTools.GetCommand(sql);
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumn.ColumnName, keyColumn.DataType, key, keyColumn.MaxLength));

				return Read(command);
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while reading row {0} [{1}].", TableName, key.ToString()), e);
			}
		}


		/// <summary>
		/// Delete the row with the indicated key from the associated table.
		/// </summary>
		/// <param name="key">
		/// This is the single key of the row to delete.
		/// </param>
		public virtual void Delete(TK key)
		{
			try
			{
				//Create a command to delete the row using the primary key.
				DataColumn keyColumn = GetKeyColumns()[0];

				string sql = string.Format("Delete from {0} Where {1} = {2}", SqlTools.EscapeName(TableName), SqlTools.EscapeName(keyColumn.ColumnName), SqlTools.GetSqlParamName(keyColumn.ColumnName));

				IDbCommand command = SqlTools.GetCommand(sql);
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumn.ColumnName, keyColumn.DataType, key, keyColumn.MaxLength));

				Data.ExecuteCommand(command);
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while deleting row {0} [{1}].", TableName, key.ToString()), e);
			}
		}

		/// <summary>
		/// Mark the row as new and set the key column to null.
		/// </summary>
		public override void MarkNew()
		{
			base.MarkNew();

			DataColumn keyColumn = GetKeyColumns()[0];
			this[keyColumn.ColumnName] = DBNull.Value;
		}

		/// <summary>
		/// Get the key value.
		/// </summary>
		/// <returns>
		/// Key value.
		/// </returns>
		new public TK GetKeyValue()
		{
			//For those rare cases where we are still using a short/Int16 key, need to do an explicit convert
			object key = base.GetKeyValue();
			if (typeof(TK) == typeof(short))
				key = Convert.ToInt16(key);
			return (TK)key;
		}

		#endregion Methods
	}

	/// <summary>
	/// This class inherits from the Row class in order to add type specific Read and Delete methods where the 
	/// type of the key is indicated using a generic class type. This class supports tables with two part keys.
	/// </summary>
	/// <remarks>
	///  Author:    LDF
	///  Created:   08/07/2006
	/// </remarks>
	[Serializable]
	public class Row<TK1, TK2> : Row
	{
		#region Constructors

		public Row(Data data, string tableName)
			: base(data, tableName)
		{ }
		public Row(Data data, string tableName, IDataReader dr)
			: base(data, tableName, dr)
		{ }

		#endregion Constructors

		#region Methods

		/// <summary>
		/// Read a row from the associated table using the two part key of the table.
		/// </summary>
		/// <param name="key1">
		/// This is the first part of the two part key of the row to read.
		/// </param>
		/// <param name="key2">
		/// This is the second part of the two part key of the row to read.
		/// </param>
		/// <returns>
		/// ReturnStatus.Success or ReturnStatus.NotFound.
		/// </returns>
		public ReturnStatus Read(TK1 key1, TK2 key2)
		{
			try
			{
				//Create a command to read the row using the two part key.
				DataColumn[] keyColumns = GetKeyColumns();

				string sql = string.Format("Select * from {0} Where {1} = {2} And {3} = {4}", SqlTools.EscapeName(TableName),
					SqlTools.EscapeName(keyColumns[0].ColumnName), SqlTools.GetSqlParamName(keyColumns[0].ColumnName),
					SqlTools.EscapeName(keyColumns[1].ColumnName), SqlTools.GetSqlParamName(keyColumns[1].ColumnName));

				IDbCommand command = SqlTools.GetCommand(sql);
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumns[0].ColumnName, keyColumns[0].DataType, key1, keyColumns[0].MaxLength));
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumns[1].ColumnName, keyColumns[1].DataType, key2, keyColumns[1].MaxLength));

				return Read(command);
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while reading row {0} [{1}/{2}].", TableName, key1.ToString(), key2.ToString()), e);
			}
		}

		/// <summary>
		/// Read columns from the associated table using the two part key of the table.
		/// </summary>
		/// <param name="key1">
		/// This is the first part of the two part key of the row to read.
		/// </param>
		/// <param name="key2">
		/// This is the second part of the two part key of the row to read.
		/// </param>
		/// <param name="columns">
		/// The columns to read.
		/// </param>
		/// <returns>
		/// ReturnStatus.Success or ReturnStatus.NotFound.
		/// </returns>
		public ReturnStatus Read(TK1 key1, TK2 key2, params string[] columns)
		{
			try
			{
				//Create a command to read the row using the two part key.
				DataColumn[] keyColumns = GetKeyColumns();

				StringBuilder sb = new StringBuilder("Select ");

				//Build the columns list for the sql statement.
				sb.Append(SqlTools.EscapeName(columns[0]));

				for (int i = 1; i < columns.Length; i++)
				{
					sb.Append(", ").Append(SqlTools.EscapeName(columns[i]));
				}

				string sql = string.Format("{0} from {1} Where {2} = {3} And {4} = {5}", sb, SqlTools.EscapeName(TableName),
					SqlTools.EscapeName(keyColumns[0].ColumnName), SqlTools.GetSqlParamName(keyColumns[0].ColumnName),
					SqlTools.EscapeName(keyColumns[1].ColumnName), SqlTools.GetSqlParamName(keyColumns[1].ColumnName));

				IDbCommand command = SqlTools.GetCommand(sql);
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumns[0].ColumnName, keyColumns[0].DataType, key1, keyColumns[0].MaxLength));
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumns[1].ColumnName, keyColumns[1].DataType, key2, keyColumns[1].MaxLength));

				return Read(command);
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while reading row {0} [{1}/{2}].", TableName, key1.ToString(), key2.ToString()), e);
			}
		}


		/// <summary>
		/// Delete the row with the indicated two part key from the associated table.
		/// </summary>
		/// <param name="key1">
		/// This is the first part of the two part key of the row to delete.
		/// </param>
		/// <param name="key2">
		/// This is the second part of the two part key of the row to delete.
		/// </param>
		public void Delete(TK1 key1, TK2 key2)
		{
			try
			{
				//Create a command to delete the row using the primary key.
				DataColumn[] keyColumns = GetKeyColumns();

				string sql = string.Format("Delete from {0} Where {1} = {2} And {3} = {4}", SqlTools.EscapeName(TableName),
					SqlTools.EscapeName(keyColumns[0].ColumnName), SqlTools.GetSqlParamName(keyColumns[0].ColumnName),
					SqlTools.EscapeName(keyColumns[1].ColumnName), SqlTools.GetSqlParamName(keyColumns[1].ColumnName));

				IDbCommand command = SqlTools.GetCommand(sql);
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumns[0].ColumnName, keyColumns[0].DataType, key1, keyColumns[0].MaxLength));
				command.Parameters.Add(SqlTools.GetDataParameter(keyColumns[1].ColumnName, keyColumns[1].DataType, key2, keyColumns[1].MaxLength));

				Data.ExecuteCommand(command);
			}
			catch (Exception e)
			{
				throw new SystemException(string.Format("Error occurred while deleting row {0} [{1}/{2}].", TableName, key1, key2), e);
			}
		}


		/// <summary>
		/// Mark the row as new and set both key columns to null.
		/// </summary>
		public override void MarkNew()
		{
			base.MarkNew();

			DataColumn[] keyColumns = GetKeyColumns();
			this[keyColumns[0].ColumnName] = DBNull.Value;
			this[keyColumns[1].ColumnName] = DBNull.Value;
		}

		#endregion Methods
	}
}
