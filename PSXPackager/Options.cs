using CommandLine;

namespace PSXPackager
{
    public class Options
    {
        [Option('i', "input", Group = "input", HelpText = "The input file or path to convert. The filename may contain wildcards.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "The output path where the converted file(s) will be written.")]
        public string OutputPath { get; set; }

        [Option('l', "level", Required = false, HelpText = "Set compression level 0-9, default 5.", Default = 5)]
        public int CompressionLevel { get; set; }

        [Option('b', "batch", Group = "input", HelpText = "The path to batch process a set of files.")]
        public string Batch { get; set; }

        [Option('e', "ext", Required = false, HelpText = "The semi-colon or pipe-separated extension(s) of the files to process in the batch folder, e.g. .7z or .iso|.bin|.img")]
        public string Filters { get; set; }
 
        [Option('d', "discs", Required = false, HelpText = "A comma-separated list of disc numbers to extract from a PBP.")]
        public string Discs { get; set; }

        [Option('v', "verbosity", Required = false, Default = 3, HelpText = "Set level of output messages. 1 = Files, Errors and Warnings only, 2 = No Info-level messages, 3 = All messages (default), 4 = Include timestamps")]
        public int Verbosity { get; set; }

        [Option('x', Required = false, HelpText = "If this option is present, will overwrite a file if it exists, otherwise will ask confirmation.")]
        public bool OverwriteIfExists { get; set; }

        [Option('s', "skip", Required = false, HelpText = "If this option is present, will skip existing files.")]
        public bool SkipIfExists { get; set; }

        [Option('f', "format", Required = false, Default = "%FILENAME%", HelpText = "Specify the filename format e.g. [%GAMEID%] [%MAINGAMEID%] %TITLE% (%REGION%) or %FILENAME%")]
        public string FileNameFormat { get; set; }

        [Option('g', "log", Required = false, HelpText = "If this option is present, will log messages to a file.")]
        public bool Log { get; set; }
    }
}
