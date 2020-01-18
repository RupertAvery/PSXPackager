using CommandLine;

namespace PSXPackager
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('l', "level", Required = false, HelpText = "Set compression level 0-9, default 5", Default = 5)]
        public int CompressionLevel { get; set; }

        [Option('o', "output", Required = false
            , HelpText = "The output path where the converted file will be written")]
        public string OutputPath { get; set; }

        [Option('i', "input", Group = "input", HelpText = "The input file to convert")]
        public string InputPath { get; set; }

        [Option('b', "batch", Group = "input", HelpText = "The path to batch process a set of files")]
        public string Batch { get; set; }

        [Option('e', "ext", Required = false, HelpText = "The extension of the files to process in the batch folder, e.g. .7z")]
        public string BatchExtension { get; set; }

    }
}
