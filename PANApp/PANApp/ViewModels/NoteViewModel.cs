using PANApp.Models;
using PANApp.Services.Implementations;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;

namespace PANApp.ViewModels;

public class NoteViewModel : ViewModelBase
{
    private readonly NoteService _noteService;
    private readonly Action _onNotesChanged;

    private string _profileId = string.Empty;
    private string _moduleId = string.Empty;

    private string _moduleName = string.Empty;
    public string ModuleName
    {
        get => _moduleName;
        set => this.RaiseAndSetIfChanged(ref _moduleName, value);
    }

    private string _panelTitle = "📝 Module notes";
    public string PanelTitle
    {
        get => _panelTitle;
        set => this.RaiseAndSetIfChanged(ref _panelTitle, value);
    }

    private ObservableCollection<Note> _notes = new();
    public ObservableCollection<Note> Notes
    {
        get => _notes;
        set => this.RaiseAndSetIfChanged(ref _notes, value);
    }

    private Note? _selectedNote;
    public Note? SelectedNote
    {
        get => _selectedNote;
        set => this.RaiseAndSetIfChanged(ref _selectedNote, value);
    }

    private string _newTitle = string.Empty;
    public string NewTitle
    {
        get => _newTitle;
        set => this.RaiseAndSetIfChanged(ref _newTitle, value);
    }

