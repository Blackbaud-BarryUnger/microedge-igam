using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace MicroEdge.Igam.Data
{
    public static class SqlTools
    {
        #region Fields

        private const string SqlResizeSql = "ALTER TABLE {0} ALTER COLUMN {1}";

        #endregion Fields

        #region Methods

        /// <summary>
        /// Builds a conditional statement from parts.
        /// </summary>
        /// <param name="boolStatement">
        /// Statement that evaluates to true/false for determining the logical fork.
        /// </param>
        /// <param name="thenStatement">
        /// Statement to use for when the boolean statement is true.
        /// </param>
        /// <param name="elseStatement">
        /// Statement to use when the boolean statement is false.
        /// </param>
        /// <returns>
        /// The string which will evaluate to the desired conditional.
        /// </returns>
        public static string BuildConditional(string boolStatement, string thenStatement, string elseStatement)
        {
            StringBuilder conditional = new StringBuilder();

            conditional.Append("CASE WHEN ");
            conditional.Append(boolStatement);
            conditional.Append(" THEN ");
            conditional.Append(thenStatement);
            conditional.Append(" ELSE ");
            conditional.Append(elseStatement);
            conditional.Append(" END");

            return conditional.ToString();
        }


        /// <summary>
        /// Checks the db for the existence of an column
        /// </summary>
        /// <param name="tableName">
        /// name of the table
        /// </param>
        /// <param name="columnName">
        /// name of the column
        /// </param>
        /// <returns>
        /// true if the column exists, false if not
        /// </returns>
        public static bool ColumnExists(string tableName, string columnName)
        {
            using (DbConnection conn = Data.Current.GetConnection() as DbConnection)
            {
                if (conn == null)
                    return false;

                conn.Open();
                DataTable column = conn.GetSchema("Columns", new[] { null, null, tableName, columnName });
                return column != null && column.Rows.Count > 0;
            }
        }

        /// <summary>
        /// Return a connection string for this database type given the connection parameters.
        /// </summary>
        /// <param name="provider">
        /// Name of the provider to use; currently ignored since we're explicitly using the SqlClient classes
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
        /// Not used in this implementation of DBTools.
        /// </param>
        /// <returns>
        /// A connection string.
        /// </returns>
        public static string GetConnectionString(string provider, string dataSource, string userId, string password, string databaseName)
        {
            SqlConnectionStringBuilder scb = new SqlConnectionStringBuilder();

            scb.DataSource = dataSource;
            scb.InitialCatalog = databaseName;
            scb.Password = password;
            scb.UserID = userId;
            //CFL 12/21/11 (TT3352) - Need this to stop SQL Server 2008 from trying to elevate transactions 
            //up to DTS's, which we don't want to use (and which makes stuff go boom)
            scb.Enlist = false;

            return scb.ConnectionString;
        }
        public static string GetConnectionString(string dataSource, string userId, string password, string databaseName)
        {
            return GetConnectionString(dataSource, null, userId, password, databaseName);
        }

        /// <summary>
        /// Return a SqlConnection for the indicated connection string.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string to use for the new connection.
        /// </param>
        /// <returns>
        /// A SqlConnection.
        /// </returns>
        public static IDbConnection GetConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Return a SqlCommand initialized with the indicated parameters.
        /// </summary>
        /// <param name="sql">
        /// The sql statement with which to initialize the command.
        /// </param>
        /// <param name="connection">
        /// The connection on which to create the command.
        /// </param>
        /// <returns>
        /// A SqlCommand.
        /// </returns>
        public static IDbCommand GetCommand(string sql, IDbConnection connection)
        {
            SqlCommand command = new SqlCommand(sql, (SqlConnection)connection);
            command.CommandTimeout = 600;
            return command;
        }
        public static IDbCommand GetCommand(string sql)
        {
            SqlCommand command = new SqlCommand(sql);
            command.CommandTimeout = Data.Current.CommandTimeout;
            return command;
        }

        /// <summary>
        /// Constructs a sql Create Index statement
        /// </summary>
        /// <param name="tableName">
        /// Name of the table to create the index on
        /// </param>
        /// <param name="index">
        /// IndexDefinition detailing the index to create
        /// </param>
        /// <returns>
        /// sql script string
        /// </returns>
        public static string GetCreateIndexScript(string tableName, IndexDefinition index)
        {
            return string.Format("CREATE {0}{1}Index {2} ON {3} ({4})",
                                 index.IsUnique ? "Unique " : "",
                                 index.IsPrimary ? "Clustered " : "",
                                 index.Name,
                                 tableName,
                                 string.Join(", ", index.Columns));
        }

        /// <summary>
        /// Constructs a sql Create Table statement
        /// </summary>
        /// <param name="tableName">
        /// Name of the table to create
        /// </param>
        /// <param name="columns">
        /// Set of ColumnDefinition objects detailing the columns that should go into the table
        /// </param>
        /// <returns>
        /// sql script string
        /// </returns>
        public static string GetCreateTableScript(string tableName, IEnumerable<ColumnDefinition> columns)
        {
            List<string> columnStrings = new List<string>();
            foreach (ColumnDefinition column in columns)
            {
                columnStrings.Add(GetColumnSql(column));
            }

            return string.Concat("CREATE TABLE dbo.", tableName, " (", string.Join(", ", columnStrings), ")");
        }

        /// <summary>
        /// Return a SqlParameter initialized with the indicated name and value.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="type">The type of the column for which this data parameter should be created.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="size">size value for the parameter</param>
        /// <param name="direction">Whether this is an input or output parameter.</param>
        /// <returns>A SqlParameter.</returns>
        public static IDbDataParameter GetDataParameter(string name, Type type, object value, int? size, ParameterDirection direction)
        {
            //Make the null a DbNull for all types.  Also, values of fields in the database can never be empty strings for 
            //anything but text, so convert an empty string to DBNull in that case
            if (value == null)
                value = DBNull.Value;
            else if (type != typeof(string) && value.Equals(string.Empty))
                value = DBNull.Value;
            else if (type == typeof(Int16) && value is bool)
            {
                //we actually use an integer field for boolean values on SQL server
                value = Tools.Tools.ToBoolean(value) ? -1 : 0;
            }

            SqlParameter param = new SqlParameter(GetSqlParamName(name), ToSqlDbType(type, size));
            param.Value = value;
            param.Direction = direction;

            //The size property should only be set for string type parameters.
            if (size != null && (ToSqlDbType(type, size) == SqlDbType.NVarChar || ToSqlDbType(type, size) == SqlDbType.NChar))
            {
                param.Size = (int)size;
            }

            return param;
        }

        public static IDbDataParameter GetDataParameter(string name, Type type, object value, int? size)
        {
            return GetDataParameter(name, type, value, size, ParameterDirection.Input);
        }

        public static IDbDataParameter GetDataParameter(string name, Type type, object value)
        {
            return GetDataParameter(name, type, value, null);
        }

        public static IDbDataParameter GetDataParameter(string name, Type type, ParameterDirection direction)
        {
            return GetDataParameter(name, type, null, null, direction);
        }
        public static IDbDataParameter GetDataParameter(string name, Type type, int size, ParameterDirection direction)
        {
            return GetDataParameter(name, type, null, size, direction);
        }

        /// <summary>
        /// Return a SqlAdapter initialized with the indicated command.
        /// </summary>
        /// <param name="selectCommand">
        /// The command with which to initialize the adapater.
        /// </param>
        /// <returns>
        /// A SqlDataAdapter.
        /// </returns>
        public static IDbDataAdapter GetDataAdapter(IDbCommand selectCommand)
        {
            return new SqlDataAdapter((SqlCommand)selectCommand);
        }

        /// <summary>
        /// Given the column name, get a command parameter name for the column.
        /// </summary>
        /// <param name="columnName">
        /// The column name for which to return a command parameter name.
        /// </param>
        /// <returns>
        /// The parameter name for the indicated column name.
        /// </returns>
        public static string GetSqlParamName(string columnName)
        {
            return "@" + columnName;
        }

        /// <summary>
        /// Returns a Datatable containing information about all of the indices for a specific 
        /// table in the database on the indicated connection.  
        /// </summary>
        /// <param name="data">
        /// the data object to connect through; if null Data.Current will be used instead
        /// </param>
        /// <param name="tableName">
        /// Name of the table for which we're interested in the indices
        /// </param>
        public static DataTable GetIndices(Data data, string tableName)
        {
            if (data == null)
                data = Data.Current;

            using (DbConnection conn = data.GetConnection() as DbConnection)
            {
                if (conn == null)
                    return null;

                conn.Open();
                return conn.GetSchema("Indexes", new[] { null, null, tableName, null });
            }
        }

        /// <summary>
        /// Gets a DataTable containing information about the tables of the current db
        /// </summary>
        /// <param name="data">
        /// the data object to connect through; if null Data.Current will be used instead
        /// </param>
        public static DataTable GetTables(Data data)
        {
            if (data == null)
                data = Data.Current;

            using (DbConnection conn = data.GetConnection() as DbConnection)
            {
                if (conn == null)
                    return null;

                conn.Open();
                return conn.GetSchema("Tables");
            }
        }

        /// <summary>
        /// Checks the db for the existence of an index
        /// </summary>
        /// <param name="tableName">
        /// name of the table
        /// </param>
        /// <param name="indexName">
        /// name of the index
        /// </param>
        /// <returns>
        /// true if the index exists, false if not
        /// </returns>
        public static bool IndexExists(string tableName, string indexName)
        {
            using (DbConnection conn = Data.Current.GetConnection() as DbConnection)
            {
                if (conn == null)
                    return false;

                conn.Open();
                DataTable index = conn.GetSchema("Indexes", new[] { null, null, tableName, indexName });
                return index != null && index.Rows.Count > 0;
            }
        }

        /// <summary>
        /// Resizes a text column
        /// </summary>
        public static void ResizeColumn(string tableName, string columnName, byte newSize)
        {
            if (ColumnExists(tableName, columnName))
                Data.Current.ExecuteCommand(string.Format(SqlResizeSql, tableName, GetColumnSql(new ColumnDefinition(columnName, typeof(string), newSize, false))));
        }

        /// <summary>
        /// Checks the db for the existence of a table
        /// </summary>
        /// <param name="tableName">
        /// name of the table
        /// </param>
        /// <returns>
        /// true if the table exists, false if not
        /// </returns>
        public static bool TableExists(string tableName)
        {
            using (DbConnection conn = Data.Current.GetConnection() as DbConnection)
            {
                if (conn == null)
                    return false;

                conn.Open();
                DataTable table = conn.GetSchema("Tables", new[] { null, null, tableName, null });
                return table != null && table.Rows.Count > 0;
            }
        }

        /// <summary>
        /// Wraps up a string for use in a sql statement as a comparison value
        /// </summary>
        /// <param name="text">text to wrap</param>
        /// <returns>wrapped text</returns>
        public static string TextValue(string text)
        {
            return string.Format("'{0}'", text.Replace("'", "''").Replace("’", "’’").Replace(((char)147).ToString(),
                                  "\"").Replace(((char)148).ToString(), "\"").Replace("\\\r\n", "\\\\\r\n\r\n"));
        }

        /// <summary>
        /// Wraps up a time for use in a sql statement
        /// </summary>
        /// <param name="date">date string</param>
        /// <returns>formatted date string</returns>
        public static string TimeValue(DateTime date)
        {
            return string.Format("'{0}'", date.ToString("hh:mm:ss tt", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Convert from a .Net data type to the appropriate SqlDbType.
        /// </summary>
        /// <param name="type">
        /// The .Net type to convert.
        /// </param>
        /// <param name="size">
        /// max size of the value; needed to distinguish between varchar and text strings
        /// </param>
        /// <returns>
        /// The corresponding SqlDbType.
        /// </returns>
        private static SqlDbType ToSqlDbType(Type type, int? size)
        {
            switch (type.Name)
            {
                case "Byte":
                case "SByte":
                    return SqlDbType.TinyInt;

                case "Int16":
                case "UInt16":
                    return SqlDbType.SmallInt;

                case "Int32":
                case "UInt32":
                case "Int64":
                case "UInt64":
                    return SqlDbType.Int;

                case "Double":
                    return SqlDbType.Float;

                case "Single":
                    return SqlDbType.Real;

                case "Decimal":
                    return SqlDbType.Money;

                case "Boolean":
                    return SqlDbType.Bit;

                case "Char":
                    return SqlDbType.Char;

                case "String":
                    if (size == null || size.Value < 256)
                        return SqlDbType.VarChar;

                    return SqlDbType.Text;

                case "DateTime":
                    return SqlDbType.DateTime;

                case "Byte[]":
                    return SqlDbType.Binary;

                case "Guid":
                    return SqlDbType.UniqueIdentifier;

                default:
                    return SqlDbType.VarChar;
            }
        }

        /// <summary>
        /// Get the appropriate keyword for use when aliasing fields/tables in SQL statemtents.
        /// </summary>
        /// <returns>
        /// The alias keyword.
        /// </returns>
        public const string AliasKeyword = " ";

        /// <summary>
        /// Creates an index on one or more columns of a table
        /// </summary>
        /// <param name="tableName">
        /// Name of table on which to create the index
        /// </param>
        /// <param name="indexName">
        /// Name of the index to create (if omitted, first column name will be used)
        /// </param>
        /// <param name="columns">
        /// array of columns to include in index
        /// </param>
        /// <param name="isUnique">
        /// whether or not the index should be unique
        /// </param>
        /// <param name="isPrimary">
        /// Whether or not this is to be the primary index for the table
        /// </param>
        public static void CreateIndex(string tableName, string indexName, IEnumerable<string> columns, bool isUnique, bool isPrimary)
        {
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName), "Table name is required.");

            if (string.IsNullOrEmpty(indexName))
                throw new ArgumentNullException(nameof(indexName), "Index name is required.");

            if (columns == null)
                throw new ArgumentNullException(nameof(columns), "One or more columns must be specified for any index.");

            IList<string> columnList = columns as IList<string> ?? columns.ToList();
            if (!columnList.Any())
                throw new ArgumentException("One or more columns must be specified for any index.");

            string sql = string.Format("CREATE {0}{1}Index {2} ON {3} ({4})", isUnique ? "Unique " : "",
                        isPrimary ? "Clustered " : "", indexName, tableName, string.Join(", ", columnList));

            Data.Current.ExecuteCommand(sql);
        }

        /// <summary>
        /// Puts together and executes a command to the db to create a new table
        /// </summary>
        /// <param name="tableName">
        /// Name of the new table
        /// </param>
        /// <param name="columns">
        /// Set of column definitions for the columns in the new table
        /// </param>
        public static void CreateTable(string tableName, IEnumerable<ColumnDefinition> columns)
        {
            Data.Current.ExecuteCommand(GetCreateTableScript(tableName, columns));
        }

        /// <summary>
        /// translates a column definition into the equivalent for use in a sql
        /// statement against a sql database
        /// </summary>
        /// <param name="column">
        /// Column definition with which to work
        /// </param>
        /// <returns>
        /// Sql string representation
        /// </returns>
        public static string GetColumnSql(ColumnDefinition column)
        {
            if (column.IsAutoNumber)
                return string.Concat(EscapeName(column.Name), " Int IDENTITY(1,1)");

            string columnSql;

            switch (ToSqlDbType(column.Type, column.Size))
            {
                case SqlDbType.BigInt:
                case SqlDbType.Int:
                    columnSql = string.Concat(EscapeName(column.Name), " Int");
                    break;

                case SqlDbType.TinyInt:
                    columnSql = string.Concat(EscapeName(column.Name), " TinyInt");
                    break;

                case SqlDbType.SmallInt:
                case SqlDbType.Bit:
                    columnSql = string.Concat(EscapeName(column.Name), " SmallInt");
                    break;

                case SqlDbType.Money:
                    columnSql = string.Concat(EscapeName(column.Name), " Money");
                    break;

                case SqlDbType.Real:
                    columnSql = string.Concat(EscapeName(column.Name), " Real");
                    break;

                case SqlDbType.Float:
                    columnSql = string.Concat(EscapeName(column.Name), " Float");
                    break;

                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.Time:
                    columnSql = string.Concat(EscapeName(column.Name), " DateTime");
                    break;

                case SqlDbType.Binary:
                    columnSql = string.Concat(EscapeName(column.Name), " Image");
                    break;

                case SqlDbType.VarChar:
                case SqlDbType.NVarChar:
                    if (column.Size == null)
                        columnSql = string.Concat(EscapeName(column.Name), " Text");
                    else
                        columnSql = string.Concat(EscapeName(column.Name), " VarChar(", column.Size.ToString(), ")");
                    break;

                case SqlDbType.Text:
                    columnSql = string.Concat(EscapeName(column.Name), " Text");
                    break;

                default:
                    throw new Exception("Unrecognized data type specified.");
            }

            if (column.AllowsNull)
                return string.Concat(columnSql, " NULL");

            return columnSql;
        }

        /// <summary>
        /// Wraps up a date for use in a sql statement
        /// </summary>
        /// <param name="date">date string</param>
        /// <returns>formatted date string</returns>
        public static string DateValue(DateTime date)
        {
            return string.Format("'{0}'", date.ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Escape this table or column name so that it may be used in a sql statement without conflicting with a reserved word.
        /// </summary>
        /// <param name="name">
        /// The name to escape.
        /// </param>
        /// <returns>
        /// The escaped name.
        /// </returns>
        public static string EscapeName(string name)
        {
            return String.Concat("[", name, "]");
        }

        /// <summary>
        /// Get the sql compatible with this backend for the indicated column function. This works with column functions which can take more than one operand.
        /// </summary>
        /// <param name="function">
        /// The function to return.
        /// </param>
        /// <param name="operands">
        /// The operands to the function.
        /// </param>
        public static string GetSqlFunction(SqlFunction function, params object[] operands)
        {
            StringBuilder sb = new StringBuilder();

            switch (function)
            {
                case SqlFunction.Year:
                    return String.Concat("Year(", operands[0], ")");

                case SqlFunction.Quarter:
                    return String.Concat("DatePart(q,", operands[0], ")");

                case SqlFunction.Month:
                    return String.Concat("Month(", operands[0], ")");

                case SqlFunction.Average:
                    return String.Concat("Avg(", operands[0], ")");

                case SqlFunction.Min:
                    return String.Concat("Min(", operands[0], ")");

                case SqlFunction.Max:
                    return String.Concat("Max(", operands[0], ")");

                case SqlFunction.Sum:
                    return String.Concat("Sum(", operands[0], ")");

                case SqlFunction.Count:
                    return String.Concat("Count(", operands[0], ")");

                case SqlFunction.Concat:
                    //Concatenate all the elements.
                    foreach (string operand in operands)
                    {
                        //We have to cast to a string to insure we don't perform addition.
                        sb.Append("Cast(");
                        sb.Append(operand);
                        sb.Append(" as varchar) + ");
                    }

                    //Remove trailing +.
                    string sql = sb.ToString();
                    if (sql != "")
                        sql = sql.Substring(0, sql.Length - 3);

                    return sql;

                case SqlFunction.ReplaceNull:
                    return String.Concat("ISNULL(", operands[0], ", ", operands[1], ")");

                case SqlFunction.If:
                    //IIf, if operand[0] = operand[1] then return operand[2] else return operand[3]
                    return String.Concat("CASE ", operands[0], " WHEN ", operands[1], " THEN ", operands[2], " ELSE ", operands[3], " END");

                case SqlFunction.ToDecimal:
                    return string.Format("CAST({0} AS dec(12,2))", operands);
            }

            return "";
        }

        /// <summary>
        /// This will put together a function for use in a SQL expression that offsets the value
        /// in a date field based on a non-standard end date for the current calendar year
        /// </summary>
        /// <param name="dateField">
        /// Name of the date field in the database being offset/selected
        /// </param>
        /// <param name="yearEndDate">
        /// Last day of the non-standard year in which the current date falls
        /// </param>
        /// <returns>
        /// Statement for use in a sql select
        /// </returns>
        public static string GetSqlFiscalYear(string dateField, DateTime yearEndDate)
        {
            // CFL - Lifted from IOBas and converted/modified
            // The strategy here is to figure out how many days are between the calendar's first day
            // and the fiscal year's first day. Then, we subtract that number of days from the given
            // date (DateX). If we take the year from this modified DateX, we'll have the fiscal year
            // of DateX.
            const string expr = "Year(DateAdd(Day, DateDiff(Day, Convert(VarChar(10), Convert(DateTime, '{0}/' +  " +
                "Convert(VarChar(4), Year({1}))), 101), Convert(DateTime, '01/01/' +  Convert(VarChar(4), Year({1})))), {1}))";

            string firstDay;
            if (yearEndDate.Month == 2 && yearEndDate.Day > 27)
                firstDay = "03/01";
            else
                firstDay = yearEndDate.AddDays(1).ToString("MM/dd");

            //If the current date is is in the same year as the year end date, but after it, we need to add 1
            bool offSetYear = false;
            if (yearEndDate.Year == DateTime.Today.Year)
            {
                if (yearEndDate.Month < DateTime.Today.Month)
                    offSetYear = true;
                else if (yearEndDate.Month == DateTime.Today.Month && yearEndDate.Day < DateTime.Today.Day)
                    offSetYear = true;
            }

            return string.Concat(string.Format(expr, firstDay, dateField), offSetYear ? " + 1" : "");
        }

        /// <summary>
        /// Get the sql statement to count records given the parts of the sql statement.
        /// </summary>
        /// <param name="key">
        /// The key of the table that will be used if distinct is true.
        /// </param>
        /// <param name="from">
        /// The from clause.
        /// </param>
        /// <param name="where">
        /// The where clause.
        /// </param>
        /// <param name="distinct">
        /// Whether we are counting distinct rows returned by the sql.
        /// </param>
        /// <returns>
        /// The sql statement to count records given the parts of the sql statement.
        /// </returns>
        public static string GetCountSql(string key, string from, string where, bool distinct)
        {
            if (!from.StartsWith("FROM ", StringComparison.OrdinalIgnoreCase))
                from = String.Concat("FROM ", from);

            if (!string.IsNullOrEmpty(where) && !where.StartsWith("WHERE ", StringComparison.OrdinalIgnoreCase))
                where = String.Concat("WHERE ", where);

            return String.Format("Select count({0}) {1} {2}", distinct ? String.Concat("Distinct ", key) : "*", from, where);
        }

        #endregion Methods

    }
}
