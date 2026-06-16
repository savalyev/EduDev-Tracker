using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Core.Helpers
{
    public static class SessionService
    {
        private const string Key = "active_profile_id";

        public static void SaveProfileId(int id)
            => Preferences.Default.Set(Key, id);

        public static int GetProfileId()
            => Preferences.Default.Get(Key, 0);

        public static void Clear()
            => Preferences.Default.Remove(Key);
    }
}
