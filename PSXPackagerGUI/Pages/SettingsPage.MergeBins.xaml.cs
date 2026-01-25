using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DiscUtils;
using Popstation;
using Popstation.Database;
using PSXPackager.Common;
using PSXPackager.Common.Cue;
using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Pages
{
    public partial class SettingsPage
    {

        private void SelectCUE_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "CUE files|*.cue|All files|*.*";
            var openResult = openFileDialog.ShowDialog();

            if (openResult is not true)
            {
                return;
            }

            var cueFile = CueFileReader.Read(openFileDialog.FileName);

            var basePath = Path.GetDirectoryName(cueFile.Path);

            Model.Converter.CueFile = cueFile;
            Model.Converter.ConvertMode = ConvertMode.CUE;
            Model.Converter.BinPaths = new ObservableCollection<string>(cueFile.FileEntries.Select(d => Path.Combine(basePath, d.FileName)));
            Model.Converter.SetSuggestedPaths();
        }

        private void SelectBins_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "BIN files|*.bin|All files|*.*";
            openFileDialog.Multiselect = true;
            var openResult = openFileDialog.ShowDialog();

            if (openResult is not true)
            {
                return;
            }

            var binPaths = openFileDialog.FileNames;

            var folderGroups = binPaths.Select(d => Path.GetDirectoryName(d))
                .GroupBy(d => d);

            if (folderGroups.Count() > 1)
            {
                throw new Exception("All .bin files must be in the same folder");
            }

            Model.Converter.ConvertMode = ConvertMode.BINS;
            Model.Converter.BinPaths = new ObservableCollection<string>(binPaths);
            Model.Converter.SetSuggestedPaths();
        }

        private void BrowseTargetPath_Click(object sender, RoutedEventArgs e)
        {
            var openFolderDialog = new Microsoft.Win32.OpenFolderDialog();
            var result = openFolderDialog.ShowDialog();

            if (result is not true)
            {
                return;
            }

            Model.Converter.TargetPath = openFolderDialog.FolderName;
        }

        private void Merge_Click(object sender, RoutedEventArgs e)
        {
            var binFilename = Model.Converter.TargetFileName + ".bin";
            var cueFilename = Model.Converter.TargetFileName + ".cue";
            var outputBinFilename = Model.Converter.TargetFileName + ".bin";
            var outputBinPath = Path.Combine(Model.Converter.TargetPath, binFilename);
            var outputCue = Path.Combine(Model.Converter.TargetPath, cueFilename);

            if (!Directory.Exists(Model.Converter.TargetPath))
            {
                var result = MessageBox.Show("The specified target folder does not exist. Do you want to create it?",
                    "Merge Bins", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                Directory.CreateDirectory(Model.Converter.TargetPath);
            }


            if (File.Exists(outputCue) || File.Exists(outputBinPath))
            {
                MessageBox.Show(Window,
                    $"The target files {cueFilename} and/or {binFilename} already exist at the specified location." +
                    $"To prevent accidental overwrite, it's recommended you select a different location.", "Merge Bins",
                    MessageBoxButton.OK, MessageBoxImage.Warning
                    );
                return;
            }

            try
            {
                CueFile cueFile = null;
                var binPaths = Model.Converter.BinPaths.ToList();

                if (Model.Converter.ConvertMode == ConvertMode.CUE)
                {
                    cueFile = Model.Converter.CueFile;
                }
                else if (Model.Converter.ConvertMode == ConvertMode.BINS)
                {
                    cueFile = GenerateCue(binPaths);
                }

                var gameIds = new HashSet<string>();

                var firstDiscIsGameData = false;

                var count = 0;

                var basePath = Path.GetDirectoryName(cueFile.Path);

                foreach (var binPath in binPaths)
                {
                    try
                    {
                        var tempPath = binPath;

                        if (!Path.IsPathFullyQualified(binPath))
                        {
                            tempPath = Path.Combine(basePath, binPath);
                        }

                        if (GameDB.TryFindGameId(tempPath, out var gameId))
                        {
                            if (count == 0)
                            {
                                firstDiscIsGameData = true;
                            }
                            gameIds.Add(gameId);
                        }
                    }
                    catch (InvalidFileSystemException exception)
                    {
                    }

                    count++;
                }

                MessageBoxResult result = MessageBoxResult.OK;

                if (gameIds.Count == 0)
                {
                    var message = "Warning: No DATA track could be identified in this list.\n\n" +
                                  "The game might not be recognized in the database, or the disc does not contain the expected executable filename format. " +
                                  "Continue at your own risk";

                    result = MessageBox.Show(message, "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                }
                else if (gameIds.Count > 1)
                {
                    result = MessageBox.Show("Warning: There appears to be more than one DATA track in this list. " +
                                             "If this is correct and you know what you are doing, you can ignore this warning.", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                }
                else if (!firstDiscIsGameData)
                {
                    result = MessageBox.Show("Warning: The first disc does not appear to be a DATA track. " +
                                             "The DATA track to be the first disc in a multi-disc set.", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                }

                if (result == MessageBoxResult.Cancel)
                {
                    return;
                }




                using (var stream = new FileStream(outputBinPath, FileMode.Create, FileAccess.Write))
                {
                    var curFile = Popstation.Processing.MergeBins(stream, outputBinFilename, cueFile);
                    CueFileWriter.Write(curFile, outputCue);
                }

                MessageBox.Show($"Files have been merged to {Model.Converter.TargetPath}");

            }
            catch (Exception exception)
            {
                MessageBox.Show(Window, "Error: " + exception.Message, "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (Model.Converter.SelectedIndex > 0)
            {
                Model.Converter.BinPaths.Move(Model.Converter.SelectedIndex, Model.Converter.SelectedIndex - 1);
            }
        }
        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            if (Model.Converter.SelectedIndex < Model.Converter.BinPaths.Count - 1)
            {
                Model.Converter.BinPaths.Move(Model.Converter.SelectedIndex, Model.Converter.SelectedIndex + 1);
            }
        }

        private CueFile GenerateCue(List<string> binPaths)
        {
            var cueFile = new CueFile();

            var index = 1;

            var folderGroups = binPaths.Select(d => Path.GetDirectoryName(d))
                .GroupBy(d => d);

            if (folderGroups.Count() > 1)
            {
                throw new Exception("All .bin files must be in the same folder");
            }

            var baseFolder = folderGroups.First().Key;

            foreach (var binPath in binPaths)
            {
                var fileName = Path.GetFileName(binPath);

                cueFile.FileEntries.Add(new CueFileEntry()
                {
                    FileName = fileName,
                    FileType = "BINARY",
                    Tracks = index == 1
                        ?
                        [
                            // Data track 
                            new CueTrack()
                            {
                                DataType = CueTrackType.Data,
                                Number = index,
                                Indexes = new List<CueIndex>()
                                {
                                    // No pre-gap for first track
                                    new CueIndex() { Number = 1, Position = new IndexPosition(0, 0, 0) }
                                }
                            }
                        ]
                        :
                        [
                            // Audio track
                            new CueTrack()
                            {
                                DataType = CueTrackType.Audio,
                                Number = index,
                                Indexes = new List<CueIndex>()
                                {
                                    // Pre-gap index
                                    new CueIndex() { Number = 0, Position = new IndexPosition(0, 0, 0) },
                                    new CueIndex() { Number = 1, Position = new IndexPosition(0, 2, 0) }
                                }
                            }
                        ]
                });

                index++;
            }

            cueFile.Path = Path.Combine(baseFolder, "DUMMY.CUE");

            return cueFile;
        }


    }
}
