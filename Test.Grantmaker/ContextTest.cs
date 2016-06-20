using System;
using FluentAssertions;
using MicroEdge.Grantmaker;
using MicroEdge.Grantmaker.Business;
using MicroEdge.Grantmaker.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Igam.Common;

namespace Test.Grantmaker
{
    [TestClass]
    public class ContextTest : TestBase
    {
        /// <summary>
        /// Confirms the correct payload response is returned by Context.CreateApplicant 
        /// when ApplicantActions.CreateApplicant returns a blank email/password response
        /// </summary>
        [TestMethod]
        public void CreateApplicantMissingEmailPasswordTest()
        {
            string errorKey = string.Concat("CreateApplicant_", CreateApplicantResult.ErrorCodes.MissingEmailOrPassword);
            string expected = Errors.ResourceManager.GetString(errorKey);
            expected.Should().NotBeNullOrEmpty();

            Payload input = new Payload {CommandType = Payload.CommandTypes.CreateApplicant};
            input.AddParameter(Payload.ParameterKeys.Email, "");

            Payload target = Context.CreateApplicant(input);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(Payload.CommandTypes.CreateApplicantError);
            target[Payload.ParameterKeys.ErrorLocation].Should().Be(expected);
        }

        /// <summary>
        /// Confirms the correct payload response is returned by Context.CreateApplicant 
        /// when ApplicantActions.CreateApplicant returns an invalid email response
        /// </summary>
        [TestMethod]
        public void CreateApplicantInvalidEmailTest()
        {
            string errorKey = string.Concat("CreateApplicant_", CreateApplicantResult.ErrorCodes.InvalidEmail);
            string expected = Errors.ResourceManager.GetString(errorKey);
            expected.Should().NotBeNullOrEmpty();

            Payload input = new Payload { CommandType = Payload.CommandTypes.CreateApplicant };
            input.AddParameter(Payload.ParameterKeys.Email, "joeblow");
            input.AddParameter(Payload.ParameterKeys.Password, "password");

            Payload target = Context.CreateApplicant(input);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(Payload.CommandTypes.CreateApplicantError);
            target[Payload.ParameterKeys.ErrorLocation].Should().Be(expected);
        }

        /// <summary>
        /// Confirms the correct payload response is returned by Context.CreateApplicant 
        /// when ApplicantActions.CreateApplicant returns an success response
        /// </summary>
        [TestMethod]
        public void CreateApplicantSuccessTest()
        {
            Payload input = new Payload { CommandType = Payload.CommandTypes.CreateApplicant };
            input.AddParameter(Payload.ParameterKeys.Email, "joeblow@noway.net");
            input.AddParameter(Payload.ParameterKeys.Password, "password");
            input.AddParameter(Payload.ParameterKeys.ApplicationId, "7777");

            Payload target = Context.CreateApplicant(input);
            target.CommandType.Should().Be(Payload.CommandTypes.CreateApplicantSuccess);
            Convert.ToInt32(target[Payload.ParameterKeys.ApplicantId]).Should().BeGreaterThan(0);
        }

    }
}
