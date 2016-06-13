using MicroEdge.Igam.Business.DataAccess;

namespace Test.Igam.Common
{
    public class TestPreferencesData : IPreferencesData
    {
        public string CFSRoot { get; set; }
        public ReturnStatus Read()
        {
            return ReturnStatus.Success;
        }
    }
}
