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
