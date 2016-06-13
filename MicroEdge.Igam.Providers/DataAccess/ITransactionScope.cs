using System;
using System.Collections.Generic;
using System.Text;

namespace MicroEdge.Provider.Dal
{
	/// <summary>
	/// Interface that any TransactionScope used by a DAL provider must implement.
	/// </summary>
	/// 
	/// <remarks>
	/// Author:  LDF
	/// Created: 5/23/2013
	/// </remarks>
	public interface ITransactionScope : IDisposable 
	{
		#region Methods

		void Complete();

		#endregion Methods
	}
}
