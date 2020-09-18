namespace GitLite.Component.TreeView;

public class BranchTreeItem : TreeChild
{
    public bool IsTracking { get; set; }
    public int AheadBy { get; set; }
    public int BehindBy { get; set; }

    public bool IsAhead => AheadBy > 0;
    public bool IsBehind => BehindBy > 0;

    public string BranchName { get; set; }
}