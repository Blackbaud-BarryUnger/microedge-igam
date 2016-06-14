using System;

namespace MicroEdge
{
	/// <summary>
	/// This is the exception class that should be used with all MicroEdge exceptions. This allows us to
	/// identify those exceptions coming from MicroEdge code and also provides static methods to format messages
	/// that allow all messages from the entire exception stack to be returned.
	/// </summary>
	/// <remarks>
	///  Author:    LDF
	///  Created:   08/02/2006
	/// </remarks>
	/// <see>
	/// </see>
	[Serializable]
	public class SystemException : System.Exception
	{
		#region Fields

		protected bool _systemException = true;

		public const string DataStoreIdKey = "DataStoreId";

		#endregion Fields

		#region Constructors

		/// <summary>
		/// These constructors allow the user to specify an error message and an inner exception.
		/// </summary>
		/// <param name="message">
		/// The error message. 
		/// </param>
		/// <param name="e">
		/// The inner exception to wrap in this new exception.
		/// </param>
		public SystemException(string message, System.Exception e) : base(message, e) {}
		public SystemException(string message) : base(message) { }

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Whether this is considered a system exception. Messages from system exceptions are not for clients, only for MicroEdge.
		/// </summary>
		public bool IsSystemException
		{
			get { return _systemException; }
		}

		/// <summary>
		/// If this is a system exception, this will be the id of the error message in the application log. 
		/// </summary>
		public int DataStoreId
		{
			get
			{
				if (Data.Contains(DataStoreIdKey))
					return Tools.ToInt32(Data[DataStoreIdKey]);
				
				return 0;
			}
			set
			{
				Data[DataStoreIdKey] = value;
			}
		}

		/// <summary>
		/// Returns true if the heart of this exception is a System.Exception or MicroEdge.SystemException.
		/// </summary>
		public bool IsInnerSystemException
		{
			get
			{
				//If there is no inner exception, return false.
				if (InnerException == null)
					return false;

				SystemException innerSystemException = InnerException as SystemException;
				//If the inner exception is not a MicroEdge.SystemException, return True.
				if (innerSystemException == null)
					return true;

				//Otherwise return the IsInnerSystemException property of the inner exception.
				return innerSystemException.IsInnerSystemException;
			}
		}

		public override string StackTrace
		{
			get
			{
				if (InnerException == null)
					return base.StackTrace;
				
				return InnerException.StackTrace + "\r\n" + base.StackTrace;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Set the Data Store Id of the exception and all inner exceptions. This id indicates the log record that holds the message of the system exception.
		/// </summary>
		/// <param name="ex"></param>
		/// <param name="id"></param>
		public static void SetDataStoreId(System.Exception ex, int id)
		{
			//Skip Csla data portal exceptions since we don't report these anyway.
			if (IsCslaDataPortalException(ex))
			{
				if (ex.InnerException != null)
					SetDataStoreId(ex.InnerException, id);
			}
			else
			{
				SystemException meEx = ex as SystemException;

				//If not inherited from MicroEdge.SystemException, this is a system exception.
				if (meEx == null)
					ex.Data[DataStoreIdKey] = id;

				else if (meEx.IsSystemException)
					meEx.DataStoreId = id;

				//If the exception is not a system exception, but it has an inner exception, pass the call on to this inner exception.
				else if (ex.InnerException != null)
					SetDataStoreId(ex.InnerException, id);
			}
		}

		/// <summary>
		/// This will return a formatted error message including all inner exception messages of the indicated
		/// exception.
		/// </summary>
		/// <param name="exception">
		/// The exception whose messages should be returned.
		/// </param>
		/// <returns>
		/// A formatted error message that may be displayed to the user.
		/// </returns>
		public static string GetMessage(System.Exception exception)
		{
			return GetMessage(exception, false);
		}


		/// <summary>
		/// This will return a formatted error message including all inner exception messages of the indicated
		/// exception.
		/// </summary>
		/// <param name="exception">
		/// The exception whose messages should be returned.
		/// </param>
		/// <param name="singleLine">
		/// True if the error message should be only a single line without line feeds.
		/// </param>
		/// <returns>
		/// A formatted error message that may be displayed to the user.
		/// </returns>
		public static string GetMessage(System.Exception exception, bool singleLine)
		{
			string message = exception.Message;

			if (Tools.ToInt32(exception.Data[DataStoreIdKey]) != 0)
			{
				//This is where we get off. The existence of the data store id indicates that we should not display
				//system exceptions. Instead we will display the id.
				return String.Concat("[System Error #", exception.Data[DataStoreIdKey], "]");
			}
			
			if (exception.InnerException == null)
			{
				return message;
			}
			
			//If this is a Csla data portal error, don't show its message, just the inner messages.
			if (IsCslaDataPortalException(exception))
				return GetMessage(exception.InnerException, singleLine);

			if (singleLine)
				return message + " " + GetMessage(exception.InnerException, true);
			
			return message + "\r\n\r\n" + GetMessage(exception.InnerException, false);
		}

		/// <summary>
		/// Returns true if this exception is a Csla data portal exception.
		/// </summary>
		public static bool IsCslaDataPortalException(System.Exception exception)
		{
			string className = exception.GetType().Name;
			return (className == "CallMethodException" || className == "DataPortalException");
		}

		#endregion Methods
	}
}
