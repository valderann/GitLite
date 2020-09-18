namespace GitLite.Component.TreeView;

public class TreeItem
{
    public string Name { get; set; }
    public bool IsNodeExpanded { get; set; }

    public bool IsSelected { get; set; }

    public TreeChild[] Children { get; set; }
}