namespace PSXPackagerGUI.Templates;

public class TextLayer : Layer
{
    public string Text { get; set; }
    public string FontFamily { get; set; }
    public double FontSize { get; set; }
    public string Color { get; set; }
    public bool DropShadow { get; set; }
    public double OriginalWidth { get; set; }
    public double OriginalHeight { get; set; }
}