using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Analytics.Models
{
    public class AnalyticsReport
    {
        public int ProfileId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string PeriodLabel { get; set; } = "";

        public int HabitsCompleted { get; set; }
        public int HabitsTotal { get; set; }
        public int HabitsCompletedPrev { get; set; }

        public int TasksClosed { get; set; }
        public int TasksOverdue { get; set; }
        public int TasksClosedPrev { get; set; }


        public int PomodoroSessions { get; set; }
        public int PomodoroMinutes { get; set; }
        public int PomodoroSessionsPrev { get; set; }

        public int NotesCreated { get; set; }
        public int NotesPinned { get; set; }
        public int NotesCreatedPrev { get; set; }

        public List<DayProductivityPoint> DailyChart { get; set; } = [];

        public List<ModuleRow> ModuleTable { get; set; } = [];

        public List<string> Insights { get; set; } = [];
        public List<string> Recommendations { get; set; } = [];
    }

    public class DayProductivityPoint
    {
        public DateTime Date { get; set; }
        public string DayLabel { get; set; } = "";
        public int HabitsCount { get; set; }
        public int TasksCount { get; set; }
        public int PomodoroCount { get; set; }
        public int TotalScore { get; set; }
        public double BarHeightRatio { get; set; } 
    }

    public class ModuleRow
    {
        public string Module { get; set; } = "";
        public string PeriodValue { get; set; } = "";
        public string PerDayValue { get; set; } = "";
        public string TrendLabel { get; set; } = "";
        public string TrendColor { get; set; } = "#2DD4BF";
    }

    public enum AnalyticsPeriod { Week, Month, Custom }
    public enum ReportModule { All, Habits, Tasks, Pomodoro, Notes }
    public enum ReportFormat { Pdf, Csv }
}
