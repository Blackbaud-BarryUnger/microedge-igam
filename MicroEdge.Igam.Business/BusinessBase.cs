using MicroEdge.Igam.Business.DataAccess;
using MicroEdge.Igam.Providers.Dal;

namespace MicroEdge.Igam.Business
{
    /// <summary>
    /// Base class for business objects that require persistence, providing a 
    /// common framework for key properties and methods
    /// </summary>
    /// <typeparam name="TData">
    /// Underlying dal object for handling CRUD functionality
    /// </typeparam>
    public class BusinessBase<TData> where TData : class, IData
    {
        #region Constructors

        protected BusinessBase() { }  

        #endregion Constructors

        #region Properties

        protected TData DalObject { get; private set; }

        public int Id { get; private set; }

        #endregion Properties

        #region Methods

        protected virtual void PopulateFromDal()
        {
            if (DalObject == null)
                return;

            Id = DalObject.Id;
        }

        protected virtual void PopulateDal()
        { }

        public void Read(int id)
        {
            if (DalObject == null)
                DalObject = DalManager.GetDalObject<TData>();

            if (DalObject == null)
                return;

            if (DalObject.Read(id) == ReturnStatus.Success)
                PopulateFromDal();
        }

        public void Update()
        {
            if (DalObject == null)
                DalObject = DalManager.GetDalObject<TData>();

            if (DalObject == null)
                return;

            PopulateDal();
            DalObject.Update();

            Id = DalObject.Id;
        }

        public void Delete()
        {
            if (DalObject == null)
                DalObject = DalManager.GetDalObject<TData>();

            if (DalObject == null)
                return;

            DalObject.Delete();
        }

        #endregion Methods
    }
}