    private string _newDescription = string.Empty;
    public string NewDescription
    {
        get => _newDescription;
        set => this.RaiseAndSetIfChanged(ref _newDescription, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => this.RaiseAndSetIfChanged(ref _isEditing, value);
    }

    private bool _isVisible;
    public bool IsVisible
    {
        get => _isVisible;
        set => this.RaiseAndSetIfChanged(ref _isVisible, value);
    }

    private bool _isAllNotesMode;
    public bool IsAllNotesMode
    {
        get => _isAllNotesMode;
        set => this.RaiseAndSetIfChanged(ref _isAllNotesMode, value);
    }

    private bool _isDetailsPopupOpen;
    public bool IsDetailsPopupOpen
    {
        get => _isDetailsPopupOpen;
        set => this.RaiseAndSetIfChanged(ref _isDetailsPopupOpen, value);
    }

    private Note? _noteForDetails;
    public Note? NoteForDetails
    {
        get => _noteForDetails;
        set => this.RaiseAndSetIfChanged(ref _noteForDetails, value);
    }

    private bool _isEditingInPopup;
    public bool IsEditingInPopup
    {
        get => _isEditingInPopup;
        set => this.RaiseAndSetIfChanged(ref _isEditingInPopup, value);
    }

    private string _popupEditTitle = string.Empty;
    public string PopupEditTitle
    {
        get => _popupEditTitle;
        set => this.RaiseAndSetIfChanged(ref _popupEditTitle, value);
    }

    private string _popupEditDescription = string.Empty;
    public string PopupEditDescription
    {
        get => _popupEditDescription;
        set => this.RaiseAndSetIfChanged(ref _popupEditDescription, value);
    }

    public string FormTitle => IsEditing ? "✏️ Edit note" : "➕ Create note";
    public string SubmitButtonText => IsEditing ? "💾 Update" : "➕ Add";

    public ReactiveCommand<Unit, Unit> AddNoteCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateNoteCommand { get; }
    public ReactiveCommand<Unit, Unit> SubmitCommand { get; }
    public ReactiveCommand<Note, Unit> StartEditNoteCommand { get; }
    public ReactiveCommand<Note, Unit> DeleteNoteCommand { get; }
    public ReactiveCommand<Note, Unit> ViewNoteDetailsCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelEditCommand { get; }
    public ReactiveCommand<Unit, Unit> ClosePanelCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseDetailsPopupCommand { get; }

    public ReactiveCommand<Unit, Unit> StartEditInPopupCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveEditInPopupCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelEditInPopupCommand { get; }

    public NoteViewModel(Action onNotesChanged)
    {
        _noteService = new NoteService();
        _onNotesChanged = onNotesChanged;

        AddNoteCommand = ReactiveCommand.Create(AddNote);
        UpdateNoteCommand = ReactiveCommand.Create(UpdateNote);
        SubmitCommand = ReactiveCommand.Create(Submit);
        StartEditNoteCommand = ReactiveCommand.Create<Note>(StartEditNote);
        DeleteNoteCommand = ReactiveCommand.Create<Note>(DeleteNote);
        ViewNoteDetailsCommand = ReactiveCommand.Create<Note>(ViewNoteDetails);
        CancelEditCommand = ReactiveCommand.Create(CancelEdit);
        ClosePanelCommand = ReactiveCommand.Create(ClosePanel);
        CloseDetailsPopupCommand = ReactiveCommand.Create(CloseDetailsPopup);

        StartEditInPopupCommand = ReactiveCommand.Create(StartEditInPopup);
        SaveEditInPopupCommand = ReactiveCommand.Create(SaveEditInPopup);
        CancelEditInPopupCommand = ReactiveCommand.Create(CancelEditInPopup);

        this.WhenAnyValue(x => x.IsEditing)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(FormTitle));
                this.RaisePropertyChanged(nameof(SubmitButtonText));
            });
    }

    public void LoadNotesForModule(string profileId, string moduleId, string moduleName)
    {
        _profileId = profileId;
        _moduleId = moduleId;
        ModuleName = moduleName;
        PanelTitle = "📝 Module notes";
        IsVisible = true;
        IsAllNotesMode = false;

        var notes = _noteService.GetNotesByModule(profileId, moduleId);
        Notes = new ObservableCollection<Note>(notes);

        CancelEdit();
    }

    public void LoadAllNotesForProfile(string profileId, string profileName)
    {
        _profileId = profileId;
        _moduleId = string.Empty;
        ModuleName = profileName;
        PanelTitle = $"📋 All notes ({profileName})";
        IsVisible = true;
        IsAllNotesMode = true;

        var notes = _noteService.GetNotesByProfile(profileId);

        foreach (var note in notes) { }

        Notes = new ObservableCollection<Note>(notes);
        CancelEdit();
    }

    public void UpdateModuleNames(Func<string, string> getModuleName)
    {
        var updatedNotes = new ObservableCollection<Note>();
        foreach (var note in Notes)
        {
            if (string.IsNullOrEmpty(note.ModuleName))
            {
                var moduleName = getModuleName(note.ModuleId);
                updatedNotes.Add(note with { ModuleName = moduleName });
            }
            else updatedNotes.Add(note);
        }
        Notes = updatedNotes;
    }

    public void ClosePanel()
    {
        IsVisible = false;
        IsAllNotesMode = false;
        CancelEdit();
    }

    private void ViewNoteDetails(Note note)
    {
        if (note == null) return;

        NoteForDetails = note;
        IsDetailsPopupOpen = true;
        IsEditingInPopup = false;
    }

    private void CloseDetailsPopup()
    {
        IsDetailsPopupOpen = false;
        NoteForDetails = null;
        IsEditingInPopup = false;
    }

    private void StartEditInPopup()
    {
        if (NoteForDetails == null) return;

        PopupEditTitle = NoteForDetails.Title;
        PopupEditDescription = NoteForDetails.Description;
        IsEditingInPopup = true;
    }

    private void SaveEditInPopup()
    {
        if (NoteForDetails == null || string.IsNullOrWhiteSpace(PopupEditTitle))
            return;

        var success = _noteService.UpdateNote(NoteForDetails.Id, PopupEditTitle, PopupEditDescription);
        if (success)
        {
            NoteForDetails = NoteForDetails with
            {
                Title = PopupEditTitle,
                Description = PopupEditDescription
            };

            var index = Notes.IndexOf(NoteForDetails);
            if (index >= 0) Notes[index] = NoteForDetails;

            IsEditingInPopup = false;
        }
    }

    private void CancelEditInPopup()
    {
        IsEditingInPopup = false;
        PopupEditTitle = string.Empty;
        PopupEditDescription = string.Empty;
    }

    private void Submit()
    {
        if (IsEditing) UpdateNote();
        else AddNote();
    }

    private void AddNote()
    {
        if (string.IsNullOrWhiteSpace(NewTitle))
            return;

        var note = _noteService.AddNoteToModule(_profileId, _moduleId, ModuleName, NewTitle, NewDescription);
        Notes.Insert(0, note);

        NewTitle = string.Empty;
        NewDescription = string.Empty;

        _onNotesChanged();
    }

    private void StartEditNote(Note note)
    {
        SelectedNote = note;
        NewTitle = note.Title;
        NewDescription = note.Description;
        IsEditing = true;
    }

    private void UpdateNote()
    {
        if (SelectedNote == null || string.IsNullOrWhiteSpace(NewTitle))
            return;

        var success = _noteService.UpdateNote(SelectedNote.Id, NewTitle, NewDescription);
        if (success)
        {
            var index = Notes.IndexOf(SelectedNote);
            if (index >= 0)
            {
                Notes[index] = SelectedNote with
                {
                    Title = NewTitle,
                    Description = NewDescription
                };
            }
            CancelEdit();
        }
    }

    private void DeleteNote(Note note)
    {
        if (note == null) return;

        var success = _noteService.DeleteNote(note.Id);
        if (success)
        {
            Notes.Remove(note);
            if (SelectedNote == note)
                CancelEdit();
            if (NoteForDetails == note)
                CloseDetailsPopup();
            _onNotesChanged();
        }
    }

    private void CancelEdit()
    {
        SelectedNote = null;
        NewTitle = string.Empty;
        NewDescription = string.Empty;
        IsEditing = false;
    }
}