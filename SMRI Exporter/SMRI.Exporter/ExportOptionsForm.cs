using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace SMRI.Exporter
{
    internal sealed class ExportOptionsForm : Form
    {
        private readonly ComboBox sequenceBox = new ComboBox();
        private readonly ComboBox formatBox = new ComboBox();
        private readonly NumericUpDown dpiBox = new NumericUpDown();
        private readonly NumericUpDown qualityBox = new NumericUpDown();
        private readonly TextBox prefixBox = new TextBox();
        private readonly TextBox suffixBox = new TextBox();
        private readonly TextBox folderBox = new TextBox();
        private readonly CheckBox rememberFolderBox = new CheckBox();
        private readonly Label destinationPreview = new Label();
        private readonly Label filePreview = new Label();

        internal string SequenceStyle { get; private set; }
        internal string ExportFormat { get; private set; }
        internal int Dpi { get; private set; }
        internal int JpegQuality { get; private set; }
        internal string FilePrefix { get; private set; }
        internal string FileSuffix { get; private set; }
        internal string BaseFolder { get; private set; }
        internal bool RememberFolder { get; private set; }

        internal ExportOptionsForm(string documentName, string savedFolder)
        {
            Text = "SMRI Exporter";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            ClientSize = new Size(610, 455);
            Font = new Font("Segoe UI", 9F);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                ColumnCount = 3,
                RowCount = 11
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            for (int row = 0; row < layout.RowCount; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);

            ConfigureCombo(sequenceBox, new object[] { "a, b, c", "1, 2, 3", "A, B, C" }, 0);
            ConfigureCombo(formatBox, new object[] { "JPG", "PNG", "TIFF", "PDF" }, 0);
            ConfigureNumber(dpiBox, 1, 100000, 150);
            ConfigureNumber(qualityBox, 1, 100, 100);
            prefixBox.Text = documentName ?? string.Empty;
            folderBox.Text = savedFolder ?? string.Empty;
            rememberFolderBox.Text = "Remember this base folder";
            rememberFolderBox.Checked = true;
            rememberFolderBox.AutoSize = true;

            AddRow(layout, 0, "Sequence", sequenceBox);
            AddRow(layout, 1, "Format", formatBox);
            AddRow(layout, 2, "DPI", dpiBox);
            AddRow(layout, 3, "JPG quality", qualityBox);
            AddRow(layout, 4, "Filename prefix", prefixBox);
            AddRow(layout, 5, "Filename suffix", suffixBox);

            folderBox.Dock = DockStyle.Fill;
            layout.Controls.Add(new Label { Text = "Base folder", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 6);
            layout.Controls.Add(folderBox, 1, 6);
            var browseButton = new Button { Text = "Browse...", Dock = DockStyle.Fill };
            browseButton.Click += BrowseFolder;
            layout.Controls.Add(browseButton, 2, 6);

            layout.Controls.Add(rememberFolderBox, 1, 7);
            layout.SetColumnSpan(rememberFolderBox, 2);

            destinationPreview.AutoSize = true;
            destinationPreview.MaximumSize = new Size(440, 0);
            destinationPreview.ForeColor = Color.DimGray;
            filePreview.AutoSize = true;
            filePreview.MaximumSize = new Size(440, 0);
            filePreview.ForeColor = Color.DimGray;
            AddRow(layout, 8, "Export folder", destinationPreview);
            AddRow(layout, 9, "Example file", filePreview);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };
            var exportButton = new Button { Text = "Export", Width = 90, DialogResult = DialogResult.None };
            var cancelButton = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            exportButton.Click += AcceptOptions;
            buttons.Controls.Add(exportButton);
            buttons.Controls.Add(cancelButton);
            layout.Controls.Add(buttons, 1, 10);
            layout.SetColumnSpan(buttons, 2);
            AcceptButton = exportButton;
            CancelButton = cancelButton;

            formatBox.SelectedIndexChanged += delegate { UpdateConditionalFields(); UpdatePreview(); };
            sequenceBox.SelectedIndexChanged += delegate { UpdatePreview(); };
            prefixBox.TextChanged += delegate { UpdatePreview(); };
            suffixBox.TextChanged += delegate { UpdatePreview(); };
            folderBox.TextChanged += delegate { UpdatePreview(); };
            UpdateConditionalFields();
            UpdatePreview();
        }

        private static void ConfigureCombo(ComboBox box, object[] values, int selectedIndex)
        {
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Dock = DockStyle.Fill;
            box.Items.AddRange(values);
            box.SelectedIndex = selectedIndex;
        }

        private static void ConfigureNumber(NumericUpDown box, decimal minimum, decimal maximum, decimal value)
        {
            box.Minimum = minimum;
            box.Maximum = maximum;
            box.Value = value;
            box.Dock = DockStyle.Left;
            box.Width = 110;
            box.ThousandsSeparator = true;
        }

        private static void AddRow(TableLayoutPanel layout, int row, string caption, Control control)
        {
            layout.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            layout.Controls.Add(control, 1, row);
            layout.SetColumnSpan(control, 2);
        }

        private void BrowseFolder(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose the base export folder";
                dialog.ShowNewFolderButton = true;
                if (Directory.Exists(folderBox.Text)) dialog.SelectedPath = folderBox.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK) folderBox.Text = dialog.SelectedPath;
            }
        }

        private void UpdateConditionalFields()
        {
            string format = Convert.ToString(formatBox.SelectedItem, CultureInfo.InvariantCulture);
            dpiBox.Enabled = format != "PDF";
            qualityBox.Enabled = format == "JPG";
        }

        private void UpdatePreview()
        {
            string baseFolder = folderBox.Text.Trim();
            string timestamp = DateTime.Now.ToString("dd-MM-yy HH-mm", CultureInfo.InvariantCulture);
            destinationPreview.Text = baseFolder.Length == 0
                ? "Choose a base folder"
                : Path.Combine(baseFolder, timestamp);

            string sequence = sequenceBox.SelectedIndex == 1 ? "1" :
                sequenceBox.SelectedIndex == 2 ? "A" : "a";
            string prefix = prefixBox.Text.Trim();
            string suffix = suffixBox.Text.Trim();
            string extension = "." + Convert.ToString(formatBox.SelectedItem,
                CultureInfo.InvariantCulture).ToLowerInvariant().Replace("tiff", "tif");
            filePreview.Text = sequence + (prefix.Length == 0 ? "" : " " + prefix) +
                " 24x36 Inch" + (suffix.Length == 0 ? "" : " " + suffix) + extension;
        }

        private void AcceptOptions(object sender, EventArgs e)
        {
            string folder = folderBox.Text.Trim();
            if (folder.Length == 0)
            {
                MessageBox.Show(this, "Choose a base export folder.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "The export folder cannot be used.\n" + ex.Message,
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SequenceStyle = sequenceBox.SelectedIndex == 1 ? "1" :
                sequenceBox.SelectedIndex == 2 ? "A" : "a";
            ExportFormat = Convert.ToString(formatBox.SelectedItem, CultureInfo.InvariantCulture);
            Dpi = Decimal.ToInt32(dpiBox.Value);
            JpegQuality = Decimal.ToInt32(qualityBox.Value);
            FilePrefix = prefixBox.Text;
            FileSuffix = suffixBox.Text;
            BaseFolder = folder;
            RememberFolder = rememberFolderBox.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
