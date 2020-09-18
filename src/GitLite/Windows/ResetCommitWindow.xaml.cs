using GitLite.Repositories;
using System.Windows;

namespace GitLite.Windows;

/// <summary>
/// Interaction logic for ResetCommitWindow.xaml
/// </summary>
public partial class ResetCommitWindow : Window
{
    public ResetCommitWindow(GitRepository repository)
    {
        InitializeComponent();
    }
}