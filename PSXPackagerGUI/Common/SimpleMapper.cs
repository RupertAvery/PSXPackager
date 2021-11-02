using System.Linq;

namespace PSXPackagerGUI.Common
{
    public class SimpleMapper
    {
        public static void Map<TSource, TTarget>(TSource source, TTarget target)
        {
            var srcProps = typeof(TSource).GetProperties();
            var targetProps = typeof(TTarget).GetProperties();
            var lookup = targetProps.ToDictionary(p => p.Name);

            foreach (var prop in srcProps)
            {
                if (!lookup.TryGetValue(prop.Name, out var targetProp)) continue;
                var value = prop.GetValue(source);
                targetProp.SetValue(target, value);
            }
        }
    }
}