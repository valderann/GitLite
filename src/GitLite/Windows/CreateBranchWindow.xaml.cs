using GitLite.Repositories;
using System.Windows;
using System.Windows.Controls;

namespace GitLite.Windows;

/// <summary>
/// Interaction logic for CreateBranchWindow.xaml
/// </summary>
public partial class CreateBranchWindow : Window
{
    private readonly GitRepository _repository;
    private readonly string _branchName;

    public CreateBranchWindow(GitRepository repository, string branchName)
    {
        InitializeComponent();

        _repository = repository;
        _branchName = branchName;

        Title = $"Create branch on {branchName}";
        BranchName.Content = branchName;
    }

    private void CreateBranchButton_OnClick(object sender, RoutedEventArgs e)
    {
        var newBranchName = NewBranchNameTextBox.Text;
        if (string.IsNullOrWhiteSpace(newBranchName)) return;

        _repository.CreateBranch(newBranchName, _branchName);

        Close();
    }

    private void NewBranchNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        CreateBranchButton.IsEnabled = !string.IsNullOrWhiteSpace(NewBranchNameTextBox.Text);
    }
}