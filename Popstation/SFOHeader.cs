namespace Popstation
{
    public class SFOHeader
    {
        public uint signature;
        public uint version;
        public uint fields_table_offs;
        public uint values_table_offs;
        public int nitems;
    }
}
