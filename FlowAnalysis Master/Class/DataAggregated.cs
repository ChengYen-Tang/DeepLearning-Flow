using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlowAnalysis.Models;
using System.Net;

namespace FlowAnalysis.Class
{
    public class SampleAggregated
    {
        private FlowDatas db = new FlowDatas();
        private string IPv4Address = string.Empty;
        private int BehaviorNumber = int.MinValue;
        public SampleAggregated(string IPv4IP,int SampleBehaviorNumber)
        {
            if (IPAddress.TryParse(IPv4IP, out IPAddress IP))
            {
                IPv4Address = IP.ToString();
                BehaviorNumber = SampleBehaviorNumber;
            }
            else
                throw new ArgumentException("IP格式錯誤");
        }

        public void Collect(int Day ,int Hour, int Min,int Sec)
        {
            FlowArrange(Day, Hour, Min, Sec);
        }

        public void Collect(int Hour,int Min, int Sec)
        {
            FlowArrange(0, Hour, Min, Sec);
        }

        public void Collect(int Min, int Sec)
        {
            FlowArrange(0, 0, Min, Sec);
        }

        private void FlowArrange(int Day, int Hour, int Min, int Sec)
        {
            object FlowSampleListLock = new object();
            DateTime StartTime = DateTime.Now
                .AddDays(-Day)
                .AddHours(-Hour)
                .AddMinutes(-Min)
                .AddSeconds(-Sec);

            NetFlow[] FlowDatas = db.NetFlows
                .Where(c => c.Source_Address == IPv4Address &&
                c.Start_Time >= StartTime
                ).ToArray();

            DateTime[] FlowTimes 
                = FlowDatas.Select(c => c.Start_Time).Distinct().ToArray();

            List<FlowSampleStatistics> FlowSampleList = new List<FlowSampleStatistics>();
            DateTime NowTime = DateTime.Now;


            Parallel.For<List<FlowSampleStatistics>>(0, FlowTimes.Length,
                () => { return new List<FlowSampleStatistics>(); },
                (Index , State, SampleList) =>
                {
                    int?[] Protocols
                = FlowDatas.Where(c => c.Start_Time == FlowTimes[Index])
                .Select(c => c.Protocol).Distinct().ToArray();

                    foreach (int Protocol in Protocols)
                        if (Protocol is 6 || Protocol is 17)
                        {
                            int?[] Ports = FlowDatas
                                .Where(c => c.Start_Time == FlowTimes[Index] &&
                                c.Protocol == Protocol)
                               .Select(c => c.Destination_Port).Distinct().ToArray();

                            foreach (int Port in Ports)
                            {
                                int ByteTotal = 0;
                                foreach (int Bytes in FlowDatas
                                        .Where(c => c.Start_Time == FlowTimes[Index] &&
                                        c.Protocol == Protocol &&
                                        c.Destination_Port == Port)
                                        .Select(c => c.Bytes).ToList())
                                    ByteTotal += Bytes;

                                SampleList.Add(
                                        new FlowSampleStatistics
                                        {
                                            Id = Guid.NewGuid(),
                                            BehaviorNumber = BehaviorNumber,
                                            CreateTime = NowTime,
                                            StartTime = FlowTimes[Index],
                                            Source_Address = IPv4Address,
                                            Protocal = (int)Protocol,
                                            Port = Port,
                                            ByteTotal = ByteTotal,
                                            Count = FlowDatas
                                        .Where(c => c.Start_Time == FlowTimes[Index] && c.Protocol == Protocol && c.Destination_Port == Port).Count()
                                        });
                            }
                        }
                        else
                        {
                            int ByteTotal = 0;
                            foreach (int Bytes in FlowDatas
                                    .Where(c => c.Start_Time == FlowTimes[Index] && c.Protocol == Protocol)
                                    .Select(c => c.Bytes).ToList())
                                ByteTotal += Bytes;

                            SampleList.Add(
                                    new FlowSampleStatistics
                                    {
                                        Id = Guid.NewGuid(),
                                        BehaviorNumber = BehaviorNumber,
                                        CreateTime = NowTime,
                                        StartTime = FlowTimes[Index],
                                        Source_Address = IPv4Address,
                                        Protocal = (int)Protocol,
                                        Port = 0,
                                        ByteTotal = ByteTotal,
                                        Count = FlowDatas
                                    .Where(c => c.Start_Time == FlowTimes[Index] && c.Protocol == Protocol).Count()
                                    });
                        }
                    return SampleList;
                },
                (SampleList) =>
                {
                    lock (FlowSampleListLock)
                        FlowSampleList.AddRange(SampleList);
                });

            db.FlowSampleStatistics.AddRange(FlowSampleList);
            db.SaveChanges();

            Console.WriteLine("IP :{0} Done", IPv4Address);
        }

