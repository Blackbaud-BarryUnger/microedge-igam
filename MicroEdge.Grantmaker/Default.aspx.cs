using System;
using System.Net;
using System.Web.UI;

namespace MicroEdge.Grantmaker
{
    public partial class Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                Grantmaker.Context.Process(Request);
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                Response.Write(ex.Message);
            }
        }
    }
}