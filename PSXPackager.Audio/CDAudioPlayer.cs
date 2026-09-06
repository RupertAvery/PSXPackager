using NAudio.Wave;
using Popstation.Media;
using PSXPackager.Common.Cue;
using System;

namespace PSXPackager.Audio
{
    public class CDAudioPlayerStopped
    {
        public CueTrack? Track { get; set; }
        public Exception? Exception { get; set; }
    }

    public class CDAudioPlayerStarted
    {
        public CueTrack Track { get; set; }
    }

    public enum CDAudioPlayerStatus
    {
        Stopped,
        Playing,
        Paused
    }

    public class CDAudioPlayer : IDisposable
    {
        const int SectorSize = 2352;
        private readonly WaveOutEvent _waveOutEvent;
        private readonly BufferedWaveProvider _buffer;

        public event EventHandler<CDAudioPlayerStarted> Started;
        public event EventHandler<CDAudioPlayerStopped> Stopped;

        public CDAudioPlayer()
        {
            var waveFormat = new WaveFormat(44100, 16, 2);
            _buffer = new BufferedWaveProvider(waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(10),
                DiscardOnBufferOverflow = false
            };

            _waveOutEvent = new WaveOutEvent();
            _waveOutEvent.Init(_buffer);

            _waveOutEvent.PlaybackStopped += WaveOutEventOnPlaybackStopped;
        }

        private void WaveOutEventOnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            Stopped?.Invoke(this, new CDAudioPlayerStopped() { Exception = e.Exception });
        }

        public void Pause()
        {
            _waveOutEvent.Pause();
        }

        public void Play()
        {
            _waveOutEvent.Play();
        }

        public void Stop()
        {
            _waveOutEvent.Stop();
        }

        private CancellationTokenSource? cts;
        private AutoResetEvent resetEvent = new AutoResetEvent(false);

        public void PlayCueTrack(CueTrack track)
        {
            if (_waveOutEvent.PlaybackState == PlaybackState.Playing)
            {
                _waveOutEvent.Stop();
            }

            resetEvent.Reset();
            cts?.Cancel();
            resetEvent.WaitOne(100);
            cts = new CancellationTokenSource();
            
            PlayCueTrack(track, cts.Token);
        }

        public void PlayCueTrack(CueTrack track, CancellationToken cancellationToken)
        {
            // Whether the track lives in a .bin, a .chd or a disc inside an EBOOT, it reads the same
            var source = DiscSource.ForTrack(track);

            try
            {
                var (startSector, endSector) = track.GetSectorRange(source.Length);

                _buffer.ClearBuffer();
                _waveOutEvent.Play();

                Play(source, startSector, endSector, cancellationToken);
            }
            catch
            {
                source.Dispose();
                throw;
            }
        }

        private void Play(DiscSource source, int startSector, int endSector, CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                try
                {
                    source.Stream.Seek((long)startSector * SectorSize, SeekOrigin.Begin);

                    byte[] sector = new byte[SectorSize];
                    int currentSector = startSector;

                    while (currentSector < endSector &&
                           source.Stream.Read(sector, 0, sector.Length) == sector.Length)
                    {
                        while (_buffer.BufferedBytes > _buffer.BufferLength - sector.Length)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                            Thread.Sleep(5); // wait for space
                        }
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        _buffer.AddSamples(sector, 0, sector.Length);

                        currentSector++;
                    }


                    while (_buffer.BufferedBytes > 0)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        Thread.Sleep(5); // wait for space
                    }
                }
                finally
                {
                    // The disc stays open only for as long as it is being read
                    source.Dispose();
                    Stop();
                }
            });
        }

        public void Dispose()
        {
            if (_waveOutEvent.PlaybackState == PlaybackState.Playing)
            {
                Stop();
            }
            _buffer.ClearBuffer();
            _waveOutEvent.PlaybackStopped -= WaveOutEventOnPlaybackStopped;
            _waveOutEvent.Dispose();
        }

    }
}
