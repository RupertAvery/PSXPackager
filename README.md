# PSXPackager

PSXPackager is tool for converting PlayStation 1 Disc images to the EBOOT.PBP format and back.

EBOOT.PBP is a Sony Playstation file format for PS1 Classics on the PSP and PS3 Playstation emulators. The format is also supported by PS1 emulators such as ePSXe, PCSX, Beetle PSX (Mednafen/RetroArch) and DuckStation.

There is a command-line executable (Windows/Linux/OSX) and a Windows-only GUI available.

The GUI allows you to select and process several images in a queue (Batch mode).

Feel free to take the Popstation library and use it as you like.

The Popstation library was ported from the `popstation-md` C source code to C#.

Looking for the Windows-only GUI README? See [PSXPackagerGUI](PSXPackagerGUI/README.md)

# Download

* [Latest Release](https://github.com/RupertAvery/PSXPackager/releases)

Not sure what to download?

* command line tool - psxpackager-win-x64.zip
* Windows GUI - PsxPackagerGUI.zip

# Features

* Convert .BIN + .CUE or .IMG to .PBP
* Extract .PBP to .BIN + .CUE file 
* Supports conversion of .7z, .zip, .rar files to .PBP
* Supports merging of single-disc, multi-track .BIN + .CUE into one .BIN + .CUE
* Supports writing multi-track CUE information to PBP for audio 
* PBP Compression levels from 0 to 9
* Supports writing multi-disc PBP using .m3u files
* Supports extracting multi-disc PBP
* Windows/Linux/OSX CLI
* GUI with PSX2PSP-like interface and batch processing

# Requirements

PSXPackager requires the [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).

# Usage

The basic usage of PSXPackager accepts a file or path to convert.

```
psxpackager -i <input_path_or_file> [-o <output_path_or_file>]
```

The `-i` or `input` parameter is required. It specifies a path to a file, or a directory and a wildcard expression. If a wildcard expression is used, it will process all matching files in the directory.

The application will assume that you want to convert any image to a PBP, and any PBP to a .BIN + .CUE.

The command:

```
psxpackager -i "Final Fantasy VII - Disc 1.cue"
```

will process `Final Fantasy VII - Disc 1.bin` and `Final Fantasy VII - Disc 1.cue` and output `Final Fantasy VII - Disc 1.pbp`.


The `-o` or `output` parameter is optional. It specifies the folder where the the converted or extracted files will be placed. If not specified, the folder specified on the input is ued.

If the input is a file and the output is also a file, the output will be renamed, instead of reusing the input filename.

## Options

```
  -i, --input                (Group: input) The input file or path to convert. The filename may contain wildcards.

  -o, --output               The output path where the converted file(s) will be written.

  -l, --level                (Default: 5) Set compression level 0-9, default 5.

  -r, --recursive            Recurse subdirectories

  -d, --discs                A comma-separated list of disc numbers to extract from a PBP.

  -v, --verbosity            (Default: 3) Set level of output messages. 1 = Files, Errors and Warnings only, 2 = No
                             Info-level messages, 3 = All messages (default), 4 = Include timestamps

  -x                         If specified, overwrite a file if it exists, otherwise ask confirmation.

  -s, --skip                 If specified, will skip existing files.

  -f, --format               (Default: %FILENAME%) Specify the filename format e.g. [%GAMEID%] [%MAINGAMEID%] %TITLE%
                             (%REGION%) or %FILENAME%

  -g, --log                  If specified, log messages to a file.

  --extract                  If specified, extract resources using the path specified by resource-format. See README for
                             more details.

  --import                   If specified, import resources using the path specified by resource-format. See README for
                             more details.

  --generate                 If specified, create empty resources folder specified by resource-format. See README for
                             more details.

  --resource-format          The format to use with extract/import/generate. See README for more details.

  --resource-root            The path where resource folders will be located. If not specified, the path will be the
                             same as the input file

  --help                     Display this help screen.

  --version                  Display version information.
```

## Convert a .BIN, .CUE, .ISO, .IMG or archive to a .PBP

PSXPackager supports several input formats. Simply pass the path to the archive, CUE sheet, or image with the `-i` parameter.

```
psxpackager -i <path_to_file> [-o <output_path>] [-l <compression_level>] [-y]
```

The output path is optional. If not specified, the path of the input file will be used.

PSXPackager will prompt if a file exists before overwriting it. Use the `-x` argument to overwrite all files in the output directory.

Set the compression level to a value from 0 to 9, with 0 being no compression and 9 being the highest compression level. If not specified, it will default to 5.

PSXPackager can automatically extract files from compressed files (archives) anc convert the contents into a PBP. The following formats are supported:

* .zip
* .7z
* .rar
* .tar
* .gz

Archives will be decompressed to a temporary folder in `%TEMP%\PSXPackager`, and will be cleaned up on exit.

## Extract a .PBP to a .BIN + .CUE

PSXPackager checks the file extension to decide whether to extract or convert, so the syntax remains the same. The output will always be a `.bin` + `.cue`.

Extracting from Multi-disc PBPs are supported. Specify the discs to extract with the `-d` or `--disc` option, which takes a comma-separated list of disc numbers to extract, e.g. `-d 1,2`. If this option is not specified, all discs will be extracted.

```
psxpackager -i <path_to_pbp> [-o <output_path>] [-d <list_of_discs>]
```

## Filename formatting

Use the `-f` or `format` option to specify the format of the output filename based on the information found on the disc (Sony GameID) and the `gameInfo.db` file.

This allows you to generate standardized output filenames that can include the GameID e.g SCUS-94163, the region (NTSC/PAL) and the game name.  

By default, it will use `%FILENAME%`, i.e. the input filename as the output filename.

See [Formatting](#formatting) for more info.

## Merging Multi-Track Games

Some games such as Tomb Raider have additional digital audio tracks on the CD, for use as background music.

These audio tracks are usually extracted by disc rippers as separate `.BIN` files. They usually are named `<Game Name> - Track nn.bin` or similar, where `nn` is a 2-digit number. They usually come with a `.CUE` file which is an index for all the related `.BIN` files.

Usually you would need to use CDMage or some other tool to merge multi-track discs.

With PSXPackager, all you need to do is pass in the `.CUE` file as the input file and it will automatically merge multi-track discs before converting to PBP as long as  

The resulting PBP file will contain the audio tracks as disc tracks and you will be able to play your game on your PSP or emulator with music.

## Creating Multi-disc PBPs

The PBP format can contain more than one disc, and this is supported by PSXPackager. This allows you to merge a multi-disc game such as Final Fantasy VII (3 discs) into a single PBP. 

To do this from the command line, first create an `.m3u` file containing a list of the discs in the order you wish them to appear in the PBP.

For example, create a text file with the following contents and save as `Final Fantasy VIII.m3u`

```
Final Fantasy VIII - Disc 1.cue
Final Fantasy VIII - Disc 2.cue
Final Fantasy VIII - Disc 3.cue
```

Call `psxpackager` with the `.m3u` as the input file.

```
psxpackager -i "Final Fantasy VIII.m3u" [-o <output_path>]
```

The file  `Final Fantasy VIII.PBP` will be created which contains the three discs in one file.

## Batch Conversion

Wildcards are now supported using the `-i` argument

```
psxpackager -i <path_containing_files>\<wildcard_filter> [-o <output_path>]
```

This will process all supported files in the folder `C:\Roms`. Use with caution, as you may be overwriting files that exist.

```
psxpackager -i "C:\Roms\*.*"
```

This will extract all PBP files in the folder `C:\Roms` to BIN+CUE.

```
psxpackager -i "C:\Roms\*.PBP"
```

This will convert all files matching `Legend of Dragoon - Disc ?.bin` in the folder `C:\Roms` to PBP.

```
psxpackager -i "C:\Roms\Legend of Dragoon - Disc ?.bin"
```

If you want faster batch conversion, use the GUI, as it supports multi-threaded processing.

# Customizing PBPs with Resources

Resource files are PBP-specific embedded resources that are normally used by the PSP, PSVita or PS3 to display an image or play audio on the XMB when the game is selected. These files usually named ICON0.PNG, PIC0.PNG, PIC1.PNG and SND0.AT3.

PSXPackager can extract (`--extract`) or embed (`--import`) resource files for a single conversion or batch conversion. To do the latter, the resource files must be located in a specific location.

There are two options that let you set how and where the resource files are found.

The `--resource-format` option lets you match a folder based on filename, gameid and other formats. See [Formatting](#formatting). omitting this option defaults the format to `%FILENAME%`, meaning the input filename without the extension will be used.

The `--resource-root` option sets the root path of your resource folders. This allows you to separate your disc images from your resource folders.  You can omit this option to use the same folder as the input file as the root path of your resource folder.

## Extract Resources

Extracts resources from a PBP.

```
psxpackager -i <path_to_file> --extract [--resource-format <resource format>] [--resource-root <resource root folder>] [-o <output_path>]
```

`--resource-format` - the format of the folder name to extract the embedded resources to. See [Formatting](#formatting).  If not specified, the default value will be `%FILENAME%\%RESOURCE%.%EXT%`

`--resource-root` - the root folder where to place the resource folders. Do not specify to place the resource folder next to the input file.

For example, if you extract the resources from a `Final Fantasy VII (DISC 1).PBP` file using the commande below:

```
psxpackager -i "Final Fantasy VII (DISC 1).PBP" --extract --resource-path %GAMEID%\%RESOURCE%.%EXT%
```

You would have extracted the following files:

```
SCUS-94163\ICON0.PNG
SCUS-94163\PIC0.PNG
SCUS-94163\PIC1.PNG
```

## Import Resources

Imports custom resources into PBP.

```
psxpackager -i <path_to_file> --import [--resource-format <resource format>] [--resource-folder <resource root folder>] [-o <output_path>]
```

`--resource-format` - the format of the folder name to import the embedded resources from. See [Formatting](#formatting).  If not specified, the default value will be `%FILENAME%\%RESOURCE%.%EXT%`

`--resource-root` - the root folder where to start searching for the resource folders. Do not specify to look for the resource folder next to the input file.

For example, if you want to build a PBP from a BIN+CUE file and you have the resources next to the file in a folder such as below:

```
Final Fantasy VII (Disc-1).bin
Final Fantasy VII (Disc-1).cue
SCUS-94163\ICON0.PNG
SCUS-94163\PIC0.PNG
SCUS-94163\PIC1.PNG
```

You can build your PBP using the following command:

```
psxpackager -i "Final Fantasy VII (Disc-1).cue" --import --resource-path %GAMEID%\%RESOURCE%.%EXT%
```

## Generate Resource Folders

This option is provided for the following scenario:

You have a bunch of images you want to convert to PBP, and you want to customize all of them.  To do this, you will need to generate resource folders, where you will later place the icons and backgrounds you want.

This option will create empty folders using the specified format.

```
psxpackager -i <path_to_file> --generate [--resource-format <resource format>] [--resource-folder <resource root folder>]
```

`--resource-format` - the format of the folder name to generate. See [Formatting](#formatting).  If not specified, the default value will be `%FILENAME%`

For example, this will generate the empty folder `SCUS-94163`:

```
psxpackager -i "Final Fantasy VII (Disc-1).cue" --generate --resource-path %GAMEID%
```

# Formatting

```
%FILENAME%   - The input filename
 
%GAMEID%     - The GAMEID of the disc. For multi-disc games, each disc will have a differnt GAMEID. 

%MAINGAMEID% - The GAMEID of the first disc in a multi-disc game.

%TITLE%      - The Disc Title of the game. This will contain the Disc number or other identifier in a mult-disc game.

%MAINTITLE%  - The Main title of the game. This will be the actual title of the game.

%REGION%     - The game region, i.e. NTSC or PAL.

%RESOURCE%   - The resource type. (ICON0, ICON1, PIC0, PIC1, SND0). Only used for resource files

%EXT%        - The resource extension. (PNG, AT3). Only used for resource files

```

For example, processing `Final Fantasy VIII - Disc 1.iso` with the following format:

```
[%GAMEID%] %TITLE% (%REGION%)
```

will generate the filename

```
[SLUS00892] Final Fantasy VIII - Disc 1 (NTSC).pbp
```

Note that when extracting from a multi-disc PBP PSXPackager will append the disc number to the file format.


# FAQ

### Does PSXPackager support audio tracks? 

Yes, but only as the original audio (no compression).

### How can I convert a game with audio tracks into a PBP? 

If you have a `.CUE` file with multiple `.BIN` files, pass the `.CUE` file as the input file and PSXPackager will automatically merge the `.BIN` files into a single file in a temporary folder.

A new CUE sheet will also be created with all tracks under the merged `.BIN`, and index positions will be updated.

This merged `.CUE` file will be used to create a TOC (Table of Contents) for the PBP ISO. This allows audio tracks to be correctly read from the PBP.

Temporary files will be deleted when conversion is complete, if the conversion is cancelled, or if an error occurs.

The temporary folder location is `%TEMP%\PSXPackager`.

# Buy me a beer?

Hey programming is fun, but it's also tiring. I mean I could have just been drinking a beer. If you saved yourself a week of converting stuff, why not consider...

* [Buy me a nice beer](https://www.paypal.me/rupertavery/5.00?locale.x=en_US)
* [Buy me a good beer](https://www.paypal.me/rupertavery/2.50?locale.x=en_US)
* [Buy me a decent beer](https://www.paypal.me/rupertavery/1.50?locale.x=en_US)

and I will raise one to you! Much appreciated!
