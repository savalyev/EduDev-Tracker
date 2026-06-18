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
        private IAudioPlayer? _currentPlayer;
        private Stream? _currentStream;

        public double Volume => _volume;
        public AudioService(IAudioManager audioManager)
        {
            _audioManager = audioManager;
        }

        public async Task PlayAsync(string soundName)
        {
            try
            {
                _currentPlayer?.Stop();
                _currentPlayer?.Dispose();
                _currentStream?.Dispose();

                _currentStream = await FileSystem.OpenAppPackageFileAsync($"{soundName}.mp3");
                _currentPlayer = _audioManager.CreatePlayer(_currentStream);
                _currentPlayer.Volume = _volume;
                _currentPlayer.Play();
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
