using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Data
{
    public static class Constants
    {
        public const string DatabaseFilename = "edudev-tracker.db3";

        public const SQLiteOpenFlags Flags =
            SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache |
            SQLiteOpenFlags.FullMutex;

        public static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

        public const int CurrentSchemaVersion = 3;
    }
}
