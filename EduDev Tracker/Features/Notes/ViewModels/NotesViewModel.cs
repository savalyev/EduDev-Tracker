using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduDev_Tracker.Core.Base;
using EduDev_Tracker.Core.Helpers;
using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using EduDev_Tracker.Features.Notes.Views;
using EduDev_Tracker.Services.Notes;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace EduDev_Tracker.Features.Notes.ViewModels
{
    public partial class NotesViewModel: BaseViewModel
    {
        private readonly INotesService _noteService;
        private readonly int _profileId;

        [ObservableProperty] private ObservableCollection<Note> _filteredNotes = new();
        [ObservableProperty] private ObservableCollection<NoteCategory> _categories = new();
        [ObservableProperty] private ObservableCollection<NoteAttachment> _selectedNoteAttachments = new();
        [ObservableProperty] private ObservableCollection<NoteVersion> _noteVersions = new();

        [ObservableProperty] private Note? _selectedNote;
        [ObservableProperty] private NoteCategory? _selectedNoteCategory;

        [ObservableProperty] private string _editTitle = string.Empty;
        [ObservableProperty] private string _editContent = string.Empty;

        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private NoteCategory? _selectedCategory;

        [ObservableProperty] private bool _isDesktopLayout = true;
        [ObservableProperty] private bool _isMobileLayout;
        [ObservableProperty] private bool _mobileShowEditor;
        [ObservableProperty] private string _autosaveStatus = "Автосохранение: включено";
        [ObservableProperty] private string _mobileHeaderTitle = "Заметки";

        public bool HasSelectedNote => SelectedNote is not null;
        public bool HasNoAttachments => !SelectedNoteAttachments.Any();
        public bool HasNoVersions => !NoteVersions.Any();
        public string NotesCountText => $"{FilteredNotes.Count} записей";
        public string WordCountText => $"Слов: {EditContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length}";
        public string TagsDisplay => SelectedNote?.Tags?.Any() == true
            ? string.Join(", ", SelectedNote.Tags.Select(t => $"#{t.Name}"))
            : "Нет тегов";
        public string ReminderDisplay => "Нет";

        public NotesViewModel(INotesService noteService)
        {
            _noteService = noteService;
            _profileId = SessionService.GetProfileId();
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            IsBusy = true;
            try
            {

                var cats = await _noteService.GetCategoriesAsync(_profileId);
                Categories = new ObservableCollection<NoteCategory>(cats);

                await RefreshNotesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotesViewModel.Load] {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshNotesAsync()
        {
            List<Note> notes;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
            
                notes = await _noteService.SearchAsync(_profileId, SearchQuery);

            }
            else
            {
                notes = await _noteService.GetByProfileAsync(_profileId, false);
            }

            if (SelectedCategory is not null)
                notes = notes.Where(n => n.CategoryId == SelectedCategory.Id).ToList();

            FilteredNotes = new ObservableCollection<Note>(notes);
            OnPropertyChanged(nameof(NotesCountText));
        }

        partial void OnSelectedNoteChanged(Note? value)
        {
            if (value is null) return;

            EditTitle = value.Title;
            EditContent = value.Content;

            OnPropertyChanged(nameof(HasSelectedNote));
            OnPropertyChanged(nameof(TagsDisplay));

            if (IsMobileLayout)
            {
                MobileShowEditor = true;
                MobileHeaderTitle = value.Title;
            }

            _ = LoadAttachmentsAsync(value.Id);
            _ = LoadVersionsAsync(value.Id);
        }

        private async Task LoadAttachmentsAsync(int noteId)
        {
            try
            {
                var attachments = await _noteService.GetAttachmentsAsync(noteId);
                SelectedNoteAttachments = new ObservableCollection<NoteAttachment>(attachments);
                OnPropertyChanged(nameof(HasNoAttachments));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadAttachments] {ex}");
            }
        }

        private async Task LoadVersionsAsync(int noteId)
        {
            try
            {
                var versions = await _noteService.GetVersionsAsync(noteId);

                var versionsWithLabel = versions
                    .Select((v, i) => { v.VersionLabel = $"v{versions.Count - i}.{0}"; return v; })
                    .ToList();

                NoteVersions = new ObservableCollection<NoteVersion>(versionsWithLabel);
                OnPropertyChanged(nameof(HasNoVersions));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadVersions] {ex}");
            }
        }

        [RelayCommand]
        private async Task CreateNoteAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var note = new Note
                {
                    ProfileId = _profileId,
                    Title = "Новая заметка",
                    Content = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _noteService.SaveNoteDirectAsync(note);
                await RefreshNotesAsync();
                SelectedNote = FilteredNotes.FirstOrDefault(n => n.Id == note.Id);

                if (IsMobileLayout) MobileShowEditor = true;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task SaveNoteAsync()
        {
            if (SelectedNote is null) return;
            IsBusy = true;
            try
            {
                await SaveVersionSnapshotAsync();

                SelectedNote.Title = EditTitle;
                SelectedNote.Content = EditContent;
                SelectedNote.UpdatedAt = DateTime.UtcNow;


                if (SelectedNoteCategory is not null)
                    SelectedNote.CategoryId = SelectedNoteCategory.Id;

                await _noteService.SaveNoteDirectAsync(SelectedNote);
                AutosaveStatus = $"Сохранено в {DateTime.Now:HH:mm}";

                var savedId = SelectedNote.Id;
                await RefreshNotesAsync();
                SelectedNote = FilteredNotes.FirstOrDefault(n => n.Id == savedId) ?? SelectedNote;
                await LoadVersionsAsync(SelectedNote.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveNote] {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveVersionSnapshotAsync()
        {
            if (SelectedNote is null)
            {
                Debug.WriteLine("[VERSION] SKIP: SelectedNote is null");
                return;
            }

            if (SelectedNote.Content == EditContent)
            {
                Debug.WriteLine($"[VERSION] SKIP: контент не изменился. Content='{SelectedNote.Content[..Math.Min(20, SelectedNote.Content.Length)]}'");
                return;
            }

            Debug.WriteLine($"[VERSION] Сохраняю версию. NoteId={SelectedNote.Id}");

            var version = new NoteVersion
            {
                NoteId = SelectedNote.Id,
                Content = SelectedNote.Content,
                SavedAt = DateTime.UtcNow
            };

            int result = await _noteService.SaveVersionAsync(version);
            Debug.WriteLine($"[VERSION] InsertAsync вернул={result}, version.Id={version.Id}");
        }

        [RelayCommand]
        private async Task DeleteNoteAsync()
        {
            if (SelectedNote is null) return;

            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Удалить заметку",
                $"Удалить «{SelectedNote.Title}»?",
                "Удалить", "Отмена");

            if (!confirm) return;

            foreach (var att in SelectedNoteAttachments)
            {
                try { if (File.Exists(att.FilePath)) File.Delete(att.FilePath); }
                catch { }
            }

            await _noteService.DeleteAsync(SelectedNote);

            SelectedNote = null;
            EditTitle = string.Empty;
            EditContent = string.Empty;

            SelectedNoteAttachments.Clear();

            NoteVersions.Clear();

            await RefreshNotesAsync();

            OnPropertyChanged(nameof(HasSelectedNote));
        }

        [RelayCommand]
        private async Task DeleteAttachmentAsync(NoteAttachment attachment)
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Удалить вложение",
                $"Удалить файл «{attachment.FileName}»?",
                "Удалить", "Отмена");

            if (!confirm) return;

            try { if (File.Exists(attachment.FilePath)) File.Delete(attachment.FilePath); }
            catch { }

            await _noteService.DeleteAttachmentAsync(attachment);
            SelectedNoteAttachments.Remove(attachment);
            OnPropertyChanged(nameof(HasNoAttachments));
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            await RefreshNotesAsync();
        }

        partial void OnSearchQueryChanged(string value)
        {
            _ = DebounceSearchAsync();
        }

        private CancellationTokenSource? _searchCts;
        private async Task DebounceSearchAsync()
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(300, _searchCts.Token);
                await RefreshNotesAsync();
            }
            catch (TaskCanceledException) { }
        }

        [RelayCommand]
        private async Task SelectCategoryAsync(NoteCategory? category)
        {
            SelectedCategory = category;
            await RefreshNotesAsync();
        }

        [RelayCommand]
        private async Task ShowPinnedAsync()
        {
            var notes = await _noteService.GetByProfileAsync(_profileId, false);
            FilteredNotes = new ObservableCollection<Note>(notes.Where(n => n.IsPinned));
            OnPropertyChanged(nameof(NotesCountText));
        }

        [RelayCommand]
        private async Task TogglePinAsync()
        {
            if (SelectedNote is null) return;

            SelectedNote.IsPinned = !SelectedNote.IsPinned;
            SelectedNote.UpdatedAt = DateTime.UtcNow;

            await _noteService.SaveNoteDirectAsync(SelectedNote);

            await RefreshNotesAsync();
        }

        [RelayCommand]
        private async Task AddAttachmentAsync()
        {
            if (SelectedNote is null) return;
            try
            {
                var result = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите файл для вложения"
                });
                if (result is null) return;

                var destDir = Path.Combine(FileSystem.AppDataDirectory, "attachments", SelectedNote.Id.ToString());
                Directory.CreateDirectory(destDir);
                var destPath = Path.Combine(destDir, result.FileName);
                using var src = await result.OpenReadAsync();
                using var dest = File.Create(destPath);
                await src.CopyToAsync(dest);

                var fileInfo = new FileInfo(destPath);
                var attachment = new NoteAttachment
                {
                    NoteId = SelectedNote.Id,
                    FilePath = destPath,
                    FileName = result.FileName,
                    MimeType = result.ContentType,
                    SizeBytes = fileInfo.Length,
                    CreatedAt = DateTime.UtcNow
                };

                await _noteService.SaveAttachmentAsync(attachment);
                await LoadAttachmentsAsync(SelectedNote.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddAttachment] {ex}");
            }
        }

        [RelayCommand]
        private async Task OpenAttachmentAsync(NoteAttachment attachment)
        {
            try
            {
                await Launcher.Default.OpenAsync(
                    new OpenFileRequest(attachment.FileName,
                                        new ReadOnlyFile(attachment.FilePath)));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpenAttachment] {ex}");
                await Shell.Current.DisplayAlertAsync("Ошибка", "Не удалось открыть файл", "OK");
            }
        }

        [RelayCommand]
        private async Task ShowVersionsAsync()
        {
            if (SelectedNote is null) return;
            await LoadVersionsAsync(SelectedNote.Id);


            var popup = new VersionsPopup(NoteVersions.ToList());

            Shell.Current.CurrentPage.ShowPopup(popup);

            var selectedVersion = await popup.Result;

            if (selectedVersion is not null)
            {
                await RestoreVersionAsync(selectedVersion);
            }
        }

        [RelayCommand]
        private async Task RestoreVersionAsync(NoteVersion version)
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Восстановить версию",
                $"Восстановить версию от {version.SavedAt:dd.MM HH:mm}? Текущий контент будет перезаписан.",
                "Восстановить", "Отмена");

            if (!confirm) return;

            await SaveVersionSnapshotAsync();

            EditContent = version.Content;
            await SaveNoteAsync();
        }

        [ObservableProperty] private int _cursorPosition;
        [ObservableProperty] private int _selectionLength;

        [RelayCommand]
        private void InsertMd(string type)
        {
            var content = EditContent ?? string.Empty;
            int pos = Math.Clamp(CursorPosition, 0, content.Length);
            int selLen = Math.Clamp(SelectionLength, 0, content.Length - pos);

            string selected = selLen > 0 ? content.Substring(pos, selLen) : string.Empty;

            string result;
            int newCursorPos;

            switch (type)
            {
                case "bold":
                    result = selLen > 0
                        ? content.Remove(pos, selLen).Insert(pos, $"**{selected}**")
                        : content.Insert(pos, "**жирный текст**");
                    newCursorPos = selLen > 0 ? pos + selLen + 4 : pos + 16;
                    break;

                case "italic":
                    result = selLen > 0
                        ? content.Remove(pos, selLen).Insert(pos, $"*{selected}*")
                        : content.Insert(pos, "*курсив*");
                    newCursorPos = selLen > 0 ? pos + selLen + 2 : pos + 8;
                    break;

                case "code" when selLen > 0 && !selected.Contains('\n'):
                    result = content.Remove(pos, selLen).Insert(pos, $"`{selected}`");
                    newCursorPos = pos + selLen + 2;
                    break;

                case "h1":
                    result = content.Insert(pos, $"\n# {selected}");
                    newCursorPos = pos + 3 + selected.Length;
                    break;

                case "h2":
                    result = content.Insert(pos, $"\n## {selected}");
                    newCursorPos = pos + 4 + selected.Length;
                    break;

                case "h3":
                    result = content.Insert(pos, $"\n### {selected}");
                    newCursorPos = pos + 5 + selected.Length;
                    break;

                case "ulist":
                    result = content.Insert(pos, $"\n- {selected}");
                    newCursorPos = pos + 3 + selected.Length;
                    break;

                case "olist":
                    result = content.Insert(pos, $"\n1. {selected}");
                    newCursorPos = pos + 4 + selected.Length;
                    break;

                case "todo":
                    result = content.Insert(pos, $"\n- [ ] {selected}");
                    newCursorPos = pos + 7 + selected.Length;
                    break;

                case "link":
                    result = selLen > 0
                        ? content.Remove(pos, selLen).Insert(pos, $"[{selected}](https://)")
                        : content.Insert(pos, "[текст ссылки](https://)");
                    newCursorPos = selLen > 0 ? pos + selLen + 12 : pos + 24;
                    break;

                case "code":
                    result = content.Insert(pos, $"\n```\n{selected}\n```\n");
                    newCursorPos = pos + 5 + selected.Length;
                    break;

                case "divider":
                    result = content.Insert(pos, "\n---\n");
                    newCursorPos = pos + 5;
                    break;

                case "tag":
                    result = content.Insert(pos, "#тег");
                    newCursorPos = pos + 4;
                    break;

                default:
                    return;
            }

            EditContent = result;
            CursorPosition = newCursorPos;
            SelectionLength = 0;

            OnPropertyChanged(nameof(WordCountText));
        }

        [RelayCommand]
        private async Task ExportAsync(string format)
        {
            if (SelectedNote is null) return;
            try
            {
                string ext = format switch { "docx" => ".docx", "md" => ".md", _ => ".txt" };
                string fileName = $"{SanitizeFileName(SelectedNote.Title)}{ext}";
                string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

                if (format != "docx")
                {
                    await File.WriteAllTextAsync(filePath, EditContent);
                }
                else
                {
                    // TODO: использовать DocumentFormat.OpenXml или MiniWord
                    await File.WriteAllTextAsync(filePath, EditContent);
                }

                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Экспорт: {SelectedNote.Title}",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Export] {ex}");
            }
        }

        private static string SanitizeFileName(string name) =>
            string.Concat(name.Split(Path.GetInvalidFileNameChars()));

        public void OnSizeChanged(double width)
        {
            IsDesktopLayout = width >= 900;
            IsMobileLayout = !IsDesktopLayout;

            if (IsDesktopLayout)
                MobileShowEditor = false;
        }

        [RelayCommand]
        private void MobileBack()
        {
            MobileShowEditor = false;
            MobileHeaderTitle = "Заметки";
            SelectedNote = null;
        }

        [ObservableProperty] private bool _isPreviewMode;
        [ObservableProperty] private string _markdownHtml = string.Empty;

        private static string? _cachedMarkedJs;

        [RelayCommand]
        private async Task TogglePreview()
        {
            IsPreviewMode = !IsPreviewMode;
            if (IsPreviewMode)
            {
                var markedJs = await LoadMarkedJsAsync();
                MarkdownHtml = BuildHtml(EditContent, markedJs);
            }
        }

        private static async Task<string> LoadMarkedJsAsync()
        {
            if (_cachedMarkedJs is not null) return _cachedMarkedJs;
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("marked.min.js");
                using var reader = new StreamReader(stream);
                _cachedMarkedJs = await reader.ReadToEndAsync();
                return _cachedMarkedJs;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoadMarkedJs] {ex}");
                return string.Empty;
            }
        }

        private static string BuildHtml(string markdown, string markedJs)
        {
            var escaped = markdown
                .Replace("\\", "\\\\")
                .Replace("`", "\\`")
                .Replace("$", "\\$");

            return @"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'/>
  <meta name='viewport' content='width=device-width, initial-scale=1'/>
  <script>MARKED_JS</script>
  <style>
    body { font-family: -apple-system, sans-serif; font-size: 15px;
           line-height: 1.7; color: #E8EAF0; background: #0F1117;
           padding: 16px; margin: 0; }
    h1,h2,h3 { color: #fff; margin-top: 20px; }
    h1 { font-size: 22px; border-bottom: 1px solid #252D40; padding-bottom: 8px; }
    h2 { font-size: 18px; }
    code { background: #1C2333; color: #4F8EF7; padding: 2px 6px;
           border-radius: 4px; font-family: Consolas, monospace; font-size: 13px; }
    pre { background: #1C2333; border: 1px solid #252D40;
          border-radius: 8px; padding: 12px; overflow-x: auto; }
    pre code { background: none; padding: 0; }
    blockquote { border-left: 3px solid #4F8EF7; margin: 0;
                 padding-left: 12px; color: #8892A4; }
    a { color: #4F8EF7; }
    hr { border: none; border-top: 1px solid #252D40; }
    ul,ol { padding-left: 20px; }
    li { margin-bottom: 4px; }
    p { margin: 8px 0; }
  </style>
</head>
<body>
  <div id='content'></div>
  <script>
    const md = `MARKDOWN_CONTENT`;
    document.getElementById('content').innerHTML = marked.parse(md);
  </script>
</body>
</html>"
                .Replace("MARKED_JS", markedJs)
                .Replace("MARKDOWN_CONTENT", escaped);
        }

        [ObservableProperty] private string _newCategoryName = string.Empty;

        [ObservableProperty] private bool _isAddingCategory;

        [RelayCommand]
        private void ShowAddCategory()
        {
            IsAddingCategory = true;
            NewCategoryName = string.Empty;
        }

        [RelayCommand]
        private void CancelAddCategory()
        {
            IsAddingCategory = false;
            NewCategoryName = string.Empty;
        }

        [RelayCommand]
        private async Task CreateCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName)) return;

            var category = new NoteCategory
            {
                Name = NewCategoryName.Trim(),
                ProfileId = _profileId
            };

            await _noteService.SaveCategoryAsync(category);

            var cats = await _noteService.GetCategoriesAsync(_profileId);
            Categories = new ObservableCollection<NoteCategory>(cats);

            NewCategoryName = string.Empty;
            IsAddingCategory = false;
        }

        [RelayCommand]
        private async Task DeleteCategoryAsync(NoteCategory category)
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Удалить категорию",
                $"Удалить «{category.Name}»? Заметки останутся, но без категории.",
                "Удалить", "Отмена");

            if (!confirm) return;

            await _noteService.DeleteCategoryAsync(category);

            if (SelectedCategory?.Id == category.Id)
                SelectedCategory = null;

            var cats = await _noteService.GetCategoriesAsync(_profileId);
            Categories = new ObservableCollection<NoteCategory>(cats);
            await RefreshNotesAsync();
        }
    }
}
