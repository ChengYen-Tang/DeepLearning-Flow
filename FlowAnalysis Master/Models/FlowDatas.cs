namespace FlowAnalysis.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;
    using System.Data.Entity.ModelConfiguration.Conventions;

    public partial class FlowDatas : DbContext
    {
        public FlowDatas()
            : base("name=FlowDatas")
        {
        }

        public virtual DbSet<NetFlow> NetFlows { get; set; }
        public virtual DbSet<FlowSampleStatistics> FlowSampleStatistics { get; set; }
        //public virtual DbSet<DataFlowStatistics> DataFlowStatistics { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }

    public partial class NetFlow
    {
        public Guid Id { get; set; }

        public DateTime Start_Time { get; set; }

        public int? Protocol { get; set; }

        public string Source_Address { get; set; }

        public string Destination_Address { get; set; }

        public int? Source_Port { get; set; }

        public int? Destination_Port { get; set; }

        public int? Bytes { get; set; }

        public string TCP_Flags { get; set; }

        public int? Bytes_per_package { get; set; }
    }

    public partial class FlowSampleStatistics
    {
        public Guid Id { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime StartTime { get; set; }

        public string Source_Address { get; set; }

        public int BehaviorNumber { get; set; }
        
        public int Protocal { get; set; }

        public int Port { get; set; }

        public int Count { get; set; }

        public Int64 ByteTotal { get; set; }
    }

    public partial class DataFlowStatistics
    {
        public Guid Id { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime StartTime { get; set; }

        public string Source_Address { get; set; }


        public int Protocal { get; set; }

        public int Port { get; set; }

        public int Count { get; set; }

        public Int64 ByteTotal { get; set; }
    }
}
