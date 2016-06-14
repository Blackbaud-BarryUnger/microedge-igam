using System.Collections.Generic;
using System.Data;
using System.Text;

namespace MicroEdge.Igam.Data
{
    public class RowCollection : List<Row>
    {
        #region Fields

        // ReSharper disable InconsistentNaming
        //Table this row is associated with.
        protected string tableName; 
        //Data connection this row is associated with.
        protected Data data;
        // ReSharper restore InconsistentNaming

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
        public RowCollection(Data data, string tableName)
        {
            this.data = data;
            this.tableName = tableName;
        }

        /// <summary>
        /// Constructor to initialize database connection and table name from which to read and intialize the values
        /// of the row with the data from a particular datareader row.
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
        public RowCollection(Data data, string tableName, IDataReader dr) : this(data, tableName)
        {
            SetRows(dr);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The MicroEdge.Igam.Data connection used to read this row of data.
        /// </summary>
        public Data Data
        {
            get { return data; }
        }

        /// <summary>
        /// The name of the table of this row.
        /// </summary>
        public string TableName
        {
            get { return tableName; }
        }

        /// <summary>
        /// If any member of this collection is dirty, the collection is dirty.
        /// </summary>
        public virtual bool IsDirty
        {
            get
            {
                foreach (Row row in this)
                    if (row.IsDirty) return true;

                return false;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Read rows of data from the associated table given the indicated where clause.
        /// </summary>
        /// <param name="where">
        /// A SQL Where clause to indicate the rows to read.
        /// </param>
        /// <returns>
        /// ReturnStatus.NotFound if record no found. ReturnStatus.Success otherwise.
        /// </returns>
        public virtual ReturnStatus ReadWhere(string where)
        {
            //Insure the where clause starts with Where.
            if (where != "" && !where.ToLower().StartsWith("where"))
                where = "Where " + where;

            string sql = "Select * From " + TableName + " " + where;

            return Read(sql);
        }

        /// <summary>
        /// Read columns of rows of data from the associated table given the indicated where clause and columns 
        /// to read.
        /// </summary>
        /// <param name="where">
        /// A SQL Where clause to indicate the rows to read.
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
            sqlColumns.Append(columns[0]);

            for (int i = 1; i < columns.Length; i++)
            {
                sqlColumns.Append(", ");
                sqlColumns.Append(columns[i]);
            }

            //Insure the where clause starts with Where.
            if (where != "" && !where.ToLower().StartsWith("where "))
                where = "Where " + where;

            string sql = "Select " + sqlColumns + " From " + TableName + " " + where;

            return Read(sql);
        }

        /// <summary>
        /// This will read rows from the database using the indicated sql.
        /// </summary>
        /// <param name="sql">
        /// A complete sql string used to read the rows of data.
        /// </param>
        /// <returns>
        /// ReturnStatus.NotFound if record not found. ReturnStatus.Success otherwise.
        /// </returns>
        public ReturnStatus Read(string sql)
        {
            return Read(SqlTools.GetCommand(sql));
        }

        /// <summary>
        /// This will read rows from the database using the indicated command.
        /// </summary>
        /// <param name="command">
        /// The command containing the sql statement used to read the rows of data.
        /// </param>
        /// <returns>
        /// ReturnStatus.NotFound if record not found. ReturnStatus.Success otherwise.
        /// </returns>
        public ReturnStatus Read(IDbCommand command)
        {
            try
            {
                ReturnStatus returnStatus = ReturnStatus.Success;

                using (IDataReader reader = data.GetReader(command))
                {
                    SetRows(reader);
                }

                return returnStatus;
            }
            catch (System.Exception e)
            {
                throw new SystemException(string.Format("Error occurred while reading a row from the {0} table.", TableName), e);
            }
        }

        /// <summary>
        /// Initialize new rows of the collection with the data from the passed datareader.
        /// </summary>
        /// <param name="dr">
        /// The datareader to user to initialize the rows of the collection.
        /// </param>
        private void SetRows(IDataReader dr)
        {
            Clear();
            while (dr.Read())
                Add(new Row(data, tableName, dr));
        }

        #endregion Methods
    }
}
