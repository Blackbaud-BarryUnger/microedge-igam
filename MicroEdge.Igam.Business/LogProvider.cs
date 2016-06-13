using System.Diagnostics;
using System.Reflection;
using MicroEdge.Igam.Providers.Logging;
using MicroEdge.Logging;
using Serilog;

namespace MicroEdge.Igam.Business.Logging
{
	/// <summary>
	/// This class implements the LogProvider for the application.
	/// </summary>
	public class LogProvider : ILogProvider
	{
        #region Fields

        private static ILogger _log;

        #endregion Fields

        #region Constructors

	    public LogProvider(string applicationName)
	    {
	        if (_log != null || string.IsNullOrEmpty(applicationName))
	            return;

            _log = Logger.GetLogger(applicationName,
                FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
        }

        #endregion Constructors

		#region Methods

        /// <summary>
        /// Log a debug message
        /// </summary>
        /// <param name="message">
        /// Text of the message (may be a template with merge fields for parameters)
        /// </param>
        /// <param name="parameters">
        /// Parameter values that should be merged into the message
        /// </param>
        public void LogDebug(string message, params object[] parameters)
	    {
            _log.Debug(message, parameters);
        }

        /// <summary>
        /// Log an information message
        /// </summary>
        /// <param name="message">
        /// Text of the message (may be a template with merge fields for parameters)
        /// </param>
        public void LogInfo(string message)
		{
            _log.Information(message);
		}

        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">
        /// Text of the message (may be a template with merge fields for parameters)
        /// </param>
		public void LogWarning(string message)
		{
            _log.Warning(message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">
        /// Text of the message (may be a template with merge fields for parameters)
        /// </param>
        /// <param name="parameters">
        /// Parameter values that should be merged into the message
        /// </param>
		public void LogError(string message, params object[] parameters)
		{
            _log.Error(message, parameters);
		}

		#endregion Methods
	}
}
