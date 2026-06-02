using Avalonia.Controls;
using Avalonia.ReactiveUI;
using PANApp.ViewModels;
using ReactiveUI;
using System.Threading.Tasks;

namespace PANApp.Views;

public partial class ProjectAnalyzerView : ReactiveUserControl<ProjectAnalyzerViewModel>
{
    public ProjectAnalyzerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel!.BrowseFolder.RegisterHandler(InteractBrowseFolder);
        });
    }

    private async Task InteractBrowseFolder(IInteractionContext<string, string> interaction)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            interaction.SetOutput(string.Empty);
            return;
        }

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Выберите папку проекта",
                AllowMultiple = false
            });

        if (result.Count > 0)
        {
            interaction.SetOutput(result[0].Path.LocalPath);
        }
        else
        {
            interaction.SetOutput(string.Empty);
        }
    }
}