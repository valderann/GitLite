using LibGit2Sharp;

namespace GitLite.Repositories.Data;

public class CommitHistoryItem : CommitItem
{
    public CommitHistoryItem(Commit commit, string path) : base(commit)
    {
        Path = path;
    }
    public string Path { get; init; }
}