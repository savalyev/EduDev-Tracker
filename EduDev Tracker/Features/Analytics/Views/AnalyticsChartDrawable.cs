using EduDev_Tracker.Features.Analytics.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EduDev_Tracker.Features.Analytics.Views
{
    public class AnalyticsChartDrawable : IDrawable
    {
        public List<DayProductivityPoint> Points { get; set; } = [];

        private static readonly Color ColorHabits = Color.FromArgb("#2DD4BF");
        private static readonly Color ColorTasks = Color.FromArgb("#818CF8");
        private static readonly Color ColorPomodoro = Color.FromArgb("#F59E0B");
        private static readonly Color ColorGrid = Color.FromArgb("#1E293B");

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Points.Count == 0) return;

            float padL = 8, padR = 8, padT = 12, padB = 32;
            float w = dirtyRect.Width - padL - padR;
            float h = dirtyRect.Height - padT - padB;
            int n = Points.Count;

            // ── Сетка ──
            canvas.StrokeColor = ColorGrid;
            canvas.StrokeSize = 1;
            for (int i = 0; i <= 4; i++)
            {
                float y = padT + h - h * i / 4f;
                canvas.DrawLine(padL, y, padL + w, y);
            }

            float stepX = n > 1 ? w / (n - 1) : w;

            // Общий максимум по всем трём рядам → линии сопоставимы по масштабу
            int maxVal = 1;
            foreach (var p in Points)
                maxVal = Math.Max(maxVal, Math.Max(p.HabitsCount, Math.Max(p.TasksCount, p.PomodoroCount)));

            // ── Три ряда ──
            DrawSeries(canvas, Points, p => p.HabitsCount,   ColorHabits,   padL, padT, h, stepX, maxVal);
            DrawSeries(canvas, Points, p => p.TasksCount,    ColorTasks,    padL, padT, h, stepX, maxVal);
            DrawSeries(canvas, Points, p => p.PomodoroCount, ColorPomodoro, padL, padT, h, stepX, maxVal);

            // ── Подписи дней ──
            canvas.FontSize = 11;
            canvas.FontColor = Color.FromArgb("#94A3B8");
            for (int i = 0; i < n; i++)
            {
                float x = padL + i * stepX;
                canvas.DrawString(Points[i].DayLabel, x - 12, padT + h + 6, 24, 16,
                                  HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        private static void DrawSeries(ICanvas canvas, List<DayProductivityPoint> pts,
            Func<DayProductivityPoint, int> valFn, Color color,
            float padL, float padT, float h, float stepX, int maxVal)
        {
            int n = pts.Count;

            float Y(int i) => padT + h - (float)valFn(pts[i]) / maxVal * h;

            // Лёгкая заливка под линией (даёт «area»-ощущение, не перекрывая соседей)
            if (n > 1)
            {
                var fill = new PathF();
                fill.MoveTo(padL, padT + h);
                for (int i = 0; i < n; i++)
                    fill.LineTo(padL + i * stepX, Y(i));
                fill.LineTo(padL + (n - 1) * stepX, padT + h);
                fill.Close();
                canvas.FillColor = color.WithAlpha(0.10f);
                canvas.FillPath(fill);
            }

            // Линия
            canvas.StrokeColor = color;
            canvas.StrokeSize = 3;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;
            for (int i = 1; i < n; i++)
                canvas.DrawLine(padL + (i - 1) * stepX, Y(i - 1), padL + i * stepX, Y(i));

            // Точки
            canvas.FillColor = color;
            for (int i = 0; i < n; i++)
                canvas.FillCircle(padL + i * stepX, Y(i), 3.5f);
        }
    }
}
