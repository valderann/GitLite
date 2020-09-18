using LibGit2Sharp;

namespace GitLite.Repositories.Data;

public class CommitFile
{
    public string FileName { get; set; }
    public ChangeKind Status { get; set; }
}

public class CommitStatistic : CommitFile
{
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
}