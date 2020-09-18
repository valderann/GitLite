namespace GitLite.Repositories.Data;

public class BranchItem
{
    public string Name { get; set; }
    public bool IsTracking { get; set; }
    public bool IsCurrent { get; set; }
    public int AheadBy { get; set; }
    public int BehindBy { get; set; }
}