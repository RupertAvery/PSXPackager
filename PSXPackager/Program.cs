using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PSXPackager
{
    class Program
    {
        private static CancellationTokenSource _cancellationTokenSource;

        static void CancelEventHandler(object sender, ConsoleCancelEventArgs args)
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                Console.WriteLine("Stopping...");
                _cancellationTokenSource.Cancel();
            }
            args.Cancel = true;
        }

        static void Main(string[] args)
        {
            SevenZip.SevenZipBase.SetLibraryPath("x64/7z.dll");

            _cancellationTokenSource = new CancellationTokenSource();

            Console.CancelKeyPress += CancelEventHandler;

            var tempPath = Path.Combine(Path.GetTempPath(), "PSXPackager");

            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            Parser.Default.ParseArguments<Options>(args)
                 .WithParsed<Options>(o =>
                 {
                     Console.WriteLine($"PSXPackager v1.3 by RupertAvery\r\n");

                     if (o.CompressionLevel < 0 || o.CompressionLevel > 9)
                     {
                         Console.WriteLine($"Invalid compression level, please enter a value from 0 to 9");
                         return;
                     }

                     if (!string.IsNullOrEmpty(o.Discs))
                     {
                         if (!Regex.IsMatch(o.Discs, "\\d(,\\d)*"))
                         {
                             Console.WriteLine($"Invalid discs specification, please enter a comma separated list of values from 1-5");
                             return;
                         }
                     }

                     if (!string.IsNullOrEmpty(o.InputPath))
                     {
                         Console.WriteLine($"Converting single file");
                         Console.WriteLine($"Input : {o.InputPath}");
                     }
                     else if (!string.IsNullOrEmpty(o.Batch))
                     {
                         Console.WriteLine($"Batch : {o.Batch}");
                         Console.WriteLine($"Extension: {o.Filters}");
                     }

                     if (string.IsNullOrEmpty(o.OutputPath))
                     {
                         if (!string.IsNullOrEmpty(o.InputPath))
                         {
                             o.OutputPath = Path.GetDirectoryName(o.InputPath);
                         }
                         else if (!string.IsNullOrEmpty(o.Batch))
                         {
                             o.OutputPath = o.Batch;
                         }
                     }

                     Console.WriteLine($"Output: {o.OutputPath}");
                     Console.WriteLine($"Compression Level: {o.CompressionLevel}");
                     if (o.OverwriteIfExists)
                     {
                         Console.WriteLine("WARNING: You have chosen to overwrite all files in the output directory!");
                     }
                     Console.WriteLine();

                     var files = new List<string>();

                     if (!string.IsNullOrEmpty(o.InputPath))
                     {
                         if (PathIsDirectory(o.InputPath))
                         {
                             files.AddRange(GetFilesFromDirectory(o.InputPath, o.Filters));
                         }
                         else
                         {
                             var filename = Path.GetFileName(o.InputPath);
                             var path = Path.GetDirectoryName(o.InputPath);
                             if (!string.IsNullOrEmpty(path) && PathIsDirectory(path) && ContainsWildCards(filename))
                             {
                                 files.AddRange(GetFilesFromDirectory(path, filename));
                             }
                             else
                             {
                                 if (ContainsWildCards(filename))
                                 {
                                     files.AddRange(GetFilesFromDirectory(".", filename));
                                 }
                                 else
                                 {
                                     files.Add(o.InputPath);
                                 }
                             }
                         }

                     }
                     else if (!string.IsNullOrEmpty(o.Batch))
                     {
                         Console.WriteLine($"WARNING: Use of -b is deprecated. Use -i with wildcards instead.");
                         files.AddRange(GetFilesFromDirectory(o.InputPath, o.Filters));
                     }

                     ProcessFiles(files, o.OutputPath, tempPath, o.Discs, !o.OverwriteIfExists, o.CompressionLevel);
                 });
        }

        private static IEnumerable<string> GetFilesFromDirectory(string path, string filterExpression)
        {
            var supportedFiles = new List<string>() { ".7z", ".zip", ".gz", ".rar", ".tar", ".bin", ".cue", ".img", ".iso", ".pbp" };
            var filters = filterExpression.Split(new char[] { ';', '|' });
            foreach (var filter in filters)
            {
                string tempFilter = filter;
                if (filter.StartsWith("."))
                {
                    tempFilter = $"*{filter}";
                }
                var files = Directory.GetFiles(path, tempFilter);
                foreach (var file in files)
                {
                    if (supportedFiles.Contains(Path.GetExtension(file).ToLower()))
                        yield return file;
                }
            }
        }

        private static bool ContainsWildCards(string filename)
        {
            return filename.Contains("?") || filename.Contains("*");
        }

        private static bool PathIsDirectory(string path)
        {
            if (ContainsWildCards(path)) return false;
            return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
        }

        private static void ProcessFiles(IReadOnlyList<string> files, string outputPath, string tempPath, string discs, bool checkIfFileExists, int compressionLevel)
        {
            var processing = new Processing();

            if (files.Count > 1)
            {
                Console.WriteLine($"Matched {files.Count} files");

                var i = 1;
                var processed = 0;
                try
                {
                    foreach (var file in files)
                    {
                        if (!File.Exists(file))
                        {
                            Console.WriteLine($"Could not find file '{file}'");
                            continue;
                        }

                        Console.WriteLine($"Processing {i} of {files.Count}:  {file}");
                        var result = processing.ProcessFile(file, outputPath, tempPath, discs, compressionLevel, checkIfFileExists, _cancellationTokenSource.Token);
                        if (_cancellationTokenSource.Token.IsCancellationRequested || processing.Cancelled)
                        {
                            break;
                        }
                        processed += result ? 1 : 0;
                        i++;
                    }

                }
                finally
                {
                    Console.WriteLine($"{i - 1} files processed");
                }

            }
            else
            {
                processing.ProcessFile(files[0], outputPath, tempPath, discs, compressionLevel, checkIfFileExists, _cancellationTokenSource.Token);
            }

        }


    }
}
