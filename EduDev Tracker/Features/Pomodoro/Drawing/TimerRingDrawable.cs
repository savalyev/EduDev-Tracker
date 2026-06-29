using EduDev_Tracker.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Features.Pomodoro.Drawing
{
    public class TimerRingDrawable: BindableObject, IDrawable
    {
        public static readonly BindableProperty ProgressProperty =
        BindableProperty.Create(nameof(Progress), typeof(double), typeof(TimerRingDrawable), 0.0,
            propertyChanged: (b, _, _) => ((TimerRingDrawable)b).Invalidated?.Invoke());

        public static readonly BindableProperty PhaseProperty =
            BindableProperty.Create(nameof(Phase), typeof(PomodoroPhase), typeof(TimerRingDrawable), PomodoroPhase.Work,
                propertyChanged: (b, _, _) => ((TimerRingDrawable)b).Invalidated?.Invoke());

        public double Progress
        {
            get => (double)GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public PomodoroPhase Phase
        {
            get => (PomodoroPhase)GetValue(PhaseProperty);
            set => SetValue(PhaseProperty, value);
        }

        // Событие — подписывается PomodoroPage для вызова GraphicsView.Invalidate()
        public event Action? Invalidated;
        public void Invalidate() => Invalidated?.Invoke();

        private Color GetArcColor() => Phase switch
        {
            PomodoroPhase.Work => Color.FromArgb("#2DD4BF"),   // teal
            PomodoroPhase.ShortBreak => Color.FromArgb("#60A5FA"),  // blue
            PomodoroPhase.LongBreak => Color.FromArgb("#A78BFA"),  // purple
            _ => Color.FromArgb("#2DD4BF")
        };

        private Color GetGlowColor() => Phase switch
        {
            PomodoroPhase.Work => Color.FromArgb("#0D9488"),
            PomodoroPhase.ShortBreak => Color.FromArgb("#2563EB"),
            PomodoroPhase.LongBreak => Color.FromArgb("#7C3AED"),
            _ => Color.FromArgb("#0D9488")
        };

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.Antialias = true;

            float cx = dirtyRect.Width / 2f;
            float cy = dirtyRect.Height / 2f;
            float radius = Math.Min(cx, cy) - 14f;
            float stroke = 10f;

            canvas.StrokeColor = Color.FromArgb("#1E293B");
            canvas.StrokeSize = stroke;
            canvas.DrawCircle(cx, cy, radius);

            if (Progress <= 0.001) return;

            float sweepAngle = (float)(Progress * 360.0);

            canvas.StrokeColor = GetArcColor();
            canvas.StrokeSize = stroke;
            canvas.StrokeLineCap = LineCap.Round;

            var path = new PathF();
            int segments = Math.Max(2, (int)(sweepAngle / 3f));
            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double rad = ((-90.0 + sweepAngle * t) * Math.PI) / 180.0;
                float px = cx + radius * (float)Math.Cos(rad);
                float py = cy + radius * (float)Math.Sin(rad);
                if (i == 0) path.MoveTo(px, py);
                else path.LineTo(px, py);
            }
            canvas.DrawPath(path);

            double angleRad = ((-90.0 + sweepAngle) * Math.PI) / 180.0;
            float tipX = cx + radius * (float)Math.Cos(angleRad);
            float tipY = cy + radius * (float)Math.Sin(angleRad);

            canvas.FillColor = GetGlowColor();
            canvas.FillCircle(tipX, tipY, stroke / 2f + 2f);

            canvas.FillColor = Color.FromArgb("#FFFFFF").WithAlpha(0.6f);
            canvas.FillCircle(tipX, tipY, stroke / 2f - 1f);
        }
    }
}
