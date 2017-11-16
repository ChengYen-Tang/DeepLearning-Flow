namespace FlowAnalysisWeb.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class FlowDatas : DbContext
    {
        public FlowDatas()
            : base("name=FlowDatas")
        {
        }

        public virtual DbSet<AnalysisResults> AnalysisResults { get; set; }
        public virtual DbSet<LockTables> LockTables { get; set; }
        public virtual DbSet<WhiteLists> WhiteLists { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }

    public partial class AnalysisResults
    {
        public Guid Id { get; set; }

        public DateTime AnalysisTime { get; set; }

        public string IP { get; set; }

        public int Result { get; set; }
    }

    public partial class LockTables
    {
        public Guid Id { get; set; }

        public string IP { get; set; }

        public string Reason { get; set; }

        public DateTime Time { get; set; }
    }

    public partial class WhiteLists
    {
        public Guid Id { get; set; }

        public string IP { get; set; }

        public string Reason { get; set; }
    }
}
