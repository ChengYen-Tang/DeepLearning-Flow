using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlowAnalysis.Models;

namespace FlowAnalysis.Class
{
    public static class DeepLearningTools
    {
        public static (double[][], double[][]) FlowSampleToLearningData(FlowSampleStatistics[] FlowSamples)
        {
            List<(double[],double[])> LearningData = new List<(double[], double[])>();
            object LearningDataLock = new object();

            DateTime[] SampleTimes = FlowSamples.Select(c => c.StartTime)
                .Distinct().ToArray();

            Parallel.For<List<(double[], double[])>>(0, SampleTimes.Length,
                () => { return new List<(double[], double[])>(); },
                (Index, State, DataList) =>
                {
                    string[] Addresss = FlowSamples.Where(c => c.StartTime == SampleTimes[Index])
                .Select(c => c.Source_Address).Distinct().ToArray();

                    foreach (var Address in Addresss)
                    {
                        List<(int, int, int, Int64)> FlowStatisticsModelsList
                            = FlowSamples.Where(c => c.StartTime == SampleTimes[Index] && c.Source_Address == Address)
                            .Select(c => (c.Protocal, c.Port, c.Count, c.ByteTotal)).ToList();

                        DataList.Add((ToLearningData(FlowStatisticsModelsList),
                                Decoder(3, FlowSamples.Where(c => c.StartTime == SampleTimes[Index] && c.Source_Address == Address).Select(c => c.BehaviorNumber).Distinct().First())));
                    }
                    return DataList;
                },
                (DataList) =>
                {
                    lock (LearningDataLock)
                        LearningData.AddRange(DataList);
                });

            List<double[]> Input = new List<double[]>();
            List<double[]> ANS = new List<double[]>();

            foreach (var q in LearningData)
            {
                Input.Add(q.Item1);
                ANS.Add(q.Item2);
            }

            return (Input.ToArray(),ANS.ToArray());
        }

        public static double[][] FlowStatisticsToLearningData(DataFlowStatistics[] FlowStatistics)
        {
            List<double[]> LearningData = new List<double[]>();
            object LearningDataLock = new object();

            //列出單位時間有哪些IP
            string[] FlowSourceAddress = FlowStatistics.Select(c => c.Source_Address)
                .Distinct().ToArray();

            Parallel.For<List<double[]>>(0, FlowSourceAddress.Length,
                () => { return new List<double[]>(); },
                (Index, State, DataList) =>
                {

                    List<(int, int, int, Int64)> FlowStatisticsModelsList
                        = FlowStatistics.Where(c => c.Source_Address == FlowSourceAddress[Index])
                            .Select(c => (c.Protocal, c.Port, c.Count, c.ByteTotal)).ToList();

                    DataList.Add(ToLearningData(FlowStatisticsModelsList));

                    return DataList;
                },
                (DataList) =>
                {
                    lock (LearningDataLock)
                        LearningData.AddRange(DataList);
                });

            return LearningData.ToArray();
        }

        public static DateTime[][] TimeGroupRandom(this List<DateTime> TimeList,int Quantity)
        {
            if (Quantity < 1)
                throw new ArgumentException("Quantity 必須大於0");

            Random Rnd = new Random(Guid.NewGuid().GetHashCode());

            List<DateTime[]> Groups = new List<DateTime[]>();

            while(TimeList.Count >= Quantity)
            {
                DateTime[] Group = new DateTime[Quantity];

                for (int i = 0;i < Quantity;i++)
                {
                    DateTime Time = TimeList[Rnd.Next(0, TimeList.Count)];
                    Group[i] = Time;
                    TimeList.Remove(Time);
                }

                Groups.Add(Group);
            }

            if (TimeList.Count != 0)
                Groups.Add(TimeList.ToArray());

            return Groups.ToArray();
        }

        public static double FormatOutputResult(double[] output)
        {
            return output.ToList().IndexOf(output.Max());
        }

