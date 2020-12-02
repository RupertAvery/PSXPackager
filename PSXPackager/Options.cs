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

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('y', "yes", Required = false, HelpText = "If specified, will overwrite a file if it exists, otherwise will ask confirmation.")]
        public bool OverwriteIfExists { get; set; }
    }
}
