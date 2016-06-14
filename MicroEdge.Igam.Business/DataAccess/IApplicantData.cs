namespace MicroEdge.Igam.Business.DataAccess
{
    public interface IApplicantData : IData
    {
        int CurrentApplicationId { get; set; }
        string Email { get; set; }
        string Password { get; set; }
    }
}
