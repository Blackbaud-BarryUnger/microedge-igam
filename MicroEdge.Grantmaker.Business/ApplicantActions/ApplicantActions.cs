using MicroEdge.Igam.Business;
using MicroEdge.Igam.Providers.Logging;
using MicroEdge.Igam.Tools;
using Newtonsoft.Json;

namespace MicroEdge.Grantmaker.Business
{
    /// <summary>
    /// Houses all routines involving Applicants and accounts
    /// </summary>
    public static class ApplicantActions
    {
        /// <summary>
        /// Does the work of creating an applicant account with the indicated 
        /// email address and password.  A "current application id" may also be
        /// supplied, but it is optional
        /// </summary>
        public static CreateApplicantResult CreateApplicant(string email, string password, int applicationId)
        {
            LogManager.LogInfo("Entering ApplicantActions.CreateApplicant");
            LogManager.LogDebug("ApplicantActions.CreateApplicant executing for email = {0}, password = {1}", email, password);

            CreateApplicantResult result = new CreateApplicantResult();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                result.ErrorCode = CreateApplicantResult.ErrorCodes.MissingEmailOrPassword;
            else if (!email.IsValidEmailAddress())
                result.ErrorCode = CreateApplicantResult.ErrorCodes.InvalidEmail;
            else
            {
                Applicant applicant = new Applicant();
                applicant.Email = email;
                applicant.Password = password;
                applicant.CurrentApplicationId = applicationId;
                applicant.Update();

                result.Success = true;
                result.ApplicantId = applicant.Id;
            }

            LogManager.LogDebug("ApplicantActions.CreateApplicant returning: {0}", JsonConvert.SerializeObject(result));
            LogManager.LogInfo("Exiting ApplicantActions.CreateApplicant");
            return result;
        }
    }
}
