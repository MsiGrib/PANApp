using ReactiveUI;
using System.Reactive;

namespace PANApp.ViewModels;

public class MainViewModel : ViewModelBase
{
    private string _currentPage = "Analyzer";
    public string CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    private ProjectAnalyzerViewModel _analyzerVm;
    public ProjectAnalyzerViewModel AnalyzerVm
    {
        get => _analyzerVm;
        set => this.RaiseAndSetIfChanged(ref _analyzerVm, value);
    }

    public ReactiveCommand<Unit, Unit> NavigateToAnalyzerCommand { get; }

    public MainViewModel()
    {
        AnalyzerVm = new ProjectAnalyzerViewModel();

        NavigateToAnalyzerCommand = ReactiveCommand.Create(() =>
        {
            CurrentPage = "Analyzer";
        });
    }
}