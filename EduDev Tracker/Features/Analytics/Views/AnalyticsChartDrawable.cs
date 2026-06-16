using EduDev_Tracker.Features.Analytics.Models;
using System;
using System.Collections.Generic;
using System.Text;

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

            // ── Заливка-области (stacked area chart) ──
            float stepX = n > 1 ? w / (n - 1) : w;
            DrawArea(canvas, dirtyRect, Points, p => p.BarHeightRatio,
                     ColorHabits, padL, padT, h, stepX);

            // ── Подписи дней ──
            canvas.FontSize = 10;
            canvas.FontColor = Color.FromArgb("#475569");
            for (int i = 0; i < n; i++)
            {
                float x = padL + i * stepX;
                canvas.DrawString(Points[i].DayLabel, x - 10, padT + h + 4, 20, 16,
                                  HorizontalAlignment.Center, VerticalAlignment.Top);
            }
        }

        private static void DrawArea(ICanvas canvas, RectF rect,
            List<DayProductivityPoint> pts, Func<DayProductivityPoint, double> valFn,
            Color color, float padL, float padT, float h, float stepX)
        {
            int n = pts.Count;
            var path = new PathF();
            path.MoveTo(padL, padT + h);

            for (int i = 0; i < n; i++)
            {
                float x = padL + i * stepX;
                float y = padT + h - (float)(valFn(pts[i]) * h);
                if (i == 0) path.LineTo(x, y);
                else path.LineTo(x, y);
            }

            path.LineTo(padL + (n - 1) * stepX, padT + h);
            path.Close();

            canvas.FillColor = color.WithAlpha(0.25f);
            canvas.FillPath(path);

            // Линия поверх
            canvas.StrokeColor = color;
            canvas.StrokeSize = 2;
            for (int i = 1; i < n; i++)
            {
                float x0 = padL + (i - 1) * stepX;
                float y0 = padT + h - (float)(valFn(pts[i - 1]) * h);
                float x1 = padL + i * stepX;
                float y1 = padT + h - (float)(valFn(pts[i]) * h);
                canvas.DrawLine(x0, y0, x1, y1);
            }
        }
    }
}
