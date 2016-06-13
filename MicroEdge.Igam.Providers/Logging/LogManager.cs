using System;

namespace MicroEdge.Igam.Providers.Logging
{
	/// <summary>
	/// Manages logging resources using a log provider
	/// </summary>
	public static class LogManager
	{
		#region Fields

		private static ILogProvider _provider;

		#endregion Fields

		#region Constructor

		/// <summary>
		/// Default constructor.
		/// </summary>
		static LogManager()
		{
			_provider = null;
		}

		#endregion Constructor

		#region Properties

		/// <summary>
		/// Get the current log provider.
		/// </summary>
		private static ILogProvider Provider
		{
			get
			{
                if (_provider == null)
				    _provider = new NoLogProvider();
                return _provider;
			}
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Initializes to use the provided log provider
		/// </summary>
		public static void Initialize(ILogProvider provider)
		{
		    _provider = provider;
		}

	    public static void LogDebug(string message, params object[] parameters)
	    {
	        Provider.LogDebug(message, parameters);
	    }

		public static void LogInfo(string message)
		{
			Provider.LogInfo(message);
		}

		public static void LogWarning(string warning)
		{
			Provider.LogWarning(warning);
		}

		public static void LogError(string error, params object[] parameters )
		{
			Provider.LogError(error, parameters);
		}

		#endregion Methods
	}

    #region NoLogProvider

    /// <summary>
    /// This is the class that is used as the log provider when there is no log provider setup in the config file.
    /// </summary>
    public class NoLogProvider : ILogProvider
    {
        public void LogDebug(string message, params object[] parameters)
        { }
        public void LogInfo(string message)
        { }
        public void LogWarning(string warning)
        { }
        public void LogError(string error, params object[] parameters)
        { }
        public void LogException(Exception ex)
        { }
        public void LogException(Exception ex, string serverAddress, string clientAddress)
        { }
    }

    #endregion NoLogProvider
}