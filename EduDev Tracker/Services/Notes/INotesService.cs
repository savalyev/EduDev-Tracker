using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Notes
{
    public interface INotesService
    {
        public Task<List<NoteCategory>> GetCategoriesAsync(int profileId);
        public Task<List<Note>> GetByProfileAsync(int profileId, bool inclideArchived);
        public Task<List<Note>> SearchAsync(int profileId, string query);
        public Task<List<NoteAttachment>> GetAttachmentsAsync(int noteId);
        public Task<List<NoteVersion>> GetVersionsAsync(int noteId);
        public Task<int> SaveAttachmentAsync(NoteAttachment attachment);
        public Task<int> SaveVersionAsync(NoteVersion version);
        public Task<int> SaveAsync(Note note);
        public Task<Note> SaveWithChildrenAsync(Note note);
    }
}
