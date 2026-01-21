using NAudio.Wave;
using PSXPackager.Common.Cue;

namespace PSXPackager.Audio
{
    public class CDAudioPlayer : IDisposable
    {
        const int SectorSize = 2352;
        private readonly WaveOutEvent _waveOutEvent;
        private readonly BufferedWaveProvider _buffer;

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
            cts?.Cancel();
            cts = new CancellationTokenSource();
            resetEvent.WaitOne(100);
            resetEvent.Reset();

            _buffer.ClearBuffer();
            PlayCueTrack(track, cts.Token);
        }

        public void PlayCueTrack(CueTrack track, CancellationToken cancellationToken)
        {
            var binPath = track.FileEntry.FileName;

            if (!Path.IsPathFullyQualified(binPath))
            {
                binPath = Path.Combine(Path.GetDirectoryName(track.FileEntry.CueFile.Path), binPath);
            }

            _waveOutEvent.Play();

            var startIndex = track.Indexes.First(i => i.Number == 1);
            int startSector = startIndex.Position.ToSector();

            int endSector;

            if (track.Next != null)
                endSector = track.Next.Indexes.First(i => i.Number == 1).Position.ToSector();
            else
            {
                long fileSize = new FileInfo(binPath).Length;
                endSector = (int)(fileSize / SectorSize);
            }

            Task.Run(() =>
            {
                using var fs = File.OpenRead(binPath);

                fs.Seek((long)startSector * SectorSize, SeekOrigin.Begin);

                byte[] sector = new byte[SectorSize];
                int currentSector = startSector;

                while (currentSector < endSector &&
                       fs.Read(sector, 0, sector.Length) == sector.Length)
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

                resetEvent.Set();
            });
        }


        public void Dispose()
        {
            if (_waveOutEvent.PlaybackState == PlaybackState.Playing)
            {
                _waveOutEvent.Stop();
            }
            _buffer.ClearBuffer();
            _waveOutEvent.Dispose();
        }
    }
}
