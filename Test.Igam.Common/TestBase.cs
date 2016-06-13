using System;
using System.Collections;
using MicroEdge.Provider.Dal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Igam.Common
{
    public class TestBase
    {
        private readonly TestDalProvider _dalProvider = new TestDalProvider();

        /// <summary>
        /// Put any logic that should happen before each test in here
        /// </summary>
        [TestInitialize]
        public void BeforeEach()
        {
            DalManager.Initialize(_dalProvider);
        }

        public void QueueTestDalObject(Type type, object dalObject)
        {
            if (!_dalProvider.DalObjects.ContainsKey(type))
                _dalProvider.DalObjects.Add(type, new Queue());

            _dalProvider.DalObjects[type].Enqueue(dalObject);
        }
    }
}
