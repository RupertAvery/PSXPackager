using Popstation;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PSXPackager
{
    class Program
    {
        static void Main(string[] args)
        {
            var info = new ConvertIsoInfo()
            {
                DiscInfos = new List<DiscInfo>()
                {
                    new DiscInfo()
                    {
                    GameID = "SLUS01324",
                    GameTitle = "Breath of Fire IV",
                    SourceIso = @"C:\ROMS\PSX\Breath of Fire IV.bin",
                    }
                },
                DestinationPbp = @"C:\ROMS\PSX\Breath of Fire IV - TEST.PBP",
                MainGameTitle = "Breath of Fire IV",
                MainGameID = "SLUS01324",
                SaveTitle = "Breath of Fire IV",
                SaveID = "SLUS01324",
                //data_psp = @"C:\Play\PSXPackager\Popstation\Resources\DATA.PSP",
                Pic0 = @"C:\Play\PSXPackager\Popstation\Resources\PIC0.PNG",
                Pic1 = @"C:\Play\PSXPackager\Popstation\Resources\PIC1.PNG",
                Icon0 = @"C:\Play\PSXPackager\Popstation\Resources\ICON0.PNG",
                BasePbp = @"C:\Play\PSXPackager\Popstation\Resources\BASE.PBP",
                CompressionLevel = 9
            };

            var popstation = new Popstation.Popstation();
            popstation.OnEvent = Notify;

            var cancelToken = new CancellationTokenSource();

            popstation.Convert(info, cancelToken.Token).GetAwaiter().GetResult();

            var extractInfo = new ExtractIsoInfo()
            {
                DestinationIso = @"C:\ROMS\PSX\test-test.img",
                SourcePbp = @"C:\ROMS\PSX\Breath of Fire IV - TEST.PBP",
            };

            popstation.Extract(extractInfo, cancelToken.Token).GetAwaiter().GetResult();


        }

        private static void Notify(PopstationEventEnum @event, object value)
        {
            Console.WriteLine(Enum.GetName(typeof(PopstationEventEnum), @event));
            //Console.WriteLine($"ISO Size: {isoSize} ({Math.Round((double)(isoSize / (1024 * 1024)), 2)}MB)");
        }

    }
}
