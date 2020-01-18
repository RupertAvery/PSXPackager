# PSXPackager

PSXPackager is a port of the `popstation-md` C source to C#.

There is no GUI right now, it's just a command line tool to help batch process files. I may add a GUI later.

Feel free to take the Popstation library and use it as you like.

# Features

* Supports direct conversion of .7z files using SevenZipExtractor
* Supports merging multi-track .BIN + .CUE
* Supports writing CUE tracks to PBP TOC (audio tracks now work)
* Supports writing track information to PBP for audio 
* Convert .BIN or .IMG to .PBP
* Extract .BIN file from .PBP
* PBP Compression levels from 0 to 9
* Uses SharpZipLib for compression
* Uses a modified DiscUtil for ISO reading to auto-detect the Disc ID

# Usage

## Convert a .7z, .BIN, .CUE, .IMG to a .PBP

```
psxpackager -i <path_to_file> [-o <output_path>] [-l <compression_level>] 
```

Since the 7zip library is being used, .7z, .rar, and .zip files are supported as inputs. The file only needs to contain a .iso, .img or .bin file.

The output path is optional. If not specified the path of the input file will be used.

Any files in the output path will be overwritten.

Set the compression level to a value from 0 to 9, with 0 being no compression and 9 being the highest compression level. If not specified, it will default to 5.

## Convert a .PBP to a .BIN + .CUE

PSXPackager checks the file extension to decide whether to extract or convert

```
psxpackager -i <path_to_file> [-o <output_path>]
```

The PBP ISO Table of Contents will be saved as a .cue file.

## Batch Conversion

To Convert all files in a folder to .PBP

```
psxpackager -b <path_containing_files> -e <extension> [-o <output_path>]
```

The `extension` parameter should be the extension you wish to search in the containing folder. 

The following command will convert all .7z files to .pbp

```
psxpackager -b C:\ROMS\PSX -e .7z -o C:\ROMS\PSX\Converted
```

# Multi-bin .cue files (Audio tracks)

If the input or compressed file has a .cue with multiple .bin files, PSXPackager will merge the .bin into a single file.

The a new cue sheet will be created with all tracks under the merged .bin, and index positions will be updated.

This merged .cue file will be used to create a TOC (Table of Contents) for the PBP ISO. This allows audio tracks to be read from the PBP.

# Buy me a beer?

Hey programming is fun, but it's also tiring. I mean I could have just been drinking a beer. If you saved yourself a week of converting stuff, why not consider...

* [Buy me a nice beer](https://www.paypal.me/rupertavery/5.00?locale.x=en_US)
* [Buy me a good beer](https://www.paypal.me/rupertavery/2.50?locale.x=en_US)
* [Buy me a decent beer](https://www.paypal.me/rupertavery/1.50?locale.x=en_US)

and I will raise one to you! Much appreciated!

# TODO

* Multi-disc PBP support
* Get DiscID from ISO in PBP
* Add support for external tools - ECM and CHDMAN