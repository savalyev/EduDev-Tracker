using Plugin.Maui.Audio;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduDev_Tracker.Services.Audio
{
    public class AudioService: IAudioService
    {
        private readonly IAudioManager _audioManager;
        private double _volume = 0.6;

        public double Volume => _volume;
        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public async Task PlayAsync(string soundName)
        {
            try
            {
                var stream = await FileSystem.OpenAppPackageFileAsync($"{soundName}.mp3");

                var player = _audioManager.CreatePlayer(stream);

                player.Volume = _volume;
                player.Play();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] Failed to play {soundName}: {ex.Message}");
            }
        }

        public void SetVolume(double volume)
        {
            _volume = Math.Clamp(volume, 0.0, 1.0);
        }
    }

    public class NullAudioService : IAudioService
    {
        public double Volume => 0;
        public Task PlayAsync(string soundName)
        {
            System.Diagnostics.Debug.WriteLine($"[NullAudio] {soundName}");
            return Task.CompletedTask;
        }
        public void SetVolume(double volume) { }
    }
}
