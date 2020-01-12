using Popstation;
using System;

namespace PSXPackager
{
    class Program
    {
        static void Main(string[] args)
        {
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

            var extractInfo = new ExtractIsoInfo()
            {
                dstISO = @"C:\ROMS\PSX\test-test.img",
                srcPBP = @"C:\ROMS\PSX\Breath of Fire IV - TEST.PBP",
            };

            popstation.Extract(extractInfo);
        }
    }
}
