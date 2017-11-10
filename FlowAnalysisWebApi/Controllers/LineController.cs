using System.Net.Http;
using System.Web.Http;

namespace LineWebApi.Controllers
{
    public class LineController : ApiController
    {
        public HttpResponseMessage Post()
        {
            string ChannelAccessToken = "9nVgx51WbzSeAPyUjLmFYRaVPQ6dM0eE2p0+3WspZBMVGMuH9gAM697rOD0sELS2+p/w7hq3Wt8zqKfNqlerjKUEXYvFAl6Es5TJzupyViNuoeyaVUW3HmLKoOSQN6Q+Yc6r05ZgMv9ly7Ebkuk5PwdB04t89/1O/w1cDnyilFU=";

            try
            {
                //取得 http Post RawData(should be JSON)
                string postData = Request.Content.ReadAsStringAsync().Result;
                //剖析JSON
                var ReceivedMessage = isRock.LineBot.Utility.Parsing(postData);
                //回覆訊息

                string Message = "";
                Message = "群組ID = " + ReceivedMessage.events[0].source.groupId;
                isRock.LineBot.Utility.ReplyMessage(ReceivedMessage.events[0].replyToken, Message, ChannelAccessToken);


                //回覆API OK
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }
            catch
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }
}