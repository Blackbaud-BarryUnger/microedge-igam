using System;
using System.Configuration;
using System.IO;
using MicroEdge.Igam.Business.DataAccess;

namespace MicroEdge.Igam.Dal
{
    public class PreferencesData : IPreferencesData
    {
        #region Constants

        private const string IniFile = "IGAM.INI";

        //Keys
        private static class PreferenceKeys
        {
            public const string CfsRoot = "CFSRoot";
        }

        //Defaults
        private const string CfsRootDefault = @"C:\CFS";

        #endregion Constants

        #region Constructors

        public PreferencesData()
        {
            CFSRoot = CfsRootDefault;
        }

        #endregion Constructors


        #region Properties

        public string CFSRoot { get; set; }

        #endregion Properties

        #region Methods

        public ReturnStatus Read()
        {
            CFSRoot = ConfigurationManager.AppSettings[PreferenceKeys.CfsRoot];

            //TODO - nuke this if/when we end up not needing it.  All settings should be moving to the web.config

            //string iniFile = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"\", IniFile);

            //if (!File.Exists(iniFile))
            //    return ReturnStatus.NotFound;

            //string[] iniContents = File.ReadAllText(iniFile).Split(Environment.NewLine.ToCharArray(),StringSplitOptions.RemoveEmptyEntries);
            //foreach (string preference in iniContents)
            //{
            //    if (preference.StartsWith(";"))
            //        continue;

            //    string[] prefKeyValue = preference.Split('=');
            //    if (prefKeyValue.Length < 2)
            //        continue;

            //    switch (prefKeyValue[0].Trim())
            //    {
            //        case PreferenceKeys.CfsRoot:
            //            CFSRoot = prefKeyValue[1].Trim();
            //            break;

            //        default:
            //            break;
            //    }
            //}

            return ReturnStatus.Success;
        }

        #endregion Methods
    }
}
