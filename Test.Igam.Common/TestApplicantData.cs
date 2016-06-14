using System.Runtime.InteropServices;
using MicroEdge.Igam.Business.DataAccess;

namespace Test.Igam.Common
{
    public class TestApplicantData : IApplicantData
    {
        private static int _counter = 1;

        public int Id { get; set; }
        public ReturnStatus Read(int id)
        {
            Id = id;
            return ReturnStatus.Success;
        }

        public void Update()
        {
            Id = _counter++;
        }

        public void Delete()
        { }

        public int CurrentApplicationId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
