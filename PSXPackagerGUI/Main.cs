using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSXPackagerGUI
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void buttonSourceBrowse_Click(object sender, EventArgs e)
        {
            if (radioButtonFileSource.Checked)
            {
                openFileDialog1.Filter = "Supported files|*.7z;*.zip;*.rar;*.img;*.iso;*.bin;*.cue;*.pbp|Image files|*.img;*.iso;*.bin;*.cue|EBOOT File (PBP)|*.pbp|All files|*.*";
                var result = openFileDialog1.ShowDialog();
                if(result == DialogResult.OK)
                {
                    textBoxSource.Text = openFileDialog1.FileName;
                    var extension = Path.GetExtension(openFileDialog1.FileName).ToLower();
                    switch (extension)
                    {
                        case ".7z":
                        case ".zip":
                        case ".rar":
                            break;
                        case ".img":
                        case ".bin":
                        case ".iso":
                        case ".cue":
                            break;
                        case ".pbp":
                            break;
                    }
                }
            }
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {

        }

        private void buttonDestinationBrowse_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxDestination.Text = folderBrowserDialog1.SelectedPath;
            }
        }
    }
}
