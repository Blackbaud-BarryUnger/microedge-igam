using System;
using MicroEdge.Igam.Business.DataAccess;
using MicroEdge.Provider.Dal;

namespace MicroEdge.Igam.Dal
{
    public class DalProvider : IDalProvider
    {
        public bool IsConnected { get; set; }
        public void Initialize(object parameters)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public virtual T GetDalObject<T>() where T : class
        {
            Type type = typeof (T);
            if (type == typeof(IPreferencesData))
                return new PreferencesData() as T;

            throw new Exception(string.Format("There is no implementation defined for {0}", type.Name));
        }

        public ITransactionScope GetTransactionScope()
        {
            throw new NotImplementedException();
        }
    }
}
