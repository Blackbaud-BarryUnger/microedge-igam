using System;
using System.Collections;
using System.Collections.Generic;
using MicroEdge.Igam.Business.DataAccess;
using MicroEdge.Igam.Providers.Dal;

namespace Test.Igam.Common
{
    public class TestDalProvider : IDalProvider
    {
        public TestDalProvider()
        {
            DalObjects = new Dictionary<Type, Queue>();
        }

        public Dictionary<Type, Queue> DalObjects { get; }

        public void Initialize(object parameters)
        { }

        public void Initialize()
        { }

        public T GetDalObject<T>() where T : class
        {
            Type type = typeof(T);

            if (DalObjects.ContainsKey(type))
                return (T) DalObjects[type].Dequeue();

            if (type == typeof(IPreferencesData))
                return new TestPreferencesData() as T;

            if (type == typeof (IApplicantData))
                return new TestApplicantData() as T;

            return null;
        }

        public ITransactionScope GetTransactionScope()
        {
            return new NoTransactionScope();
        }
    }
}