        public void Reset(string IPv4IP, int SampleBehaviorNumber)
        {
            if (IPAddress.TryParse(IPv4IP, out IPAddress IP))
            {
                IPv4Address = IP.ToString();
                BehaviorNumber = SampleBehaviorNumber;
            }
            else
                throw new ArgumentException("IP格式錯誤");
        }
    }

    public static class FlowAggregated
    {
        private static FlowDatas db = new FlowDatas();
        public static List<DataFlowStatistics> Collect(int Day, int Hour, int Min, int Sec)
        {
            return FlowArrange(Day, Hour, Min, Sec);
        }

        public static List<DataFlowStatistics> Collect(int Hour, int Min, int Sec)
        {
            return FlowArrange(0, Hour, Min, Sec);
        }

        public static List<DataFlowStatistics> Collect(int Min, int Sec)
        {
            return FlowArrange(0, 0, Min, Sec);
        }

        private static List<DataFlowStatistics> FlowArrange(int Day, int Hour, int Min, int Sec)
        {
            object FlowSampleListLock = new object();
            DateTime StartTime = DateTime.Now
                .AddDays(-Day)
                .AddHours(-Hour)
                .AddMinutes(-Min)
                .AddSeconds(-Sec);

            DateTime EndTime = StartTime.AddMinutes(1);

            NetFlow[] FlowDatas = db.NetFlows
                .Where(c => c.Start_Time >= StartTime && c.Start_Time <= EndTime
                ).ToArray();

            string[] FlowSourceAddress
                = FlowDatas.Select(c => c.Source_Address).Distinct().ToArray();

            List<DataFlowStatistics> FlowStatisticsList = new List<DataFlowStatistics>();
            DateTime NowTime = DateTime.Now;


            Parallel.For<List<DataFlowStatistics>>(0, FlowSourceAddress.Length,
                () => { return new List<DataFlowStatistics>(); },
                (Index, State, SampleList) =>
                {
                    int?[] Protocols
                        = FlowDatas.Where(c => c.Source_Address == FlowSourceAddress[Index])
                        .Select(c => c.Protocol).Distinct().ToArray();

                    foreach (int Protocol in Protocols)
                        if (Protocol is 6 || Protocol is 17)
                        {
                            int?[] Ports = FlowDatas
                                .Where(c => c.Source_Address == FlowSourceAddress[Index] &&
                                c.Protocol == Protocol)
                               .Select(c => c.Destination_Port).Distinct().ToArray();

                            foreach (int Port in Ports)
                            {
                                int ByteTotal = 0;
                                foreach (int Bytes in FlowDatas
                                        .Where(c => c.Source_Address == FlowSourceAddress[Index] &&
                                        c.Protocol == Protocol &&
                                        c.Destination_Port == Port)
                                        .Select(c => c.Bytes).ToList())
                                    ByteTotal += Bytes;

                                SampleList.Add(
                                        new DataFlowStatistics
                                        {
                                            Id = Guid.NewGuid(),
                                            CreateTime = NowTime,
                                            StartTime = StartTime,
                                            Source_Address = FlowSourceAddress[Index],
                                            Protocal = (int)Protocol,
                                            Port = Port,
                                            ByteTotal = ByteTotal,
                                            Count = FlowDatas
                                        .Where(c => c.Source_Address == FlowSourceAddress[Index] && c.Protocol == Protocol && c.Destination_Port == Port).Count()
                                        });
                            }
                        }
                        else
                        {
                            int ByteTotal = 0;
                            foreach (int Bytes in FlowDatas
                                    .Where(c => c.Source_Address == FlowSourceAddress[Index] && c.Protocol == Protocol)
                                    .Select(c => c.Bytes).ToList())
                                ByteTotal += Bytes;

                            SampleList.Add(
                                    new DataFlowStatistics
                                    {
                                        Id = Guid.NewGuid(),
                                        CreateTime = NowTime,
                                        StartTime = StartTime,
                                        Source_Address = FlowSourceAddress[Index],
                                        Protocal = (int)Protocol,
                                        Port = 0,
                                        ByteTotal = ByteTotal,
                                        Count = FlowDatas
                                    .Where(c => c.Source_Address == FlowSourceAddress[Index] && c.Protocol == Protocol).Count()
                                    });
                        }
                    return SampleList;
                },
                (SampleList) =>
                {
                    lock (FlowSampleListLock)
                        FlowStatisticsList.AddRange(SampleList);
                });

            //db.DataFlowStatistics.AddRange(FlowStatisticsList);
            //db.SaveChanges();

            Console.WriteLine("Time :{0} Done", StartTime);

            return FlowStatisticsList;
        }
    }
}
