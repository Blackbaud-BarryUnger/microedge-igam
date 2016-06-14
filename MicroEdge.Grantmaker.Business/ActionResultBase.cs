namespace MicroEdge.Grantmaker.Business
{
    /// <summary>
    /// Simplest version of an action result class; used to return information
    /// about the results of a grantmaker application, form, etc. action method
    /// </summary>
    public class ActionResultBase
    {
        /// <summary>
        /// Holds constant value denoting reason for failure, usually due to an action-specific business rule
        /// </summary>
        public string ErrorCode { get; set; }
        /// <summary>
        /// If true, action was executed successfully.
        /// </summary>
        public bool Success { get; set; }
    }
}
