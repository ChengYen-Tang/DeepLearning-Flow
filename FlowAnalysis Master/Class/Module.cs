using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using FlowAnalysis.Models;
using FlowAnalysis.Class;

namespace Project.Module
{
    public static class Tools
    {
        private static FlowDatas db = new FlowDatas();

        public static string StringListTostring(List<string> Source)
        {
            string Tmp = "";
            foreach (var q in Source)
                Tmp += q + ",";

            Tmp.Remove(Tmp.Length - 1);

            return Tmp;
        }

        public static List<List<T>> ChunkBy<T>(this List<T> source, double chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / Convert.ToInt32(Math.Ceiling(source.Count() / chunkSize)))
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static void CallLineBot(this List<(string, int)> Input,DateTime Time)
        {
            List<(string, int)> BadUser = Input.Where(C => C.Item2 != 0).ToList();

            var DBBadUser =
                from c in db.AnalysisResults
                where c.AnalysisTime >= DateTime.Today && c.Result != 0
                select new
                {
                    IP = c.IP,
                    Result = c.Result
                };

            var DBBadUserList = DBBadUser.Distinct().ToList();

            List<string> ErrorMessage = new List<string>();
            object needCallUserLock = new object();

            Parallel.For<List<string>>(0, BadUser.Count,
                () => { return new List<string>(); },
                (Index, State, Message) =>
                { 
                    if (DBBadUserList.Where(c => c.IP == BadUser[Index].Item1 && c.Result == BadUser[Index].Item2).Count() is 0)
                        Message.Add("Time :" + Time.ToString("yyyy-MM-dd HH:mm") + " IP :" + BadUser[Index].Item1 + " Behavior" + AnalysisTools.Conversion(BadUser[Index].Item2));
                    return Message;
                },
                (Message) =>
                {
                    lock (needCallUserLock)
                        ErrorMessage.AddRange(Message);
                });

            if (ErrorMessage.Count != 0)
                LineBot(string.Join("\n", ErrorMessage));
        }

        public static void LineBot(string Message)
        {
            string Url = "http://172.16.61.26//api/sendmessage";
            HttpWebRequest request = WebRequest.Create(Url) as HttpWebRequest;
            request.Method = WebRequestMethods.Http.Post;
            request.KeepAlive = true;
            request.ContentType = "application/x-www-form-urlencoded";
            string param = "=" + Message;//注意有個「=」
            byte[] bs = Encoding.Default.GetBytes(param);
            request.ContentLength = bs.Length;

            using (Stream reqStream = request.GetRequestStream())
            {
                reqStream.Write(bs, 0, bs.Length);
                reqStream.Flush();
            }
            try
            {
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        //Server回傳的資料。
                        Stream data = response.GetResponseStream();
                        StreamReader sr = new StreamReader(data);
                        string retMsg = sr.ReadToEnd();
                        sr.Close();
                        data.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

        public static void SaveToDataBase(this List<(string, int)> Input, DateTime Time)
        {
            List<AnalysisResult> AnalysisResultList = new List<AnalysisResult>();

            foreach (var q in Input)
                AnalysisResultList.Add(new AnalysisResult
                {
                    Id = Guid.NewGuid(),
                    IP = q.Item1,
                    Result = q.Item2,
                    AnalysisTime = Time
                });

            db.AnalysisResults.AddRange(AnalysisResultList);
            db.SaveChangesAsync();
        }

        public static Stream FileToStream(string Path)
        {
            try
            {
                return new FileStream(Path, FileMode.Open);
            }
            catch
            {
                throw new ArgumentException("路徑錯誤");
            }
        }

        public static string ToMD5(Stream stream)
        {
            try
            {
                byte[] HashBytes;
                using (stream)
                {
                    MD5 MD5Class = new MD5CryptoServiceProvider();
                    HashBytes = MD5Class.ComputeHash(stream);
                    MD5Class.Dispose();
                }
                StringBuilder SB = new StringBuilder();
                foreach (var q in HashBytes)
                    SB.Append(q.ToString("x2"));
                HashBytes = null;
                return SB.ToString();
            }
            catch(Exception ex)
            {
                throw new ArgumentException(ex.ToString());
            }
        }
    }
}

namespace NetWork.Class
{
    public class ClientEventArgs : EventArgs
    {
        public string Message { get; set; }
        public CommunicationBase CommunicationBase { get; set; }
    }

    public class ClientEvent
    {
        public event EventHandler ClientEventHandler;

        public void ClientEventCall(ClientEventArgs e)
        {
            ClientEventHandler(this, e);
        }
    }

    public class HandleServer : IDisposable
    {
        private bool disposed = false;
        private Thread listenThread;
        private bool MasterSwitch = true;
        private TcpListener listener;
        private List<TcpClient> Clients = new List<TcpClient>();
        private object ClientsLock = new object();
        private string MD5Data = string.Empty;

        public HandleServer(int Port,string MD5String)
        {
            listener = new TcpListener(IPAddress.Any, Port);
            MD5Data = MD5String;
            listenThread = new Thread(ListenWork) { IsBackground = true };
            listenThread.Start();
        }

