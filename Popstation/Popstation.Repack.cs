using Popstation.Pbp;
using System.IO;
using System.Threading;

namespace Popstation
{

    public partial class Popstation
    {

        public void Repack(ExtractOptions options, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(options.SourcePbp, FileMode.Open, FileAccess.Read))
            {

                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                try
                {
                    var pbpStreamReader = new PbpStreamReader(stream);


                    ExtractResource(pbpStreamReader.GetResourceStream(ResourceType.SFO, stream), path, "PARAM.SFO");
                    ExtractResource(pbpStreamReader.GetResourceStream(ResourceType.ICON0, stream), path, "ICON0.PNG");
                    ExtractResource(pbpStreamReader.GetResourceStream(ResourceType.ICON1, stream), path, "ICON1.PMF");
                    ExtractResource(pbpStreamReader.GetResourceStream(ResourceType.PIC0, stream), path, "PIC0.PNG");
                    ExtractResource(pbpStreamReader.GetResourceStream(ResourceType.PIC1, stream), path, "PIC1.PNG");
                    ExtractResource(pbpStreamReader.GetResourceStream(ResourceType.SND0, stream), path, "SND0.AT3");
                    ExtractResource(pbpStreamReader.GetResourceStream(ResourceType.PSP, stream), path, "DATA.PSP");
                    ExtractResource(pbpStreamReader.GetResourceStream(ResourceType.PSAR, stream), path, "DATA.PSAR");



                }
                catch
                {
                    File.Delete(Path.Combine(path, "PARAM.SFO"));
                    File.Delete(Path.Combine(path, "ICON0.PNG"));
                    File.Delete(Path.Combine(path, "ICON1.PMF"));
                    File.Delete(Path.Combine(path, "PIC0.PNG"));
                    File.Delete(Path.Combine(path, "PIC1.PNG"));
                    File.Delete(Path.Combine(path, "SND0.AT3"));
                    File.Delete(Path.Combine(path, "DATA.PSP"));
                    File.Delete(Path.Combine(path, "DATA.PSAR"));
                }
            }
        }
    }
}
