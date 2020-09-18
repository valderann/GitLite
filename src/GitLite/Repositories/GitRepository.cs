using GitLite.Repositories.Data;
using GitLite.Repositories.Filters;
using LibGit2Sharp;
using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GitLite.Repositories;

public class GitRepository
{
    private readonly Repository _repo;
    private readonly string _path;
    public GitRepository(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path", nameof(path));
        _path = path;
        _repo = new Repository(path);
    }

    public string Path => _path;

    public BranchItem[] GetBranches(BranchFilter filter = null)
    {
        var branchQuery = _repo.Branches.AsQueryable();
        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.SearchText)) branchQuery = branchQuery.Where(t => t.FriendlyName.Contains(filter.SearchText));
            if (filter.IsRemote != null) branchQuery = branchQuery.Where(t => t.IsRemote == filter.IsRemote);
        }

        return branchQuery.Select(t => new BranchItem
        {
            Name = t.FriendlyName,
            IsCurrent = t.IsCurrentRepositoryHead,
            IsTracking = t.IsTracking,
            AheadBy = t.TrackingDetails.AheadBy ?? 0,
            BehindBy = t.TrackingDetails.BehindBy ?? 0,
        }).ToArray();
    }

    public PatchItem[] UnStagedChanges()
    {
        return _repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false, IncludeUnaltered = false }).Modified
            .Select(t => new PatchItem
            {
                FileName = t.FilePath,
                Status = t.State.ToString()
            })
            .ToArray();
    }


    public PatchItem[] StagedChanges()
        => _repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false, IncludeUnaltered = false }).Staged
            .Select(t => new PatchItem
            {
                FileName = t.FilePath,
                Status = t.State.ToString()
            })
            .ToArray();

    public int LocalChangesCount()
        => _repo.RetrieveStatus(new StatusOptions { IncludeIgnored = false, IncludeUnaltered = false }).Count();

    public void StageChanges(string[] paths)
    {
        try
        {
            Commands.Stage(_repo, paths);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public void UnStageChanges(string[] paths)
    {
        try
        {
            Commands.Unstage(_repo, paths);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public void CommitChanges(string message)
    {
        try
        {
            _repo.Commit(message, GetSignature(),
                GetSignature());
        }
        catch (Exception)
        {
            // ignored
        }
    }


    public IQueryable<CommitItem> GetCommits(string branchName, Filters.CommitFilter filter)
    {
        var commitQuery = _repo.Branches[branchName].Commits.AsQueryable();
        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchText)) commitQuery = commitQuery.Where(t => t.Message.Contains(filter.SearchText));
            if (!string.IsNullOrWhiteSpace(filter.Author)) commitQuery = commitQuery.Where(t => NormalizeName(t.Author.Name) == NormalizeName(filter.Author));
        }

        return commitQuery.Select(t => new CommitItem(t));
    }

    public IEnumerable<CommitItem> GetFileHistory(string filePathRelativeToRepository)
    {
        var fileHistory = _repo.Commits.QueryBy(filePathRelativeToRepository);
        foreach (var version in fileHistory)
        {
            yield return new CommitHistoryItem(version.Commit, version.Path);
        }
    }

    public IEnumerable<Reference> GetReferences(Commit commit)
        => new List<Reference>(_repo.Refs.ReachableFrom(new[] { commit }));
    public Commit GetPreviousCommitOfFile(string filePathRelativeToRepository, string commitSha = null)
    {
        var versionMatchesGivenVersion = false;
        var fileHistory = _repo.Commits.QueryBy(filePathRelativeToRepository);
        foreach (var version in fileHistory)
        {
            if (string.IsNullOrWhiteSpace(commitSha) || versionMatchesGivenVersion)
                return version.Commit;

            if (version.Commit.Sha.Equals(commitSha))
                versionMatchesGivenVersion = true;
        }

        return null;
    }

    public void CreateBranch(string name, string branchName)
    {
        var branch = _repo.Branches[branchName];
        if (branch == null) return;

        _repo.Branches.Add(name, branch.Tip);
    }

    public void RemoveBranch(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        _repo.Branches.Remove(name);
    }

    public void PushBranch(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName)) return;
        var head = _repo.Head;
        var remote = _repo.Network.Remotes[head.RemoteName];

        var credentials = GetCredentials(remote);
        if (credentials == null) return;

        var options = new PushOptions
        {
            CredentialsProvider = (_, _, _) => credentials
        };
        _repo.Network.Push(_repo.Branches[branchName], options);
    }

    public void Pull()
    {
        var head = _repo.Head;
        var remote = _repo.Network.Remotes[head.RemoteName];

        var credentials = GetCredentials(remote);
        if (credentials == null) return;

        var signature = GetSignature();

        // Pull
        try
        {
            var status = Commands.Pull(_repo, signature, new PullOptions
            {
                FetchOptions = new FetchOptions
                {
                    CredentialsProvider = (_, _, _) => credentials
                }
            });
        }
        catch (Exception)
        {

        }
    }

    private static UsernamePasswordCredentials GetCredentials(Remote remote)
    {
        var secrets = new SecretStore("git");
        var auth = new BasicAuthentication(secrets);

        var targetUrl = new TargetUri($"https://{new Uri(remote.Url).Host}");
        var remoteUrl = new Uri(remote.Url);

        if (remoteUrl.Host == "dev.azure.com")
        {
            var firstIndex = remoteUrl.AbsolutePath.Split("/")[1];
            targetUrl = new TargetUri($"https://{new Uri(remote.Url).Host}/{firstIndex}");
        }

        var credentials = auth.GetCredentials(targetUrl);
        if (credentials == null) return null;

        return new UsernamePasswordCredentials
        {
            Username = credentials.Username,
            Password = credentials.Password
        };
    }

    public void Fetch()
    {
        var head = _repo.Head;
        var remote = _repo.Network.Remotes[head.RemoteName];

        var credentials = GetCredentials(remote);
        if (credentials == null) return;

        try
        {
            _repo.Network.Fetch(remote.Name, remote.FetchRefSpecs.Select(rs => rs.Specification), new FetchOptions
            {
                CredentialsProvider = (_, _, _) => credentials
            });
        }
        catch
        {
        }
    }

    public string GetBlobContent(Commit commit, string fileName)
    {
        if (commit == null) return string.Empty;
        var treeEntry = commit[fileName];

        if (treeEntry == null) return string.Empty;
        if (treeEntry.TargetType != TreeEntryTargetType.Blob) return string.Empty;

        var blob = (Blob)treeEntry.Target;
        if (blob.IsBinary) return string.Empty;

        return blob.GetContentText();
    }

    public Stream GetBlobStream(Commit commit, string fileName)
    {
        if (commit == null) return null;
        var treeEntry = commit[fileName];

        if (treeEntry == null) return null;
        if (treeEntry.TargetType != TreeEntryTargetType.Blob) return null;

        var blob = (Blob)treeEntry.Target;
        return blob.GetContentStream();
    }

    public Commit GetCommit(string objectId)
        => _repo.Lookup<Commit>(objectId);

    public IEnumerable<Tag> GetTags()
        => _repo.Tags.Select(t => t);

    public CommitFile[] GetFiles(string commitId)
    {
        var commit1 = _repo.Lookup<Commit>(commitId);
        if (commit1 == null) return Array.Empty<CommitFile>();
        if (!commit1.Parents.Any())
        {
            return RecursivelyDumpTreeContent(commit1.Tree)
                .Select(t => new CommitFile { FileName = t.Path, Status = ChangeKind.Added })
                .ToArray();
        }

        var commit2 = commit1.Parents.First();

        var treeChanges = _repo.Diff.Compare<TreeChanges>(commit2.Tree, commit1.Tree);
        return treeChanges.Select(t => new CommitFile { FileName = t.Path, Status = t.Status })
            .ToArray();
    }
    public CommitStatistic[] GetCommitStatistics(string commitId)
    {
        var commit1 = _repo.Lookup<Commit>(commitId);
        if (commit1 == null) return Array.Empty<CommitStatistic>();
        if (!commit1.Parents.Any())
        {
            return RecursivelyDumpTreeContent(commit1.Tree).Select(t => new CommitStatistic { FileName = t.Path, Status = ChangeKind.Added, LinesAdded = 0, LinesDeleted = 0 })
                .ToArray();
        }

        var commit2 = commit1.Parents.First();

        var treeChanges = _repo.Diff.Compare<Patch>(commit2.Tree, commit1.Tree);
        return treeChanges.Select(t => new CommitStatistic { FileName = t.Path, Status = t.Status, LinesAdded = t.LinesAdded, LinesDeleted = t.LinesDeleted })
            .ToArray();
    }

    private Signature GetSignature()
    {
        var email = _repo.Config.Get<string>("user.email");
        var name = _repo.Config.Get<string>("user.name");

        return new Signature(name.Value, email.Value, DateTime.UtcNow);
    }

    private static IEnumerable<TreeEntry> RecursivelyDumpTreeContent(Tree tree)
    {
        foreach (var treeEntry in tree)
        {
            var gitObject = treeEntry.Target;

            switch (treeEntry.TargetType)
            {
                case TreeEntryTargetType.Tree:
                    {
                        var items = RecursivelyDumpTreeContent((Tree)gitObject);
                        foreach (var item in items)
                        {
                            yield return item;
                        }

                        break;
                    }
                case TreeEntryTargetType.Blob:
                    yield return treeEntry;
                    break;
            }
        }
    }

    private static string NormalizeName(string name)
        => name.Replace(".", "")
            .Replace(" ", "").ToLower();
}