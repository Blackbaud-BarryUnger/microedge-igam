using System;

namespace MicroEdge
{
	/// <summary>
	/// This is the exception class that should be used with all MicroEdge system exceptions. 
	/// </summary>
	/// <remarks>
	///  Author:    LDF
	///  Created:   09/25/2008
	/// </remarks>
	/// <see>
	/// </see>
	[Serializable]
	public class Exception : SystemException
	{
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
		public Exception(string message, System.Exception e) : base(message, e) 
		{
			//Set this field so that this will not be seen as a system exception.
			_systemException = false;
		}
		public Exception(string message) : this(message, null)
		{}

		#endregion Constructors
	}
	}
