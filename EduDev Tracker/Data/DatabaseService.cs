using EduDev_Tracker.Data.Models;
using EduDev_Tracker.Data.Models.Joins;
using SQLite;
using SQLiteNetExtensions.Attributes;

namespace EduDev_Tracker.Data
{
    public  class DatabaseService
    {
        private SQLiteAsyncConnection? _connection;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public SQLiteAsyncConnection Connection
            => _connection ?? throw new InvalidOperationException("Call InitAsync() first");

        public async Task InitAsync()
        {
            if (_connection is not null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_connection is not null) return;

                _connection = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);

                await _connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
                await _connection.ExecuteAsync("PRAGMA journal_mode = WAL;");
                await _connection.ExecuteAsync("PRAGMA synchronous = NORMAL;");

                await _connection.CreateTableAsync<Profile>();
                await _connection.CreateTableAsync<Tag>();
                await _connection.CreateTableAsync<Habit>();
                await _connection.CreateTableAsync<HabitSchedule>();
                await _connection.CreateTableAsync<HabitLog>();
                await _connection.CreateTableAsync<HabitTag>();
                await _connection.CreateTableAsync<Project>();
                await _connection.CreateTableAsync<TaskItem>();
                await _connection.CreateTableAsync<TaskRecurrence>();
                await _connection.CreateTableAsync<TaskTag>();
                await _connection.CreateTableAsync<NoteCategory>();
                await _connection.CreateTableAsync<Note>();
                await _connection.CreateTableAsync<NoteAttachment>();
                await _connection.CreateTableAsync<NoteTag>();
                await _connection.CreateTableAsync<PomodoroPreset>();
                await _connection.CreateTableAsync<PomodoroSession>();
                await _connection.CreateTableAsync<CheatsheetCategory>();
                await _connection.CreateTableAsync<Cheatsheet>();
                await _connection.CreateTableAsync<CheatsheetTag>();
                await _connection.CreateTableAsync<ConversionHistory>();
                await _connection.CreateTableAsync<Reminder>();

                await CreateFtsAsync();

                await MigrateAsync();

#if DEBUG
                _connection.Tracer = msg => System.Diagnostics.Debug.WriteLine("[SQL] " + msg);
                _connection.Trace = true;
#endif
            }
            finally { _initLock.Release(); }
        }

        private async Task CreateFtsAsync()
        {
            await _connection!.ExecuteAsync(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS notes_fts USING fts5(
                title, content,
                content='notes', content_rowid='Id',
                tokenize='unicode61 remove_diacritics 1');");
            await _connection.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS notes_ai AFTER INSERT ON notes BEGIN
              INSERT INTO notes_fts(rowid,title,content) VALUES (new.Id,new.Title,new.Content);
            END;");
            await _connection.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS notes_ad AFTER DELETE ON notes BEGIN
              INSERT INTO notes_fts(notes_fts,rowid,title,content) VALUES ('delete',old.Id,old.Title,old.Content);
            END;");
            await _connection.ExecuteAsync(@"
            CREATE TRIGGER IF NOT EXISTS notes_au AFTER UPDATE ON notes BEGIN
              INSERT INTO notes_fts(notes_fts,rowid,title,content) VALUES ('delete',old.Id,old.Title,old.Content);
              INSERT INTO notes_fts(rowid,title,content) VALUES (new.Id,new.Title,new.Content);
            END;");
        }

        private async Task MigrateAsync()
        {
            var v = await _connection!.ExecuteScalarAsync<int>("PRAGMA user_version");
            if (v < Constants.CurrentSchemaVersion)
            {
                await _connection.ExecuteAsync($"PRAGMA user_version = {Constants.CurrentSchemaVersion};");
            }
        }

        public Task RunInTransactionAsync(Action<SQLiteConnection> action)
    => Connection.RunInTransactionAsync(action);
    }
}
