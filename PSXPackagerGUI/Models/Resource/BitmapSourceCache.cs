using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace PSXPackagerGUI.Models.Resource;

public class BitmapSourceCache
{
    public Dictionary<string, BitmapSource> _cache = new Dictionary<string, BitmapSource>();

    public void Add(string key, BitmapSource bitmap)
    {
        _cache[key] = bitmap;
    }

    public bool TryGet(string key, out BitmapSource? bitmapSource)
    {
        if (_cache.TryGetValue(key, out var bitmap))
        {
            bitmapSource = bitmap;
            return true;
        }
        bitmapSource = null;
        return false;
    }

    public bool Contains(string hash)
    {
        return _cache.ContainsKey(hash);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}