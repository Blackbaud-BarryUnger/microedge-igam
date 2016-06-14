namespace MicroEdge.Grantmaker.Business
{
    public class CreateApplicantResult : ActionResultBase
    {
        public static class ErrorCodes
        {
            public const string MissingEmailOrPassword = "MISSING_EMAIL_PASSWORD";
            public const string InvalidEmail = "INVALID_EMAIL";
        }

        public int ApplicantId { get; set; }
    }
}
