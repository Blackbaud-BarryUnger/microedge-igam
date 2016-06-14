using System.IO;
using FluentAssertions;
using MicroEdge.Grantmaker;
using MicroEdge.Grantmaker.Properties;
using MicroEdge.Igam.Business.DataAccess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Igam.Common;

namespace Test.Grantmaker
{
    [TestClass]
    public class CFSTest : TestBase
    {
        /// <summary>
        /// Confirms we get the proper error response when sending in a blank/null serial number
        /// </summary>
        [TestMethod]
        public void AuthenticateBlankSerialNumberTest()
        {
            CFS cfs = new CFS();
            Payload target = cfs.Authenticate("123", null, "checksum", false);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(Errors.CFS_MissingSerialNumber);

            target = cfs.Authenticate("123", string.Empty, "checksum", false);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(Errors.CFS_MissingSerialNumber);
        }

        /// <summary>
        /// Confirms we get the proper error response when sending in a blank/null checksum
        /// </summary>
        [TestMethod]
        public void AuthenticateBlankCheckSumTest()
        {
            CFS cfs = new CFS();
            Payload target = cfs.Authenticate("123", "12345", null, false);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(Errors.CFS_MissingCheckSum);

            target = cfs.Authenticate("123", "12345", string.Empty, false);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(Errors.CFS_MissingCheckSum);
        }

        /// <summary>
        /// Confirms we get the proper error response when no CFS directory exists for the sid sent in
        /// </summary>
        [TestMethod]
        public void AuthenticateNoCfsFolderTest()
        {
            const string testDirectory = @"c:\cfstest";
            Directory.CreateDirectory(testDirectory);

            TestPreferencesData data = new TestPreferencesData();
            data.CFSRoot = testDirectory;
            QueueTestDalObject(typeof(IPreferencesData), data);

            CFS cfs = new CFS();
            Payload target = cfs.Authenticate("456", "12345", "checksum", false);
            target.CommandType.Should().Be(Payload.CommandTypes.Error);
            target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.AuthenticationError);
            target[Payload.ParameterKeys.ErrorLocation].Should()
                .Be(Errors.CFS_MissingCFSDirectory);
        }

        /// <summary>
        /// Confirms we get the proper error response when sending in a serial number that doesn't match the config on disk
        /// </summary>
        [TestMethod]
        public void AuthenticateWrongSerialNumberTest()
        {
            const string testDirectory = @"c:\cfstest\SID_123\GLOBAL_RESOURCES";
            try
            {
                Directory.CreateDirectory(testDirectory);
                File.WriteAllText(string.Concat(testDirectory, @"\gifts.cfg"),
                    string.Join(CFS.FieldMarker.ToString(), "", "", "", "99877"));

                TestPreferencesData data = new TestPreferencesData();
                data.CFSRoot = @"c:\cfstest";
                QueueTestDalObject(typeof (IPreferencesData), data);

                CFS cfs = new CFS();
                Payload target = cfs.Authenticate("123", "12345", "checksum", false);
                target.CommandType.Should().Be(Payload.CommandTypes.Error);
                target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.NoLicenseError);
                target[Payload.ParameterKeys.ErrorLocation].Should()
                    .Be(Errors.CFS_WrongSerialNumber);
            }
            finally
            {
                Directory.Delete(testDirectory, true);
            }
        }

        /// <summary>
        /// Confirms we get the proper error response when the config on disk doesn't include IGAM
        /// </summary>
        [TestMethod]
        public void AuthenticateNotConfiguredTest()
        {
            const string testDirectory = @"c:\cfstest\SID_123\GLOBAL_RESOURCES";
            try
            {
                Directory.CreateDirectory(testDirectory);
                File.WriteAllText(string.Concat(testDirectory, @"\gifts.cfg"),
                    string.Join(CFS.FieldMarker.ToString(), "10", "123", "CFS Test Org", "12345", "", "", "5.4567890"));

                TestPreferencesData data = new TestPreferencesData();
                data.CFSRoot = @"c:\cfstest";
                QueueTestDalObject(typeof(IPreferencesData), data);

                CFS cfs = new CFS();
                Payload target = cfs.Authenticate("123", "12345", "5.4567890", false);
                target.CommandType.Should().Be(Payload.CommandTypes.Error);
                target[Payload.ParameterKeys.ErrorCode].Should().Be(CFS.AuthenticateResponseTypes.NoLicenseError);
                target[Payload.ParameterKeys.ErrorLocation].Should()
                    .Be(Errors.CFS_NotConfigured);
            }
            finally
            {
                Directory.Delete(testDirectory, true);
            }
        }

        /// <summary>
        /// Confirms we get a null response when the client is successfully authenticated
        /// </summary>
        [TestMethod]
        public void AuthenticateSuccessTest()
        {
            const string testDirectory = @"c:\cfstest\SID_123\GLOBAL_RESOURCES";
            try
            {
                Directory.CreateDirectory(testDirectory);
                File.WriteAllText(string.Concat(testDirectory, @"\gifts.cfg"),
                    string.Join(CFS.FieldMarker.ToString(), "131", "123", "CFS Test Org", "12345", "", "", "5.456789012987654532212"));

                TestPreferencesData data = new TestPreferencesData();
                data.CFSRoot = @"c:\cfstest";
                QueueTestDalObject(typeof(IPreferencesData), data);

                CFS cfs = new CFS();
                Payload target = cfs.Authenticate("123", "12345", "5.456789012", false);
                target.Should().BeNull();
            }
            finally
            {
                Directory.Delete(testDirectory, true);
            }
        }

    }
}
