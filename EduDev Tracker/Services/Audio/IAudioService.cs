using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Audio
{
    public interface IAudioService
    {
        Task PlayAsync(string soundName);
        void SetVolume(double volume);
        double Volume { get; }
    }
}
