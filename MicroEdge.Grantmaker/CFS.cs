using System.IO;
using MicroEdge.Grantmaker.Properties;
using MicroEdge.Igam.Business;
using MicroEdge.Igam.Tools;

namespace MicroEdge.Grantmaker
{
    public class CFS
    {
        public const string GiftsConfig = "gifts.cfg";
        public const char FieldMarker = (char)255;

        public static class AuthenticateResponseTypes
        {
            public const string AuthenticationError = "AUTHENTICATION_ERROR";
            public const string NoLicenseError = "NO_IGAM_LICENSE";
        }

        public virtual Payload Authenticate(string siteId, string serialNumber, string checkSum, bool isForConfigUpload)
        {
            const string globalResourcesPath = "GLOBAL_RESOURCES";

            ClientDb clientDb = null;
            bool configurationUpdateRequired = false;
            string invalidMsg = string.Empty;

            // Ensure we have Serial Number and Check Sum.
            if (string.IsNullOrEmpty(serialNumber))
                return Payload.CreateErrorPayload(AuthenticateResponseTypes.AuthenticationError, Errors.CFS_MissingSerialNumber);

            if (string.IsNullOrEmpty(checkSum))
                return Payload.CreateErrorPayload(AuthenticateResponseTypes.AuthenticationError, Errors.CFS_MissingCheckSum);

            Payload objPayloadOut = new Payload();

            // If we have no Site ID but we have a serial number, get the Site ID now.
            if (string.IsNullOrEmpty(siteId))
                siteId = ClientDb.GetSiteId(serialNumber);

            if (string.IsNullOrEmpty(siteId))
                configurationUpdateRequired = true;
            else
            {
                objPayloadOut.CommandType = Payload.CommandTypes.SidConfirmed;
                objPayloadOut.AddParameter(Payload.ParameterKeys.SiteId, siteId);

                //If we have a site ID but the CFS directory structure for the site does not exist, this is an error.
                //The client may have been disabled by MicroEdge.
                clientDb = new ClientDb(siteId);
                if (!Directory.Exists(clientDb.Root))
                    return Payload.CreateErrorPayload(AuthenticateResponseTypes.AuthenticationError, Errors.CFS_MissingCFSDirectory);

                // check if the config file exists.
                string configPath = string.Concat(clientDb.Root, @"\", globalResourcesPath, @"\", GiftsConfig);
                if (!File.Exists(configPath))
                {
                    // Config file does not exist on SERVER. Config upload is required.
                    configurationUpdateRequired = true;
                    invalidMsg = Errors.CFS_ConfigNotFound;
                }
                else
                {
                    // Since the config file exists, read it.
                    string[] configuration = File.ReadAllText(configPath).Split(FieldMarker);
                    // Make sure the Serial No matches.
                    if (configuration[3] != serialNumber)
                        return Payload.CreateErrorPayload (AuthenticateResponseTypes.NoLicenseError, Errors.CFS_WrongSerialNumber);
                    
                    //See if the check sum matches
                    if (!ChecksumValidate(checkSum, configuration[6]))
                    {
                        configurationUpdateRequired = true;
                        invalidMsg = Errors.CFS_InvalidValidationKey;
                    }
                    else if (!HasIgam(Tools.ToInt64(configuration[0])))
                    {
                        return Payload.CreateErrorPayload(AuthenticateResponseTypes.NoLicenseError, Errors.CFS_NotConfigured);
                    }
                }
            }

            // We need to upload the config file to the server. Check to see if the gate key is on.
            if (configurationUpdateRequired)
            {
                //TODO - finish converting this
                if (!string.IsNullOrEmpty(siteId))
                {
                    if (clientDb == null)
                        clientDb = new ClientDb(siteId);

                    // If the Gate is not open to receiving a new config, then they are NOT authenticated
                    if (clientDb.AdminSettings.GateIsClosed)
                        return Payload.CreateErrorPayload(AuthenticateResponseTypes.AuthenticationError, invalidMsg);
                }
                
                //If we're not authenticating the receipt of a config, response is that we need one
                if (!isForConfigUpload)
                {
                    Payload response = new Payload { CommandType = Payload.CommandTypes.InitializeAccess};
                    response.AddParameter(Payload.ParameterKeys.SiteId, siteId);
                    return response;               
                }
            }

            // If we're here, we're authenticated.  If we still don't have a siteId, 
            //we'll need to create a new account now for this serial number (i.e. bootstrap)
            return string.IsNullOrEmpty(siteId) 
                ? AccountCreate(serialNumber, siteId) 
                : null;
        }

        private static Payload AccountCreate(string serialNumber, string siteId)
        {
            try
            {

            }
            catch (System.Exception ex)
            {
                return Payload.CreateErrorPayload("ACCOUNT_CREATE", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Does an equality check between two check sum strings (where source has
        /// presumably pulled from a generated gifts.cfg file)
        /// </summary>
        /// <returns>
        /// True if they effectively match, false if not
        /// </returns>
        private static bool ChecksumValidate(string target, string source)
        {
            //if target is blank/null, only a match if source is too
            if (string.IsNullOrEmpty(target))
                return string.IsNullOrEmpty(source);

            //and vice versa
            if (string.IsNullOrEmpty(source))
                return string.IsNullOrEmpty(target);

            //if source is longer, we'll chop off anything beyond the length of target
            if (source.Length > target.Length)
                source = source.Substring(0,target.Length);

            //If they are identical, it's a match
            if (source == target)
                return true;

            //They should both be decimals; split on the decimal
            string[] sourceParts = source.Split('.');
            string[] targetParts = target.Split('.');

            //If the whole number value is different, not a match
            if (sourceParts[0] != targetParts[0])
                return false;

            //We'll consider a match on first 9 decimal places
            return sourceParts[0].Substring(0,9) ==  targetParts[1].Substring(0,9);
        }

        private static bool HasIgam(long configOptions)
        {
            return (configOptions & 128) > 0;
        }
    }
}
