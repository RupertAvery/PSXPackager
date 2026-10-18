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
        public int Total { get; set; }
    }

    public class CDAudioPlayerProgress
    {
        public int Current { get; set; }
    }

    public enum CDAudioPlayerStatus
    {
        Stopped,
        Playing,
        Paused
    }

    public delegate void UpdatePositionEventHandler(long position);

    public class ProgressTrackingStream : WaveStream
    {
        private readonly IWaveProvider sourceProvider;
        private long totalBytesRead;

        public event UpdatePositionEventHandler UpdatePosition;

        public ProgressTrackingStream(IWaveProvider sourceProvider)
        {
            this.sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
        }

        public override WaveFormat WaveFormat => sourceProvider.WaveFormat;

        // This property keeps track of exactly how much audio has been sent to the speaker
        public override long Position
        {
            get => totalBytesRead;
            set => throw new NotSupportedException("Seeking is not supported on a live buffered stream.");
        }

        // Since it's a live buffer, the total length is technically unknown or indefinite
        public override long Length => _length;
        private long _length;

        public void SetLength(long length)
        {
            _length = length;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = sourceProvider.Read(buffer, offset, count);
            totalBytesRead += bytesRead;
            return bytesRead;
        }

        public void Reset()
        {
            totalBytesRead = 0;
        }
    }

    public class CDAudioPlayer : IDisposable
    {
        const int SectorSize = 2352;
        private readonly WaveOutEvent _waveOutEvent;
        private readonly BufferedWaveProvider _buffer;

        public event EventHandler<CDAudioPlayerStarted> Started;
        public event EventHandler<CDAudioPlayerStopped> Stopped;
        public event EventHandler<CDAudioPlayerProgress> Progress;

        private readonly ProgressTrackingStream _trackingStream;

        public CDAudioPlayer()
        {
            var waveFormat = new WaveFormat(44100, 16, 2);
            _buffer = new BufferedWaveProvider(waveFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(10),
                DiscardOnBufferOverflow = false
            };

            _trackingStream = new ProgressTrackingStream(_buffer);

            _waveOutEvent = new WaveOutEvent();
            _waveOutEvent.Init(_trackingStream);

            _waveOutEvent.PlaybackStopped += WaveOutEventOnPlaybackStopped;
        }

        private long _positionBase;
        private long _lastPosition;

        /// <summary>
        /// How many bytes of the current track have been played. This follows the audio device's own
        /// clock, which advances continuously, rather than the reads from the buffer, which arrive
        /// in chunks of a few hundred milliseconds.
        /// </summary>
        public long CurrentPosition
        {
            get
            {
                if (_waveOutEvent.PlaybackState == PlaybackState.Stopped)
                {
                    return _lastPosition;
                }

                try
                {
                    var played = _waveOutEvent.GetPosition() - _positionBase;

                    if (played < _lastPosition) played = _lastPosition;

                    var total = TotalLength;
                    if (total > 0 && played > total) played = total;

                    _lastPosition = played;
                }
                catch (Exception)
                {
                    // The device may be closing, keep showing the last known position
                }

                return _lastPosition;
            }
        }

        public long TotalLength { get; set; }

        private void WaveOutEventOnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            // The end of a track is reported by the reader, which is the only part that knows
            // which track it was. Only a fault in the device itself is left to report here.
            if (e.Exception != null)
            {
                Stopped?.Invoke(this, new CDAudioPlayerStopped() { Exception = e.Exception });
            }
        }

        public void SetVolume(float volume)
        {
            _waveOutEvent.Volume = volume;
        }

        public bool IsPlaying => _waveOutEvent.PlaybackState == PlaybackState.Playing;

        public bool IsPaused => _waveOutEvent.PlaybackState == PlaybackState.Paused;

        private int _seekSector = -1;

        /// <summary>
        /// Moves the track being played to the given byte offset from its start.
        /// </summary>
        public void Seek(long position)
        {
            var total = TotalLength;
            if (position < 0) position = 0;
            if (position > total) position = total;

            Volatile.Write(ref _seekSector, (int)(position / SectorSize));
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
            // The reader has to be told as well, or it sits waiting for room in a buffer that
            // nothing is draining any more
            cts?.Cancel();
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

                Play(source, track, startSector, endSector, cancellationToken);
            }
            catch
            {
                source.Dispose();
                throw;
            }
        }

        private void Play(DiscSource source, CueTrack track, int startSector, int endSector, CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                Exception? exception = null;

                try
                {
                    source.Stream.Seek((long)startSector * SectorSize, SeekOrigin.Begin);

                    byte[] sector = new byte[SectorSize];
                    int currentSector = startSector;

                    var size = (endSector - startSector) * SectorSize;

                    _trackingStream.Reset();
                    _trackingStream.SetLength(size);

                    TotalLength = size;
                    _lastPosition = 0;
                    _positionBase = _waveOutEvent.GetPosition();
                    Volatile.Write(ref _seekSector, -1);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var seekSector = Interlocked.Exchange(ref _seekSector, -1);
                        if (seekSector >= 0)
                        {
                            currentSector = startSector + seekSector;
                            source.Stream.Seek((long)currentSector * SectorSize, SeekOrigin.Begin);
                            _buffer.ClearBuffer();

                            var offset = (long)seekSector * SectorSize;
                            _positionBase = _waveOutEvent.GetPosition() - offset;
                            _lastPosition = offset;
                            continue;
                        }

                        if (currentSector < endSector)
                        {
                            if (_buffer.BufferedBytes > _buffer.BufferLength - sector.Length)
                            {
                                Thread.Sleep(5); // wait for space
                                continue;
                            }

                            if (source.Stream.Read(sector, 0, sector.Length) != sector.Length)
                            {
                                currentSector = endSector;
                                continue;
                            }

                            _buffer.AddSamples(sector, 0, sector.Length);
                            currentSector++;
                        }
                        else if (_buffer.BufferedBytes > 0)
                        {
                            Thread.Sleep(5); // let the rest play out, a seek may still come
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
                finally
                {
                    // The disc stays open only for as long as it is being read
                    source.Dispose();

                    // A cancelled read no longer owns the device: either it has already been
                    // stopped, or the track that replaced this one is playing through it
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        _waveOutEvent.Stop();
                    }

                    Stopped?.Invoke(this, new CDAudioPlayerStopped() { Track = track, Exception = exception });
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
