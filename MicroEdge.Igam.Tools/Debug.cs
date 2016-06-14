using System.Diagnostics;
using System;

namespace MicroEdge
{
	/// <summary>
	/// Static class of debugging tools.
	/// </summary>
	public static class Debug
	{
		#region Methods

		/// <summary>
		/// Checks for a condition; if the condition is false, throws an exception.
		/// </summary>
		/// <param name="condition">Condition to check.</param>
		/// <remarks>
		/// This is on evaluated in DEBUG.
		/// </remarks>
		[ConditionalAttribute("DEBUG")]
		public static void Assert(bool condition)
		{
			if (!condition)
			{
				throw new SystemException("Debug assertion failure.");
			}
		}

		/// <summary>
		/// Checks for a condition; if the condition is false, throws an exception with a message.
		/// </summary>
		/// <param name="condition">Condition to check.</param>
		/// <param name="message">Message to throw in exception on failure.</param>
		/// <remarks>
		/// This is on evaluated in DEBUG.
		/// </remarks>
		[ConditionalAttribute("DEBUG")]
		public static void Assert(bool condition, string message)
		{
			if (!condition)
			{
				throw new SystemException(message);
			}
		}

		/// <summary>
		/// Throws an exception with a message.
		/// </summary>
		/// <param name="message">Message to throw in exception.</param>
		/// <remarks>
		/// This is on evaluated in DEBUG.
		/// </remarks>
		[ConditionalAttribute("DEBUG")]
		public static void Fail(string message)
		{
			throw new SystemException(message);
		}

		/// <summary>
		/// Reports the message to the output window.
		/// </summary>
		/// <param name="message">The message.</param>
		[ConditionalAttribute("DEBUG")]
		public static void ReportMessage(string message)
		{
			ReportMessage(message, false);
		}

		/// <summary>
		/// Reports the message to the output window and optionally throws an exception.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="throwException">if set to <c>true</c> [throw exception].</param>
		[ConditionalAttribute("DEBUG")]
		public static void ReportMessage(string message, bool throwException)
		{
			Console.WriteLine(message);

			if (throwException)
			{
				throw new SystemException(message);
			}
		}

		#endregion
	}
}