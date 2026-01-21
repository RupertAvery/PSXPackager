# PSXPackagerGUI

PSXPackagerGUI is tool for converting PlayStation 1 Disc images to the EBOOT.PBP format and back.

EBOOT.PBP is a Sony Playstation file format for PS1 Classics on the PSP and PS3 Playstation emulators. The format is also supported by PS1 emulators such as ePSXe, PCSX, Beetle PSX (Mednafen/RetroArch) and DuckStation.

# Features

* Convert .BIN+CUE, .IMG to .PBP
* Extact and convert .7z, .RAR directly to .PBP (if only one BIN/CUE/ISO is found inside)
* Auto-merge multi-track discs (games with multiple .BINs named TRACK01,TRACK02...)
* Convert multi-disc games like Final Fantasy 7 into one .PBP
* Extract .PBP files into .BIN+CUE
* Batch convert files to/from .PBP
* PSP file output mode  (SLUSXXXXX\EBOOT.PBP) for batch processing
* Customize PBP with ICON0/ICON1/PIC0/PIC1/SND0 resources (with batch support)
* Extract resources to .PNG/.AT3

# Requirements

PSXPackagerGUI requires the [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).


# Getting Started

When you open PSXPackagerGUI for the first time, you will be asked if you wish to use PSP settings for batch conversion.

Choosing "Yes" will set the Batch output file format to "`%GAMEID%\EBOOT`", meaning that in the output folder, a subfolder named after 
the Game ID will be created, and the output `EBOOT.PBP` will be written there.

Choosing "No" will set the Batch output file format to "`%FILENAME%`", meaning that in the output folder, a file named after the 
input file with the .PBP extension will be written there.

This can be changed at any time in the **Settings** > **Filename Format** tab.

PSXPackagerGUI has two modes, **Single Mode** and **Batch Mode**. The application will open up in **Single Mode**. You can change modes by clicking wither of the first two icons in the top menu.

# Single Conversion Mode

Click on the left-most icon (single package) to enter single conversion mode.

## Create a new PBP

To create a new PBP, click on the New PBP icon. If there are any pending changes, you will be asked to confirm. Confirming will clear any existing changes.

To create a PBP, you need to add one or more Playstation 1 disc images.

Select a disc by clicking the ... icon in the first slot, and select **Load ISO**.

You can select `.BIN`, `.CUE` or `.IMG` files. If your disc has audio tracks, make sure to select the `.CUE` file.

## Customizing the PBP

For users who will be installing the PBP on their PSPs, you have the option to customize the game icon, background picture and animation and sound. Note that animation and sound creation are not part of this tool. You need to find a way to create the `.PMF` and `.AT3` files yourself.  

You will find a list of Resources on the right side of the application.  

|  Resource |  File Type | Description     | Max Resolution |
|-----------|------------|-----------------|----------------| 
|  ICON0    | .PNG       | Main Icon       |   144 x 80     |
|  ICON1    | .PNG,.PMF  | Animated Icon / selected image |   310 x 180    |
|  PIC0     | .PNG       | Information image |   480 x 272    |
|  PIC1     | .PNG       | Background image |   480 x 272    |
|  SND0     | .AT3       | Sound played when selected |       |

Select a resource by clicking ... and selecting **Load Resource** 

If you don't set any of the resources, the default ones will be used.  

## Open an existing PBP file

To extract a disc image or resource from an existing PBP file, click on the **Open PBP** icon.

To extract a disc image, click on ... next to the disc image you want to extract, and select **Save As**.  The file will be extracted as a `.BIN` and `.CUE` file. If the embedded disc image contains audio tracks, they will be included in the `.BIN`, and the `.CUE` will contain the information about the multiple tracks.

To extract a resource, click on ... next to the resource you want to extract and select **Save As**


# Batch Conversion Mode

* Scan a folder for .BIN+CUE, .M3U, .IMG files to convert to PBP
* Scan a folder for .PBP files to convert to .BIN+.CUE,
* Use custom resources from disk when converting to .PBP
* Extract resources from scanned files
* Generate placeholder resource folders from scanned files

Batch Mode allows you to process more than one file at a time.

## Customizing PBPs in Batch Mode

To enable Batch cusomizations of PBPs, go to the Resources tab of the Settings page, and check the option **Use custom resources when batch creating PBPs**.

PSXPackager will check for a matching resource file and use it as the corresponding resource when building a PBP from a PSX disc image.

How does this work?

When PSXPackager processes a disc image and batch customizations are enabled, PSXPackager will search for resource files using the **Match Path** format.

For example, if you have `Final Fantasy VII - Disc 2.bin`, and your Match Path is `%FILENAME%`, PSXPackager will try to look for the following files:

```
Final Fantasy VII - Disc 2.bin         -- input disc image
Final Fantasy VII - Disc 2\ICON0.PNG
Final Fantasy VII - Disc 2\ICON1.PMF
Final Fantasy VII - Disc 2\PIC0.PNG
Final Fantasy VII - Disc 2\PIC1.PNG
Final Fantasy VII - Disc 2\SND.AT3
```

If a file does not exist, it will be ignored, except for ICON0 and PIC1, which will default to base PNGs in the `Resources` folder.

By default, Match Path will look relative to the folder where the input disc image is, but if desired, you can change the `Source Path` in the `Resources` tab to change the location where you want PSXPackager to look for `Match Path`.

## Batch Converting Multi-Disc games into a single PBP

Multi-Disc games such as Final Fantasy VII, Metal Gear Solid have one .bin per *disc*. You can either convert them into separate PBPs, or put them into a Multi-Disc PBP. 

A Multi-Disc PBP simply contains all the discs in one PBP, so you can swap between them in the PSP Pops menu while playing, or using RetroArch.

To create a multi-disc PBP, create a .M3U file with each line containing the filename of the disc image or .CUE file.

```
Final Fantasy VII - Disc 1.bin
Final Fantasy VII - Disc 2.bin
Final Fantasy VII - Disc 3.bin
```

Use Batch Mode to scan for .M3U files. When the .M3U option is checked, PSXPackager will ignore .BINs and .CUEs that are already included in an .M3U.

The resulting .PBP will contain all discs.

# Merge Multi-track .BINs into a single .BIN+.CUE

Some games may come in multi-track format, with one .bin per track. All the tracks belong to one disc.

 Usually the extra tracks are uncompressed audio data, and if you try to create a PBP with only the first (game data) track .bin, it will still work, but it will not have the CD Audio (usually background music).

If you have a game with multiple .bins with different Track numbers, for example, Tomb Raider (USA), and you don't have a .CUE file, you can use the **Merge Tool** in **Settings > Tools** 

```
Tomb Raider (USA) (v1.0) (Track 01).bin
Tomb Raider (USA) (v1.0) (Track 02).bin
Tomb Raider (USA) (v1.0) (Track 03).bin
.
.
.
Tomb Raider (USA) (v1.0) (Track 57).bin
```

The files MUST have the format "Track #" somewhere in the filename, or this will not work.

Click Browse, then select ALL the tracks, and click OK.  Select the output filename where you will save the merged .BIN, and click OK. After a while a single .BIN and .CUE file will be generated.

**NOTE** The generated .CUE file assumes the first track is the DATA track, and all the rest are AUDIO tracks. 

You do not have to merge manually if you already have proper .CUE file that references all the .BINs.

It's still recommended to find a .CUE file for your game in https://github.com/opsxcq/psx-cue-sbi-collection. Note that you may have to edit the .CUE file to match the filenames of your .BIN files.
