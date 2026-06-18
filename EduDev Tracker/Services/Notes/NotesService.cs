using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Repositories.Implementations;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Notes
{
    public class NotesService: INotesService
    {
        private readonly NoteRepository _repo;

        public NotesService(NoteRepository noteRepository)
        {
            _repo = noteRepository;
        }

        public Task<List<NoteCategory>> GetCategoriesAsync(int profileId)
            => _repo.GetCategoriesAsync(profileId);
        public Task<List<Note>> GetByProfileAsync(int profileId, bool includeArchived)
            => _repo.GetByProfileAsync(profileId, includeArchived);
        public Task<List<Note>> SearchAsync(int profileId, string query)
            => _repo.SearchAsync(profileId, query);
        public Task<List<NoteAttachment>> GetAttachmentsAsync(int noteId)
            => _repo.GetAttachmentsAsync(noteId);
        public Task<List<NoteVersion>> GetVersionsAsync(int noteId)
            => _repo.GetVersionsAsync(noteId);
        public Task<int> SaveAttachmentAsync(NoteAttachment attachment)
            => _repo.SaveAttachmentAsync(attachment);
        public Task<int> SaveVersionAsync(NoteVersion version)
            => _repo.SaveVersionAsync(version);
        public Task<int> SaveAsync(Note note)
            => _repo.SaveAsync(note);
        public Task<Note> SaveNoteDirectAsync(Note note)
            => _repo.SaveNoteDirectAsync(note);
        public Task<int> DeleteAsync(Note note)
            => _repo.DeleteAsync(note);
        public Task<int> DeleteAttachmentAsync(NoteAttachment attachment)
            => _repo.DeleteAttachmentAsync(attachment);
        public Task<int> SaveCategoryAsync(NoteCategory category)
            => _repo.SaveCategoryAsync(category);
        public Task<int> DeleteCategoryAsync(NoteCategory category)
            => _repo.DeleteCategoryAsync(category);
    }
}