        private static double[] ToLearningData(List<(int, int, int, Int64)> Input)
        {
            double[] LearningData = new double[14638];
            //double[] LearningData = new double[131324];

            List<int> CompressionOther = new List<int>();

            for (int i = 0; i < LearningData.Length; i++)
                LearningData[i] = 0;

            foreach (var FlowStatistics in Input)
            {
                if (FlowStatistics.Item1 >= 0 && FlowStatistics.Item1 <= 5)
                {
                    LearningData[FlowStatistics.Item1] = ConntToLearningType(FlowStatistics.Item3);
                    //LearningData[FlowStatistics.Item1 + 131326] = ByteToLearningType(FlowStatistics.Item4);
                }

                if (FlowStatistics.Item1 >= 7 && FlowStatistics.Item1 <= 16)
                {
                    LearningData[FlowStatistics.Item1 - 1] = ConntToLearningType(FlowStatistics.Item3);
                    //LearningData[FlowStatistics.Item1 + 131325] = ByteToLearningType(FlowStatistics.Item4);
                }

                if (FlowStatistics.Item1 >= 18 && FlowStatistics.Item1 <= 255)
                {
                    LearningData[FlowStatistics.Item1 - 2] = ConntToLearningType(FlowStatistics.Item3);
                    //LearningData[FlowStatistics.Item1 + 131324] = ByteToLearningType(FlowStatistics.Item4);
                }

                if (FlowStatistics.Item1 is 6)
                {
                    if (FlowStatistics.Item2 > 4000 && FlowStatistics.Item2 <= 8080)
                    {
                        LearningData[Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 4000)) / 2) + 4001 + 254] += FlowStatistics.Item3;
                        if (!CompressionOther.Contains(Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 4000)) / 2) + 4001 + 254))
                            CompressionOther.Add(Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 4000)) / 2) + 4001 + 254);
                    }   
                    else if (FlowStatistics.Item2 > 8080)
                    {
                        LearningData[Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 8080)) / 50) + 6042 + 254] += FlowStatistics.Item3;
                        if (!CompressionOther.Contains(Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 8080)) / 50) + 6042 + 254))
                            CompressionOther.Add(Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 8080)) / 50) + 6042 + 254);
                    }
                    else
                        LearningData[FlowStatistics.Item2 + 254] = ConntToLearningType(FlowStatistics.Item3);
                    //LearningData[FlowStatistics.Item2 + 131580] = ByteToLearningType(FlowStatistics.Item4);
                }

                if (FlowStatistics.Item1 is 17)
                {
                    if (FlowStatistics.Item2 > 4000 && FlowStatistics.Item2 <= 8080)
                    {
                        LearningData[Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 4000)) / 2) + 4001 + 7446] += FlowStatistics.Item3;
                        if (!CompressionOther.Contains(Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 4000)) / 2) + 4001 + 7446))
                            CompressionOther.Add(Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 4000)) / 2) + 4001 + 7446);
                    } 
                    else if (FlowStatistics.Item2 > 8080)
                    {
                        LearningData[Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 8080)) / 50) + 6042 + 7446] += FlowStatistics.Item3;
                        if (!CompressionOther.Contains(Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 8080)) / 50) + 6042 + 7446))
                            CompressionOther.Add(Convert.ToInt32(Math.Floor(Convert.ToDouble(FlowStatistics.Item2 - 8080)) / 50) + 6042 + 7446);
                    }  
                    else
                        LearningData[FlowStatistics.Item2 + 7446] = ConntToLearningType(FlowStatistics.Item3);
                    //LearningData[FlowStatistics.Item2 + 197116] = ByteToLearningType(FlowStatistics.Item4);
                }
            }

            foreach (var q in CompressionOther)
                LearningData[q] = ConntToLearningType(Convert.ToInt32(LearningData[q]));

            return LearningData;
        }

        private static double ConntToLearningType(int Count)
        {
            if (Count <= 10)
                return 0.1;
            else if (Count <= 50)
                return 0.2;
            else if (Count <= 100)
                return 0.3;
            else if (Count <= 500)
                return 0.4;
            else if (Count <= 1000)
                return 0.5;
            else if (Count <= 5000)
                return 0.6;
            else if (Count <= 10000)
                return 0.7;
            else if (Count <= 25000)
                return 0.8;
            else if (Count <= 50000)
                return 0.9;
            else
                return 1;
        }

        private static double ByteToLearningType(Int64 Byte)
        {
            double tmp = Convert.ToDouble(Byte) / 6000000000;

            if (tmp > 1)
                return 1;

            return tmp;
        }

        private static double[] Decoder(int Size, int Input)
        {
            if (Input > Size)
                throw new ArgumentException("輸入值超出解碼器大小");

            double[] Buffer = new double[Size + 1];

            for (int i = 0; i <= Size; i++)
                Buffer[i] = 0;
            Buffer[Input] = 1;

            return Buffer;
        }
    }
}
