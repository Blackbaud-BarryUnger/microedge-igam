namespace MicroEdge.Igam.Providers.Dal
{
	/// <summary>
	/// Interface that any DAL provider used by the DAL manager must implement.
	/// </summary>
	/// 
	/// <remarks>
	/// Author:  LDF
	/// Created: 5/9/2013
	/// </remarks>
	public interface IDalProvider
	{
		#region Methods

		void Initialize(object parameters);
		void Initialize();
		T GetDalObject<T>() where T : class;
		ITransactionScope GetTransactionScope();

		#endregion Methods
	}
}
