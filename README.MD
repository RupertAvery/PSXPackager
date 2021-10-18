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

## Convert a .7z, .BIN, .CUE, .ISO or .IMG to a .PBP

PSXPackager supports several input formats. Simply pass the path to the archive, CUE sheet, or image with the `-i` parameter.

```
psxpackager -i <path_to_file> [-o <output_path>] [-l <compression_level>] [-y]
```

 Since 7z is used for decompression, any format the 7z supports, such as `.rar` or `.zip` can be used as an input, so long as PSXPackager can find an image or CUE sheet within.

The output path is optional. If not specified the path of the input file will be used.

PSXPackager will prompt if a file exists before overwriting it. Use the `-x` argument to overwrite all files in the output directory.

Set the compression level to a value from 0 to 9, with 0 being no compression and 9 being the highest compression level. If not specified, it will default to 5.

Archives will be decompressed to a temporary folder in `%TEMP%\PSXPackager`, and will be cleaned up on exit.

## Extract a .PBP to a .BIN + .CUE

PSXPackager checks the file extension to decide whether to extract or convert, so the syntax remains the same. The output will always be a `.bin` + `.cue`.

Multi-disc PBPs are supported. If no disc is specified, all discs will be extracted. Specify the discs to extract with the `-d` or `--disc` option, which takes a comma-separated list of disc numbers to extract, e.g. `-d 1,2`

```
psxpackager -i <path_to_pbp> [-o <output_path>] [-d <list_of_discs>]
```

## Convert multiple .BIN files to a multi-disc .PBP

Multi-disc PBPs are supported by PSXPackager.

First, create an `.m3u` file containing a list of the discs in the order you wish them to appear in the PBP.

For example, create a text file with the following contents and save as `Final Fantasy VIII.m3u`

```
Final Fantasy VIII - Disc 1.bin
Final Fantasy VIII - Disc 2.bin
Final Fantasy VIII - Disc 3.bin
```

Call `psxpackager` with the `.m3u` as the input file.

```
psxpackager -i "Final Fantasy VIII.m3u" [-o <output_path>]
```

The file  `Final Fantasy VIII.PBP` will be created which contains the three discs in one file.

## Convert multiple .CUE files to a multi-disc .PBP

You can also use `.cue` files in the `.m3u`. If the `.cue` contains a single `.bin` track, it will use the `.bin` as the source image. If the `.cue` file contains mulitple tracks, such as audio tracks, the files will be merged into a single `.bin`. 

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

A PBP also contains 5 resource media files that are used by the PSP/PS3.

* ICON0.PNG - the icon displayed for the game in the XMB
* PIC0.PNG - information about the game
* PIC1.PNG - the background to display when the game is selected in the XMB
* ICON1.PMF - The animation to display in the icon area (optional)
* SND0.AT3 - The sound to play when the game is selected in the XMB (optional)

When creating a PBP, the default PSX2PSP resource files that are embedded in the application are used.

## Extracting resource files

To extract resource files from an existing PBP, use the following option.

```
--extract-resources <gameid|title|filename> 
```

* `gameid` - extract resources into a folder named using the GameID
* `title` - extract resources into a new folder named using the game Title in the game database
* `filename` - extract resources into a new folder named using the input filename 
* none specified - use `filename`

If the filename is `EBOOT.PBP`, the game title will be used.

## Importing resource files

To import resource files when converting to a PBP, use the following option.

```
--import-resources <gameid|title|filename> 
```

```
psxpackager "Final Fantasy VII - Disc1.iso" --import-resources gameid 

SCUS94163
   ICON0.PNG
   ICON1.PMF
   PIC0.PNG
   PIC1.PNG
   SND0.AT3
Final Fantasy VII - Disc1.iso
```

If the corresponding resouce does not exist, the default internal one will be used.


## Generating resource folders

To simplify resource preparation, PSXPackager can generate empty folders that match the GameID, title, or filename.

```
--generate-resource-folder <gameid|title|filename> 
```

If the filename is `EBOOT.PBP`, the game title will be used.

## Resource folders location

The default the location where resource folders will be searched or created is the same path as the ISO or PBP.  You may also specify the location with the following option, to locate the resource folders separate from the ISOs.

```
--resource-folder-location <path> 
```

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