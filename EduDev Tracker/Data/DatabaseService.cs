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
                System.Diagnostics.Debug.WriteLine(Constants.DatabasePath);
                _connection = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
                System.Diagnostics.Debug.WriteLine("DB: opened");

                //await _connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
                //System.Diagnostics.Debug.WriteLine("DB: foreign_keys");

                //await _connection.ExecuteAsync("PRAGMA journal_mode = WAL;");
                //System.Diagnostics.Debug.WriteLine("DB: WAL");

                //await _connection.ExecuteAsync("PRAGMA synchronous = NORMAL;");
                //System.Diagnostics.Debug.WriteLine("DB: sync");

                await _connection.CreateTableAsync<Profile>();
                System.Diagnostics.Debug.WriteLine("DB: Profile");

                await _connection.CreateTableAsync<Tag>();
                System.Diagnostics.Debug.WriteLine("DB: Tag");

                await _connection.CreateTableAsync<Habit>();
                System.Diagnostics.Debug.WriteLine("DB: Habit");

                await _connection.CreateTableAsync<HabitSchedule>();
                System.Diagnostics.Debug.WriteLine("DB: HabitSchedule");

                await _connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS habit_logs (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    HabitId     INTEGER NOT NULL REFERENCES habits(Id) ON DELETE CASCADE,
                    LogDate     TEXT    NOT NULL,
                    Value       REAL    NOT NULL DEFAULT 1,
                    Note        TEXT,
                    CompletedAt TEXT    NOT NULL,
                    UNIQUE(HabitId, LogDate)
                )");
                System.Diagnostics.Debug.WriteLine("DB: HabitLog");

                await _connection.CreateTableAsync<HabitTag>();
                System.Diagnostics.Debug.WriteLine("DB: HabitTag");

                await _connection.CreateTableAsync<Project>();
                System.Diagnostics.Debug.WriteLine("DB: Project");

                await _connection.CreateTableAsync<TaskItem>();
                System.Diagnostics.Debug.WriteLine("DB: TaskItem");

                await _connection.CreateTableAsync<TaskRecurrence>();
                System.Diagnostics.Debug.WriteLine("DB: TaskRecurrence");

                await _connection.CreateTableAsync<TaskTag>();
                System.Diagnostics.Debug.WriteLine("DB: TaskTag");

                await _connection.CreateTableAsync<NoteCategory>();
                System.Diagnostics.Debug.WriteLine("DB: NoteCategory");

                await _connection.CreateTableAsync<Note>();
                System.Diagnostics.Debug.WriteLine("DB: Note");

                await _connection.CreateTableAsync<NoteAttachment>();
                System.Diagnostics.Debug.WriteLine("DB: NoteAttachment");

                await _connection.CreateTableAsync<NoteTag>();
                System.Diagnostics.Debug.WriteLine("DB: NoteTag");

                await _connection.CreateTableAsync<NoteVersion>();
                System.Diagnostics.Debug.WriteLine("DB: NoteVersion");

                await _connection.CreateTableAsync<PomodoroPreset>();
                await _connection.CreateTableAsync<PomodoroSession>();
                await _connection.CreateTableAsync<CheatsheetCategory>();
                await _connection.CreateTableAsync<Cheatsheet>();
                await _connection.CreateTableAsync<CheatsheetTag>();
                await _connection.CreateTableAsync<ConversionHistory>();
                await _connection.ExecuteAsync(
                    @"CREATE INDEX IF NOT EXISTS idx_conv_profile_time
                    ON conversionhistory(ProfileId, CreatedAt DESC)");

                await _connection.CreateTableAsync<Reminder>();

                System.Diagnostics.Debug.WriteLine("DB: tables done");

                await CreateFtsAsync();
                System.Diagnostics.Debug.WriteLine("DB: fts done");

                await MigrateAsync();
                System.Diagnostics.Debug.WriteLine("DB: migrate done");

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
