using System;
using System.Collections.Generic;
using System.Linq;

namespace Popstation.Pbp
{
    public class SFOBuilder
    {
        private readonly List<SFOEntry> _entries = new List<SFOEntry>();

        public SFOBuilder() { }

        public SFOBuilder(IEnumerable<SFOEntry> entries)
        {
            _entries.AddRange(entries);
        }


        public void AddEntry(string key, object value)
        {
            _entries.Add(new SFOEntry() { Key = key, Value = value });
        }

        public SFOData Build()
        {
            SFOData sfo = new SFOData();

            sfo.Magic = 0x46535000; // _PSF
            sfo.Version = 0x00000101;
            sfo.Entries = new List<SFODir>();

            var headerSize = 20;
            var indexTableSize = _entries.Count * 16;

            var keyTableSize = _entries.Sum(x => x.Key.Length + 1);

            if (keyTableSize % 4 != 0)
            {
                sfo.Padding = (uint)(4 - keyTableSize % 4);
            }

            sfo.KeyTableOffset = (uint)(headerSize + indexTableSize);

            sfo.DataTableOffset = sfo.KeyTableOffset + (uint)keyTableSize + sfo.Padding;

            ushort keyOffset = 0;
            uint dataOffset = 0;

            foreach (var entry in _entries)
            {
                var entryLength = GetEntryLength(entry.Key, entry.Value);
                var maxLength = GetMaxLength(entry.Key);

                if (entryLength > maxLength)
                {
                    throw new Exception("Value for {entry.Key} exceeds maximum allowed length");
                }

                sfo.Entries.Add(new SFODir()
                {
                    KeyOffset = keyOffset,
                    Format = GetEntryType(entry.Key),
                    Length = entryLength,
                    MaxLength = maxLength,
                    DataOffset = dataOffset,
                    Key = entry.Key,
                    Value = entry.Value,
                });

                dataOffset += maxLength;
                keyOffset += (ushort)(entry.Key.Length + 1);
            }

            sfo.Size = sfo.DataTableOffset + dataOffset;

            return sfo;
        }

        private uint GetMaxLength(string key)
        {
            return key switch
            {
                SFOKeys.BOOTABLE => 4,
                SFOKeys.CATEGORY => 4,
                SFOKeys.DISC_ID => 16,
                SFOKeys.DISC_VERSION => 8,
                SFOKeys.LICENSE => 512,
                SFOKeys.PARENTAL_LEVEL => 4,
                SFOKeys.PSP_SYSTEM_VER => 8,
                SFOKeys.REGION => 4,
                SFOKeys.TITLE => 128,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private ushort GetEntryType(string key)
        {
            const ushort stringType = 0x0204;
            const ushort intType = 0x0404;

            return key switch
            {
                SFOKeys.BOOTABLE => intType,
                SFOKeys.CATEGORY => stringType,
                SFOKeys.DISC_ID => stringType,
                SFOKeys.DISC_VERSION => stringType,
                SFOKeys.LICENSE => stringType,
                SFOKeys.PARENTAL_LEVEL => intType,
                SFOKeys.PSP_SYSTEM_VER => stringType,
                SFOKeys.REGION => intType,
                SFOKeys.TITLE => stringType,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private ushort GetEntryLength(string key, object value)
        {
            // string length + null terminator
            ushort strlen = 0;

            if (value is string s)
            {
                strlen = (ushort)(s.Length + 1);
            }

            return key switch
            {
                SFOKeys.BOOTABLE => 4,
                SFOKeys.CATEGORY => strlen,
                SFOKeys.DISC_ID => strlen,
                SFOKeys.DISC_VERSION => strlen,
                SFOKeys.LICENSE => strlen,
                SFOKeys.PARENTAL_LEVEL => 4,
                SFOKeys.PSP_SYSTEM_VER => strlen,
                SFOKeys.REGION => 4,
                SFOKeys.TITLE => strlen,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

    }
}