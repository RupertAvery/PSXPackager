using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;

namespace PSXPackagerGUI.Shaders
{
    public class WavesEffect : ShaderEffect
    {
        private static readonly PixelShader shader = new PixelShader
        {
            UriSource = new Uri("pack://application:,,,/PSXPackagerGUI;component/Shaders/pspwaves.ps.cso", UriKind.Absolute)
        };

        public WavesEffect()
        {
            PixelShader = shader;
            UpdateShaderValue(TimeProperty);
        }

        public double Time
        {
            get => (double)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(
                "Time",
                typeof(double),
                typeof(WavesEffect),
                new UIPropertyMetadata(0.0, PixelShaderConstantCallback(0)));
    }
}
