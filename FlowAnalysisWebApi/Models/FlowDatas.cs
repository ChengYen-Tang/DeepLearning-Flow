namespace FlowAnalysisWebApi.Models
{
    using System;
    using System.Data.Entity;

    public partial class FlowDatas : DbContext
    {
        public FlowDatas()
            : base("name=FlowDatas")
        {
        }

        public virtual DbSet<LockTable> LockUsers { get; set; }
        public virtual DbSet<WhiteList> WhiteUsers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        public partial class LockTable
        {
            public Guid Id { get; set; }
            public string IP { get; set; }
            public DateTime Time { get; set; }
            public string Reason { get; set; }
        }

        public partial class WhiteList
        {
            public Guid Id { get; set; }
            public string IP { get; set; }
            public string Reason { get; set; }
        }

    }
}
