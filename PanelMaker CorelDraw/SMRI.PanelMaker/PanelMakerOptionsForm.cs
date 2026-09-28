using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace SMRI.PanelMaker
{
    internal sealed class PanelMakerOptionsForm : Form
    {
        private readonly ComboBox presetBox = new ComboBox();
        private readonly TextBox presetNameBox = new TextBox();
        private readonly TextBox widthsBox = new TextBox();
        private readonly ComboBox directionBox = new ComboBox();
        private readonly NumericUpDown overlapBox = new NumericUpDown();
        private readonly CheckBox markersBox = new CheckBox();
        private readonly Label summaryLabel = new Label();

        internal double[] MediaWidths { get; private set; }
        internal bool HorizontalCut { get; private set; }
        internal double Overlap { get; private set; }
        internal bool AddBleedMarkers { get; private set; }

        internal PanelMakerOptionsForm()
        {
            Text = "SMRI Panel Maker";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(620, 385);
            Font = new Font("Segoe UI", 9F);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                ColumnCount = 3,
                RowCount = 8
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            for (int row = 0; row < layout.RowCount; row++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(layout);

            presetBox.DropDownStyle = ComboBoxStyle.DropDownList;
            presetBox.Dock = DockStyle.Fill;
            presetBox.Items.Add("Custom widths");
            for (int slot = 1; slot <= 5; slot++)
                presetBox.Items.Add(CorelPanelMaker.SavedPresetLabel(slot));

            widthsBox.Text = "39,49,59";
            presetNameBox.Text = "My Preset";
            directionBox.DropDownStyle = ComboBoxStyle.DropDownList;
            directionBox.Items.AddRange(new object[]
            {
                "Vertical panels — split artwork width",
                "Horizontal panels — split artwork height"
            });
            directionBox.SelectedIndex = 0;
            directionBox.Dock = DockStyle.Fill;

            overlapBox.Minimum = 0;
            overlapBox.Maximum = 100000;
            overlapBox.DecimalPlaces = 3;
            overlapBox.Increment = 0.125M;
            overlapBox.Value = 0.5M;
            overlapBox.Width = 120;
            markersBox.Text = "Add outside bleed / overlap markers";
            markersBox.Checked = true;
            markersBox.AutoSize = true;

            AddRow(layout, 0, "Width preset", presetBox);
            AddRow(layout, 1, "Media widths (in)", widthsBox);

            layout.Controls.Add(new Label { Text = "Preset name", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            presetNameBox.Dock = DockStyle.Fill;
            layout.Controls.Add(presetNameBox, 1, 2);
            var savePresetButton = new Button { Text = "Save preset", Dock = DockStyle.Fill };
            savePresetButton.Click += SaveSelectedPreset;
            layout.Controls.Add(savePresetButton, 2, 2);

            AddRow(layout, 3, "Panel direction", directionBox);
            AddRow(layout, 4, "Overlap (in)", overlapBox);
            layout.Controls.Add(markersBox, 1, 5);
            layout.SetColumnSpan(markersBox, 2);

            summaryLabel.AutoSize = true;
            summaryLabel.ForeColor = Color.DimGray;
            summaryLabel.MaximumSize = new Size(450, 0);
            AddRow(layout, 6, "Configuration", summaryLabel);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft
            };
            var createButton = new Button { Text = "Create Panels", Width = 105 };
            var cancelButton = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            createButton.Click += AcceptOptions;
            buttons.Controls.Add(createButton);
            buttons.Controls.Add(cancelButton);
            layout.Controls.Add(buttons, 1, 7);
            layout.SetColumnSpan(buttons, 2);
            AcceptButton = createButton;
            CancelButton = cancelButton;

            presetBox.SelectedIndexChanged += PresetChanged;
            widthsBox.TextChanged += delegate { UpdateSummary(); };
            directionBox.SelectedIndexChanged += delegate { UpdateSummary(); };
            overlapBox.ValueChanged += delegate { UpdateSummary(); };
            markersBox.CheckedChanged += delegate { UpdateSummary(); };

            int initialSlot = 0;
            for (int slot = 1; slot <= 5; slot++)
            {
                if (!string.IsNullOrWhiteSpace(CorelPanelMaker.LoadPresetWidths(slot)))
                {
                    initialSlot = slot;
                    break;
                }
            }
            presetBox.SelectedIndex = initialSlot;
            UpdateSummary();
        }

        private static void AddRow(TableLayoutPanel layout, int row, string caption, Control control)
        {
            layout.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            layout.Controls.Add(control, 1, row);
            layout.SetColumnSpan(control, 2);
        }

        private void PresetChanged(object sender, EventArgs e)
        {
            int slot = presetBox.SelectedIndex;
            if (slot > 0)
            {
                string savedWidths = CorelPanelMaker.LoadPresetWidths(slot);
                widthsBox.Text = string.IsNullOrWhiteSpace(savedWidths) ? "39,49,59" : savedWidths;
                string savedName = CorelPanelMaker.GetPresetValue(slot, "Name");
                presetNameBox.Text = string.IsNullOrWhiteSpace(savedName)
                    ? "Preset " + slot.ToString(CultureInfo.InvariantCulture)
                    : savedName;
            }
            else
            {
                presetNameBox.Text = "My Preset";
            }
            UpdateSummary();
        }

        private void SaveSelectedPreset(object sender, EventArgs e)
        {
            int slot = presetBox.SelectedIndex;
            if (slot < 1 || slot > 5)
            {
                MessageBox.Show(this, "Select preset slot 1–5 before saving.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double[] ignored;
            if (!TryParseWidths(widthsBox.Text, out ignored))
            {
                ShowWidthError();
                return;
            }

            string name = presetNameBox.Text.Trim();
            if (name.Length == 0)
            {
                MessageBox.Show(this, "Enter a preset name.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CorelPanelMaker.SavePreset(slot, name, widthsBox.Text.Trim());
            presetBox.Items[slot] = CorelPanelMaker.SavedPresetLabel(slot);
            MessageBox.Show(this, "Preset " + slot + " saved.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AcceptOptions(object sender, EventArgs e)
        {
            double[] widths;
            if (!TryParseWidths(widthsBox.Text, out widths))
            {
                ShowWidthError();
                return;
            }

            double overlap = Decimal.ToDouble(overlapBox.Value);
            if (overlap >= widths[0])
            {
                MessageBox.Show(this, "Overlap must be smaller than the smallest media width.",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MediaWidths = widths;
            HorizontalCut = directionBox.SelectedIndex == 1;
            Overlap = overlap;
            AddBleedMarkers = markersBox.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UpdateSummary()
        {
            string direction = directionBox.SelectedIndex == 1 ? "Horizontal" : "Vertical";
            summaryLabel.Text = "Widths: " + widthsBox.Text.Trim() + " in  |  " + direction +
                "  |  Overlap: " + overlapBox.Value.ToString("0.###", CultureInfo.CurrentCulture) +
                " in  |  Markers: " + (markersBox.Checked ? "Yes" : "No");
        }

        private void ShowWidthError()
        {
            MessageBox.Show(this, "Enter valid positive media widths separated by commas, such as 39,49,59.",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static bool TryParseWidths(string text, out double[] widths)
        {
            widths = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var values = new List<double>();
            foreach (string part in text.Split(','))
            {
                string valueText = part.Trim();
                if (valueText.Length == 0) continue;
                double value;
                if ((!double.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out value) &&
                     !double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) || value <= 0)
                    return false;
                if (!values.Any(item => Math.Abs(item - value) < 0.001)) values.Add(value);
            }
            if (values.Count == 0) return false;
            widths = values.OrderBy(value => value).ToArray();
            return true;
        }
    }
}
