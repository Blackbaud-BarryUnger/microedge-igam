using System.Collections.Generic;
using System.Xml.Linq;
using MicroEdge.Igam.Business;

namespace MicroEdge.Grantmaker.Business
{
    public class Payload
    {
        #region Constants

        private const string GrantMakerServer = "GRANTMAKER_SERVER";
        
        public static class CommandTypes
        {
            public const string CreateApplicant = "APPLICANT_CREATE";
            public const string CreateApplicantError = "APPLICANT_CREATE_ERROR";
            public const string CreateApplicantSuccess = "APPLICANT_CREATE_SUCCESS";
            public const string Error = "ERROR";
            public const string SidConfirmed = "SID_CONFIRMED";
        }

        public static class ParameterKeys
        {
            public const string Email = "EMAIL";
            public const string ErrorCode = "ERROR_CODE";
            public const string ErrorLocation = "ERROR_LOCATION";
            public const string Password = "PASSWORD";
            public const string SiteId = "SID";
        }

        #endregion Constants

        #region Constructors

        public Payload()
        {
            Parameters = new Dictionary<string, string>();
        }

        public Payload(string payloadXml)
            : this()
        {
            if (string.IsNullOrEmpty(payloadXml))
                return;

            XDocument payloadDoc = XDocument.Parse(payloadXml);
            CommandType = payloadDoc.Element("COMMAND").GetAttributeValueOrEmptyString("TYPE");
        }

        #endregion Constructors

        #region Properties

        public string CommandType { get; set; }

        private Dictionary<string, string> Parameters { get; set; }

        public string this[string key]
        {
            get
            {
                return !Parameters.ContainsKey(key) 
                    ? string.Empty 
                    : Parameters[key];
            }
        }

        #endregion Properties

        #region Methods

        public void AddParameter(string key, string value)
        {
            if (Parameters.ContainsKey(key))
                Parameters[key] = value;
            else
                Parameters.Add(key, value);
        }

        /// <summary>
        /// Generates an error payload with the indicated code and message
        /// </summary>
        public static Payload CreateErrorPayload(string errorCode, string errorMessage)
        {
            Payload errorPayload = new Payload {CommandType = CommandTypes.Error};
            errorPayload.AddParameter(ParameterKeys.ErrorCode, errorCode);
            errorPayload.AddParameter(ParameterKeys.ErrorLocation, errorMessage);
            return errorPayload;
        }

        #endregion Methods
    }
}
