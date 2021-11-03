# Customizing PBPs

A PBP also contains 5 resource media files that are used by the PSP/PS3.

* ICON0.PNG - the icon displayed for the game in the XMB
* PIC0.PNG - information about the game
* PIC1.PNG - the background to display when the game is selected in the XMB
* ICON1.PMF - The animation to display in the icon area (optional)
* SND0.AT3 - The sound to play when the game is selected in the XMB (optional)

When creating a PBP, the default PSX2PSP resource files that are embedded in the application are used.

# Extracting resource files

To extract resource files from an existing PBP, use the following option.  The input file must be a PBP, or a folder with a wildcard returning PBPs.

```
--extract-resources <format> 
```

## Format

```
%FILENAME%   - The input filename
 
%GAMEID%     - The GAMEID of the disc. For multi-disc games, each disc will have a differnt GAMEID. 

%MAINGAMEID% - The GAMEID of the first disc in a multi-disc game.

%TITLE%      - The Disc Title of the game. This will contain the Disc number or other identifier in a mult-disc game.

%MAINTITLE%  - The Main title of the game. This will be the actual title of the game.

%REGION%     - The game region, i.e. NTSC or PAL.

%RESOURCE%   - The resource type. (ICON0, ICON1, PIC0, PIC1, SND0)

%EXT%        - The resource extension. (PNG, AT3)

```

If no format is specified, the default format `%FILENAME%\%RESOURCE%.%EXT%` is used.

e.g.

```
psxpackager "Final Fantasy VII - Disc1.pbp" --extract-resources
```

will extract resources to:

```
Final Fantasy VII - Disc1\ICON0.PNG
Final Fantasy VII - Disc1\ICON1.PMF
Final Fantasy VII - Disc1\PIC0.PNG
Final Fantasy VII - Disc1\PIC1.PNG
Final Fantasy VII - Disc1\SND0.AT3
```

You can also include a path in the format.

# Importing resource files

To import resource files when converting to a PBP, use the following option.

```
--import-resources <format> 
```

e.g.

```
psxpackager "Final Fantasy VII - Disc1.iso" --import-resources "Custom Icons\%RESOURCE%\%FILENAME%.%EXT%"
```

will load resources from:

```
Custom Icons\ICON0\Final Fantasy VII - Disc1.PNG
Custom Icons\ICON1\Final Fantasy VII - Disc1.PMF
Custom Icons\PIC0\Final Fantasy VII - Disc1.PNG
Custom Icons\PIC1\Final Fantasy VII - Disc1.PNG
Custom Icons\SND0\Final Fantasy VII - Disc1.AT3
```

If a required resouce does not exist, the default internal one will be used.  The required resources are ICON0 (the game icon) and PIC1 (the game background).

## Generating resource folders

To simplify resource preparation, PSXPackager can generate empty folders that match the format.  This command is primarily designed to work with wildcard inputs.

```
--generate-resource-folder <format> 
```

e.g.

```
psxpackager "D:\roms\psx" --generate-resource-folder "Custom Icons\%FILENAME%"
```

will generate folders

```
D:\roms\psx\Custom Icons\Final Fantasy VII
D:\roms\psx\Custom Icons\Crash Bandicoot
D:\roms\psx\Custom Icons\Person 2
```

## Resource folders location

The default the location where resource folders will be searched or created is the same path as the ISO or PBP.  You may also specify the location with the following option, to locate the resource folders separate from the ISOs.

```
--resource-folder-location <path> 
```
