using System.IO;
using MicroEdge.Grantmaker.Business;
using MicroEdge.Igam.Business;

namespace MicroEdge.Grantmaker
{
    public class CFS
    {
        public static class AuthenticateResponseTypes
        {
            public const string AuthenticationError = "AUTHENTICATION_ERROR";
            public const string NoLicenseError = "NO_IGAM_LICENSE";
        }

        public virtual Payload Authenticate(string siteId, string serialNumber, string checkSum)
        {
            const string globalResourcesPath = "GLOBAL_RESOURCES";

            bool configurationUpdateRequired = false;
            string invalidMsg;

            // Ensure we have Serial Number and Check Sum.
            if (string.IsNullOrEmpty(serialNumber))
                return Payload.CreateErrorPayload(AuthenticateResponseTypes.AuthenticationError, Properties.Errors.CFS_MissingSerialNumber);

            if (string.IsNullOrEmpty(checkSum))
                return Payload.CreateErrorPayload(AuthenticateResponseTypes.AuthenticationError, Properties.Errors.CFS_MissingCheckSum);

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
                ClientDb clientDb = new ClientDb(siteId);
                if (!Directory.Exists(clientDb.Root))
                    return Payload.CreateErrorPayload(AuthenticateResponseTypes.AuthenticationError, Properties.Errors.CFS_MissingCFSDirectory);

                // check if the config file exists.
                string configPath = string.Concat(clientDb.Root, @"\", globalResourcesPath, @"\gifts.cfg");
                if (!File.Exists(configPath))
                {
                    // Config file does not exist on SERVER. Config upload is required.
                    configurationUpdateRequired = true;
                    invalidMsg = Properties.Errors.CFS_ConfigNotFound;
                }
                else
                {
                    // Since the config file exists, read it.
                    string[] configuration = File.ReadAllText(configPath).Split(Tools.FieldMarker);
                    // Make sure the Serial No matches.
                    if (configuration[3] != serialNumber)
                        return Payload.CreateErrorPayload (AuthenticateResponseTypes.NoLicenseError, Properties.Errors.CFS_WrongSerialNumber);

                    //TODO - finish converting this

                    //               If blnDebug Then ProgramTrace "CFS:Authenticate: 13"
                    //               ' make sure that the check sum value matches the one in the config found in the CFS.
                    //               If CheckSumValidate(checkSum, configuration[6]) = meSUCCESS Then
                    //                  If blnDebug Then ProgramTrace "CFS:Authenticate: 14"
                    //                  ' ok now we need to check for an appropriate LICENSE.
                    //                  If ClientDB.ClientConfig.GrantApplicationManager Then
                    //                     If blnDebug Then ProgramTrace "CFS:Authenticate: 15"
                    //                     Authenticate = True
                    //                  Else
                    //                     objPayloadOut.CreateErrorPayload "NO_IGAM_LICENSE", "GRANTMAKER_SERVER", Properties.Errors.CFS_NotConfigured
                    //                     If blnDebug Then ProgramTrace "CFS:Authenticate: 16"
                    //                  End If
                    //               Else
                    //                  If blnDebug Then ProgramTrace "CFS:Authenticate: 17"
                    //                  blnConfigUpload = True
                    //                  strInvalidMsg = Properties.Errors.CFS_InvalidValidationKey
                    //               End If
                    //            Else
                    //               If blnDebug Then ProgramTrace "CFS:Authenticate: 18"
                    //               objPayloadOut
                    //            End If
                    //         Else
                    //            If blnDebug Then ProgramTrace "CFS:Authenticate: 19"
                    //            objPayloadOut.CreateErrorPayload "AUTHENTICATION_ERROR", "GRANTMAKER_SERVER", ErrorMsgGet(True, True)
                    //         End If
                }
            }

            // We need to upload the config file to the server. Check to see if the gate key is on.
            if (configurationUpdateRequired)
            {
                //TODO - finish converting this

                //      If blnDebug Then ProgramTrace "CFS:Authenticate: 21"
                //      If strSiteID <> "" Then
                //         ClientDB.Initialize strSiteID
                //         blnGateClosed = ClientDB.AdminSettings.GateClosed
                //      End If


                //      If blnDebug And Not ClientDB.AdminSettings Is Nothing Then
                //         ProgramTrace "CFS:Authenticate: 22, contents of AdminSettings:" & ClientDB.AdminSettings.XML
                //      End If


                //      If Not blnGateClosed Then
                //         ' ok Gate is open, indicate that a new CFG file is needed.
                //         If UCase$(objPayloadIn.CommandType) = "CFS_FILE_UPDATE" Then
                //            'Client is sending us the config file now. Make sure this is a GIFTS.CFG file. If so, consider
                //            'the user authenticated.
                //            If UCase$(objPayloadIn.Parameters.ChildValue("FILE_NAME")) = mstrGIFTS_CFG Then
                //               Authenticate = True
                //            Else
                //               'This is not the initialize access payload, tell the user we need it now.
                //               objPayloadOut.CommandType = "INITIALIZE_ACCESS"
                //               objPayloadOut.Parameters.ChildValue("SID") = strSiteID
                //            End If
                //         Else
                //            'This is not the initialize access payload, tell the user we need it now.
                //            objPayloadOut.CommandType = "INITIALIZE_ACCESS"
                //            objPayloadOut.Parameters.ChildValue("SID") = strSiteID
                //         End If
                //      Else
                //         'Gate is NOT open, and invalid authentication data supplied.
                //         objPayloadOut.CreateErrorPayload "AUTHENTICATION_ERROR", "GRANTMAKER_SERVER", strInvalidMsg
                //      End If
            }


            //   'If authenticated and the user does not yet have a site ID, create one now along with the appropriate
            //   'subdirectories.
            //   If Authenticate And strSiteID = "" Then
            //      If blnDebug Then ProgramTrace "CFS:Authenticate: 24"
            //      If AccountCreate(strSerialNo, strSiteID) = meERROR_OCCURRED Then
            //         If blnDebug Then ProgramTrace "CFS:Authenticate: 25"
            //         objPayloadOut.CreateErrorPayload "ACCOUNT_CREATE", "GRANTMAKER_SERVER", ErrorMsgGet(True, True)
            //      End If
            //   End If

            return null;
        }
    }
}
