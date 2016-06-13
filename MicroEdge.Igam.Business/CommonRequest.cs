using System.Collections.Generic;
using System.Net;
using System.Web;

namespace MicroEdge.Igam.Business
{
    public class CommonRequest
    {
        #region Constructor

        public CommonRequest(HttpRequest webRequest)
        {
            RequestData = new Dictionary<string, object>();

            //Move in query string values
            foreach (string key in webRequest.QueryString)
            {
                RequestData.Add(key, webRequest[key]);
            }

            //Move in form values
            foreach (string key in webRequest.Form.Keys)
            {
                RequestData.Add(key, webRequest.Unvalidated[key]);
            }

            //Default to all clear
            ResponseHttpStatus = HttpStatusCode.OK;
        }

        #endregion Constructor

        #region Properties

        public HttpStatusCode ResponseHttpStatus { get; set; }

        public Dictionary<string, object> RequestData { get; set; }

        #endregion Properties        
    }
}
