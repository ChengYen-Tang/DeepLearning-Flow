using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowAnalysis.Models;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Accord.Math;
using System.Diagnostics;

namespace FlowAnalysis.Class
{
    public class FeatureLearning
    {
        private string Path = string.Empty;
        private DeepBeliefNetwork DBNetwork;

        public FeatureLearning(string DLSavePath)
        {
            Path = DLSavePath;
        }

        public static void AddUserBehaviorToFlowSampleStatistics(
            string IPAddress, int Index)
        {
            SampleAggregated SC = new SampleAggregated(IPAddress, Index);
            SC.Collect(10, 0);
        }

        public bool Run()
        {
            bool IsDone = false;

            try
            {
                FlowDatas db = new FlowDatas();
                (double[][] Inputs, double[][] Outputs) 
                    = DeepLearningTools.FlowSampleToLearningData(db.FlowSampleStatistics.Where(c => c.BehaviorNumber != 0).ToArray());
                DBNetwork = new DeepBeliefNetwork(Inputs.First().Length,
                    (int)((Inputs.First().Length + Outputs.First().Length) / 1.5),
                    (int)((Inputs.First().Length + Outputs.First().Length) / 2),
                    Outputs.First().Length);
                new GaussianWeights(DBNetwork, 0.1).Randomize();
                DBNetwork.UpdateVisibleWeights();

                DeepBeliefNetworkLearning teacher
                    = new DeepBeliefNetworkLearning(DBNetwork)
                    {
                        Algorithm = (h, v, i) =>
                            new ContrastiveDivergenceLearning(h, v)
                            {
                                LearningRate = 0.01,
                                Momentum = 0.5,
                                Decay = 0.001,
                            }
                    };

                //設置批量輸入學習。
                int batchCount1 = Math.Max(1, Inputs.Length / 10);
                //創建小批量加速學習。
                int[] groups1
                    = Accord.Statistics.Classes.Random(Inputs.Length, batchCount1);
                double[][][] batches = Inputs.Subgroups(groups1);
                //學習指定圖層的數據。
                double[][][] layerData;

                for (int layerIndex = 0; layerIndex < DBNetwork.Machines.Count - 1; layerIndex++)
                {
                    teacher.LayerIndex = layerIndex;
                    layerData = teacher.GetLayerInput(batches);
                    for (int i = 0; i < 200; i++)
                    {
                        double error = teacher.RunEpoch(layerData) / Inputs.Length;
                        if (i % 10 == 0)
                        {
                            Console.WriteLine(i + ", Error = " + error);
                        }
                    }
                }

                //對整個網絡進行監督學習，提供輸出分類。
                var teacher2 = new ParallelResilientBackpropagationLearning(DBNetwork);

                double error1 = double.MaxValue;

                //運行監督學習。
                for (int i = 0; i < 500; i++)
                {
                    error1 = teacher2.RunEpoch(Inputs, Outputs) / Inputs.Length;
                    Console.WriteLine(i + ", Error = " + error1);

                    DBNetwork.Save(Path);
                    Console.WriteLine("Save Done");
                }

                DBNetwork.Save(Path);
                Console.WriteLine("Save Done");

                IsDone = true;
            }
            catch(Exception ex)
            {
                Debug.Write(ex.ToString());
            }

            return IsDone;
        }
    }

    public static class BehaviorAnalysis
    {
        private static DeepBeliefNetwork DBNetwork;

        public static bool Initialization(string Path)
        {
            try
            {
                DBNetwork = DeepBeliefNetwork.Load(Path);
                Console.WriteLine("建置完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        public static List<(string, int)> Analysis(List<DataFlowStatistics> FlowStatistics)
        {
            List<(string, int)> Result = new List<(string, int)>();
            object ResultLock = new object();
            Parallel.ForEach(FlowStatistics.Select(c => c.Source_Address).Distinct(), (q) =>
            {
                int MaxCount = FlowStatistics.Where(C => C.Source_Address == q).OrderByDescending(c => c.Count).Select(c => c.Count).First();
                int PortCount = FlowStatistics.Where(C => C.Source_Address == q).Count();
                lock (ResultLock)
                    if (MaxCount > 200 || PortCount > 1000)
                        Result.AddRange(DeepAnalysis(FlowStatistics.Where(C => C.Source_Address == q).ToArray()));
                    else
                        Result.Add((q,0));
            });
            return Result;
        }

        private static List<(string, int)> DeepAnalysis(DataFlowStatistics[] FlowStatistics)
        {
            List<(double[], string)> Inputs = DeepLearningTools.FlowStatisticsToLearningData(FlowStatistics);
            List<(string, int)> Result = new List<(string, int)>();
            for (int i = 0; i < Inputs.Count; i++)
            {
                double[] outputValues = DBNetwork.Compute(Inputs[i].Item1);
                Result.Add((Inputs[i].Item2,Convert.ToInt32(DeepLearningTools.FormatOutputResult(outputValues))));
            }
            return Result;
        }
    }

    public static class AnalysisTools
    {
        public static string Conversion(int Input)
        {
            if (Input is 0)
                return "Normal behavior";
            else if (Input is 1)
                return "WannaCry Virus";
            else if (Input is 2)
                return "DDOS Attack Web";
            else
                return "BT Download";
        }
    }
}
