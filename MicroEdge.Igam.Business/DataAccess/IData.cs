namespace MicroEdge.Igam.Business.DataAccess
{
    public enum ReturnStatus
    {
        Success = 0,
        NotFound = 1,
    }

    /// <summary>
    /// Base interface for dal object interfaces that support full CRUD functionality
    /// </summary>
    public interface IData
    {
        int Id { get; }

        ReturnStatus Read(int id);
        void Update();
        void Delete();
    }
}
