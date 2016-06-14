using FluentAssertions;
using MicroEdge.Grantmaker;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Grantmaker
{
    [TestClass]
    public class CFSTest
    {
        /// <summary>
        /// Confirms we get the proper error response when sending in a blank/null serial number
        /// </summary>
        [TestMethod]
        public void AuthenticateBlankSerialNumberTest()
        {
            CFS cfs = new CFS();
            Payload target = cfs.Authenticate("123", null, "checksum");
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(MicroEdge.Grantmaker.Properties.Errors.CFS_MissingSerialNumber);

            target = cfs.Authenticate("123", string.Empty, "checksum");
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(MicroEdge.Grantmaker.Properties.Errors.CFS_MissingSerialNumber);
        }

        /// <summary>
        /// Confirms we get the proper error response when sending in a blank/null checksum
        /// </summary>
        [TestMethod]
        public void AuthenticateBlankCheckSumTest()
        {
            CFS cfs = new CFS();
            Payload target = cfs.Authenticate("123", "12345", null);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(MicroEdge.Grantmaker.Properties.Errors.CFS_MissingCheckSum);

            target = cfs.Authenticate("123", "12345", string.Empty);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(MicroEdge.Grantmaker.Properties.Errors.CFS_MissingCheckSum);
        }
    }
}
