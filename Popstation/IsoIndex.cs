namespace Popstation
{
    public class IsoIndex
    {
        public uint offset;
        public uint length;
        public uint[] dummy;

        public IsoIndex()
        {
            dummy = new uint[6];
        }
    }
}
