using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Threading;
using Popstation.Database;
using PSXPackager.Common;
using PSXPackager.Common.Notification;
using PSXPackagerGUI.Models;
using PSXPackagerGUI.Pages;

namespace PSXPackagerGUI.Processing
{
    public class Processor
    {
        private readonly Dispatcher _dispatcher;
        private readonly GameDB _gameDb;
        private readonly SettingsModel _settings;
        private readonly IEventHandler _eventHandler;
        private readonly Channel<ConvertJob> _channel = Channel.CreateUnbounded<ConvertJob>();
        private int _degreeOfParallelism;

        public Processor(Dispatcher dispatcher, GameDB gameDb, SettingsModel settings, IEventHandler eventHandler)
        {
            _degreeOfParallelism = 4;
            _dispatcher = dispatcher;
            _gameDb = gameDb;
            _settings = settings;
            _eventHandler = eventHandler;
        }

        public void Add(ConvertJob job)
        {
            _channel.Writer.WriteAsync(job);
        }

        public Task Start(BatchModel model, CancellationToken token)
        {
            var consumers = new List<Task>();

            for (var i = 0; i < _degreeOfParallelism; i++)
            {
                consumers.Add(ProcessTask(model, token));
            }

            _channel.Writer.Complete();

            return Task.WhenAll(consumers);
        }

        private async Task ProcessTask(BatchModel model, CancellationToken token)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "PSXPackager");

            
            while (await _channel.Reader.WaitToReadAsync())
            {
                var job = await _channel.Reader.ReadAsync();

                var notifier = new ProcessNotifier(_dispatcher);
                notifier.Entry = job.Entry;

                var processing = new Popstation.Processing(notifier, _eventHandler, _gameDb);


                var processOptions = new ProcessOptions()
                {
                    ////Files = files,
                    OutputPath = model.Settings.OutputPath,
                    TempPath = tempPath,
                    Discs = Enumerable.Range(1, 5).ToList(),
                    //CheckIfFileExists = !o.OverwriteIfExists,
                    //SkipIfFileExists = o.SkipIfExists,
                    FileNameFormat = _settings.FileNameFormat,
                    CompressionLevel = _settings.CompressionLevel,
                    ////Verbosity = o.Verbosity,
                    ////Log = o.Log,
                    ExtractResources = model.ExtractResources,
                    ImportResources = _settings.UseCustomResources,
                    GenerateResourceFolders = model.GenerateResourceFolders,
                    CustomResourceFormat = _settings.CustomResourcesFormat,
                    ResourceFoldersPath = _settings.CustomResourcesPath, 
                };

                await Task.Run(() =>
                {
                    processing.ProcessFile(Path.Combine(model.Settings.InputPath, job.Entry.RelativePath), processOptions, token);
                });

            }
        }

    }
}