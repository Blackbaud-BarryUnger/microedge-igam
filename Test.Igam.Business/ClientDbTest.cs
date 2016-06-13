using FluentAssertions;
using MicroEdge.Igam.Business;
using MicroEdge.Igam.Business.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Igam.Common;

namespace Test.Igam.Business
{
    [TestClass]
    public class ClientDbTest : TestBase
    {
        /// <summary>
        /// Confirm that an instance of the class is properly initialized on construction
        /// </summary>
        [TestMethod]
        public void ConstructorTest()
        {
            const string rootPath = @"c:\cfsroot\test";

            TestPreferencesData dal = new TestPreferencesData();
            dal.CFSRoot = rootPath;
            QueueTestDalObject(typeof(IPreferencesData), dal);

            ClientDb target = new ClientDb("TEST123");
            target.SiteId.Should().Be("TEST123");
            target.Root.Should().Be(string.Concat(rootPath, @"\SID_TEST123"));
        }
    }
}