        private void ListenWork()
        {
            listener.Start();
            while (MasterSwitch)
            {
                TcpClient tcpClient = listener.AcceptTcpClient();

                if (tcpClient.Connected)
                {
                    Task.Run(() => {
                        TcpClient client = tcpClient;
                        SpinWait.SpinUntil(() => false, 1000);
                        if (ClientWork(client, MD5Data) is "success")
                        {
                            Debug.WriteLine("連線成功!");

                            lock (ClientsLock)
                                Clients.Add(client);
                        }
                        else
                        {
                            Debug.WriteLine("MD5 檢查失敗");

                            client.Close();
                            client.Dispose();
                        }
                    });
                }

            }
            Debug.WriteLine("TcpListener Closed");
        }

        private string ClientWork(TcpClient ThisTcpClient, string SendMessage)
        {
            try
            {
                CommunicationBase CB = new CommunicationBase(ThisTcpClient);
                CB.SendMsg(SendMessage);
                return CB.ReceiveMsg();
            }
            catch
            {
                Debug.WriteLine("客戶端強制關閉連線!");
                ThisTcpClient.Close();
                ThisTcpClient.Dispose();
                lock (ClientsLock)
                    if (Clients.Contains(ThisTcpClient))
                        Clients.Remove(ThisTcpClient);
            }
            return string.Empty;
        }

        public string Message(TcpClient tcpClient, string Message)
        {
            Task<string> MessageTask = Task<string>.Factory.StartNew(() =>
            {
                return ClientWork(tcpClient, Message);
            });

            return MessageTask.Result;
        }

        public List<TcpClient> GetClient { get { return Clients; } }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            MasterSwitch = false;
            listener.Stop();
            foreach (var q in Clients)
            {
                q.Close();
                q.Dispose();
            }
            Clients.Clear();



            if (disposing)
            {
                listenThread = null;
                listener = null;
                Clients = null;
                ClientsLock = null;
            }

            disposed = true;
        }

        ~HandleServer()
        {
            Dispose(false);
        }
    }

    public class HandleClient : IDisposable
    {
        private TcpClient Server;
        private Thread ClientThread;
        private CommunicationBase CB;
        private bool Switch = true;
        private bool disposed = false;
        private string MD5Data = string.Empty;
        public ClientEvent Event = new ClientEvent();

        public HandleClient(IPEndPoint iPEndPoint)
        {
            Server = new TcpClient();
            Server.Connect(iPEndPoint);
        }

        public void Start(string MD5String)
        {
            if (Server.Connected)
            {
                Debug.WriteLine("連線成功!");
                CB = new CommunicationBase(Server);
                if (CB.ReceiveMsg() == MD5String)
                {
                    CB.SendMsg("success");
                    ClientThread = new Thread(TCPClientWork) { IsBackground = true };
                    ClientThread.Start();
                }
                else
                {
                    CB.SendMsg("Fail");
                    Server.Close();
                    Server.Dispose();
                    throw new ArgumentException("MD5檢查失敗");
                }
            }
        }

        public void TCPClientWork()
        {
            try
            {
                while (Switch)
                {
                    Console.WriteLine("等待Server訊息");
                    ClientEventArgs CEA = new ClientEventArgs
                    {
                        CommunicationBase = CB,
                        Message = CB.ReceiveMsg()
                    };
                    Console.WriteLine("收到Server訊息");
                    Event.ClientEventCall(CEA);
                }
            }
            catch
            {
                Console.WriteLine("伺服器端強制關閉連線!");
                Server.Close();
            }
        }

        public void SendMessage(string Message)
        {
            try
            {
                CB.SendMsg(Message);
            }
            catch
            {
                Console.WriteLine("伺服器端強制關閉連線!");
                Server.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            Switch = false;
            Server.Close();
            Server.Dispose();

            if (disposing)
            {
                Server = null;
                ClientThread = null;
                CB = null;
            }

            disposed = true;
        }

        ~HandleClient()
        {
            Dispose(false);
        }
    }


    public class CommunicationBase
    {
        private TcpClient mTcpClient;

        public CommunicationBase(TcpClient _tmpTcpClient)
        {
            this.mTcpClient = _tmpTcpClient;
        }

        /// <summary>
        /// 傳送訊息
        /// </summary>
        /// <param name="msg">要傳送的訊息</param>
        /// <param name="tmpTcpClient">TcpClient</param>
        public void SendMsg(string msg)
        {
            NetworkStream ns = mTcpClient.GetStream();
            if (ns.CanWrite)
            {
                byte[] msgByte = Encoding.Default.GetBytes(msg);
                ns.Write(msgByte, 0, msgByte.Length);
            }
        }

        /// <summary>
        /// 接收訊息
        /// </summary>
        /// <param name="tmpTcpClient">TcpClient</param>
        /// <returns>接收到的訊息</returns>
        public string ReceiveMsg()
        {
            string receiveMsg = string.Empty;
            byte[] receiveBytes = new byte[mTcpClient.ReceiveBufferSize];
            int numberOfBytesRead = 0;
            NetworkStream ns = mTcpClient.GetStream();

            if (ns.CanRead)
            {
                do
                {
                    numberOfBytesRead = ns.Read(receiveBytes, 0, mTcpClient.ReceiveBufferSize);
                    receiveMsg = Encoding.Default.GetString(receiveBytes, 0, numberOfBytesRead);
                }
                while (ns.DataAvailable);
            }
            return receiveMsg;
        }
    }
}