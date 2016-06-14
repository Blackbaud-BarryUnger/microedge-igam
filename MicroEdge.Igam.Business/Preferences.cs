using MicroEdge.Igam.Business.DataAccess;
using MicroEdge.Igam.Providers.Dal;

namespace MicroEdge.Igam.Business
{
    public class Preferences 
    {
        #region Fields

        private static Preferences _current;

        #endregion Fields


        public string CFSRoot { get; set; }

        public static Preferences Current
        {
            get
            {
                if (_current != null)
                    return _current;

                _current = new Preferences();
                _current.Read();
                return _current;
            }
        }

        #region Methods

        public void Read()
        {
            IPreferencesData data = DalManager.GetDalObject<IPreferencesData>();
            if (data.Read() == ReturnStatus.Success)
            {
                CFSRoot = data.CFSRoot;
            }
        }

        #endregion Methods
    }
}
