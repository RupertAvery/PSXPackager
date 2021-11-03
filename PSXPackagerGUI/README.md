# PSXPackagerGUI

A GUI for PSXPacakger

# Requirements

PSXPackagerGUI requires .NET Core 3.1.

Download the **.NET Desktop Runtime** for 3.1 from https://dotnet.microsoft.com/download/dotnet/3.1

# Single Mode

In **Single Mode**, you can:

* Open an existing PBP file
* Create a new PBP
* Extract a .BIN+.CUE from one of the five disc slots
* Load a .BIN/.CUE, .ISO or .IMG into a disc slot
* Remove a disc image from a disc slot.

You can also customize a PBP: 

* Extract a .PNG into the ICON0, ICON1, PIC0, PIC1, or a .AT3 into the SND0 resource slots
* Load a .PNG, .PMF or .AT3 into the appropriate resource slot
* Remove a resource from a resource slot

# Batch Mode

In **Batch Mode** you can:

* Scan a folder for .BIN/.CUE, .M3U, .ISO or .IMG files to convert to PBP
* Scan a folder for .PBP files to convert to .BIN+.CUE,
* Use custom resources from disk when converting to .PBP
* Extract resources from scanned files
* Generate placeholder resource folders from scanned files

Batch Mode allows you to process more than one file at a time.

# Customizing PBPs in Batch Mode

To enable Batch cusomizations of PBPs, go to the Resources tab of the Settings page, and check the option **Use custom resources when batch creating PBPs**.

PSXPackager will check for a matching resource file and use it as the corresponding resource when building a PBP from a PSX disc image.

How does this work?

When PSXPackager processes a disc image and batch customizations are enabled, PSXPackager will search for resource files using the **Match Path** format.

For example, if you have `Final Fanatsy VII - Disc 2.bin`, and your Match Path is `%FILENAME%\%RESOURCE%.%EXT%`, PSXPackager will try to look for the following files:

```
Final Fanatsy VII - Disc 2.bin         -- input disc image
Final Fanatsy VII - Disc 2\ICON0.PNG
Final Fanatsy VII - Disc 2\ICON1.PMF
Final Fanatsy VII - Disc 2\PIC0.PNG
Final Fanatsy VII - Disc 2\PIC1.PNG
Final Fanatsy VII - Disc 2\SND.AT3
```

If a file does not exist, it will be ignored, except for ICON0 and PIC1, which will default to base PNGs in the `Resources` folder.

By default, Match Path will look relative to the folder where the input disc image is, but if desired, you can change the `Source Path` in the `Resources` tab to change the location where you want PSXPackager to look for `Match Path`.

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

# Multi-Disc games in a single PBP

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