namespace PSXPackagerGUI
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.buttonConvert = new System.Windows.Forms.Button();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.radioButtonTitleEboot = new System.Windows.Forms.RadioButton();
            this.radioButtonTitle = new System.Windows.Forms.RadioButton();
            this.radioButtonFilename = new System.Windows.Forms.RadioButton();
            this.comboBoxExtension = new System.Windows.Forms.ComboBox();
            this.radioButtonFolderSource = new System.Windows.Forms.RadioButton();
            this.radioButtonFileSource = new System.Windows.Forms.RadioButton();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxFilename = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxDestination = new System.Windows.Forms.TextBox();
            this.buttonDestinationBrowse = new System.Windows.Forms.Button();
            this.textBoxSource = new System.Windows.Forms.TextBox();
            this.buttonSourceBrowse = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // trackBar1
            // 
            this.trackBar1.Location = new System.Drawing.Point(18, 28);
            this.trackBar1.Maximum = 9;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(313, 45);
            this.trackBar1.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(184, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Compression (0 - None, 9 - Maximium)";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(784, 521);
            this.tabControl1.TabIndex = 19;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.buttonConvert);
            this.tabPage1.Controls.Add(this.label9);
            this.tabPage1.Controls.Add(this.label8);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.radioButtonTitleEboot);
            this.tabPage1.Controls.Add(this.radioButtonTitle);
            this.tabPage1.Controls.Add(this.radioButtonFilename);
            this.tabPage1.Controls.Add(this.comboBoxExtension);
            this.tabPage1.Controls.Add(this.radioButtonFolderSource);
            this.tabPage1.Controls.Add(this.radioButtonFileSource);
            this.tabPage1.Controls.Add(this.label6);
            this.tabPage1.Controls.Add(this.textBoxFilename);
            this.tabPage1.Controls.Add(this.progressBar1);
            this.tabPage1.Controls.Add(this.textBox3);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.textBoxDestination);
            this.tabPage1.Controls.Add(this.buttonDestinationBrowse);
            this.tabPage1.Controls.Add(this.textBoxSource);
            this.tabPage1.Controls.Add(this.buttonSourceBrowse);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(776, 495);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Convert";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.textBox4);
            this.groupBox1.Controls.Add(this.textBox5);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Location = new System.Drawing.Point(459, 6);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(309, 279);
            this.groupBox1.TabIndex = 41;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Disc Info";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 13);
            this.label4.TabIndex = 25;
            this.label4.Text = "Game Title";
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(9, 32);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(294, 20);
            this.textBox4.TabIndex = 24;
            // 
            // textBox5
            // 
            this.textBox5.Location = new System.Drawing.Point(9, 73);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(101, 20);
            this.textBox5.TabIndex = 26;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 55);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 13);
            this.label5.TabIndex = 27;
            this.label5.Text = "Game ID";
            // 
            // buttonConvert
            // 
            this.buttonConvert.Location = new System.Drawing.Point(378, 262);
            this.buttonConvert.Name = "buttonConvert";
            this.buttonConvert.Size = new System.Drawing.Size(75, 23);
            this.buttonConvert.TabIndex = 40;
            this.buttonConvert.Text = "Convert";
            this.buttonConvert.UseVisualStyleBackColor = true;
            this.buttonConvert.Click += new System.EventHandler(this.buttonConvert_Click);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 207);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(117, 13);
            this.label9.TabIndex = 39;
            this.label9.Text = "Sample output filename";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(192, 10);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(53, 13);
            this.label8.TabIndex = 38;
            this.label8.Text = "Extension";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 10);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(41, 13);
            this.label7.TabIndex = 37;
            this.label7.Text = "Source";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(245, 134);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(127, 39);
            this.label1.TabIndex = 36;
            this.label1.Text = "%FILE% - Filename\r\n%TITLE% - Game Title\r\n%GAMEID% - Game ID";
            // 
            // radioButtonTitleEboot
            // 
            this.radioButtonTitleEboot.AutoSize = true;
            this.radioButtonTitleEboot.Location = new System.Drawing.Point(11, 157);
            this.radioButtonTitleEboot.Name = "radioButtonTitleEboot";
            this.radioButtonTitleEboot.Size = new System.Drawing.Size(189, 17);
            this.radioButtonTitleEboot.TabIndex = 35;
            this.radioButtonTitleEboot.Text = "Use Database Title + EBOOT.PBP";
            this.radioButtonTitleEboot.UseVisualStyleBackColor = true;
            // 
            // radioButtonTitle
            // 
            this.radioButtonTitle.AutoSize = true;
            this.radioButtonTitle.Location = new System.Drawing.Point(11, 134);
            this.radioButtonTitle.Name = "radioButtonTitle";
            this.radioButtonTitle.Size = new System.Drawing.Size(116, 17);
            this.radioButtonTitle.TabIndex = 34;
            this.radioButtonTitle.Text = "Use Database Title";
            this.radioButtonTitle.UseVisualStyleBackColor = true;
            // 
            // radioButtonFilename
            // 
            this.radioButtonFilename.AutoSize = true;
            this.radioButtonFilename.Location = new System.Drawing.Point(11, 111);
            this.radioButtonFilename.Name = "radioButtonFilename";
            this.radioButtonFilename.Size = new System.Drawing.Size(116, 17);
            this.radioButtonFilename.TabIndex = 33;
            this.radioButtonFilename.Text = "Use Input Filename";
            this.radioButtonFilename.UseVisualStyleBackColor = true;
            // 
            // comboBoxExtension
            // 
            this.comboBoxExtension.FormattingEnabled = true;
            this.comboBoxExtension.Location = new System.Drawing.Point(251, 6);
            this.comboBoxExtension.Name = "comboBoxExtension";
            this.comboBoxExtension.Size = new System.Drawing.Size(121, 21);
            this.comboBoxExtension.TabIndex = 32;
            // 
            // radioButtonFolderSource
            // 
            this.radioButtonFolderSource.AutoSize = true;
            this.radioButtonFolderSource.Location = new System.Drawing.Point(102, 8);
            this.radioButtonFolderSource.Name = "radioButtonFolderSource";
            this.radioButtonFolderSource.Size = new System.Drawing.Size(54, 17);
            this.radioButtonFolderSource.TabIndex = 31;
            this.radioButtonFolderSource.Text = "Folder";
            this.radioButtonFolderSource.UseVisualStyleBackColor = true;
            // 
            // radioButtonFileSource
            // 
            this.radioButtonFileSource.AutoSize = true;
            this.radioButtonFileSource.Checked = true;
            this.radioButtonFileSource.Location = new System.Drawing.Point(55, 8);
            this.radioButtonFileSource.Name = "radioButtonFileSource";
            this.radioButtonFileSource.Size = new System.Drawing.Size(41, 17);
            this.radioButtonFileSource.TabIndex = 30;
            this.radioButtonFileSource.TabStop = true;
            this.radioButtonFileSource.Text = "File";
            this.radioButtonFileSource.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 95);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(49, 13);
            this.label6.TabIndex = 29;
            this.label6.Text = "Filename";
            // 
            // textBoxFilename
            // 
            this.textBoxFilename.Location = new System.Drawing.Point(11, 180);
            this.textBoxFilename.Name = "textBoxFilename";
            this.textBoxFilename.Size = new System.Drawing.Size(361, 20);
            this.textBoxFilename.TabIndex = 28;
            this.textBoxFilename.Text = "%TITLE%";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(11, 464);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(757, 23);
            this.progressBar1.TabIndex = 23;
            // 
            // textBox3
            // 
            this.textBox3.BackColor = System.Drawing.SystemColors.Window;
            this.textBox3.Location = new System.Drawing.Point(11, 291);
            this.textBox3.Multiline = true;
            this.textBox3.Name = "textBox3";
            this.textBox3.ReadOnly = true;
            this.textBox3.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox3.Size = new System.Drawing.Size(757, 167);
            this.textBox3.TabIndex = 22;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 21;
            this.label2.Text = "Output Folder";
            // 
            // textBoxDestination
            // 
            this.textBoxDestination.Location = new System.Drawing.Point(11, 72);
            this.textBoxDestination.Name = "textBoxDestination";
            this.textBoxDestination.Size = new System.Drawing.Size(361, 20);
            this.textBoxDestination.TabIndex = 20;
            // 
            // buttonDestinationBrowse
            // 
            this.buttonDestinationBrowse.Location = new System.Drawing.Point(378, 70);
            this.buttonDestinationBrowse.Name = "buttonDestinationBrowse";
            this.buttonDestinationBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonDestinationBrowse.TabIndex = 19;
            this.buttonDestinationBrowse.Text = "Browse";
            this.buttonDestinationBrowse.UseVisualStyleBackColor = true;
            this.buttonDestinationBrowse.Click += new System.EventHandler(this.buttonDestinationBrowse_Click);
            // 
            // textBoxSource
            // 
            this.textBoxSource.Location = new System.Drawing.Point(11, 33);
            this.textBoxSource.Name = "textBoxSource";
            this.textBoxSource.Size = new System.Drawing.Size(361, 20);
            this.textBoxSource.TabIndex = 17;
            // 
            // buttonSourceBrowse
            // 
            this.buttonSourceBrowse.Location = new System.Drawing.Point(378, 31);
            this.buttonSourceBrowse.Name = "buttonSourceBrowse";
            this.buttonSourceBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonSourceBrowse.TabIndex = 16;
            this.buttonSourceBrowse.Text = "Browse";
            this.buttonSourceBrowse.UseVisualStyleBackColor = true;
            this.buttonSourceBrowse.Click += new System.EventHandler(this.buttonSourceBrowse_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.SystemColors.Control;
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.trackBar1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(776, 495);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Options";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 521);
            this.Controls.Add(this.tabControl1);
            this.Name = "Main";
            this.Text = "PSXPackager v1.0";
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button buttonConvert;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton radioButtonTitleEboot;
        private System.Windows.Forms.RadioButton radioButtonTitle;
        private System.Windows.Forms.RadioButton radioButtonFilename;
        private System.Windows.Forms.ComboBox comboBoxExtension;
        private System.Windows.Forms.RadioButton radioButtonFolderSource;
        private System.Windows.Forms.RadioButton radioButtonFileSource;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxFilename;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxDestination;
        private System.Windows.Forms.Button buttonDestinationBrowse;
        private System.Windows.Forms.TextBox textBoxSource;
        private System.Windows.Forms.Button buttonSourceBrowse;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}

