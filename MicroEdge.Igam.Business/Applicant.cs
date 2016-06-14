using MicroEdge.Igam.Business.DataAccess;

namespace MicroEdge.Igam.Business
{
    /// <summary>
    /// Business class for an Applicant (i.e. user)
    /// </summary>
    public class Applicant : BusinessBase<IApplicantData>
    {
        public int CurrentApplicationId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        protected override void PopulateFromDal()
        {
            if (DalObject == null)
                return;

            CurrentApplicationId = DalObject.CurrentApplicationId;
            Email = DalObject.Email;
            Password = DalObject.Password;

            base.PopulateFromDal();
        }

        protected override void PopulateDal()
        {
            if (DalObject == null)
                return;

            DalObject.CurrentApplicationId = CurrentApplicationId;
            DalObject.Email = Email;
            DalObject.Password = Password;
            base.PopulateDal();
        }
    }
}
