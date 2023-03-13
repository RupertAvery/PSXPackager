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

        [Option('r', "recursive", HelpText = "Recurse subdirectories")]
        public bool Recursive { get; set; }

        [Option('d', "discs", Required = false, HelpText = "A comma-separated list of disc numbers to extract from a PBP.")]
        public string Discs { get; set; }

        [Option('v', "verbosity", Required = false, Default = 3, HelpText = "Set level of output messages. 1 = Files, Errors and Warnings only, 2 = No Info-level messages, 3 = All messages (default), 4 = Include timestamps")]
        public int Verbosity { get; set; }

        [Option('x', Required = false, HelpText = "If specified, overwrite a file if it exists, otherwise ask confirmation.")]
        public bool OverwriteIfExists { get; set; }

        [Option('s', "skip", Required = false, HelpText = "If specified, will skip existing files.")]
        public bool SkipIfExists { get; set; }

        [Option('f', "format", Required = false, Default = "%FILENAME%", HelpText = "Specify the filename format e.g. [%GAMEID%] [%MAINGAMEID%] %TITLE% (%REGION%) or %FILENAME%")]
        public string FileNameFormat { get; set; }

        [Option('g', "log", Required = false, HelpText = "If specified, log messages to a file.")]
        public bool Log { get; set; }

        [Option("extract", Required = false, HelpText = "If specified, extract resources using the path specified by resource-format. See README for more details.")]
        public bool ExtractResources { get; set; }

        [Option("import", Required = false,  HelpText = "If specified, import resources using the path specified by resource-format. See README for more details.")]
        public bool ImportResources { get; set; }

        [Option("generate", Required = false, HelpText = "If specified, create empty resources folder specified by resource-format. See README for more details.")]
        public bool GenerateResourceFolders { get; set; }
        
        [Option("resource-format", Required = false, HelpText = "The format to use with extract/import/generate. See README for more details.")]
        public string ResourceFormat { get; set; }

        [Option("resource-root", Required = false, HelpText = "The path where resource folders will be located. If not specified, the path will be the same as the input file")]
        public string ResourceRoot { get; set; }

    }
}
