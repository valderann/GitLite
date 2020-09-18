using GitLite.Component.TreeView;
using GitLite.Repositories;
using GitLite.Repositories.Data;
using GitLite.Storage;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace GitLite.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly ConfigStore _configStore = new();

    private GitRepository _gitRepository;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void AddSettings()
    {
        using var fbd = new FolderBrowserDialog();
        var result = fbd.ShowDialog();

        if (result != System.Windows.Forms.DialogResult.OK || string.IsNullOrWhiteSpace(fbd.SelectedPath)) return;


        var selectedPath = fbd.SelectedPath;
        if (!Repository.IsValid(fbd.SelectedPath)) return;
        var settings = _configStore.Load();
        var repoList = settings.Repositories.ToList();

        if (repoList.Any(t => t.Location == selectedPath)) return;

        repoList.Add(new RepoSettings { Location = selectedPath, Name = selectedPath.Split('/').LastOrDefault() });
        settings.Repositories = repoList.ToArray();
        _configStore.Store(settings);

        LoadRepositories();
    }

    private void LoadRepositories()
    {
        var settings = _configStore.Load();
        CmbRepositories.ItemsSource = settings.Repositories;
    }

    private void cmbRepositories_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var settings = (RepoSettings)CmbRepositories.SelectedItem;
        if (settings == null) return;

        SetCurrentValue(TitleProperty, $"GitLite - {settings.Name}");

        var configSettings = _configStore.Load();
        configSettings.SelectedRepoName = settings.Name;
        _configStore.Store(configSettings);

        _gitRepository = new GitRepository(settings.Location);

        RefreshBranches();
    }

    private static IEnumerable<TreeChild> GetBranchArray(IEnumerable<TreeChild> items)
        => items.OrderByDescending(t => t.Children is { Length: > 0 })
            .ThenBy(t => t.Name);

    private static IEnumerable<TreeChild> GetBranchItems(IEnumerable<BranchItem> items, int position = 0)
    {
        var folderList = items.Where(t => t.Name.Split("/").Length > position)
            .GroupBy(t => new { Name = t.Name.Split("/")[position], IsFolder = t.Name.Split("/").Length > position + 1 })
            .Select(t => new { t.Key, Items = t })
            .ToArray();

        foreach (var item in folderList)
        {
            if (item.Key.IsFolder)
            {
                var newPosition = position + 1;
                yield return new FolderItem
                {
                    Name = item.Key.Name,
                    Children = GetBranchArray(GetBranchItems(item.Items, newPosition)).ToArray()
                };
            }
            else
            {
                foreach (var child in item.Items)
                {
                    var splitName = child.Name.Split("/").Length > position
                        ? child.Name.Split("/")[position]
                        : child.Name;

                    yield return new BranchTreeItem
                    {
                        AheadBy = child.AheadBy,
                        BehindBy = child.BehindBy,
                        IsSelected = child.IsCurrent,
                        IsTracking = child.IsTracking,
                        BranchName = child.Name,
                        Name = splitName
                    };
                }
            }
        }
    }

    private void lstBranches_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (LstBranches.SelectedItem is TreeItem { Name: "Local changes" })
        {
            ContentTab.SelectedIndex = 0;
            LocalChangesContentControl.SetRepository(_gitRepository);
            LocalChangesContentControl.RefreshCommitView();

            return;
        }
        ContentTab.SelectedIndex = 1;

        if (LstBranches.SelectedItem is not BranchTreeItem selectedItem) return;

        var configSettings = _configStore.Load();
        configSettings.SelectedBranchName = selectedItem.BranchName;
        _configStore.Store(configSettings);

        CommitContentControl.SetRepository(_gitRepository);
        CommitContentControl.RefreshCommits(selectedItem.BranchName);
    }

    private void MenuItemStat_Click(object sender, RoutedEventArgs e)
    {
        if (LstBranches.SelectedItem is not BranchTreeItem selectedItem) return;

        var window = new Statistics(_gitRepository, selectedItem.BranchName);
        window.Show();
    }

    private void MainWindow_OnContentRendered(object sender, EventArgs e)
    {
        LoadRepositories();

        var configSettings = _configStore.Load();
        if (!string.IsNullOrEmpty(configSettings.SelectedRepoName))
        {
            var repoSetting =
                configSettings.Repositories.FirstOrDefault(t => t.Name == configSettings.SelectedRepoName);
            if (repoSetting != null) CmbRepositories.SelectedIndex = CmbRepositories.Items.IndexOf(repoSetting);
        }

        _configStore.Store(configSettings);
    }

    private void AddRepositoryMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        AddSettings();
    }

    private void FetchRepositoryMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        _gitRepository.Fetch();
    }

    private void RefreshBranches()
    {
        var localBranches = _gitRepository.GetBranches(new Repositories.Filters.BranchFilter { IsRemote = false });
        var remoteBranches = _gitRepository.GetBranches(new Repositories.Filters.BranchFilter { IsRemote = true });
        var tags = _gitRepository.GetTags();

        var branches = new[]
        {
            new TreeItem()
            {
                Name = "Local changes"
            },
            // ReSharper disable once CoVariantArrayConversion
            new BranchesTreeItem
            {
                Name = "Local branches", Count = localBranches.Length,
                Children = GetBranchArray(GetBranchItems(localBranches)).ToArray(),
                IsNodeExpanded = true
            },
            // ReSharper disable once CoVariantArrayConversion
            new BranchesTreeItem
            {
                Name = "Remote branches", Count = remoteBranches.Length,
                Children = GetBranchArray(GetBranchItems(remoteBranches)).ToArray(),
                IsNodeExpanded = false
            },
            // ReSharper disable once CoVariantArrayConversion
            new TagsItem
            {
                Name = "Tags",
                Children = tags.Select(ve => new TagTreeItem {Name = ve.FriendlyName}).Take(20)
                    .OrderByDescending(t => t.Name).ToArray(),
                IsNodeExpanded = false
            }
        };
        LstBranches.ItemsSource = branches;
    }

    private void DeleteBranchMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (LstBranches.SelectedItem is not BranchTreeItem selectedItem) return;

        _gitRepository.RemoveBranch(selectedItem.BranchName);

        RefreshBranches();
    }

    private void PullMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (LstBranches.SelectedItem is not BranchTreeItem) return;

        _gitRepository.Fetch();

        _gitRepository.Pull();
        RefreshBranches();
    }

    private void CreateBranchMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (LstBranches.SelectedItem is not BranchTreeItem branchItem) return;

        var window = new CreateBranchWindow(_gitRepository, branchItem.BranchName);
        window.Show();

        RefreshBranches();
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshBranches();
    }
}