using GitLite.Repositories;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Windows;

namespace GitLite.Windows;

/// <summary>
/// Interaction logic for EditorWindow.xaml
/// </summary>
public partial class EditorWindow : Window
{
    public EditorWindow(GitRepository repository, string fileName, string commitId)
    {
        InitializeComponent();

        Title = $"Edit - {fileName}";

        var commit = repository.GetCommit(commitId);
        var stream = repository.GetBlobStream(commit, fileName);
        if (stream == null) return;

        avEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(System.IO.Path.GetExtension(fileName));
        avEditor.Load(stream);
    }
}