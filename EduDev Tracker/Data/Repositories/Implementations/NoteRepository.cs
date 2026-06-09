using EduDev_Tracker.Data.Models;
using SQLiteNetExtensionsAsync.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Data.Repositories.Implementations
{
    public partial class NoteRepository: BaseRepository<Note>
    {
        public NoteRepository(DatabaseService db): base (db) { }

        public Task<List<NoteCategory>> GetCategoriesAsync(int profileId)
        {
            return Connection.Table<NoteCategory>()
                .Where(h => h.ProfileId == profileId)
                .ToListAsync();
        }

        public Task<List<Note>> GetByProfileAsync(int profileId, bool inclideArchived = false)
        {
            var q = Connection.Table<Note>()
                .Where(n => n.ProfileId == profileId);

            if (!inclideArchived)
            {
                q = q.Where(n => !n.IsArchived);
            }
            return q
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public Task<List<Note>> SearchAsync(int profileId, string query)
        {
            var fts = query.Replace("\"", "\"\"");
            return Connection.QueryAsync<Note>(
                @"SELECT n.* FROM notes n
                  JOIN notesfts f ON f.rowid = n.Id
                  WHERE n.ProfileId = ? AND notesfts MATCH ?
                    AND n.IsArchived = 0
                  ORDER BY rank",
                profileId, $"\"{fts}\"*");
        }

        public Task<List<NoteAttachment>> GetAttachmentsAsync(int noteId) =>
               Connection.Table<NoteAttachment>()
              .Where(a => a.NoteId == noteId)
              .ToListAsync();

        public Task<List<NoteVersion>> GetVersionsAsync(int noteId) =>
               Connection.Table<NoteVersion>()
              .Where(v => v.NoteId == noteId)
              .OrderByDescending(v => v.SavedAt)
              .ToListAsync();

        public Task<int> SaveAttachmentAsync(NoteAttachment attachment) =>
            attachment.Id == 0
            ? Connection.InsertAsync(attachment)
            : Connection.UpdateAsync(attachment);

        public Task<int> SaveVersionAsync(NoteVersion version) =>
            Connection.InsertAsync(version);

        public new Task<int> SaveAsync(Note note) =>
            note.Id == 0
            ? Connection.InsertAsync(note)
            : Connection.UpdateAsync(note);

        public async Task<Note> SaveWithChildrenAsync(Note note)
        {
            note.UpdatedAt = DateTime.UtcNow;
            if (note.Id == 0)
            {
                note.CreatedAt = DateTime.UtcNow;
                await Connection.InsertWithChildrenAsync(note, recursive: true);
            }
            else
            {
                await Connection.UpdateWithChildrenAsync(note);
            }
            return note;
        }
    }
}
