using GitLite.Extensions;
using LibGit2Sharp;
using System;
using System.Windows.Media.Imaging;

namespace GitLite.Repositories.Data;

public class CommitItem
{
    public CommitItem(Commit commit)
    {
        Message = commit.MessageShort;
        Author = commit.Author.Name;
        Commiter = commit.Committer.Name;
        Id = commit.Id.Sha;
        Date = commit.Author.When.ToLocalTime();
        Gravatar = new BitmapImage(ImageExtensions.GetGravatar(commit.Author.Email));
    }

    public string Message { get; set; }
    public DateTimeOffset Date { get; set; }
    public string Author { get; set; }
    public string Commiter { get; set; }
    public BitmapImage Gravatar { get; set; }
    public string Id { get; set; }
}