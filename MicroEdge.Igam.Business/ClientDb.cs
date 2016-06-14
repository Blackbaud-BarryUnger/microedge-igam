using MicroEdge.Igam.Providers.Logging;
using MicroEdge.Logging;

namespace MicroEdge.Igam.Business
{
    [EnterExitMethodLogging]
    public class ClientDb
    {
        #region Constructors

        public ClientDb(string siteId)
        {
            const string rootFormat = @"{0}\SID_{1}";

            Root = string.Format(rootFormat, Preferences.Current.CFSRoot, siteId);
            SiteId = siteId;
            AdminSettings = new AdminSettings();
        }

        #endregion Constructors

        #region Properties

        public AdminSettings AdminSettings { get; private set; }

        public string SiteId { get; private set; }

        public string Root { get; private set; }

        #endregion Properties

        #region Methods

        public static string GetSiteId(string serialNumber)
        {
            LogManager.LogInfo("Entering ClientDb.GetSiteId");
            LogManager.LogDebug("ClientDb.GetSiteId executing with serialNumber: {0}", serialNumber);

            //TODO - convert this from VB6
            string siteId = string.Empty;

            LogManager.LogDebug("ClientDb.GetSiteId returning: {0}", siteId);
            LogManager.LogInfo("Exiting ClientDb.GetSiteId");

            return siteId;
        }

        #endregion Methods
    }
}
