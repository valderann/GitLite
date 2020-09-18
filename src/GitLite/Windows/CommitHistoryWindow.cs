using GitLite.Repositories;
using GitLite.Repositories.Data;
using System.Windows;
using System.Windows.Controls;

namespace GitLite.Windows;

/// <summary>
/// Interaction logic for CommitHistoryWindow.xaml
/// </summary>.0
public partial class CommitHistoryWindow : Window
{
    private readonly GitRepository _repository;

    public CommitHistoryWindow(GitRepository repository, string fileName)
    {
        InitializeComponent();

        _repository = repository;

        Title = $"Commit history - {fileName}";
        ListChanges.ItemsSource = repository.GetFileHistory(fileName);
    }

    private void ListChanges_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListChanges.SelectedItem is not CommitHistoryItem commitItem) return;

        var commit = _repository.GetCommit(commitItem.Id);
        if (commit == null) return;

        DiffViewer.OldText = GetOldText(commitItem.Path, commit.Sha);
        DiffViewer.NewText = _repository.GetBlobContent(commit, commitItem.Path);
    }

    private string GetOldText(string path, string commitSha)
    {
        var commit = _repository.GetPreviousCommitOfFile(path, commitSha);
        if (commit == null) return string.Empty;

        return _repository.GetBlobContent(commit, path);
    }
}