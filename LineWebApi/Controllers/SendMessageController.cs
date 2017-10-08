using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace LineWebApi.Controllers
{
    public class SendMessageController : ApiController
    {
        // POST api/values
        public void Post([FromBody]string value)
        {
            string ChannelAccessToken = "9nVgx51WbzSeAPyUjLmFYRaVPQ6dM0eE2p0+3WspZBMVGMuH9gAM697rOD0sELS2+p/w7hq3Wt8zqKfNqlerjKUEXYvFAl6Es5TJzupyViNuoeyaVUW3HmLKoOSQN6Q+Yc6r05ZgMv9ly7Ebkuk5PwdB04t89/1O/w1cDnyilFU=";
            isRock.LineBot.Utility.PushMessage("C84442ab68db2fb8b7c2690a217d96595", value, ChannelAccessToken);
        }
    }
}