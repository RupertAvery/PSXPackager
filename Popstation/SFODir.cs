namespace Popstation
{
    public class SFODir
    {
        public ushort field_offs;
        public byte unk;
        public byte type; // 0x2 -> string, 0x4 -> number
        public uint length;
        public uint size;
        public ushort val_offs;
        public ushort unk4;
    }
}
