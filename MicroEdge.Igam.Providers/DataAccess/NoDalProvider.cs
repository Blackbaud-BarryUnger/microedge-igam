namespace MicroEdge.Provider.Dal
{
	/// <summary>
	/// This is the class that is used for the DalProvider when there is no dal provider setup in the 
	/// config file. 
	/// </summary>
	/// 
	/// <remarks>
	/// Author:  LDF
	/// Created: 5/23/2013
	/// </remarks>
	public class NoDalProvider : IDalProvider
	{
		#region Methods

		/// <summary>
		/// Initialize the data connection. Nothing to be done for no provider.
		/// </summary>
		/// <param name="parameters"></param>
		public void Initialize(object parameters)
		{}
		public void Initialize()
		{ }

        /// <summary>
        /// Instantiate a DAL object given the type of the object.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the DAL object to instantiate.
        /// </typeparam>
        /// <returns>
        /// An instance of the indicated DAL class from the appropriate assembly.
        /// </returns>
        public T GetDalObject<T>() where T : class
		{
			return default(T);
		}

		/// <summary>
		/// Get the ITransactionScope object to be used to perform transaction processing when there is no DAL provider. This will be NoTransactionScope.
		/// </summary>
		/// <returns>
		/// The ITransactionScope object to be used to perform transaction processing when there is no DAL provider
		/// </returns>
		public ITransactionScope GetTransactionScope()
		{
			return new NoTransactionScope();
		}

		#endregion Methods
	}
}
