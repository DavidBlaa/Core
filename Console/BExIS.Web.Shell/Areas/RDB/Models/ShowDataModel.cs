namespace BExIS.Modules.Rdb.UI.Models
{
    public class ShowModel
    {
        public long Id { get; set; }

        public long MetadataStructureId { get; set; }
        public long DataStructureId { get; set; }
        public long ResearchPlanId { get; set; }

        public string Title { get; set; }
        public bool ViewAccess { get; set; }
        public bool GrantAccess { get; set; }
        public bool IsCheckedIn { get; set; }
    }
}