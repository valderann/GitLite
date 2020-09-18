using GitLite.Extensions;
using GitLite.Repositories;
using LibGit2Sharp;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Definitions.Charts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GitLite.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class Statistics
{
    private readonly string _branchName;
    private readonly GitRepository _gitRepo;
    private readonly bool _isInitialized;

    public Statistics(GitRepository repo, string branchName)
    {
        InitializeComponent();

        Title = $"Statistics - {branchName}";

        _branchName = branchName;
        _gitRepo = repo;

        FromDatePicker.SelectedDate = DateTime.Now.AddYears(-1);
        ToDatePicker.SelectedDate = DateTime.Now;


        LoadAuthors();

        _isInitialized = true;
        LoadCharts();
    }

    private void LoadAuthors()
    {
        CmbAuthors.Items.Clear();
        var authors = _gitRepo.GetCommits(_branchName, new Repositories.Filters.CommitFilter())
            .Select(t => t.Author).Distinct().OrderBy(t => t);

        CmbAuthors.Items.Add("All");
        foreach (var author in authors)
        {
            CmbAuthors.Items.Add(author);
        }
    }

    private string GetSelectedAuthor()
    {
        if (CmbAuthors.SelectedItem == null) return null;
        if ((string)CmbAuthors.SelectedItem == "All") return null;
        return (string)CmbAuthors.SelectedItem;
    }

    private FileStatistics GetFileStatistics(string commitId, bool checkFiles, bool checkLines)
    {
        if (!checkFiles && !checkLines) return new FileStatistics();

        if (checkLines)
        {
            var lines = _gitRepo.GetCommitStatistics(commitId);
            return new FileStatistics
            {
                ChangedFiles = lines.Length,
                AddedLines = lines.Sum(t => t.LinesAdded),
                DeletedLines = lines.Sum(t => t.LinesDeleted),
                AddedFiles = lines.Count(t => t.Status == ChangeKind.Added),
                DeletedFiles = lines.Count(t => t.Status == ChangeKind.Deleted)
            };
        }

        var files = _gitRepo.GetFiles(commitId);
        return new FileStatistics
        {
            ChangedFiles = files.Length,
            AddedLines = 0,
            DeletedLines = 0,
            AddedFiles = files.Count(t => t.Status == ChangeKind.Added),
            DeletedFiles = files.Count(t => t.Status == ChangeKind.Deleted)
        };
    }

    private void LoadCharts()
    {
        if (!_isInitialized) return;
        ChartView.Visibility = Visibility.Collapsed;
        LoaderPanel.Visibility = Visibility.Visible;

        var from = FromDatePicker.SelectedDate;
        var to = ToDatePicker.SelectedDate;
        var author = GetSelectedAuthor();
        var checkLines = ChangedLinesCheckbox.IsChecked ?? false;
        var checkFiles = ChangedFilesCheckbox.IsChecked ?? false;

        Task.Run(() =>
        {
            var commits = _gitRepo.GetCommits(_branchName, new Repositories.Filters.CommitFilter
            {
                Author = author
            }).FilterByDate(from, to)
                .Select(t => new { Commit = t, Files = GetFileStatistics(t.Id, checkFiles, checkLines) })
                .ToArray();

            //var commitByStatus = commits.Where(t => t.Files.ChangedFiles > 20 && t.Commit.Date.Year >= 2020)
            //    .OrderByDescending(t => t.Commit.Date).Select(t => t.Commit).Take(150).ToArray();

            var results = new StatisticsResults
            {
                ByMonth = commits
                    .GroupBy(t => new { Date = new DateTime(t.Commit.Date.Year, t.Commit.Date.Month, 1) },
                        (t, v) => new DateModel { DateTime = t.Date, Value = v.Count() }).OrderBy(t => t.DateTime)
                    .ToArray(),
                ByHour = commits
                    .GroupBy(t => new { t.Commit.Date.Hour },
                        (t, v) => new HourModel { Hour = t.Hour, Value = v.Count() }).OrderBy(t => t.Hour)
                    .ToArray(),
                ByFilesChangesMonth = commits
                    .GroupBy(t => new { Date = new DateTime(t.Commit.Date.Year, t.Commit.Date.Month, 1) },
                        (t, v) => new DateModel
                        { DateTime = t.Date, Value = v.Sum(l => l.Files.ChangedFiles) }).OrderBy(t => t.DateTime)
                    .ToArray()
            };

            if (author == null)
            {
                if (checkLines)
                {
                    results.ByAddedLines = commits
                        .GroupBy(t => NormalizeName(t.Commit.Author),
                            (t, v) => new AuthorCount
                            {
                                AuthorName = t,
                                Count = v.Sum(p => p.Files.AddedLines)
                            })
                        .ToArray();

                    results.ByDeletedLines = commits
                        .GroupBy(t => NormalizeName(t.Commit.Author),
                            (t, v) => new AuthorCount
                            {
                                AuthorName = t,
                                Count = v.Sum(p => p.Files.DeletedLines)
                            })
                        .ToArray();
                    results.ByLinesAddedMonth = commits.GroupBy(
                            t => new { Date = new DateTime(t.Commit.Date.Year, t.Commit.Date.Month, 1) },
                            (t, v) => new DateModel
                            { DateTime = t.Date, Value = v.Sum(l => l.Files.AddedLines) }).OrderBy(t => t.DateTime)
                        .ToArray();
                    results.ByLinesDeletedMonth = commits.GroupBy(
                            t => new { Date = new DateTime(t.Commit.Date.Year, t.Commit.Date.Month, 1) },
                            (t, v) => new DateModel
                            { DateTime = t.Date, Value = v.Sum(l => l.Files.DeletedLines) }).OrderBy(t => t.DateTime)
                        .ToArray();
                }

                if (checkFiles)
                {
                    results.ByAddedFiles = commits
                        .GroupBy(t => NormalizeName(t.Commit.Author),
                            (t, v) => new AuthorCount
                            {
                                AuthorName = t,
                                Count = v.Sum(p => p.Files.AddedFiles)
                            })
                        .ToArray();
                    results.ByDeletedFiles = commits
                        .GroupBy(t => NormalizeName(t.Commit.Author),
                            (t, v) => new AuthorCount
                            {
                                AuthorName = t,
                                Count = v.Sum(p => p.Files.DeletedFiles)
                            })
                        .ToArray();

                    results.ByChangedFiles = commits
                        .GroupBy(t => NormalizeName(t.Commit.Author),
                            (t, v) => new AuthorCount
                            { AuthorName = t, Count = v.Sum(p => p.Files.ChangedFiles) })
                        .ToArray();
                }

                results.ByCommiter = commits
                    .GroupBy(t => NormalizeName(t.Commit.Commiter),
                        (t, v) => new AuthorCount { AuthorName = t, Count = v.Count() })
                    .ToArray();
                results.ByAuthor = commits
                    .GroupBy(t => NormalizeName(t.Commit.Author),
                        (t, v) => new AuthorCount { AuthorName = t, Count = v.Count() })
                    .ToArray();
            }
            return results;

        }).ContinueWith(v =>
            {
                AuthorPanel.Visibility = Visibility.Collapsed;
                LoaderPanel.Visibility = Visibility.Collapsed;

                ByFileAuthorPanel.Visibility = Visibility.Collapsed;
                ByLineAuthorPanel.Visibility = Visibility.Collapsed;
                ByFileTimePanel.Visibility = Visibility.Collapsed;
                ByLinesRemovedTimePanel.Visibility = Visibility.Collapsed;
                ByLinesAddedTimePanel.Visibility = Visibility.Collapsed;

                if (author == null)
                {

                    PrintAuthorLines(ByCommiter, v.Result.ByCommiter);
                    PrintAuthorLines(ByAuthor, v.Result.ByAuthor);

                    if (checkFiles)
                    {
                        PrintAuthorLines(ByFilesAuthor, v.Result.ByChangedFiles);
                        PrintAuthorLines(ByAddedFilesAuthor, v.Result.ByAddedFiles);
                        PrintAuthorLines(ByDeletedFilesAuthor, v.Result.ByDeletedFiles);
                        ByFileAuthorPanel.Visibility = Visibility.Visible;
                    }

                    if (checkLines)
                    {
                        PrintAuthorLines(ByAddedLinesAuthor, v.Result.ByAddedLines);
                        PrintAuthorLines(ByDeletedLinesAuthor, v.Result.ByDeletedLines);
                        ByLineAuthorPanel.Visibility = Visibility.Visible;
                        ByLinesAddedTimePanel.Visibility = Visibility.Visible;
                        ByLinesRemovedTimePanel.Visibility = Visibility.Visible;
                    }

                    AuthorPanel.SetCurrentValue(VisibilityProperty, Visibility.Visible);
                }

                PrintHourSeries(CommitsByHour, v.Result.ByHour);
                PrintLineSeries("Commits by month", CommitsTime, v.Result.ByMonth);
                if (checkFiles)
                {
                    PrintLineSeries("File changes by month", FileChangesTime, v.Result.ByFilesChangesMonth);
                    ByFileTimePanel.Visibility = Visibility.Visible;
                }

                if (checkLines)
                {
                    PrintLineSeries("Lines added by month", ByLinesAddedTime, v.Result.ByLinesAddedMonth);
                    PrintLineSeries("Lines removed by month", ByLinesRemovedTime, v.Result.ByLinesDeletedMonth);
                }

                ChartView.Visibility = Visibility.Visible;
            },
            TaskScheduler.FromCurrentSynchronizationContext());
    }

    private class FileStatistics
    {
        public int ChangedFiles { get; set; }
        public int AddedFiles { get; set; }
        public int AddedLines { get; set; }
        public int DeletedLines { get; set; }
        public int DeletedFiles { get; set; }
    }

    private class StatisticsResults
    {
        public AuthorCount[] ByChangedFiles { get; set; }
        public AuthorCount[] ByAddedFiles { get; set; }
        public AuthorCount[] ByCommiter { get; set; }
        public AuthorCount[] ByAddedLines { get; set; }
        public AuthorCount[] ByDeletedLines { get; set; }
        public AuthorCount[] ByAuthor { get; set; }
        public AuthorCount[] ByDeletedFiles { get; set; }

        public DateModel[] ByLinesAddedMonth { get; set; }
        public DateModel[] ByLinesDeletedMonth { get; set; }
        public DateModel[] ByFilesChangesMonth { get; set; }
        public DateModel[] ByMonth { get; set; }
        public HourModel[] ByHour { get; set; }
    }

    private static string NormalizeName(string name)
        => name.Replace(".", "").Replace(" ", "").ToLower();

    private static void PrintHourSeries(CartesianChart chart, IEnumerable<HourModel> values)
    {
        var dayConfig = Mappers.Xy<HourModel>()
            .X(dayModel => dayModel.Hour)
            .Y(dayModel => dayModel.Value);

        chart.Series.Clear();
        chart.Series = new SeriesCollection(dayConfig)
        {
            new ColumnSeries
            {
                Title = "Commits by hour",
                Values = new ChartValues<HourModel>(values.AsEnumerable()),
            }
        };

        chart.AxisX.Clear();

        chart.AxisX.Add(new Axis
        {
            Title = "Hour",
            LabelFormatter = value => value.ToString(CultureInfo.InvariantCulture)
        });
    }

    private static void PrintLineSeries(string title, CartesianChart chart, IEnumerable<DateModel> values)
    {
        var dayConfig = Mappers.Xy<DateModel>()
            .X(dayModel => (double)dayModel.DateTime.Ticks / TimeSpan.FromDays(1).Ticks)
            .Y(dayModel => dayModel.Value);

        chart.Series.Clear();
        chart.SetCurrentValue(LiveCharts.Wpf.Charts.Base.Chart.SeriesProperty, new SeriesCollection(dayConfig)
        {
            new LineSeries
            {
                Title = title,
                Values = new ChartValues<DateModel>(values)
            }
        });

        chart.AxisX.Clear();
        chart.AxisX.Add(new Axis
        {
            Title = "Date",
            LabelFormatter = value => new DateTime((long)(value * TimeSpan.FromDays(1).Ticks)).ToShortDateString()
        });
    }

    public class DateModel
    {
        public DateTime DateTime { get; set; }
        public int Value { get; set; }
    }

    public class HourModel
    {
        public int Hour { get; set; }
        public int Value { get; set; }
    }

    private static void PrintAuthorLines(IChartView chart, IEnumerable<AuthorCount> results)
    {
        chart.Series.Clear();
        foreach (var result in results.OrderByDescending(t => t.Count))
        {
            chart.Series.Add(new PieSeries { Title = $"{result.AuthorName}", Values = new ChartValues<long>(new List<long> { result.Count }.AsEnumerable()) });
        }
    }

    private class AuthorCount
    {
        public string AuthorName { get; set; }
        public int Count { get; set; }
    }

    private void FromDatePicker_OnSelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ToDatePicker.SelectedDate.HasValue || !FromDatePicker.SelectedDate.HasValue) return;
        LoadCharts();
    }

    private void ToDatePicker_OnSelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ToDatePicker.SelectedDate.HasValue || !FromDatePicker.SelectedDate.HasValue) return;
        LoadCharts();
    }

    private void CmbAuthors_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadCharts();
    }

    private void ChangedFilesCheckbox_OnClick(object sender, RoutedEventArgs e)
    {
        LoadCharts();
    }

    private void ChangedLinesCheckbox_OnClick(object sender, RoutedEventArgs e)
    {
        LoadCharts();
    }
}