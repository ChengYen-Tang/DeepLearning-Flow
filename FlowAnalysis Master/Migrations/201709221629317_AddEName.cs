namespace FlowAnalysis_Master.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddEName : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AnalysisResults",
                c => new
                    {
                        Id = c.Guid(nullable: false),
                        AnalysisTime = c.DateTime(nullable: false),
                        IP = c.String(),
                        Result = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.AnalysisResults");
        }
    }
}
