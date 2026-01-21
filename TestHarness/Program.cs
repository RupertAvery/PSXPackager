// See https://aka.ms/new-console-template for more information
using NAudio.Wave;
using PSXPackager.Audio;
using PSXPackager.Common.Cue;
using static System.Net.Mime.MediaTypeNames;

Console.WriteLine("Hello, World!");

//var waveFormat = new WaveFormat(44100, 16, 2);

//var buffer = new BufferedWaveProvider(waveFormat)
//{
//    BufferDuration = TimeSpan.FromSeconds(10),
//    DiscardOnBufferOverflow = false
//};

//var output = new WaveOutEvent();
//output.Init(buffer);
//output.Play();

//using var fs = File.OpenRead(@"D:\roms\PSX\Twisted Metal (USA) (Track 2).bin");

//Task.Run(() =>
//{
//    byte[] sector = new byte[2352];

//    while (fs.Read(sector, 0, sector.Length) == sector.Length)
//    {
//        while (buffer.BufferedBytes > buffer.BufferLength - sector.Length)
//        {
//            Thread.Sleep(5); // wait for space
//        }

//        buffer.AddSamples(sector, 0, sector.Length);
//    }
//});

var cuePath = @"D:\roms\PSX\Twisted Metal (USA).cue";

var cue = CueFileReader.Read(cuePath);

//var cueDir = Path.GetDirectoryName(cuePath);

var tracks = cue.FileEntries.SelectMany(d => d.Tracks).ToList();

//var tracks = cue.FileEntries.SelectMany(d => d.Tracks.Select(p => new EntryTrack(cueDir, d, p))).ToList();


//for (int i = 0; i < tracks.Count; i++)
//{
//    EntryTrack current = tracks[i];
//    current.Next = i + 1 < current.ParentEntry.Tracks.Count ? current.ParentEntry.Tracks[i + 1] : null;
//}

var track = tracks.Where(d => d.DataType == "AUDIO" && d.Number == 6).First();

var cdPlayer = new CDAudioPlayer();

cdPlayer.PlayCueTrack(track);

Console.WriteLine($"Finished reading audio data");
Console.ReadLine();

