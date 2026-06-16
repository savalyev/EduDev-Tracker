using EduDev_Tracker.Data;
using EduDev_Tracker.Features.Analytics.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace EduDev_Tracker.Services.Analytics
{
    public class AnalyticsService: IAnalyticsService
    {
        private readonly DatabaseService _db;

        public AnalyticsService(DatabaseService db)
        {
            _db = db;
        }

        public async Task<AnalyticsReport> BuildReportAsync(
    int profileId, DateTime from, DateTime to, ReportModule module = ReportModule.All)
        {
            var conn = _db.Connection;
            var days = Math.Max(1, (to - from).Days + 1);

            var prevFrom = from.AddDays(-days);
            var prevTo = from.AddDays(-1);

            var fromStr = from.ToString("yyyy-MM-dd");
            var toStr = to.ToString("yyyy-MM-dd");
            var prevFromStr = prevFrom.ToString("yyyy-MM-dd");
            var prevToStr = prevTo.ToString("yyyy-MM-dd");

            var fromDt = from.Date.ToUniversalTime();
            var toDt = to.Date.AddDays(1).ToUniversalTime();
            var prevFromDt = prevFrom.Date.ToUniversalTime();
            var prevToDt = prevTo.Date.AddDays(1).ToUniversalTime();

            //Привычки
            int habitsCompleted = 0, habitsTotal = 0, habitsCompletedPrev = 0;
            if (module is ReportModule.All or ReportModule.Habits)
            {
                habitsCompleted = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM habit_logs WHERE LogDate BETWEEN ? AND ? " +
                    "AND HabitId IN (SELECT Id FROM habits WHERE ProfileId=? AND IsArchived=0)",
                    fromStr, toStr, profileId);

                habitsTotal = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM habits WHERE ProfileId=? AND IsArchived=0",
                    profileId);

                habitsCompletedPrev = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM habit_logs WHERE LogDate BETWEEN ? AND ? " +
                    "AND HabitId IN (SELECT Id FROM habits WHERE ProfileId=? AND IsArchived=0)",
                    prevFromStr, prevToStr, profileId);
            }

            // Задачи
            int tasksClosed = 0, tasksOverdue = 0, tasksClosedPrev = 0;
            if (module is ReportModule.All or ReportModule.Tasks)
            {
                tasksClosed = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM tasks WHERE ProfileId=? AND Status='Done' " +
                    "AND CompletedAt BETWEEN ? AND ?",
                    profileId, fromDt, toDt);

                tasksOverdue = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM tasks WHERE ProfileId=? AND Status!='Done' " +
                    "AND IsArchived=0 AND DueAt < ?",
                    profileId, DateTime.Now);

                tasksClosedPrev = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM tasks WHERE ProfileId=? AND Status='Done' " +
                    "AND CompletedAt BETWEEN ? AND ?",
                    profileId, prevFromDt, prevToDt);
            }

            // Помодоро
            int pomodoroSessions = 0, pomodoroMinutes = 0, pomodoroSessionsPrev = 0;
            if (module is ReportModule.All or ReportModule.Pomodoro)
            {
                pomodoroSessions = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM pomodoro_sessions WHERE ProfileId=? AND Phase='Work' " +
                    "AND WasInterrupted=0 AND StartedAt BETWEEN ? AND ?",
                    profileId, fromDt, toDt);

                pomodoroMinutes = await conn.ExecuteScalarAsync<int>(
                    "SELECT IFNULL(SUM(ActualMinutes),0) FROM pomodoro_sessions " +
                    "WHERE ProfileId=? AND Phase='Work' AND WasInterrupted=0 " +
                    "AND StartedAt BETWEEN ? AND ?",
                    profileId, fromDt, toDt);

                pomodoroSessionsPrev = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM pomodoro_sessions WHERE ProfileId=? AND Phase='Work' " +
                    "AND WasInterrupted=0 AND StartedAt BETWEEN ? AND ?",
                    profileId, prevFromDt, prevToDt);
            }

            // Заметки
            int notesCreated = 0, notesPinned = 0, notesCreatedPrev = 0;
            if (module is ReportModule.All or ReportModule.Notes)
            {
                notesCreated = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM notes WHERE ProfileId=? AND IsArchived=0 " +
                    "AND CreatedAt BETWEEN ? AND ?",
                    profileId, fromDt, toDt);

                notesPinned = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM notes WHERE ProfileId=? AND IsPinned=1 AND IsArchived=0",
                    profileId);

                notesCreatedPrev = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM notes WHERE ProfileId=? AND IsArchived=0 " +
                    "AND CreatedAt BETWEEN ? AND ?",
                    profileId, prevFromDt, prevToDt);
            }

            var dailyChart = await BuildDailyChartAsync(profileId, from, to, module);
            var moduleTable = BuildModuleTable(
                habitsCompleted, habitsCompletedPrev, days,
                tasksClosed, tasksClosedPrev,
                pomodoroSessions, pomodoroSessionsPrev,
                notesCreated, notesCreatedPrev,
                module);
            var (insights, recs) = BuildInsights(
                habitsCompleted, habitsCompletedPrev, habitsTotal,
                tasksClosed, tasksOverdue,
                pomodoroSessions, pomodoroMinutes,
                dailyChart);

            return new AnalyticsReport
            {
                ProfileId = profileId,
                From = from,
                To = to,
                PeriodLabel = $"{from:dd.MM} – {to:dd.MM.yyyy}",
                HabitsCompleted = habitsCompleted,
                HabitsTotal = habitsTotal,
                HabitsCompletedPrev = habitsCompletedPrev,
                TasksClosed = tasksClosed,
                TasksOverdue = tasksOverdue,
                TasksClosedPrev = tasksClosedPrev,
                PomodoroSessions = pomodoroSessions,
                PomodoroMinutes = pomodoroMinutes,
                PomodoroSessionsPrev = pomodoroSessionsPrev,
                NotesCreated = notesCreated,
                NotesPinned = notesPinned,
                NotesCreatedPrev = notesCreatedPrev,
                DailyChart = dailyChart,
                ModuleTable = moduleTable,
                Insights = insights,
                Recommendations = recs
            };
        }

        private async Task<List<DayProductivityPoint>> BuildDailyChartAsync(
             int profileId, DateTime from, DateTime to, ReportModule module)
        {
            var conn = _db.Connection;
            var fromDt = from.Date.ToUniversalTime();
            var toDt = to.Date.AddDays(1).ToUniversalTime();

            var hDict = new Dictionary<string, int>();
            var tDict = new Dictionary<string, int>();
            var pDict = new Dictionary<string, int>();

            if (module is ReportModule.All or ReportModule.Habits)
            {
                var rows = await conn.QueryAsync<DayStat>(
                    "SELECT LogDate AS Day, COUNT(*) AS Cnt FROM habit_logs " +
                    "WHERE HabitId IN (SELECT Id FROM habits WHERE ProfileId=? AND IsArchived=0) " +
                    "AND LogDate BETWEEN ? AND ? GROUP BY LogDate",
                    profileId, from.ToString("yyyy-MM-dd"), to.ToString("yyyy-MM-dd"));
                hDict = rows.ToDictionary(x => x.Day, x => x.Cnt);
            }

            if (module is ReportModule.All or ReportModule.Tasks)
            {
                var rows = await conn.QueryAsync<DayStat>(
                    "SELECT date(CompletedAt,'localtime') AS Day, COUNT(*) AS Cnt FROM tasks " +
                    "WHERE ProfileId=? AND Status='Done' " +
                    "AND CompletedAt BETWEEN ? AND ? GROUP BY Day",
                    profileId, fromDt, toDt);
                tDict = rows.ToDictionary(x => x.Day, x => x.Cnt);
            }

            if (module is ReportModule.All or ReportModule.Pomodoro)
            {
                var rows = await conn.QueryAsync<DayStat>(
                    "SELECT date(StartedAt,'localtime') AS Day, COUNT(*) AS Cnt FROM pomodoro_sessions " +
                    "WHERE ProfileId=? AND Phase='Work' AND WasInterrupted=0 " +
                    "AND StartedAt BETWEEN ? AND ? GROUP BY Day",
                    profileId, fromDt, toDt);
                pDict = rows.ToDictionary(x => x.Day, x => x.Cnt);
            }

            var dayNames = new[] { "Вс", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
            var points = new List<DayProductivityPoint>();
            int maxScore = 1;

            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                var key = d.ToString("yyyy-MM-dd");
                var h = hDict.GetValueOrDefault(key);
                var t = tDict.GetValueOrDefault(key);
                var p = pDict.GetValueOrDefault(key);
                var score = h + t * 2 + p;
                if (score > maxScore) maxScore = score;

                points.Add(new DayProductivityPoint
                {
                    Date = d,
                    DayLabel = dayNames[(int)d.DayOfWeek],
                    HabitsCount = h,
                    TasksCount = t,
                    PomodoroCount = p,
                    TotalScore = score
                });
            }

            foreach (var pt in points)
                pt.BarHeightRatio = (double)pt.TotalScore / maxScore;

            return points;
        }

        private class DayStat { public string Day { get; set; } = ""; public int Cnt { get; set; } }

        private static List<ModuleRow> BuildModuleTable(
            int hCur, int hPrev, int days,
            int tCur, int tPrev,
            int pCur, int pPrev,
            int nCur, int nPrev,
            ReportModule module)
            {
            var rows = new List<ModuleRow>();
            if (module is ReportModule.All or ReportModule.Habits)
                rows.Add(MakeRow("Привычки", $"{hCur} выполнено", (double)hCur / days, hCur, hPrev));
            if (module is ReportModule.All or ReportModule.Tasks)
                rows.Add(MakeRow("Задачи", $"{tCur} закрыто", (double)tCur / days, tCur, tPrev));
            if (module is ReportModule.All or ReportModule.Pomodoro)
                rows.Add(MakeRow("Помодоро", $"{pCur} сессий", (double)pCur / days, pCur, pPrev));
            if (module is ReportModule.All or ReportModule.Notes)
                rows.Add(MakeRow("Заметки", $"{nCur} создано", (double)nCur / days, nCur, nPrev));
            return rows;
            }

        private static ModuleRow MakeRow(string module, string period, double perDay, int cur, int prev)
        {
            var diff = prev == 0 ? 0.0 : (double)(cur - prev) / prev * 100;
            string trend; string color;
            if (diff > 15) { trend = "↑ сильный рост"; color = "#2DD4BF"; }
            else if (diff > 5) { trend = "↑ стабильно растёт"; color = "#2DD4BF"; }
            else if (diff > -5) { trend = "→ примерно как раньше"; color = "#94A3B8"; }
            else if (diff > -15) { trend = "↓ небольшой спад"; color = "#F59E0B"; }
            else { trend = "↓ значительный спад"; color = "#EF4444"; }
            if (prev == 0 && cur == 0) { trend = "— без изменений"; color = "#475569"; }

            return new ModuleRow
            {
                Module = module,
                PeriodValue = period,
                PerDayValue = perDay.ToString("F1"),
                TrendLabel = trend,
                TrendColor = color
            };
        }

        private static (List<string> insights, List<string> recs) BuildInsights(
           int hCur, int hPrev, int habitsTotal,
           int tClosed, int tOverdue,
           int pomSessions, int pomMinutes,
           List<DayProductivityPoint> chart)
        {
            var insights = new List<string>();
            var recs = new List<string>();

            if (chart.Count >= 2)
            {
                var weekdays = chart.Where(d => d.Date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday).Sum(d => d.HabitsCount);
                var weekends = chart.Where(d => d.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday).Sum(d => d.HabitsCount);
                if (weekdays > weekends * 1.3)
                    insights.Add("• Ты стабильно выполняешь больше привычек в будни, чем в выходные.");

                var bestPomDay = chart.OrderByDescending(d => d.PomodoroCount).FirstOrDefault();
                if (bestPomDay != null && bestPomDay.PomodoroCount > 0)
                    insights.Add($"• Наибольшее количество Помодоро приходится на {bestPomDay.DayLabel} ({bestPomDay.Date:dd.MM}).");
            }

            if (tOverdue > 0)
                insights.Add($"• Просроченных задач: {tOverdue}. Стоит разобраться до конца недели.");

            if (pomMinutes > 0)
                insights.Add($"• Чистое время фокуса за период: {pomMinutes / 60} ч {pomMinutes % 60} мин.");

            if (habitsTotal > 0)
            {
                var rate = (double)hCur / habitsTotal * 100;
                insights.Add($"• Средний процент выполнения привычек: {rate:F0}%.");
            }

            if (tOverdue > 3)
                recs.Add("• Перенеси сложные задачи на утро вторника и среды — там у тебя пик фокус-времени.");

            if (hPrev > 0 && hCur < hPrev * 0.8)
                recs.Add("• Попробуй ставить жёсткие дедлайны задачам, которые обычно переносятся.");

            if (pomSessions < 5)
                recs.Add("• Добавь напоминание по привычкам на выходные, чтобы выровнять серию.");

            if (recs.Count == 0)
                recs.Add("• Отличный темп! Продолжай в том же духе и попробуй увеличить цель на 10%.");

            return (insights, recs);
        }

        public async Task<string> ExportPdfAsync(AnalyticsReport r)
        {
            const string css = """
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: 'Segoe UI', Arial, sans-serif;
               background: #0B1120; color: #F1F5F9; padding: 32px; }
        .header { border-bottom: 2px solid #2DD4BF;
                  padding-bottom: 16px; margin-bottom: 24px; }
        .header h1 { font-size: 24px; color: #2DD4BF; }
        .header p  { color: #64748B; margin-top: 4px; font-size: 13px; }
        .section   { margin-bottom: 28px; }
        .section h2 { font-size: 15px; color: #94A3B8;
                      text-transform: uppercase; letter-spacing: 1px;
                      margin-bottom: 12px; }
        .metrics { display: grid; grid-template-columns: repeat(4,1fr); gap: 12px; }
        .metric  { background: #111827; border: 1px solid #1E293B;
                   border-radius: 12px; padding: 14px; }
        .metric .label { font-size: 11px; color: #64748B; margin-bottom: 6px; }
        .metric .value { font-size: 28px; font-weight: bold; color: #F1F5F9; }
        .metric .delta { font-size: 11px; color: #2DD4BF; margin-top: 4px; }
        table { width: 100%; border-collapse: collapse;
                background: #111827; border-radius: 12px; overflow: hidden; }
        th { background: #1E293B; color: #94A3B8; font-size: 11px;
             text-transform: uppercase; letter-spacing: 0.5px;
             padding: 10px 14px; text-align: left; }
        td { padding: 10px 14px; font-size: 13px; color: #CBD5E1;
             border-top: 1px solid #1E293B; }
        .insights { background: #111827; border: 1px solid #1E293B;
                    border-radius: 12px; padding: 16px; }
        .insights li { font-size: 13px; color: #CBD5E1;
                       margin-bottom: 8px; list-style: none; }
        .insights li::before { content: '•'; color: #2DD4BF; margin-right: 8px; }
        .recs li::before { color: #F59E0B; }
        .footer { margin-top: 32px; text-align: center;
                  font-size: 11px; color: #334155; }
        @media print {
          body { background: white; color: #111; }
          .metric { border-color: #ddd; background: #f9f9f9; }
          table { border: 1px solid #ddd; }
        }
        """;

            // Строим таблицу модулей отдельно
            var moduleRows = new System.Text.StringBuilder();
            foreach (var row in r.ModuleTable)
            {
                moduleRows.Append("<tr>");
                moduleRows.Append($"<td><strong>{row.Module}</strong></td>");
                moduleRows.Append($"<td>{row.PeriodValue}</td>");
                moduleRows.Append($"<td>{row.PerDayValue}</td>");
                moduleRows.Append($"<td style=\"color:{row.TrendColor}\">{row.TrendLabel}</td>");
                moduleRows.Append("</tr>");
            }

            // Строим инсайты
            var insightItems = new System.Text.StringBuilder();
            foreach (var i in r.Insights)
                insightItems.Append($"<li>{System.Net.WebUtility.HtmlEncode(i.TrimStart('•', ' '))}</li>");

            var recItems = new System.Text.StringBuilder();
            foreach (var rec in r.Recommendations)
                recItems.Append($"<li>{System.Net.WebUtility.HtmlEncode(rec.TrimStart('•', ' '))}</li>");

            // Метрика-карточки
            var overdueColor = r.TasksOverdue > 0 ? "#F59E0B" : "#2DD4BF";
            var overdueText = r.TasksOverdue > 0 ? $"{r.TasksOverdue} просрочено" : "все в срок";
            var habitsDelta = FormatDelta(r.HabitsCompleted, r.HabitsCompletedPrev);
            var pomodoroHours = r.PomodoroMinutes / 60;
            var pomodoroMins = r.PomodoroMinutes % 60;

            var html = new System.Text.StringBuilder();
            html.Append("<!DOCTYPE html><html lang=\"ru\"><head>");
            html.Append("<meta charset=\"utf-8\"/>");
            html.Append("<title>EduDev Tracker — Отчёт</title>");
            html.Append("<style>");
            html.Append(css);
            html.Append("</style></head><body>");

            // Header
            html.Append("<div class=\"header\">");
            html.Append("<h1>EduDev Tracker — Аналитический отчёт</h1>");
            html.Append($"<p>Период: {r.PeriodLabel} &nbsp;|&nbsp; Создан: {DateTime.Now:dd.MM.yyyy HH:mm}</p>");
            html.Append("</div>");

            // Метрики
            html.Append("<div class=\"section\"><h2>Ключевые метрики</h2><div class=\"metrics\">");

            html.Append("<div class=\"metric\">");
            html.Append("<div class=\"label\">Привычки выполнено</div>");
            html.Append($"<div class=\"value\">{r.HabitsCompleted}<span style=\"font-size:16px;color:#475569\">/{r.HabitsTotal}</span></div>");
            html.Append($"<div class=\"delta\">{habitsDelta} к прошлому периоду</div>");
            html.Append("</div>");

            html.Append("<div class=\"metric\">");
            html.Append("<div class=\"label\">Задачи закрыто</div>");
            html.Append($"<div class=\"value\">{r.TasksClosed}</div>");
            html.Append($"<div class=\"delta\" style=\"color:{overdueColor}\">{overdueText}</div>");
            html.Append("</div>");

            html.Append("<div class=\"metric\">");
            html.Append("<div class=\"label\">Сессии Помодоро</div>");
            html.Append($"<div class=\"value\">{r.PomodoroSessions}</div>");
            html.Append($"<div class=\"delta\">{pomodoroHours} ч {pomodoroMins} мин фокуса</div>");
            html.Append("</div>");

            html.Append("<div class=\"metric\">");
            html.Append("<div class=\"label\">Заметки создано</div>");
            html.Append($"<div class=\"value\">{r.NotesCreated}</div>");
            html.Append($"<div class=\"delta\">{r.NotesPinned} закреплено</div>");
            html.Append("</div>");

            html.Append("</div></div>"); // /metrics /section

            // Таблица модулей
            html.Append("<div class=\"section\"><h2>Разбивка по модулям</h2>");
            html.Append("<table><thead><tr>");
            html.Append("<th>Модуль</th><th>За период</th><th>В день</th><th>Тренд</th>");
            html.Append("</tr></thead><tbody>");
            html.Append(moduleRows);
            html.Append("</tbody></table></div>");

            // Инсайты
            html.Append("<div class=\"section\"><h2>Инсайты</h2>");
            html.Append("<div class=\"insights\"><ul>");
            html.Append(insightItems);
            html.Append("</ul></div></div>");

            // Рекомендации
            html.Append("<div class=\"section\"><h2>Рекомендации</h2>");
            html.Append("<div class=\"insights recs\"><ul>");
            html.Append(recItems);
            html.Append("</ul></div></div>");

            // Footer
            html.Append("<div class=\"footer\">EduDev Tracker &nbsp;•&nbsp; Отчёт сформирован автоматически</div>");
            html.Append("</body></html>");

            var fileName = $"EduDev_Report_{r.From:yyyyMMdd}_{r.To:yyyyMMdd}.html";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(path, html.ToString(), System.Text.Encoding.UTF8);
            return path;
        }

        private static string FormatDelta(int cur, int prev)
        {
            if (prev == 0) return cur > 0 ? $"+{cur}" : "—";
            var diff = cur - prev;
            return diff >= 0 ? $"+{diff}" : $"{diff}";
        }
        public async Task<string> ExportCsvAsync(AnalyticsReport r)
        {
            var sep = ";";
            var sb = new System.Text.StringBuilder();

            // Заголовок
            sb.AppendLine($"EduDev Tracker — Отчёт за период: {r.PeriodLabel}");
            sb.AppendLine($"Создан: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine();

            // Ключевые метрики
            sb.AppendLine("=== КЛЮЧЕВЫЕ МЕТРИКИ ===");
            sb.AppendLine($"Показатель{sep}Значение{sep}Δ к прошлому периоду");
            sb.AppendLine($"Привычки выполнено{sep}{r.HabitsCompleted}/{r.HabitsTotal}{sep}{FormatDelta(r.HabitsCompleted, r.HabitsCompletedPrev)}");
            sb.AppendLine($"Задачи закрыто{sep}{r.TasksClosed}{sep}{FormatDelta(r.TasksClosed, r.TasksClosedPrev)}");
            sb.AppendLine($"Задачи просрочено{sep}{r.TasksOverdue}{sep}");
            sb.AppendLine($"Сессии Помодоро{sep}{r.PomodoroSessions}{sep}{FormatDelta(r.PomodoroSessions, r.PomodoroSessionsPrev)}");
            sb.AppendLine($"Минут фокуса{sep}{r.PomodoroMinutes}{sep}");
            sb.AppendLine($"Заметки создано{sep}{r.NotesCreated}{sep}{FormatDelta(r.NotesCreated, r.NotesCreatedPrev)}");
            sb.AppendLine($"Заметки закреплено{sep}{r.NotesPinned}{sep}");
            sb.AppendLine();

            // Разбивка по модулям
            sb.AppendLine("=== РАЗБИВКА ПО МОДУЛЯМ ===");
            sb.AppendLine($"Модуль{sep}За период{sep}В день{sep}Тренд");
            foreach (var row in r.ModuleTable)
                sb.AppendLine($"{row.Module}{sep}{row.PeriodValue}{sep}{row.PerDayValue}{sep}{row.TrendLabel}");
            sb.AppendLine();

            // Дневная динамика
            sb.AppendLine("=== ДИНАМИКА ПО ДНЯМ ===");
            sb.AppendLine($"Дата{sep}День{sep}Привычки{sep}Задачи{sep}Помодоро{sep}Итого");
            foreach (var pt in r.DailyChart)
                sb.AppendLine($"{pt.Date:dd.MM.yyyy}{sep}{pt.DayLabel}{sep}{pt.HabitsCount}{sep}{pt.TasksCount}{sep}{pt.PomodoroCount}{sep}{pt.TotalScore}");
            sb.AppendLine();

            // Инсайты
            sb.AppendLine("=== ИНСАЙТЫ ===");
            foreach (var i in r.Insights)
                sb.AppendLine(i);
            sb.AppendLine();

            sb.AppendLine("=== РЕКОМЕНДАЦИИ ===");
            foreach (var rec in r.Recommendations)
                sb.AppendLine(rec);

            var fileName = $"EduDev_Report_{r.From:yyyyMMdd}_{r.To:yyyyMMdd}.csv";
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(path, sb.ToString(),
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)); // BOM для Excel
            return path;
        }
    }
}
