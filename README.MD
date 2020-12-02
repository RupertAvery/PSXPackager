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

There are updates to batch conversion, please see the Batch section.

## Convert a .7z, .BIN, .CUE, .ISO or .IMG to a .PBP

PSXPackager supports several input formats. Simply pass the path to the archive, CUE sheet, or image with the `-i` parameter.
```
psxpackager -i <path_to_file> [-o <output_path>] [-l <compression_level>] [-y]
```

 Since 7z is used for decompression, any format the 7z supports, such as `.rar` or `.zip` can be used as an input, so long as PSXPackager can find an image or CUE sheet within.

The output path is optional. If not specified the path of the input file will be used.

PSXPackager will prompt if a file exists before overwriting it. Use the `-y` argument to overwrite all files in the output directory.

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

## Batch Conversion (Old method)

**NOTE:** This option will eventually be removed.

To convert several files in a folder to .PBP

```
psxpackager -b <path_containing_files> -e <extension> [-o <output_path>]
```

The `-e` or `--extension` parameter should be a comma-separated list of extensions you wish to search in the containing folder. 

The following command will convert all .7z and .iso files to .pbp

```
psxpackager -b C:\ROMS\PSX -e .7z,.iso -o C:\ROMS\PSX\Converted
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