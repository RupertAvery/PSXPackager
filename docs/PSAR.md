# PSAR File Format

*This originally came from: http://endlessparadigm.com/forum/showthread.php?tid=14. This has been reformatted for markdown and slightly reworded.*

This only explores the PSAR section of the EBOOT.  The rest of the EBOOT is the same as standard EBOOTs so see the PBP File structure guide for more info.  Also, this refers to the decrypted EBOOT format used in the 3.0x OE popstation, not offical Sony EBOOTs.

This only focuses on the PSAR section of the EBOOT for PSX EBOOTs.  For 3.02 OE-B EBOOTs, the PSAR section had to go at 0x50000 (according to Dark_AleX - I've heard reports that the start can go elsewhere).  Now the PSAR section isn't fixed, starting from 3.03 OE-A.

# Contents

* PSAR Section 1
* PSAR Section 2
* TOC (Table of Contents)
* ISO Indexes

The PSAR is divided into two parts.

# PSAR Section 1

First part contains the actual ISO:

 Offset  | Type/Size  | Purpose                                                               
---------|------------|----------------------------------------------------------------------
 0x00	 | 12 bytes   | PSAR Signature - always is the string "`PSISOIMG0000`"                 
 0x1B	 | DWORD	  | Offset to the second part                                            
 0x400	 | 11 bytes   |	Game ID in the format "\_????\_#####" (?=character, #=digit)
 0x800	 |            | Start of TOC information (see TOC for more info)
 0x122C  | 128 bytes  | Game Save Title (if the text is too short, the remaining bytes are null)
 0x4000  |            | Start of ISO Indexes (see ISO Indexes for more info)
 0x10000 |            |	Beginning of ISO - note the ISO may be padded with null characters

# PSAR Section 2

The second part of the PSAR immediately follows the ISO:

Offset	| Type/Size  | Purpose
--------|------------|----------------------------------------------------------------------
0x00	| 8 bytes	 | The string "`STARTDAT`"
0x10	| DWORD      | Size of the "STARTDAT" header in bytes (always seems to be 0x50)
0x14	| DWORD      | The size of the gameboot PNG, in bytes

This header section is padded with nulls to satisfy the "STARTDAT header" value.  Following this section, is the gameboot PNG file.  Following the PNG is a "PGD" - possibly something to do with the Documentation???

# TOC (Table of Contents)

The PSP's pops emulator uses the standard TOC format.  The TOC describes track information - some games use CD-DA audio, meaning that there's one data track, and multiple audio tracks on the disc.

Here's an example:

```
00020800h: 41 00 A0 00 00 00 00 01 20 00 01 00 A1 00 00 00 ; A. ..... ...¡...
00020810h: 00 15 00 00 01 00 A2 00 00 00 00 69 59 74 41 00 ; ......¢....iYtA.
00020820h: 01 00 00 00 00 00 02 00 01 00 02 02 42 48 00 02 ; ............BH..
00020830h: 44 48 01 00 03 02 51 18 00 02 53 18 01 00 04 04 ; DH....Q...S.....
00020840h: 06 67 00 04 08 67 01 00 05 05 19 03 00 05 21 03 ; .g...g........!.
00020850h: 01 00 06 08 31 41 00 08 33 41 01 00 07 11 50 10 ; ....1A..3A....P.
00020860h: 00 11 52 10 01 00 08 15 01 54 00 15 03 54 01 00 ; ..R......T...T..
00020870h: 09 18 19 42 00 18 21 42 01 00 10 21 33 41 00 21 ; ...B..!B...!3A.!
00020880h: 35 41 01 00 11 21 43 10 00 21 45 10 01 00 12 24 ; 5A...!C..!E....$
00020890h: 07 34 00 24 09 34 01 00 13 25 04 25 00 25 06 25 ; .4.$.4...%.%.%.%
000208a0h: 01 00 14 26 18 25 00 26 20 25 01 00 15 29 42 38 ; ...&.%.& %...)B8
000208b0h: 00 29 44 38 00 00 00 00 00 00 00 00 00 00 00 00 ; .)D8............
```

Sorting it out into 10 byte chunks, where each chunk is a TOC entry.

```
41  00  A0  00 00 00  00  01 20 00   // describes the first track
01  00  A1  00 00 00  00  15 00 00   // describes the last track
01  00  A2  00 00 00  00  69 59 74   // describes the disc length

41  00  01  00 00 00  00  00 02 00   // Track 01 (first)
01  00  02  02 42 48  00  02 44 48   // Track 02
   [...]
01  00  15  29 42 38  00  29 44 38   // Track 15 (last)
```

## Binary Coded Decimal (BCD)

The TOC uses binary coded decimal (BCD) to store the track addresses. In BCD, The upper nybble of a byte is used to store the tens, and the lower nybble is used to store ones. Only the values 0-9 are used out of the hex range of 0-F.

To convert the decimal value 10 into BCD you would take the tens, divide by 10 and multiply by 16, and add the ones, to get 16 decimal or 0x10 hexadecimal.

```
var ones = dec % 10;
var tens = dec / 10;
bcd = tens * 16 + ones;
```

Conversely, to get from BCD to decimal:

```
var ones = bcd % 16;
var tens = bcd / 16;
dec = tens * 10 + ones;
```

The format of each TOC entry is as follows:

Offset	| Type/Size  | Purpose
--------|------------|----------------------------------------------------------------------
`0x00`	| 1 byte	 | Track type - `0x41` = data track, `0x01` = audio track
`0x01`	| 1 byte	 | Always `0x00`
`0x02`	| 1 byte	 | The track number in binary decimal
`0x03`	| 3 bytes	 | The absolute track start address in binary decimal - first byte is minutes, second is seconds, third is frames
`0x06`	| 1 byte	 | Always `0x00`
`0x07`	| 3 bytes	 | The "relative" track address - same as before, and uses MM:SS:FF format
  
There are three special tracks at the beginning. These have track "numbers" `0xA0`, `0xA1` and `0xA2`. Note that a TOC cannot have more than 99 tracks.

The structure of these are slightly different - the absolute track field is always null:

**Track `0xA0` - First Track Metadata**

```
41 00 A0 00 00 00 00 01 20 00
|     |              |  |
|     |              |  +- Disc Type (0x20 = XA)
|     |              +- First track number
|     +- Special Track type
+- Type of first track
```

**Track `0xA1` - Last Track Metadata**

```
01 00 A1 00 00 00 00 15 00 00
|     |              |  
|     |              |  
|     |              +- Last track number
|     +- Special Track type
+- Type of last track
```

**Track `0xA2` - Disc Length Metadata**

```
01 00 A2 00 00 00 00 69 59 74
|     |              |  
|     |              |  
|     |              +- Length of disc in MM:SS:FF
|     +- Special Track type
+- Type of last track
```


# ISO Indexes

Each index is 32 bytes long:

Offset	| Type/Size  | Purpose
--------|------------|----------------------------------------------------------------------
`0x00`	| DWORD	     | Offset of block
`0x04`	| DWORD	     | Size of block (see note below)
`0x08`  | 24 bytes   | Unused / Junk

## Blocks

Uncompressed blocks are 16 * 2352 bytes (there's 2352 bytes per CD sector). Although ISO sectors only have 2048 bytes per sector (don't include RAW data) the uncompressed block size is always 16 * 2352 bytes long.

Compressed blocks are compressed with zlib.