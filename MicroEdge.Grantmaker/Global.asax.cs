using System;
using System.Configuration;
using System.Web;
using MicroEdge.Igam.Business.Logging;
using MicroEdge.Igam.Providers.Logging;
using MicroEdge.Igam.Providers.Dal;

namespace MicroEdge.Grantmaker
{
    public class Global : HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            string applicationName = ConfigurationManager.AppSettings["LogentriesApplicationName"];
            if (!string.IsNullOrEmpty(applicationName))
                LogManager.Initialize(new LogProvider(applicationName));

            DalManager.Initialize(new DalProvider());
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}