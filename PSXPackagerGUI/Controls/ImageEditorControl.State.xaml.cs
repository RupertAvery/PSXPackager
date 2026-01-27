using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PSXPackagerGUI.Controls
{
    public partial class ImageEditorControl
    {

        private void UIElement_OnGotFocus(object sender, RoutedEventArgs e)
        {
            Composite.SaveState();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            UndoState();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            RedoState();
        }

        private void UndoState()
        {
            var selectedIndex = -1;
            if (SelectedLayer != null)
                selectedIndex = Composite.Layers.IndexOf(SelectedLayer);

            Composite.UndoState();

            if (selectedIndex != -1)
            {
                SelectedLayer = Composite.Layers[selectedIndex];
            }

            UpdateSelection();
            Update();
        }

        private void RedoState()
        {
            var selectedIndex = -1;
            if (SelectedLayer != null)
                selectedIndex = Composite.Layers.IndexOf(SelectedLayer);

            Composite.RedoState();

            if (selectedIndex != -1)
            {
                SelectedLayer = Composite.Layers[selectedIndex];
            }

            UpdateSelection();
            Update();
        }
    }
}
