namespace Popstation
{
    public class IsoIndex
    {
        public uint Offset { get; set; }
        public uint Length { get; set; }
        public uint[] Dummy { get; set; }

        public IsoIndex()
        {
            Dummy = new uint[6];
        }
    }
}
