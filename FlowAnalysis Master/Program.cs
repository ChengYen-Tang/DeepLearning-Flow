using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowAnalysis.Class;
using FlowAnalysis.Models;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Accord.Math;

namespace FlowAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            SampleAggregated SC = new SampleAggregated("172.16.62.130", 0);
            SC.Collect(10, 0);

            FlowDatas db = new FlowDatas();

            //foreach (var q in db.NetFlows.Where(c => c.Source_Address.Contains("172.16.62.")).Select(c => c.Source_Address).Distinct())
            //{
            //    SC.Reset(q, 0);
            //    SC.Collect(10, 0);
            //}

            //DateTime test = db.FlowSampleStatistics.Where(c => c.Protocal != 6).Select(c => c.StartTime).First();
            //var q = DeepLearningTools.FlowSampleToLearningData(db.FlowSampleStatistics.Where(c => c.StartTime == test).ToArray());

            //DateTime[][] TimeGroup = DeepLearningTools.TimeGroupRandom(db.FlowSampleStatistics.Select(c => c.StartTime).Distinct().ToList(), 50);

            //List<FlowSampleStatistics> Buffer = new List<FlowSampleStatistics>();

            //foreach (var q in TimeGroup[1])
            //    Buffer.AddRange(db.FlowSampleStatistics.Where(c => c.StartTime == q).ToList());
            //double[][] Inputs;
            //double[][] Inputs1;
            //double[][] Outputs1;

            //Inputs = DeepLearningTools.FlowStatisticsToLearningData(FlowAggregated.Collect(5, 0).Where(c => c.Source_Address == "172.16.62.130").ToArray());
            //(Inputs1, Outputs1) = DeepLearningTools.FlowSampleToLearningData(db.FlowSampleStatistics.Where(c => c.BehaviorNumber != 0).ToArray());

            //DeepBeliefNetwork DBNetwork = DeepBeliefNetwork.Load("C:\\Users\\User\\Desktop\\NetflowDBN");
            //Console.WriteLine("建置完成");

            //new GaussianWeights(DBNetwork, 0.01).Randomize();
            //DBNetwork.UpdateVisibleWeights();


            //DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
            //Console.WriteLine("Save Done");


            //DeepBeliefNetworkLearning teacher
            //    = new DeepBeliefNetworkLearning(DBNetwork)
            //    {
            //        Algorithm = (h, v, i) =>
            //        new ContrastiveDivergenceLearning(h, v)
            //        {
            //            LearningRate = 0.01,
            //            Momentum = 0.5,
            //            Decay = 0.001,
            //        }
            //    };

            ////設置批量輸入學習。
            //int batchCount1 = Math.Max(1, Inputs1.Length / 10);
            ////創建小批量加速學習。
            //int[] groups1
            //    = Accord.Statistics.Classes.Random(Inputs1.Length, batchCount1);
            //double[][][] batches1 = Inputs1.Subgroups(groups1);
            ////學習指定圖層的數據。
            //double[][][] layerData1;

            ////每個隱藏層的無監督學習，輸出層除外。
            ////for (int layerIndex = 0; layerIndex < DBNetwork.Machines.Count - 1; layerIndex++)
            ////{
            ////    teacher.LayerIndex = layerIndex;
            ////    layerData = teacher.GetLayerInput(batches);


            ////    for (int i = 0; i < 20; i++)
            ////    {
            ////        double error = teacher.RunEpoch(layerData) / Inputs.Length;
            ////        Console.WriteLine(i + ", Error = " + error);
            ////        DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
            ////    }
            ////}

            //teacher.LayerIndex = 1;
            //layerData1 = teacher.GetLayerInput(batches1);

            //for (int i = 0; i < 200; i++)
            //{
            //    double error = teacher.RunEpoch(layerData1) / Inputs1.Length;
            //    Console.WriteLine(i + ", Error = " + error);
            //    DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
            //    Console.WriteLine("Save Done");
            //}

            //Inputs = null;
            //Outputs = null;

            //對整個網絡進行監督學習，提供輸出分類。
            //var teacher2 = new ParallelResilientBackpropagationLearning(DBNetwork);

            //double error1 = double.MaxValue;

            ////運行監督學習。
            //for (int i = 0; i < 500; i++)
            //{
            //    error1 = teacher2.RunEpoch(Inputs1, Outputs1) / Inputs1.Length;
            //    Console.WriteLine(i + ", Error = " + error1);

            //    DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
            //    Console.WriteLine("Save Done");
            //}

            //DBNetwork.Save("C:\\Users\\User\\Desktop\\NetflowDBN");
            //Console.WriteLine("Save Done");

            //int correct = 0;
            //for (int i = 0; i < Inputs.Length; i++)
            //{
            //    double[] outputValues = DBNetwork.Compute(Inputs[i]);
            //    Console.WriteLine(DeepLearningTools.FormatOutputResult(outputValues));
            //}
            //Console.WriteLine("失敗數 " + correct + "/" + Inputs.Length + ", " + Math.Round(((double)correct / (double)Inputs.Length * 100), 2) + "%");
            Console.Read();
        }
    }
}
