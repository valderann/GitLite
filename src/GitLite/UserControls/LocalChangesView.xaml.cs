using GitLite.Repositories;
using GitLite.Repositories.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GitLite.UserControls;

/// <summary>
/// Interaction logic for LocalChangesView.xaml
/// </summary>
public partial class LocalChangesView : UserControl
{
    private GitRepository _gitRepository;
    public LocalChangesView()
    {
        InitializeComponent();
    }

    public void SetRepository(GitRepository repository)
    {
        _gitRepository = repository;
    }

    public void RefreshCommitView()
    {
        StagedFileChangesList.ItemsSource = _gitRepository.StagedChanges();
        UnStagedFileChangesList.ItemsSource = _gitRepository.UnStagedChanges();
    }

    private void StageAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        var allChanges = _gitRepository.UnStagedChanges();
        _gitRepository.StageChanges(allChanges.Select(t => t.FileName).ToArray());

        RefreshCommitView();
    }

    private void CommitButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CommitMessage.Text)) return;

        _gitRepository.CommitChanges(CommitMessage.Text);
        RefreshCommitView();
    }

    private void UnstageAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        var allChanges = _gitRepository.StagedChanges();
        _gitRepository.UnStageChanges(allChanges.Select(t => t.FileName).ToArray());

        RefreshCommitView();
    }

    private void UnStagedFileChangesList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UnStagedFileChangesList.SelectedItem is not PatchItem patch) return;

        DiffLocalChange.NewText = string.Empty;
        DiffLocalChange.OldText = string.Empty;

        var localFilePaths = Path.Combine(_gitRepository.Path, patch.FileName);
        if (File.Exists(localFilePaths))
        {
            DiffLocalChange.NewText = File.ReadAllText(localFilePaths);
        }

        var previousCommit = _gitRepository.GetPreviousCommitOfFile(patch.FileName);
        if (previousCommit != null)
        {
            DiffLocalChange.OldText = _gitRepository.GetBlobContent(previousCommit, patch.FileName);
        }
    }

    private void CommitMessage_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        CommitButton.IsEnabled = !string.IsNullOrWhiteSpace(CommitMessage.Text);
    }
}