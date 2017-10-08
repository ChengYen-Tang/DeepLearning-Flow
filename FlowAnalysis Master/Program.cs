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

namespace FlowAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.WriteLine("1.Master");
            Console.WriteLine("2.Slave");
            Console.Write("請輸入模式:");
            int Mode = int.Parse(Console.ReadLine());
            
            if (Mode is 1)
            {
                int Port = int.MinValue;string Path = string.Empty;
                Console.Write("請輸入Port: ");
                Port = int.Parse(Console.ReadLine());
                Console.Write("請輸入DL位置: ");
                Path = Console.ReadLine();
                Master(Port, Path);
            }
            else if(Mode is 2)
            {
                int Port = int.MinValue; string IP = string.Empty; string Path = string.Empty;
                Console.Write("請輸入Master IP位置: ");
                IP =Console.ReadLine();
                Console.Write("請輸入Port: ");
                Port = int.Parse(Console.ReadLine());
                Console.Write("請輸入DL位置: ");
                Path = Console.ReadLine();
                Slave(IP, Port, Path);
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


        //static void Main(string[] args)
        //{
        //    //SampleAggregated SC = new SampleAggregated("172.16.62.130", 0);
        //    //SC.Collect(10, 0);
        //    if (BehaviorAnalysis.Initialization())
        //    {
        //        Task Read = new Task(() =>
        //        {
        //            while (true)
        //            {
        //                //var buffer = FlowAggregated.Collect(3, 0).Where(c => c.Source_Address.Contains("172.16.62."));

        //                foreach (var q in BehaviorAnalysis.Analysis(FlowAggregated.Collect(3, 0).Where(c => c.Source_Address.Contains("172.16.62.")).ToList()))
        //                    Console.WriteLine("IP: {0} , 行為: {1}", q.Item1, AnalysisTools.Conversion(q.Item2));
        //                //Parallel.ForEach(buffer.Select(c => c.Source_Address).Distinct(), (q) =>
        //                //{
        //                //    int Count = 0;
        //                //    foreach (var r in buffer.Where(C => C.Source_Address == q).Select(c => c.Count))
        //                //        Count += r;
        //                //    if (Count >= 400)
        //                //        Console.WriteLine("IP: {0}, Count: {1}", q, Count);

        //                //});
        //                //buffer = null;
        //                Console.WriteLine("{0} Done", DateTime.Now);
        //                System.Threading.SpinWait.SpinUntil(() => false, 60000);
        //            }
        //        }, TaskCreationOptions.LongRunning);
        //        Read.Start();
        //    }
        //    else
        //        Console.WriteLine("初始化失敗");

        //    //FlowDatas db = new FlowDatas();
        //    //foreach (var q in db.NetFlows.Where(c => c.Source_Address.Contains("172.16.62.")).Select(c => c.Source_Address).Distinct())
        //    //{
        //    //    SC.Reset(q, 0);
        //    //    SC.Collect(10, 0);
        //    //}

        //    //DateTime test = db.FlowSampleStatistics.Where(c => c.Protocal != 6).Select(c => c.StartTime).First();
        //    //var q = DeepLearningTools.FlowSampleToLearningData(db.FlowSampleStatistics.Where(c => c.StartTime == test).ToArray());

        //    //DateTime[][] TimeGroup = DeepLearningTools.TimeGroupRandom(db.FlowSampleStatistics.Select(c => c.StartTime).Distinct().ToList(), 50);

        //    //List<FlowSampleStatistics> Buffer = new List<FlowSampleStatistics>();

        //    //foreach (var q in TimeGroup[1])
        //    //    Buffer.AddRange(db.FlowSampleStatistics.Where(c => c.StartTime == q).ToList());
        //    //double[][] Inputs;
        //    //double[][] Inputs1;
        //    //double[][] Outputs1;






        //    //Inputs = DeepLearningTools.FlowStatisticsToLearningData(FlowAggregated.Collect(5, 0).Where(c => c.Source_Address == "172.16.62.130").ToArray());
        //    //(Inputs1, Outputs1) = DeepLearningTools.FlowSampleToLearningData(db.FlowSampleStatistics.Where(c => c.BehaviorNumber != 0).ToArray());

        //    //DeepBeliefNetwork DBNetwork = DeepBeliefNetwork.Load("C:\\Users\\User\\Desktop\\NetflowDBN");
        //    //Console.WriteLine("建置完成");

        //    //new GaussianWeights(DBNetwork, 0.01).Randomize();
        //    //DBNetwork.UpdateVisibleWeights();


        //    //DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
        //    //Console.WriteLine("Save Done");


        //    //DeepBeliefNetworkLearning teacher
        //    //    = new DeepBeliefNetworkLearning(DBNetwork)
        //    //    {
        //    //        Algorithm = (h, v, i) =>
        //    //        new ContrastiveDivergenceLearning(h, v)
        //    //        {
        //    //            LearningRate = 0.01,
        //    //            Momentum = 0.5,
        //    //            Decay = 0.001,
        //    //        }
        //    //    };

        //    ////設置批量輸入學習。
        //    //int batchCount1 = Math.Max(1, Inputs1.Length / 10);
        //    ////創建小批量加速學習。
        //    //int[] groups1
        //    //    = Accord.Statistics.Classes.Random(Inputs1.Length, batchCount1);
        //    //double[][][] batches1 = Inputs1.Subgroups(groups1);
        //    ////學習指定圖層的數據。
        //    //double[][][] layerData1;

        //    ////每個隱藏層的無監督學習，輸出層除外。
        //    ////for (int layerIndex = 0; layerIndex < DBNetwork.Machines.Count - 1; layerIndex++)
        //    ////{
        //    ////    teacher.LayerIndex = layerIndex;
        //    ////    layerData = teacher.GetLayerInput(batches);


        //    ////    for (int i = 0; i < 20; i++)
        //    ////    {
        //    ////        double error = teacher.RunEpoch(layerData) / Inputs.Length;
        //    ////        Console.WriteLine(i + ", Error = " + error);
        //    ////        DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
        //    ////    }
        //    ////}

        //    //teacher.LayerIndex = 0;
        //    //layerData1 = teacher.GetLayerInput(batches1);

        //    //for (int i = 0; i < 200; i++)
        //    //{
        //    //    double error = teacher.RunEpoch(layerData1) / Inputs1.Length;
        //    //    Console.WriteLine(i + ", Error = " + error);
        //    //    DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
        //    //    Console.WriteLine("Save Done");
        //    //}

        //    //Inputs = null;
        //    //Outputs = null;

        //    //對整個網絡進行監督學習，提供輸出分類。
        //    //var teacher2 = new ParallelResilientBackpropagationLearning(DBNetwork);

        //    //double error1 = double.MaxValue;

        //    ////運行監督學習。
        //    //for (int i = 0; i < 500; i++)
        //    //{
        //    //    error1 = teacher2.RunEpoch(Inputs1, Outputs1) / Inputs1.Length;
        //    //    Console.WriteLine(i + ", Error = " + error1);

        //    //    DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
        //    //    Console.WriteLine("Save Done");
        //    //}

        //    //DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
        //    //Console.WriteLine("Save Done");

        //    //int correct = 0;
        //    //for (int i = 0; i < Inputs.Length; i++)
        //    //{
        //    //    double[] outputValues = DBNetwork.Compute(Inputs[i]);
        //    //    Console.WriteLine(DeepLearningTools.FormatOutputResult(outputValues));
        //    //}
        //    //Console.WriteLine("失敗數 " + correct + "/" + Inputs.Length + ", " + Math.Round(((double)correct / (double)Inputs.Length * 100), 2) + "%");
        //    Console.Read();
        //}
    }
}
