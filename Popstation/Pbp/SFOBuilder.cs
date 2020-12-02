using System;
using System.Collections.Generic;
using System.Linq;

namespace Popstation.Pbp
{
    public class SFOBuilder
    {
        private List<SFOEntry> entries = new List<SFOEntry>();

        public void AddEntry(string key, object value)
        {
            entries.Add(new SFOEntry() { Key = key, Value = value });
        }

        public SFOData Build()
        {
            SFOData sfo = new SFOData();

            sfo.Magic = 0x46535000; // _PSF
            sfo.Version = 0x00000101;
            sfo.Entries = new List<SFODir>();

            var headerSize = 20;
            var indexTableSize = entries.Count * 16;

            var keyTableSize = entries.Sum(x => x.Key.Length + 1);

            if (keyTableSize % 4 != 0)
            {
                sfo.Padding = (uint)(4 - keyTableSize % 4);
            }

            sfo.KeyTableOffset = (uint)(headerSize + indexTableSize);

            sfo.DataTableOffset = sfo.KeyTableOffset + (uint)keyTableSize + sfo.Padding;

            ushort keyOffset = 0;
            uint dataOffset = 0;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
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
                keyOffset += (ushort)(entries[i].Key.Length + 1);
            }

            sfo.Size = sfo.DataTableOffset + dataOffset;

            return sfo;
        }

        private uint GetMaxLength(string key)
        {
            switch (key)
            {
                case SFOKeys.BOOTABLE:
                    return 4;
                case SFOKeys.CATEGORY:
                    return 4;
                case SFOKeys.DISC_ID:
                    return 16;
                case SFOKeys.DISC_VERSION:
                    return 8;
                case SFOKeys.LICENSE:
                    return 512;
                case SFOKeys.PARENTAL_LEVEL:
                    return 4;
                case SFOKeys.PSP_SYSTEM_VER:
                    return 8;
                case SFOKeys.REGION:
                    return 4;
                case SFOKeys.TITLE:
                    return 128;
            }
            throw new ArgumentOutOfRangeException();
        }

        private ushort GetEntryType(string key)
        {
            const ushort stringType = 0x0204;
            const ushort intType = 0x0404;

            switch (key)
            {
                case SFOKeys.BOOTABLE:
                    return intType;
                case SFOKeys.CATEGORY:
                    return stringType;
                case SFOKeys.DISC_ID:
                    return stringType;
                case SFOKeys.DISC_VERSION:
                    return stringType;
                case SFOKeys.LICENSE:
                    return stringType;
                case SFOKeys.PARENTAL_LEVEL:
                    return intType;
                case SFOKeys.PSP_SYSTEM_VER:
                    return stringType;
                case SFOKeys.REGION:
                    return intType;
                case SFOKeys.TITLE:
                    return stringType;
            }
            throw new ArgumentOutOfRangeException();
        }

        private ushort GetEntryLength(string key, object value)
        {
            // string length + null terminator
            ushort strlen = 0;

            if (value.GetType() == typeof(string))
            {
                strlen = (ushort)(((string)value).Length + 1);
            }

            switch (key)
            {
                case SFOKeys.BOOTABLE:
                    return 4;
                case SFOKeys.CATEGORY:
                    return strlen;
                case SFOKeys.DISC_ID:
                    return strlen;
                case SFOKeys.DISC_VERSION:
                    return strlen;
                case SFOKeys.LICENSE:
                    return strlen;
                case SFOKeys.PARENTAL_LEVEL:
                    return 4;
                case SFOKeys.PSP_SYSTEM_VER:
                    return strlen;
                case SFOKeys.REGION:
                    return 4;
                case SFOKeys.TITLE:
                    return strlen;
            }
            throw new ArgumentOutOfRangeException();
        }

    }
}