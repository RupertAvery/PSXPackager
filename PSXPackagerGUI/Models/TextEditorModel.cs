using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using PSXPackagerGUI.Models.Resource;

namespace PSXPackagerGUI.Models;

public class TextEditorModel : BaseNotifyModel
{
    private FontFamily _fontFamily;
    private double _fontSize;
    private string _text;
    private ObservableCollection<FontFamily> _fontFamilies;
    private List<double> _fontSizes;
    private Brush _color;

    public TextEditorModel()
    {
        FontFamilies = new ObservableCollection<FontFamily>()
        {
            FontManager.NewRodinProDBFontFamily,
            new FontFamily("Arial"),
            new FontFamily("Aptos"),
            new FontFamily("Calibri"),
            new FontFamily("Comic Sans MS"),
            new FontFamily("Courier New"),
            new FontFamily("Tahoma"),
            new FontFamily("Times New Roman"),
            new FontFamily("Verdana"),
        };
        FontSizes = new List<double>()
        {
            8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72
        };
        FontFamily = FontManager.NewRodinProDBFontFamily;
        FontSize = 20;
    }

    public List<double> FontSizes
    {
        get => _fontSizes;
        set => SetProperty(ref _fontSizes, value);
    }

    public ObservableCollection<FontFamily> FontFamilies
    {
        get => _fontFamilies;
        set => SetProperty(ref _fontFamilies, value);
    }

    public FontFamily FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value);
    }

    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public Brush Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    public bool DropShadow { get; set; }
}