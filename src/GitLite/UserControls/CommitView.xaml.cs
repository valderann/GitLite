using GitLite.Extensions;
using GitLite.Repositories;
using GitLite.Repositories.Data;
using GitLite.Windows;
using LibGit2Sharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GitLite.UserControls;

/// <summary>
/// Interaction logic for CommitView.xaml
/// </summary>
public partial class CommitView : UserControl
{
    private GitRepository _gitRepository;
    private string _currentBranchName;

    public CommitView()
    {
        InitializeComponent();
    }

    public void SetRepository(GitRepository repository)
    {
        _gitRepository = repository;
    }

    public void RefreshCommits(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
        {
            if (CommitsList != null) CommitsList.ItemsSource = null;
            if (FilesList != null) FilesList.ItemsSource = null;
            return;
        }

        _currentBranchName = branchName;

        if (CommitsList.ItemsSource == null) CommitsList.Items.Clear();
        CommitsList.ItemsSource = _gitRepository.GetCommits(branchName,
                new Repositories.Filters.CommitFilter {SearchText = CommitSearch.Text})
            .Take(500);

        if (CommitsList.Items.Count > 0)
        {
            CommitsList.SelectedIndex = 0;
        }
        else
        {
            if (FilesList is {ItemsSource: { }})
            {
                FilesList.ItemsSource = Array.Empty<PatchItem>();
            }
        }
    }

    private void CommitsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count != 1) return;
        var selected = e.AddedItems[0];

        if (selected is not CommitItem commitItem) return;
        var commit1 = _gitRepository.GetCommit(commitItem.Id);
        if (commit1 == null) return;

        CommiterName.Content = $"{commit1.Committer.Name}<{commit1.Committer.Email}>";
        CommiterImage.Source = new BitmapImage(ImageExtensions.GetGravatar(commit1.Committer.Email));

        AuthorName.Content = $"{commit1.Author.Name}<{commit1.Author.Email}>";
        AuthorImage.Source = new BitmapImage(ImageExtensions.GetGravatar(commit1.Author.Email));

        CommitMessage.Text = $"{commit1.Message}";
        CommitSha.Content = $"{commit1.Id}";
        CommitDate.Content = $"{commit1.Author.When}";

        var patch = _gitRepository.GetFiles(commit1.Id.Sha);

        CommitFileCount.Content = patch.Length.ToString();

        if (FilesList.ItemsSource == null) FilesList.Items.Clear();
        FilesList.ItemsSource = patch.Select(t => new PatchItem
        {
            FileName = t.FileName,
            Status = GetStatusString(t.Status)
        });

        DiffPanel.Visibility = Visibility.Collapsed;
        CommitGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
        CommitGrid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
    }

    private void FilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DiffPanel.Visibility != Visibility.Visible)
        {
            DiffPanel.Visibility = Visibility.Visible;
            CommitGrid.ColumnDefinitions[0].Width = new GridLength(0.5, GridUnitType.Star);
            CommitGrid.ColumnDefinitions[1].Width = new GridLength(0.5, GridUnitType.Star);
        }

        if (CommitsList?.SelectedItem is not CommitItem commitItem) return;
        if (e.AddedItems.Count == 0) return;

        if (e.AddedItems[0] is not PatchItem file) return;

        var commit = _gitRepository.GetCommit(commitItem.Id);

        FileContent.OldText = _gitRepository.GetBlobContent(commit.Parents.FirstOrDefault(), file.FileName);
        FileContent.NewText = _gitRepository.GetBlobContent(commit, file.FileName);
    }

    private void CopyCommitShaMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (CommitsList?.SelectedItem is not CommitItem commitItem) return;
        Clipboard.SetText(commitItem.Id);
    }


    private void OpenMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (FilesList.SelectedItem is not PatchItem selectedItem) return;

        var path = Path.Combine(_gitRepository.Path, selectedItem.FileName);
        if (!File.Exists(path)) return;

        var startInformation = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        };

        Process.Start(startInformation);
    }

    private void HistoryMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (FilesList is not {SelectedItem: PatchItem selectedItem}) return;

        var window = new CommitHistoryWindow(_gitRepository, selectedItem.FileName);
        window.Show();
    }

    private void EditMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (FilesList is not {SelectedItem: PatchItem selectedItem}) return;

        if (CommitsList?.SelectedItem is not CommitItem commitItem) return;

        var window = new EditorWindow(_gitRepository, selectedItem.FileName, commitItem.Id);
        window.Show();
    }

    private static string GetStatusString(ChangeKind kind)
    {
        return kind switch
        {
            ChangeKind.Added => "+",
            ChangeKind.Deleted => "-",
            ChangeKind.Modified => "c",
            ChangeKind.Renamed => "r",
            _ => string.Empty
        };
    }

    private void CommitSearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshCommits(_currentBranchName);
    }

    private void CommitsList_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged) return;
        var view = CommitsList.View as GridView;
        var border = VisualTreeHelper.GetChild(CommitsList, 0) as Decorator;
        if (border is not {Child: ScrollViewer scroller}) return;
        if (scroller.Content is not ItemsPresenter presenter) return;
        view.Columns[0].SetCurrentValue(GridViewColumn.WidthProperty, presenter.ActualWidth);
        for (var i = 1; i < view.Columns.Count; i++)
        {
            view.Columns[0].Width -= view.Columns[i].ActualWidth;
        }
    }

    private void FilesList_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged) return;
        var view = FilesList.View as GridView;
        var border = VisualTreeHelper.GetChild(FilesList, 0) as Decorator;
        if (border is not { Child: ScrollViewer scroller }) return;
        if (scroller.Content is not ItemsPresenter presenter) return;
        view.Columns[2].SetCurrentValue(GridViewColumn.WidthProperty, presenter.ActualWidth);
        for (var i = 0; i < view.Columns.Count -1; i++)
        {
            view.Columns[2].Width -= view.Columns[i].ActualWidth;
        }
    }
}