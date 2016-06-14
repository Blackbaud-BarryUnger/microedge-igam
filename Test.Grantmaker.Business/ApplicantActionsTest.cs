using FluentAssertions;
using MicroEdge.Grantmaker.Business;
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
            CreateApplicantResult result = ApplicantActions.CreateApplicant(null, "password");
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
            result = ApplicantActions.CreateApplicant(string.Empty, "password");
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
        }

        [TestMethod]
        public void BlankPasswordTest()
        {
            CreateApplicantResult result = ApplicantActions.CreateApplicant("email", null);
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
            result = ApplicantActions.CreateApplicant("email", string.Empty);
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
        }

        [TestMethod]
        public void InvalidEmailTest()
        {
            CreateApplicantResult result = ApplicantActions.CreateApplicant("email", "password");
            result.Success.Should().Be(false);
            result.ErrorCode.Should().Be(CreateApplicantResult.ErrorCodes.InvalidEmail);
        }
    }
}
