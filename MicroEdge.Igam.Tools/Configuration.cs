using System;
using System.Configuration;

namespace MicroEdge.Framework
{
    /// <summary>
    /// This class serves as a wrapper to the ConfigurationManager class.
    /// </summary>
    ///
    ///<remarks>
    ///  Author:     LDF
    ///  Created:    6/1/2007
    ///</remarks>
    public static class Configuration
    {
        #region Fields

        private const string HostedConnectionKey = "GiftsOnline";
        private const string DatabaseTypeKey = "DatabaseType";
        private const string ClientHostedKey = "ClientHosted";
        private const string UploadDirectoryKey = "UploadDirectory";
        private const string AttachmentsDirectoryKey = "AttachmentsDirectory";
        private const string AttachmentsVirtualDirectoryKey = "AttachmentsVirtualDirectory";
        private const string GiftsDirectoryKey = "GiftsDirectory";
        private const string DocumentManagementServicesUrlKey = "DocumentManagementServicesUrl";
        private const string SharePointDocumentsEnabledKey = "SharePointDocumentsEnabled";
        private const string SharePointDocumentsEnabledCacheLengthKey = "SharePointDocumentsEnabledCacheLength";
        private const string ReportsDirectoryKey = "ReportsDirectory";

        #endregion Fields

        #region Properties

        /// <summary>
        /// The main GIFTS directory.
        /// </summary>
        public static string GiftsDirectory
        {
            get
            {
                string value = ConfigurationManager.AppSettings[GiftsDirectoryKey];
                if (String.IsNullOrEmpty(value))
                    return "c:\\Gifts";
                else
                    return value;
            }
        }

        /// <summary>
        /// The reports directory.
        /// </summary>
        public static string ReportsDirectory
        {
            get
            {
                string value = ConfigurationManager.AppSettings[ReportsDirectoryKey];
                if (String.IsNullOrEmpty(value))
                    return "https://reports.gogiftsonline.com";
                else
                    return value;
            }
        }

        /// <summary>
        /// Gets the upload directory.
        /// </summary>
        public static string UploadDirectory
        {
            get
            {
                string value = ConfigurationManager.AppSettings[UploadDirectoryKey];
                if (String.IsNullOrEmpty(value))
                    return "Upload";
                else
                    return value;
            }
        }

        /// <summary>
        /// Gets the attachments directory.
        /// </summary>
        public static string AttachmentsDirectory
        {
            get
            {
                string value = ConfigurationManager.AppSettings[AttachmentsDirectoryKey];
                if (String.IsNullOrEmpty(value))
                    return "Attachments";
                else
                    return value;
            }
        }

        /// <summary>
        /// Gets the attachments directory.
        /// </summary>
        public static string AttachmentsVirtualDirectory
        {
            get
            {
                string value = ConfigurationManager.AppSettings[AttachmentsVirtualDirectoryKey];
                if (String.IsNullOrEmpty(value))
                    return "Attachments";
                else
                    return value;
            }
        }

		///// <summary>
		///// The type of the database. Default to MS SQL.
		///// </summary>
		//public static Data.Data.DbType DatabaseType
		//{
		//	get
		//	{
		//		const Data.Data.DbType Default = MicroEdge.Data.Data.DbType.MsSql;

		//		string value = ConfigurationManager.AppSettings[DatabaseTypeKey];
		//		if (String.IsNullOrEmpty(value))
		//			return Default;
		//		else
		//			return (Data.Data.DbType)Tools.ToInt32(value);
		//	}
		//}

        /// <summary>
        /// This will indicate whether or not this is a client hosted version of Gifts Online.
        /// </summary>
        public static bool IsClientHosted
        {
            get
            {
                const bool Default = false;

                string value = ConfigurationManager.AppSettings[ClientHostedKey];
                if (String.IsNullOrEmpty(value))
                    return Default;
                else
                    return Tools.ToBoolean(value);
            }
        }

        /// <summary>
        /// Gets the Url for the document management Web service
        /// </summary>
        public static string DocumentManagementServicesUrl
        {
            get
            {
                return ConfigurationManager.AppSettings[DocumentManagementServicesUrlKey];
            }
        }

        /// <summary>
        /// Gets whether SharePoint Documents are Enabled at all
        /// </summary>
        public static bool AreSharePointDocumentsEnabled
        {
            get
            {
                const bool Default = true;

                string value = ConfigurationManager.AppSettings[SharePointDocumentsEnabledKey];
                if (String.IsNullOrEmpty(value))
                    return Default;
                else
                    return Tools.ToBoolean(value);
            }
        }

        public static int AreSharePointDocumentsEnabledCacheLength
        {
            get
            {
                const int Default = 5;
                string value = ConfigurationManager.AppSettings[SharePointDocumentsEnabledCacheLengthKey];
                if (string.IsNullOrEmpty(value))
                    return Default;
                else
                    return Tools.ToInt32(value);
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Get the connection string for the indicated client. If no client is specified we will assume this
        /// is a client hosted version of Gifts Online and will return the GiftsOnline connection string.
        /// </summary>
        /// <param name="client">
        /// The name of the client for whose connection string should be returned.
        /// </param>
        /// <returns>
        /// The connection string for the indicated client.
        /// </returns>
        public static string GetConnectionString(string client)
        {
            //Insure that a client string has been specified if this app is not client hosted.
            if (!Configuration.IsClientHosted && String.IsNullOrEmpty(client))
                throw new MicroEdge.SystemException("Unable to get a database connection string from the configuration file. No client key was specified. If this application is not hosted by the client, a client key must be specified.");

            ConnectionStringSettings setting = null;
            if (String.IsNullOrEmpty(client))
                setting = ConfigurationManager.ConnectionStrings[HostedConnectionKey];
            else
                setting = ConfigurationManager.ConnectionStrings[client];

            if (setting == null)
                return "";
            else
                return setting.ToString();
        }
        public static string GetConnectionString()
        {
            return GetConnectionString("");
        }

        #endregion Methods
    }
}
