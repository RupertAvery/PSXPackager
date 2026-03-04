using System.Collections.Generic;
using Popstation.Database;

namespace PSXPackagerGUI.Models;

public class ScanEntry
{
    public ScanEntryType Type { get; set; }
    public string Path { get; set; }
    public GameEntry? GameEntry { get; set; }
    public bool HasError { get; set; }
    public string ErrorMesage { get; set; }
    public List<string> Discs { get; set; }
    public List<SubEntry> SubEntries { get; set; }
}