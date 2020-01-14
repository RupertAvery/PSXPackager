# PSXPackager

PSXPackager is a port of the `popstation-md` C source to C#.

There is no GUI right now, it's just a command line tool to help batch process files. I may add a GUI later.

Feel free to take the Popstation library and use it as you like.

# Features

* Supports direct conversion of .7z files using SevenZipExtractor  
* Supports joining multi-track images (multiple bin files) 
* Convert .BIN or .IMG to .PBP
* Extract .IMG file from .PBP
* PBP Compression levels from 0 to 9
* Uses SharpZipLib for compression
* Uses a modified DiscUtil for ISO reading to auto-detect the Disc ID

# Usage

## Convert a .7z, .BIN, .IMG or .ISO to a .PBP

```
psxpackager -i <path_to_file> -o <output_path> [-l <compression_level>] 
```

Since the 7zip library is being used, .7z, .rar, and .zip files are supported as inputs. The file only needs to contain a .iso, .img or .bin file.

If the compressed file has multiple .bin files, PSXPackager will look for a .cue file and merge them into a single .bin file.

Set the compression level to a value from 0 to 9, with 0 being no compression and 9 being the highest compression level.

## Convert a .PBP to a .BIN

PSXPackager checks the file extension to decide whether to extract or convert

```
psxpackager -i <path_to_file> -o <output_path>
```

## Convert all .7z files in a folder to .PBP

```
psxpackager -b <path_containing_files> -e .7z -o <output_path>
```

# TODO

* Multi-disc PBP support
* Get DiscID from ISO in PBP
 