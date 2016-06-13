namespace MicroEdge.Provider.Dal
{
	/// <summary>
	/// DalManager manages access to the Data Access Layer by leveraging a DalProvider 
	/// </summary>
	public static class DalManager
	{
		#region Fields

		private static IDalProvider _provider;

		#endregion Fields

		#region Constructor

		/// <summary>
		/// Default constructor.
		/// </summary>
		static DalManager()
		{
			_provider = null;
		}

		#endregion Constructor

		#region Properties

		/// <summary>
		/// Get the current cache provider. 
		/// </summary>
		/// <remarks>
		/// Note that unlike other providers, this provider manager allows the provider to be set so
		/// that the same DAL provider doesn't need to be used throughout a project.
		/// </remarks>
		public static IDalProvider Provider
		{
			get
			{
				if (_provider == null)
                    _provider = new NoDalProvider();
				return _provider;
			}
			set { _provider = value; }
		}

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initializes to use the provided log provider
        /// </summary>
        public static void Initialize(IDalProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Initialize the data connection.
        /// </summary>
        /// <param name="parameters">
        /// Any parameters needed to initialize the connection.
        /// </param>
        public static void Initialize(object parameters)
		{
			Provider.Initialize(parameters);
		}
		public static void Initialize()
		{
			Provider.Initialize();
		}

		/// <summary>
		/// Instantiate a DAL object given the type of the object.
		/// </summary>
		/// <returns>
		/// An instance of the indicated DAL class from the appropriate assembly.
		/// </returns>
		public static T GetDalObject<T>() where T : class
		{
			return Provider.GetDalObject<T>();
		}

		/// <summary>
		/// Get the ITransactionScope object to be used to perform transaction processing in the current DAL provider.
		/// </summary>
		/// <returns>
		/// The ITransactionScope object to be used to perform transaction processing in the current DAL provider.
		/// </returns>
		public static ITransactionScope GetTransactionScope()
		{
			return Provider.GetTransactionScope();
		}

		#endregion Methods
	}
}
