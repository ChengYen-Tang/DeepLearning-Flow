using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlowAnalysis.Class;
using FlowAnalysis.Models;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Accord.Math;
using Project.Module;
using System.IO;
using NetWork.Class;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;

namespace FlowAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            bool Check = true;
            try
            {
                string Buffer = string.Empty;
                StreamReader sr;
                using(sr = new StreamReader("./Config.JSON", Encoding.Default))
                {
                    Buffer = sr.ReadToEnd();
                }

                dynamic JSONBuffer = JsonConvert.DeserializeObject(Buffer);
                if ((int)JSONBuffer["Mode"] is 1)
                {
                    Master((int)JSONBuffer["Port"],(string)JSONBuffer["DLPath"]);
                }
                else if ((int)JSONBuffer["Mode"] is 2)
                {
                    Slave((string)JSONBuffer["MasterIP"], (int)JSONBuffer["Port"], (string)JSONBuffer["DLPath"]);
                }
            }
            catch
            {
                Check = false;
            }

            if (!Check)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Console.WriteLine("0:Learning");
                Console.WriteLine("1.Master");
                Console.WriteLine("2.Slave");
                Console.Write("請輸入模式:");
                int Mode = int.Parse(Console.ReadLine());

                if (Mode is 1)
                {
                    int Port = int.MinValue; string Path = string.Empty;
                    Console.Write("請輸入Port: ");
                    Port = int.Parse(Console.ReadLine());
                    Console.Write("請輸入DL位置: ");
                    Path = Console.ReadLine();
                    Master(Port, Path);
                }
                else if (Mode is 2)
                {
                    int Port = int.MinValue; string IP = string.Empty; string Path = string.Empty;
                    Console.Write("請輸入Master IP位置: ");
                    IP = Console.ReadLine();
                    Console.Write("請輸入Port: ");
                    Port = int.Parse(Console.ReadLine());
                    Console.Write("請輸入DL位置: ");
                    Path = Console.ReadLine();
                    Slave(IP, Port, Path);
                }
                else if (Mode is 0)
                {
                    string ANS = string.Empty; string Path = string.Empty;
                    Console.Write("是否增加特徵到特徵庫(Y/N) :");
                    ANS = Console.ReadLine();
                    if (ANS.ToUpper() == "Y")
                    {
                        Console.Write("請輸入異常行為的IP :");
                        string IPAddress = Console.ReadLine();
                        Console.Write("請輸入特徵編號 :");
                        int Index = int.Parse(Console.ReadLine());

                        FeatureLearning.AddUserBehaviorToFlowSampleStatistics(IPAddress, Index);
                    }

                    Console.Write("請輸入Deep belief network 狀態儲存路徑 :");
                    FeatureLearning FL = new FeatureLearning(Console.ReadLine());
                    if (!FL.Run())
                        Console.WriteLine("學習發生錯誤");
                }
            }
            Console.ReadLine();
        }

        private static void Master(int Port,string Path)
        {
            Stream DLstream = Project.Module.Tools.FileToStream(Path);
            HandleServer NetworkServer = new HandleServer(Port, Project.Module.Tools.ToMD5(DLstream));

            if (BehaviorAnalysis.Initialization(Path))
            {
                Thread Read = new Thread(MasterWork) { IsBackground = true};
                Read.Start(NetworkServer);
            }
            else
                Console.WriteLine("初始化失敗");
        }

        private static void Slave(string IPaddress,int Port ,string Path)
        {
            string DLMD5 = Project.Module.Tools.ToMD5(Project.Module.Tools.FileToStream(Path));
            
            HandleClient NetworkClient = new HandleClient(new IPEndPoint(IPAddress.Parse(IPaddress), Port));

            try
            {
                if (BehaviorAnalysis.Initialization(Path))
                {
                    NetworkClient.Event.ClientEventHandler += new EventHandler(ClientWork);
                    NetworkClient.Start(DLMD5);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void ClientWork(object sender, EventArgs e)
        {
            Task t = Task.Run(() => {
                string Temp = (e as ClientEventArgs).Message;
                string[] Buffer = Temp.Split(',');
                DateTime Time = DateTime.Now;

                List<(string, int)> Analysis = BehaviorAnalysis.Analysis(FlowAggregated.Collect(3, 0).Where(c => Buffer.Contains(c.Source_Address)).ToList());
                Analysis.CallLineBot(Time);
                Analysis.LockIP();
                Analysis.SaveToDataBase(Time);

                foreach (var q in Analysis)
                    Console.WriteLine("IP: {0} , 行為: {1}", q.Item1, AnalysisTools.Conversion(q.Item2));

                Console.WriteLine("{0} Done", DateTime.Now);
                (e as ClientEventArgs).CommunicationBase.SendMsg("Done");
            });

            TimeSpan ts = TimeSpan.FromMilliseconds(60000);
            if (!t.Wait(ts))
                Console.WriteLine("{0} 任務失敗", DateTime.Now);
        }

        private static void MasterWork(object Server)
        {
            HandleServer NetworkServer = (HandleServer)Server;
            while (true)
            {
                DateTime NowTime = DateTime.Now;
                if (NetworkServer.GetClient.Count() is 0)
                {
                    List<(string, int)> Analysis = BehaviorAnalysis.Analysis(FlowAggregated.Collect(3, 0).Where(c => c.Source_Address.Contains("172.16.62.")).ToList());
                    Analysis.CallLineBot(NowTime);
                    Analysis.LockIP();
                    Analysis.SaveToDataBase(NowTime);

                    foreach (var q in Analysis)
                        Console.WriteLine("IP: {0} , 行為: {1}", q.Item1, AnalysisTools.Conversion(q.Item2));

                    Console.WriteLine("{0} Done", DateTime.Now);
                }
                else
                {
                    string[] User = FlowAggregated.FlowIP(0, 0, 3, 0);
                    List<List<string>> Work = Project.Module.Tools.ChunkBy(User.ToList(), NetworkServer.GetClient.Count());
                    for (int i = 0; i < NetworkServer.GetClient.Count; i++)
                    {
                        AssignationWork(NetworkServer, Work, i);
                    }
                }

                Console.WriteLine("Sleep");
                System.Threading.SpinWait.SpinUntil(() => DateTime.Now > NowTime.AddMinutes(1));
                Console.WriteLine("up");
            }
        }

        //把工作分派給其他Slave
        private static void AssignationWork(HandleServer NetworkServer, List<List<string>> Work, int i)
        {
            Task.Run(() => {
                try
                {
                    NetworkServer.Message(NetworkServer.GetClient[i], Project.Module.Tools.StringListTostring(Work[i]));
                    Console.WriteLine("Slave {0} 運算數量: {1}", i, Work[i].Count);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            });
        }
    }
}
