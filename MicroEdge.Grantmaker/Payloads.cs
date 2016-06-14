using System;
using System.Collections.Generic;
using System.Xml.Linq;
using MicroEdge.Igam.Business;

namespace MicroEdge.Grantmaker
{
    public class Payloads : List<Payload>
    {
        #region Constructors

        public Payloads()
        {
        }

        public Payloads(string payloadsXml)
        {
            if (string.IsNullOrEmpty(payloadsXml))
                throw new Exception(Properties.Errors.InvalidPayload);

            XDocument payload = XDocument.Parse(payloadsXml);
            if (payload.Root == null)
                return;

            XElement authenticationNode = payload.Root.Element("AUTHENTICATION");
            if (authenticationNode == null)
                return;

            ContainsAuthentication = true;
            SerialNumber = authenticationNode.GetElementValueOrEmptyString("CSN");
            SiteId = authenticationNode.GetElementValueOrEmptyString("SID");
            CheckSum = authenticationNode.GetElementValueOrEmptyString("CFG_CHECKSUM");
        }

        #endregion Constructors

        #region Properties

        public string CheckSum { get; private set; }

        public bool ContainsAuthentication { get; private set; }

        public string SerialNumber { get; private set; }

        public string SiteId { get; private set; }

        #endregion Properties
    }
}
