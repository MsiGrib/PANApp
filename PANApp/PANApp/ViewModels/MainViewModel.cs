using ReactiveUI;
using System;
using System.Reactive;

namespace PANApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => this.RaiseAndSetIfChanged(ref _currentViewModel, value);
    }

    public ReactiveCommand<Unit, Unit> ShowAnalyzerCommand { get; }

    public MainViewModel()
    {
        ShowAnalyzerCommand = ReactiveCommand.Create(() =>
        {
            CurrentViewModel = new ProjectAnalyzerViewModel();
        });

        ShowAnalyzerCommand.Execute().Subscribe();
    }
}