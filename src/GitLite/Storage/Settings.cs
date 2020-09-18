using System;

namespace GitLite.Storage;

public class Settings
{
    public Settings()
    {
        Repositories = Array.Empty<RepoSettings>();
    }

    public string SelectedRepoName { get; set; }
    public string SelectedBranchName { get; set; }

    public RepoSettings[] Repositories { get; set; }
}