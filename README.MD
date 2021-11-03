# PSXPackager

PSXPackager is a port of the `popstation-md` C source to C#.

There is no GUI right now, it's just a command line tool to help batch process files. I may add a GUI later.

Feel free to take the Popstation library and use it as you like.

# Features

* Supports direct conversion of .7z files using SevenZipExtractor
* Supports merging multi-track .BIN + .CUE
* Supports writing multi-track CUE information to PBP for audio 
* Convert .BIN or .IMG to .PBP
* Extract .BIN file from .PBP
* PBP Compression levels from 0 to 9
* Uses SharpZipLib for compression
* Supports writing multi-disc PBP using .m3u files
* Supports extracting multi-disc PBP

# Usage

The basic usage of PSXPackager accepts the file or path to convert.

```
psxpackager -i <path_to_file>
```

The `-i` or `input` parameter is required. It specifies a path to a file, or a directory and a wildcard expression. If a wildcard expression is used, it will process all matching files in the directory.

The `-o` or `output` parameter is optional. It specifies the folder where the the converted or extracted files will be placed. If not specified, the folder specified on the input is ued.


```
  -i, --input        The input file or path to convert. The filename may contain wildcards.

  -o, --output       The output path where the converted file(s) will be written.

  -l, --level        (Default: 5) Set compression level 0-9, default 5.

  -d, --discs        A comma-separated list of disc numbers to extract from a PBP.

  -v, --verbosity    (Default: 3) Set level of output messages. 1 = Files, Errors and Warnings only, 2 = No Info-level
                     messages, 3 = All messages (default), 4 = Include timestamps

  -x                 If this option is present, will overwrite a file if it exists, otherwise will ask confirmation.

  -f, --format       (Default: %FILENAME%) Specify the filename format e.g. [%GAMEID%] [%MAINGAMEID%] %TITLE% (%REGION%)
                     or %FILENAME%

  -g, --log          If this option is present, will log messages to a file.

  --help             Display this help screen.

  --version          Display version information.

```

## Convert a .BIN, .CUE, .ISO, .IMG or .7z to a .PBP

PSXPackager supports several input formats. Simply pass the path to the archive, CUE sheet, or image with the `-i` parameter.

```
psxpackager -i <path_to_file> [-o <output_path>] [-l <compression_level>] [-y]
```

Since 7z is used for decompression, any format the 7z supports, such as `.rar` or `.zip` can be used as an input, so long as PSXPackager can find an image or CUE sheet within the archive.

The output path is optional. If not specified, the path of the input file will be used.

PSXPackager will prompt if a file exists before overwriting it. Use the `-x` argument to overwrite all files in the output directory.

Set the compression level to a value from 0 to 9, with 0 being no compression and 9 being the highest compression level. If not specified, it will default to 5.

Archives will be decompressed to a temporary folder in `%TEMP%\PSXPackager`, and will be cleaned up on exit.

## Extract a .PBP to a .BIN + .CUE

PSXPackager checks the file extension to decide whether to extract or convert, so the syntax remains the same. The output will always be a `.bin` + `.cue`.

Extracting from Multi-disc PBPs are supported. Specify the discs to extract with the `-d` or `--disc` option, which takes a comma-separated list of disc numbers to extract, e.g. `-d 1,2`. If this option is not specified, all discs will be extracted.

```
psxpackager -i <path_to_pbp> [-o <output_path>] [-d <list_of_discs>]
```

## Merging Multi-Track Games

Some games such as Tomb Raider 

## Creating Multi-disc PBPs

PBPs containing more than one disc are supported by PSXPackager. This allows you to compress a multi-disc game such as Final Fantasy VII (3 discs) into a single PBP. 

First, create an `.m3u` file containing a list of the discs in the order you wish them to appear in the PBP.

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

## Filename Formatting

Use the `-f` or `format` option to specify the format of the output filename. By default, it will use the input filename as the output filename.

```
%FILENAME%   - The input filename
 
%GAMEID%     - The GAMEID of the disc. For multi-disc games, each disc will have a differnt GAMEID. 

%MAINGAMEID% - The GAMEID of the first disc in a multi-disc game.

%TITLE%      - The Disc Title of the game. This will contain the Disc number or other identifier in a mult-disc game.

%MAINTITLE%  - The Main title of the game. This will be the actual title of the game.

%REGION%     - The game region, i.e. NTSC or PAL.
```

For example, processing `Final Fantasy VIII - Disc 1.iso` with the following format:

```
[%GAMEID%] %TITLE (%REGION)
```

will generate the filename

```
[SLUS00892] Final Fantasy VIII - Disc 1 (NTSC).pbp
```

Note that when extracting from a multi-disc PBP PSXPackager will append the disc number to the file format.

# Customizing PBPs

See [Customizing PBPs](docs/Customizing.md)

# Multi-track .CUE files (Audio tracks)

If the input or compressed file has a `.cue` with multiple tracks, PSXPackager will merge the `.bins` into a single file in a temporary folder.

A new CUE sheet will also be created with all tracks under the merged `.bin`, and index positions will be updated.

This merged `.cue` file will be used to create a TOC (Table of Contents) for the PBP ISO. This allows audio tracks to be correctly read from the PBP.

Temporary files will be deleted when conversion is complete, if the conversion is cancelled, or if an error occurs.

The temporary folder location is `%TEMP%\PSXPackager`.

# Buy me a beer?

Hey programming is fun, but it's also tiring. I mean I could have just been drinking a beer. If you saved yourself a week of converting stuff, why not consider...

* [Buy me a nice beer](https://www.paypal.me/rupertavery/5.00?locale.x=en_US)
* [Buy me a good beer](https://www.paypal.me/rupertavery/2.50?locale.x=en_US)
* [Buy me a decent beer](https://www.paypal.me/rupertavery/1.50?locale.x=en_US)

and I will raise one to you! Much appreciated!

# TODO

* Get DiscID from ISO in PBP
* Add support for external tools - ECM and CHDMAN