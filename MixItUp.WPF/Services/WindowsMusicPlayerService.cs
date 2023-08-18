﻿using Id3;
using MixItUp.Base;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using NAudio.Wave;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.WPF.Services
{
    public class WindowsMusicPlayerService : IMusicPlayerService
    {
        private static readonly ISet<string> AllowedFileExtensions = new HashSet<string>() { ".mp3" };

        public event EventHandler SongChanged = delegate { };

        public MusicPlayerState State { get; private set; }

        public int Volume
        {
            get { return ChannelSession.Settings.MusicPlayerVolume; }
            set { ChannelSession.Settings.MusicPlayerVolume = value; }
        }

        public MusicPlayerSong CurrentSong
        {
            get
            {
                if (0 <= this.currentSongIndex && this.currentSongIndex < this.songs.Count)
                {
                    return this.songs[this.currentSongIndex];
                }
                return null;
            }
        }

        public ObservableCollection<MusicPlayerSong> Songs { get { return this.songs; } }

        private ThreadSafeObservableCollection<MusicPlayerSong> songs = new ThreadSafeObservableCollection<MusicPlayerSong>();
        private int currentSongIndex = 0;

        private CancellationTokenSource backgroundPlayThreadTokenSource = new CancellationTokenSource();
        private WaveOutEvent currentWaveOutEvent;

        private SemaphoreSlim sempahore = new SemaphoreSlim(1); 

        public async Task Play()
        {
            if (this.songs.Count == 0)
            {
                await this.SetSongs();
            }

            if (this.songs.Count > 0)
            {
                if (this.State == MusicPlayerState.Paused)
                {
                    await this.sempahore.WaitAndRelease(() =>
                    {
                        this.State = MusicPlayerState.Playing;
                        if (this.currentWaveOutEvent != null)
                        {
                            this.currentWaveOutEvent.Play();
                        }
                        return Task.CompletedTask;
                    });
                }
                else if (this.State == MusicPlayerState.Stopped)
                {
                    await this.sempahore.WaitAndRelease(() =>
                    {
                        this.State = MusicPlayerState.Playing;
                        this.PlayInternal(this.CurrentSong.FilePath);
                        return Task.CompletedTask;
                    });
                }
            }
        }

        public async Task Pause()
        {
            if (this.State == MusicPlayerState.Playing)
            {
                await this.sempahore.WaitAndRelease(() =>
                {
                    this.State = MusicPlayerState.Paused;
                    if (this.currentWaveOutEvent != null)
                    {
                        this.currentWaveOutEvent.Pause();
                    }
                    return Task.CompletedTask;
                });
            }
        }

        public async Task Stop()
        {
            await this.sempahore.WaitAndRelease(() =>
            {
                this.State = MusicPlayerState.Stopped;

                if (this.currentWaveOutEvent != null)
                {
                    this.currentWaveOutEvent.Stop();
                }
                this.currentWaveOutEvent = null;

                if (this.backgroundPlayThreadTokenSource != null)
                {
                    this.backgroundPlayThreadTokenSource.Cancel();
                }
                this.backgroundPlayThreadTokenSource = null;

                return Task.CompletedTask;
            });
        }

        public async Task Next()
        {
            await this.Stop();
            await this.sempahore.WaitAndRelease(() =>
            {
                this.currentSongIndex++;
                if (this.currentSongIndex >= this.songs.Count)
                {
                    this.currentSongIndex = 0;
                }
                return Task.CompletedTask;
            });
            await this.Play();
        }

        public async Task Previous()
        {
            await this.Stop();
            await this.sempahore.WaitAndRelease(() =>
            {
                this.currentSongIndex--;
                if (this.currentSongIndex < 0)
                {
                    this.currentSongIndex = Math.Max(this.songs.Count - 1, 0);
                }
                return Task.CompletedTask;
            });
            await this.Play();
        }

        public async Task ChangeVolume(int amount)
        {
            await this.sempahore.WaitAndRelease(() =>
            {
                this.Volume = amount;
                if (this.currentWaveOutEvent != null)
                {
                    this.currentWaveOutEvent.Volume = (ServiceManager.Get<IAudioService>() as WindowsAudioService).ConvertVolumeAmount(this.Volume);
                }
                return Task.CompletedTask;
            });
        }

        public async Task SetSongs()
        {
            await this.sempahore.WaitAndRelease(async () =>
            {
                WindowsFileService fileService = ServiceManager.Get<IFileService>() as WindowsFileService;
                foreach (string folder in ChannelSession.Settings.MusicPlayerFolders)
                {
                    List<string> files = new List<string>();
                    files.AddRange(await fileService.GetFilesInDirectory(folder));
                    foreach (string subFolder in await fileService.GetFoldersInDirectory(folder))
                    {
                        files.AddRange(await fileService.GetFilesInDirectory(subFolder));
                    }

                    List<MusicPlayerSong> tempSongs = new List<MusicPlayerSong>();
                    foreach (string file in files)
                    {
                        string extension = Path.GetExtension(file).ToLower();
                        if (AllowedFileExtensions.Contains(extension))
                        {
                            using (var mp3 = new Mp3(file))
                            {
                                MusicPlayerSong song = null;

                                var v2Tags = mp3.GetTag(Id3TagFamily.Version2X);
                                if (v2Tags != null)
                                {
                                    song = new MusicPlayerSong()
                                    {
                                        FilePath = file,
                                        Title = v2Tags.Title.Value,
                                        Length = v2Tags.Length.IsAssigned ? (int)v2Tags.Length.Value.TotalSeconds : 0
                                    };

                                    if (v2Tags.Artists.IsAssigned && v2Tags.Artists.Value.Count > 0)
                                    {
                                        song.Artist = string.Join(", ", v2Tags.Artists.Value);
                                    }
                                    else if (v2Tags.Band.IsAssigned)
                                    {
                                        song.Artist = v2Tags.Band.Value;
                                    }
                                    else if (v2Tags.Composers.IsAssigned && v2Tags.Composers.Value.Count > 0)
                                    {
                                        song.Artist = string.Join(", ", v2Tags.Artists.Value);
                                    }
                                }
                                else
                                {
                                    var v1Tags = mp3.GetTag(Id3TagFamily.Version1X);
                                    if (v1Tags != null)
                                    {
                                        song = new MusicPlayerSong()
                                        {
                                            FilePath = file,
                                            Title = v1Tags.Title.Value,
                                            Length = v1Tags.Length.IsAssigned ? (int)v1Tags.Length.Value.TotalSeconds : 0
                                        };

                                        if (v1Tags.Artists.IsAssigned && v1Tags.Artists.Value.Count > 0)
                                        {
                                            song.Artist = string.Join(", ", v1Tags.Artists.Value);
                                        }
                                        else if (v1Tags.Band.IsAssigned)
                                        {
                                            song.Artist = v1Tags.Band.Value;
                                        }
                                        else if (v1Tags.Composers.IsAssigned && v1Tags.Composers.Value.Count > 0)
                                        {
                                            song.Artist = string.Join(", ", v1Tags.Artists.Value);
                                        }
                                    }
                                    else
                                    {
                                        song = new MusicPlayerSong() { FilePath = file, Title = Path.GetFileNameWithoutExtension(file) };
                                    }
                                }

                                if (song != null)
                                {
                                    tempSongs.Add(song);
                                }
                            }
                        }
                    }

                    this.songs.Clear();
                    foreach (MusicPlayerSong song in tempSongs.Shuffle())
                    {
                        this.songs.Add(song);
                    }
                }
            });
        }

        private void PlayInternal(string filePath)
        {
            if (this.backgroundPlayThreadTokenSource != null)
            {
                this.backgroundPlayThreadTokenSource.Cancel();
            }
            this.backgroundPlayThreadTokenSource = new CancellationTokenSource();

            WindowsAudioService audioService = ServiceManager.Get<IAudioService>() as WindowsAudioService;
            this.currentWaveOutEvent = audioService.PlayWithOutput(filePath, this.Volume, ChannelSession.Settings.MusicPlayerAudioOutput);
            Task backgroundPlayThreadTask = Task.Run(async () => await this.PlayBackground(this.currentWaveOutEvent), this.backgroundPlayThreadTokenSource.Token);

            this.SongChanged.Invoke(this, new EventArgs());
        }

        private async Task PlayBackground(WaveOutEvent waveOutEvent)
        {
            using (waveOutEvent)
            {
                while (waveOutEvent != null && (waveOutEvent.PlaybackState == PlaybackState.Playing || waveOutEvent.PlaybackState == PlaybackState.Paused))
                {
                    await Task.Delay(500);
                }
                waveOutEvent.Dispose();
            }
        }
    }
}
