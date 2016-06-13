using System.Net;
using System.Web;
using FluentAssertions;
using MicroEdge.Igam.Business;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Igam.Common;

namespace Test.Igam.Business
{
    [TestClass]
    public class CommonRequestTest : TestBase
    {
        [TestMethod]
        public void ConstructorTest()
        {
            HttpRequest request = new HttpRequest("", "http://www.commonrequest.test", "me=test&you=not");

            CommonRequest target = new CommonRequest(request);
            target.Should().NotBeNull();
            target.ResponseHttpStatus.Should().Be(HttpStatusCode.OK);

            target.RequestData.Should().ContainKey("me");
            target.RequestData["me"].Should().Be("test");

            target.RequestData.Should().ContainKey("you");
            target.RequestData["you"].Should().Be("not");
        }
    }
}
