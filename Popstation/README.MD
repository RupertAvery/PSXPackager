# Popstation.NET

Popstation.NET is a port of the `popstation-md` C source to C#.

# Features

* Convert .BIN or .IMG to .PBP
* Ectract .IMG file from .PBP
* PBP Compression levels from 1 to 9
* Uses SharpZipLib for compression

# Samples

## Convert BIN/IMG to PBP

```cs
var info = new ConvertIsoInfo()
{
    multiDiscInfo = new MultiDiscInfo()
    {
        fileCount = 1,
        gameID1 = "SLUS01324",
        gameTitle1 = "Breath of Fire IV",
        srcISO1 = @"C:\ROMS\PSX\Breath of Fire IV.bin",
    },
    srcISO = @"C:\ROMS\PSX\Breath of Fire IV.bin",
    dstPBP = @"C:\ROMS\PSX\Breath of Fire IV - TEST.PBP",
    gameTitle = "Breath of Fire IV",
    gameID = "SLUS01324",
    saveTitle = "Breath of Fire IV",
    saveID = "SLUS01324",
    data_psp = @"C:\Play\PSXPackager\Popstation\Resources\DATA.PSP",
    pic0 = @"C:\Play\PSXPackager\Popstation\Resources\PIC0.PNG",
    pic1 = @"C:\Play\PSXPackager\Popstation\Resources\PIC1.PNG",
    icon0 = @"C:\Play\PSXPackager\Popstation\Resources\ICON0.PNG",
    _base = @"C:\Play\PSXPackager\Popstation\Resources\BASE.PBP",
    compLevel = 9
};

var popstation = new Popstation.Popstation();

popstation.Convert(info);
```

```cs
var extractInfo = new ExtractIsoInfo()
{
    dstISO = @"C:\ROMS\PSX\test-test.img",
    srcPBP = @"C:\ROMS\PSX\Breath of Fire IV - TEST.PBP",
};

var popstation = new Popstation.Popstation();

popstation.Extract(extractInfo);
```