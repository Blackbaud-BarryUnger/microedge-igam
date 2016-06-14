using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroEdge.Igam.Providers.Dal
{
	/// <summary>
	/// This is the class that is returned by the DalProvider's GetTransactionScope method when the DalProvider doesn't 
	/// support transcations.
	/// </summary>
	/// 
	/// <remarks>
	/// Author:  LDF
	/// Created: 5/23/2013
	/// </remarks>
	public class NoTransactionScope : ITransactionScope
	{
		#region Methods

		public void Complete()
		{}

		public void Dispose()
		{}

		#endregion Methods
	}
}
