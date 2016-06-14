using FluentAssertions;
using MicroEdge.Grantmaker.Business;
using MicroEdge.Igam.Business.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Igam.Common;

namespace Test.Grantmaker.Business
{
    [TestClass]
    public class ApplicantActionsTest : TestBase
    {
        [TestMethod]
        public void BlankEmailTest()
        {
            CreateApplicantResult result = ApplicantActions.CreateApplicant(null, "password", 0);
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
            result = ApplicantActions.CreateApplicant(string.Empty, "password", 0);
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
        }

        [TestMethod]
        public void BlankPasswordTest()
        {
            CreateApplicantResult result = ApplicantActions.CreateApplicant("email", null, 0);
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
            result = ApplicantActions.CreateApplicant("email", string.Empty, 0);
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
        }

        [TestMethod]
        public void InvalidEmailTest()
        {
            CreateApplicantResult result = ApplicantActions.CreateApplicant("email", "password", 0);
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.InvalidEmail);
        }

        [TestMethod]
        public void SuccessTest()
        {
            TestApplicantData data = new TestApplicantData();
            QueueTestDalObject(typeof(IApplicantData), data);

            CreateApplicantResult result = ApplicantActions.CreateApplicant("jimbob@noway.net", "password", 12345);
            result.Success.Should().BeTrue();
            data.Id.Should().NotBe(0);
            data.Email.Should().Be("jimbob@noway.net");
            data.Password.Should().Be("password");
            data.CurrentApplicationId.Should().Be(12345);
            result.ApplicantId.Should().Be(data.Id);
        }
    }
}
