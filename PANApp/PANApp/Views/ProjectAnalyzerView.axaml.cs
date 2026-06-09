using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using PANApp.Models.Configs;
using PANApp.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace PANApp.Views;

public partial class ProjectAnalyzerView : ReactiveUserControl<ProjectAnalyzerViewModel>
{
    private bool _isRestoringZoom = false;

    public ProjectAnalyzerView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            ViewModel!.BrowseFolder.RegisterHandler(InteractBrowseFolder);

            ViewModel.WhenAnyValue(x => x.SelectedProfile)
                .Subscribe(OnSelectedProfileChanged)
                .DisposeWith(disposables);

            GraphZoomBorder.ZoomChanged += OnZoomOrPanChanged;

            PopupOverlay.PointerPressed += OnPopupOverlayClicked;

            Disposable.Create(() =>
            {
                GraphZoomBorder.ZoomChanged -= OnZoomOrPanChanged;
                PopupOverlay.PointerPressed -= OnPopupOverlayClicked;
            }).DisposeWith(disposables);
        });
    }

    private void OnPopupOverlayClicked(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel?.NoteVm != null && ViewModel.NoteVm.IsDetailsPopupOpen)
            ViewModel.NoteVm.CloseDetailsPopupCommand.Execute().Subscribe();
    }

    private void OnSelectedProfileChanged(ProjectProfileConfig? profile)
    {
        _isRestoringZoom = true;
        try
        {
            if (profile != null && profile.HasAnalyzedGraph)
                GraphZoomBorder.SetMatrix(profile.TransformMatrix);
            else GraphZoomBorder.ResetMatrix();
        }
        finally
        {
            _isRestoringZoom = false;
        }
    }

    private void OnZoomOrPanChanged(object? sender, EventArgs e)
    {
        if (_isRestoringZoom || ViewModel?.SelectedProfile == null)
            return;

        ViewModel.SelectedProfile.TransformMatrix = GraphZoomBorder.Matrix;
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

        if (result.Count > 0) interaction.SetOutput(result[0].Path.LocalPath);
        else interaction.SetOutput(string.Empty);
    }
}