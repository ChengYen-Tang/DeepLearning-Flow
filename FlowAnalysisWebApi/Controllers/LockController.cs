using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LineWebApi.Controllers
{
    public class LockController : ApiController
    {
        public async Task<HttpResponseMessage> Post()
        {
            try
            {
                string PostData = Request.Content.ReadAsStringAsync().Result;
                dynamic JSONData = JsonConvert.DeserializeObject(PostData);
                
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }
            catch
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}