using Popstation.Pbp;
using PSXPackagerGUI.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SFOEntry = PSXPackagerGUI.Models.SFOEntry;

namespace PSXPackagerGUI.Pages
{
    public partial class SinglePage
    {
        /// <summary>
        /// Creates a list of default SFO entries with standard key-value pairs for a PlayStation SFO file.
        /// </summary>
        /// <remarks>The returned list includes commonly required SFO keys such as BOOTABLE, CATEGORY,
        /// DISC_ID, DISC_VERSION, LICENSE, PARENTAL_LEVEL, PSP_SYSTEM_VER, REGION, and TITLE. The values are set to
        /// standard defaults appropriate for most use cases. Callers can modify the returned list to customize SFO
        /// entries as needed.</remarks>
        /// <returns>A list of <see cref="SFOEntry"/> objects representing the default SFO entries required for a typical
        /// PlayStation SFO file.</returns>
        private List<SFOEntry> GetDefaultSFOEntries()
        {
            return
            [
                new SFOEntry() { Key = SFOKeys.BOOTABLE, Value = 0x01 },
                new SFOEntry() { Key = SFOKeys.CATEGORY, Value = SFOValues.PS1Category },
                new SFOEntry() { Key = SFOKeys.DISC_ID, Value = "" },
                new SFOEntry() { Key = SFOKeys.DISC_VERSION, Value = "1.00" },
                new SFOEntry() { Key = SFOKeys.LICENSE, Value = SFOValues.License },
                new SFOEntry() { Key = SFOKeys.PARENTAL_LEVEL, Value = SFOValues.ParentalLevel },
                new SFOEntry() { Key = SFOKeys.PSP_SYSTEM_VER, Value = SFOValues.PSPSystemVersion },
                new SFOEntry() { Key = SFOKeys.REGION, Value = 0x8000 },
                new SFOEntry() { Key = SFOKeys.TITLE, Value = "" },
            ];
        }

        /// <summary>
        /// Determines the display order index for a specified SFO key.
        /// </summary>
        /// <remarks>The returned order index can be used to sort or arrange SFO keys in a consistent
        /// manner. Unrecognized keys are assigned a high index value to appear after known keys.</remarks>
        /// <param name="key">The SFO key for which to retrieve the display order. This value should correspond to a defined key in <see
        /// cref="SFOKeys"/>.</param>
        /// <returns>An integer representing the display order of the specified key. Returns 99 if the key is not recognized.</returns>
        private int GetKeyOrder(string key)
        {
            return key switch
            {
                SFOKeys.BOOTABLE => 0,
                SFOKeys.CATEGORY => 1,
                SFOKeys.DISC_ID => 2,
                SFOKeys.DISC_VERSION => 3,
                SFOKeys.LICENSE => 4,
                SFOKeys.PARENTAL_LEVEL => 5,
                SFOKeys.PSP_SYSTEM_VER => 6,
                SFOKeys.REGION => 7,
                SFOKeys.TITLE => 8,
                _ => 99
            };
        }

        /// <summary>
        /// Configures the metadata properties for all SFO entries in the model according to their key types and
        /// expected constraints.
        /// </summary>
        /// <remarks>This method sets the entry type, editability, maximum length, validation rules, and
        /// tooltips for each SFO entry based on its key. It should be called to ensure that SFO entries are properly
        /// initialized before they are displayed or edited. The configuration enforces constraints such as valid value
        /// ranges and input formats for specific SFO fields.</remarks>
        private void SetSFOMetaData()
        {
            foreach (var entry in Model.SFOEntries)
            {
                switch (entry.Key)
                {
                    case SFOKeys.BOOTABLE:
                        {
                            // Value = 0x01,
                            entry.EntryType = SFOEntryType.NUM;
                            entry.IsEditable = false;
                            break;
                        }
                    case SFOKeys.CATEGORY:
                        {
                            //Value = SFOValues.PS1Category,
                            entry.EntryType = SFOEntryType.STR;
                            entry.IsEditable = false;
                            break;
                        }
                    case SFOKeys.DISC_ID:
                        {
                            // Value = "", 
                            entry.EntryType = SFOEntryType.STR;
                            entry.IsEditable = true;
                            entry.MaxLength = 9;
                            entry.Validator = ValidateGameID;
                            entry.ToolTip = "Game ID, e.g. SLUS12345";
                            break;
                        }
                    case SFOKeys.DISC_VERSION:
                        {
                            //Value = "1.00";
                            entry.EntryType = SFOEntryType.STR;
                            entry.IsEditable = true;
                            entry.MaxLength = 9;
                            entry.Validator = ValidateVersion;
                            entry.ToolTip = "Decimal value e.g. 1.00";
                            break;
                        }
                    case SFOKeys.LICENSE:
                        {
                            //Value = SFOValues.License;
                            entry.EntryType = SFOEntryType.STR;
                            entry.IsEditable = true;
                            entry.MaxLength = 512;
                            break;
                        }
                    case SFOKeys.PARENTAL_LEVEL:
                        {
                            //Value = SFOValues.ParentalLevel;
                            entry.EntryType = SFOEntryType.NUM;
                            entry.IsEditable = true;
                            entry.Validator = ValidateParentalLevel;
                            entry.ToolTip = "1 (No Restriction) - 11 (Restricted)";
                            break;
                        }
                    case SFOKeys.PSP_SYSTEM_VER:
                        {
                            //Value = SFOValues.PSPSystemVersion;
                            entry.EntryType = SFOEntryType.STR;
                            entry.IsEditable = true;
                            entry.Validator = ValidateVersion;
                            entry.ToolTip = "Minimum required System Version e.g. 3.01";
                            break;
                        }
                    case SFOKeys.REGION:
                        {
                            //Value = 0x8000;
                            entry.EntryType = SFOEntryType.NUM;
                            entry.IsEditable = true;
                            entry.ToolTip = "Valid regions";
                            break;
                        }
                    case SFOKeys.TITLE:
                        {
                            // Value = "";
                            entry.EntryType = SFOEntryType.STR;
                            entry.IsEditable = true;
                            entry.MaxLength = 128;
                            entry.ToolTip = "Game Save Title";
                            break;
                        }
                }

            }
        }

        // Regular expressions for validation
        private Regex genericGameIDRegex = new Regex("[A-Z]{4}[0-9]{5}");
        private Regex versionRegex = new Regex("^\\d+\\.\\d{2}");

        private bool ValidateVersion(string value)
        {
            return versionRegex.IsMatch(value);
        }

        private bool ValidateParentalLevel(string value)
        {
            value = value.Trim();

            if (value is { Length: 0 }) return true;

            if (int.TryParse(value, out var intValue))
            {
                return intValue is >= 0 and <= 11;
            }

            return false;
        }

        private bool ValidateGameID(string value)
        {
            return gameIDregex.IsMatch(value);
        }

    }
}
