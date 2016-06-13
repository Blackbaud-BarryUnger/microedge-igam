using System;
using System.Collections.Generic;
using System.Net;
using System.Web;
using MicroEdge.Grantmaker.Business;
using MicroEdge.Igam.Business;
using MicroEdge.Igam.Providers.Logging;
using Serilog.Context;

namespace MicroEdge.Grantmaker
{
    public class Context
    {
        const string PayloadKey = "IGAM_PAYLOAD_DOCUMENT";

        public static void Process(HttpRequest webRequest)
        {
            using (LogContext.PushProperty("CorrelationId", webRequest.Headers["me-log-correlation-id"]))
            {
                LogManager.LogInfo("Entering Context.Process");
                LogManager.LogDebug("Context.Process executing with webRequest = {0}", webRequest);

                try
                {
                    CommonRequest request = new CommonRequest(webRequest);
                    RequestProcess(request);
                }
                catch (Exception ex)
                {
                    LogManager.LogError("Context.Process exception: {0}", ex.Message);
                    throw;
                }

                LogManager.LogInfo("Exiting Context.Process");
            }
        }

        internal static void RequestProcess(CommonRequest request)
        {
            LogManager.LogInfo("Entering Context.RequestProcess");
            LogManager.LogDebug("Context.RequestProcess executing with request = {0}", request);

            if (request == null)
                return;

            if (!request.RequestData.ContainsKey(PayloadKey))
                throw new Exception(Properties.Errors.InvalidPayload);

            Payloads payloadsIn = new Payloads(request.RequestData[PayloadKey].ToString());
            Payloads payloadsOut = new Payloads();

            //Authenticate.
            if (ClientAuthenticate(payloadsIn, payloadsOut))
            {
                //Process the payload if we are authenticated.
                payloadsOut.AddRange(PayloadsProcess(payloadsIn));
            }

            if (payloadsOut.Count == 0)
                request.ResponseHttpStatus = HttpStatusCode.NotFound;
            else
            {
                //TO-DO: finish converting this over 

                //return the new payload object to the client & add a header
                //         if (mobjPayloadOut.BinaryFileStream != null)
                //         {
                //             request.ResponseBodyBinaryStream = mobjPayloadOut.BinaryFileStream;
                //             request.ResponseBodyBinaryFileName = mobjPayloadOut.BinaryFileName;
                //             request.ResponseBodyBinaryType = mobjPayloadOut.BinaryFileType;
                //         }
                //         else if TypeName(mobjPayloadOut.BinaryFile) <> "Empty" Then
                //         {
                //             mobjRequest.ResponseBodyBinary = mobjPayloadOut.BinaryFile
                //             mobjRequest.ResponseBodyBinaryFileName = mobjPayloadOut.BinaryFileName
                //             mobjRequest.ResponseBodyBinaryType = mobjPayloadOut.BinaryFileType
                //         }
                //         else
                //         {
                //             mobjRequest.ResponseBody = mobjPayloadOut.XMLDocument.XML
                //            mobjRequest.ResponseHeader.Add "Content-Type", "text/xml"
                //         }
            }
            LogManager.LogInfo("Exiting Context.RequestProcess");
        }

        /// <summary>
        /// Attempts to check if the client request comes from a ligit source.
        /// </summary>
        /// <param name="requestPayloads">
        /// Incoming payloads containing any authentication daat
        /// </param>
        /// <param name="responsePayloads">
        /// Holder for the payloads we're sending back.  We may be adding to it here
        /// </param>
        /// <returns>
        /// True if legit, false if not
        /// </returns>
        internal static bool ClientAuthenticate(Payloads requestPayloads, Payloads responsePayloads)
        {
            LogManager.LogInfo("Entering Context.ClientAuthenticate");
            LogManager.LogDebug("Context.ClientAuthenticate executing with requestPayloads = {0}, responsePayloads = {1}", requestPayloads, responsePayloads);

#if DEBUG
            if (requestPayloads.Count == 1 && requestPayloads[0].CommandType == "GRANTMAKER_TEST")
                return true;
#endif
            if (!requestPayloads.ContainsAuthentication)
                return false;

            CFS cfs = new CFS();
            Payload authenticationPayload = cfs.Authenticate(requestPayloads.SiteId, requestPayloads.SerialNumber, requestPayloads.CheckSum);
            if (authenticationPayload == null)
                return true;

            if (!string.IsNullOrEmpty(authenticationPayload.CommandType))
                responsePayloads.Add(authenticationPayload);

            bool isAuthenticated = authenticationPayload.CommandType != "ERROR";
            LogManager.LogDebug("Context.ClientAuthenticate returning: {0}", isAuthenticated);
            LogManager.LogInfo("Context GCS.ClientAuthenticate");
            return isAuthenticated;
        }

        internal static List<Payload> PayloadsProcess(Payloads payloadsIn)
        {
            List<Payload> payloadsOut = new List<Payload>();

            foreach (Payload payload in payloadsIn)
            {
                switch (payload.CommandType)
                {
                    case (Payload.CommandTypes.CreateApplicant):
                        payloadsOut.Add(ApplicantActions.CreateApplicant(payload[Payload.ParameterKeys.Email], payload[Payload.ParameterKeys.Password]));
                        break;

                    //TODO - add cases for all the other command types grantmaker supports
                    default:
                        break;
                }
            }
            return payloadsOut;
        }
    }
}
