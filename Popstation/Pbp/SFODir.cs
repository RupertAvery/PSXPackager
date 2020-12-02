namespace Popstation.Pbp
{
    public class SFODir
    {
        public ushort KeyOffset { get; set; }
        public ushort Format { get; set; }
        public uint Length { get; set; }
        public uint MaxLength { get; set; }
        public uint DataOffset { get; set; }
        public string Key { get; set; }
        public object Value { get; set; }
    }
}
