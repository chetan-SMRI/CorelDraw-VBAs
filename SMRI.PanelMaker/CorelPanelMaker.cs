using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SMRI.PanelMaker
{
    internal sealed class CorelPanelMaker
    {
        private const int CdrInch = 1;
        private const bool CdrFalse = false;
        private const double Gap = 1.0;
        private const double BlendMarkerLength = 1.0;

        public void Run()
        {
            dynamic app = GetRunningCorelDraw();
            dynamic document = app.ActiveDocument;
            if (document == null)
            {
                MessageBox.Show("Please open a CorelDRAW document first.", "SMRI Panel Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dynamic selection = app.ActiveSelectionRange;
            if (selection == null || selection.Count == 0)
            {
                MessageBox.Show("Please select your artwork/image first.", "SMRI Panel Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double[] mediaWidths = PromptForMediaWidths();
            if (mediaWidths == null)
            {
                return;
            }

            bool? horizontalCutValue = PromptForDirection();
            if (!horizontalCutValue.HasValue)
            {
                return;
            }

            double? overlapValue = PromptForOverlap(mediaWidths[0]);
            if (!overlapValue.HasValue)
            {
                return;
            }

            bool? addBleedMarkersValue = PromptForBleedMarkers();
            if (!addBleedMarkersValue.HasValue)
            {
                return;
            }

            bool horizontalCut = horizontalCutValue.Value;
            double overlap = overlapValue.Value;
            bool addBleedMarkers = addBleedMarkersValue.Value;
            object oldUnit = document.Unit;

            try
            {
                document.Unit = CdrInch;
                CreatePanels(document, selection, mediaWidths, overlap, horizontalCut, addBleedMarkers);
            }
            finally
            {
                document.Unit = oldUnit;
            }
        }

        private static void CreatePanels(dynamic document, dynamic source, double[] mediaWidths, double overlap, bool horizontalCut, bool addBleedMarkers)
        {
            double x = 0;
            double y = 0;
            double width = 0;
            double height = 0;
            source.GetBoundingBox(ref x, ref y, ref width, ref height, true);

            double artworkSpan = horizontalCut ? height : width;
            double[] panelWidths;
            if (!BuildPanelSuggestions(artworkSpan, mediaWidths, overlap, out panelWidths))
            {
                MessageBox.Show("Could not build panel suggestions from the entered media widths.", "SMRI Panel Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double startX = x;
            double startY = y - height - Gap;
            double currentSourceX = x;
            double currentSourceY = y;
            double currentDestX = startX;
            double currentDestY = y;
            int createdPanels = 0;
            var suggestionText = new System.Text.StringBuilder();
            suggestionText.AppendLine("Panel suggestions:");
            dynamic activeLayer = document.ActiveLayer;

            document.BeginCommandGroup("SMRI Auto Panel PowerClips");

            try
            {
                for (int i = 0; i < panelWidths.Length; i++)
                {
                    if (horizontalCut)
                    {
                        double panelTop = currentSourceY;
                        double panelHeight = panelWidths[i];
                        double remainingHeight = (y + height) - panelTop;
                        if (panelHeight > remainingHeight)
                        {
                            panelHeight = remainingHeight;
                        }

                        if (panelHeight <= 0)
                        {
                            break;
                        }

                        double destX = x + width + Gap;
                        double destY = currentDestY;
                        createdPanels++;
                        suggestionText.AppendLine("Panel " + (i + 1) + " " + FormatInches(panelHeight) + "\"");

                        dynamic box = activeLayer.CreateRectangle2(destX, destY, width, panelHeight);
                        box.Name = "Panel_" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                        box.Fill.ApplyNoFill();
                        box.Outline.Width = 0.01;

                        dynamic duplicate = source.Duplicate();
                        duplicate.Move(destX - x, destY - panelTop);
                        duplicate.AddToPowerClip(box, CdrFalse);

                        if (addBleedMarkers)
                        {
                            AddBlendMarkers(activeLayer, destX, destY, width, panelHeight, i, panelWidths.Length, true, overlap);
                        }

                        double horizontalLabelX = destX - 0.35;
                        double horizontalLabelY = destY;
                        dynamic label = activeLayer.CreateArtisticText(horizontalLabelX, horizontalLabelY,
                            "Tile " + (i + 1) +
                            " | Total Height: " + FormatInches(height) + "\"" +
                            " | Panel Height: " + FormatInches(panelHeight) + "\"");

                        label.Text.Story.Size = 18;
                        label.Text.Story.Font = "Arial";
                        label.Text.Story.Bold = true;
                        label.RotationCenterX = horizontalLabelX;
                        label.RotationCenterY = horizontalLabelY;
                        label.Rotate(90);

                        currentSourceY += panelHeight - overlap;
                        currentDestY += panelHeight + Gap;
                    }
                    else
                    {
                        double panelLeft = currentSourceX;
                        double panelWidth = panelWidths[i];
                        double remainingWidth = (x + width) - panelLeft;
                        if (panelWidth > remainingWidth)
                        {
                            panelWidth = remainingWidth;
                        }

                        if (panelWidth <= 0)
                        {
                            break;
                        }

                        double destX = currentDestX;
                        createdPanels++;
                        suggestionText.AppendLine("Panel " + (i + 1) + " " + FormatInches(panelWidth) + "\"");

                        dynamic box = activeLayer.CreateRectangle2(destX, startY, panelWidth, height);
                        box.Name = "Panel_" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                        box.Fill.ApplyNoFill();
                        box.Outline.Width = 0.01;

                        dynamic duplicate = source.Duplicate();
                        duplicate.Move(destX - panelLeft, startY - y);
                        duplicate.AddToPowerClip(box, CdrFalse);

                        if (addBleedMarkers)
                        {
                            AddBlendMarkers(activeLayer, destX, startY, panelWidth, height, i, panelWidths.Length, false, overlap);
                        }

                        dynamic label = activeLayer.CreateArtisticText(destX, startY + height + 0.14,
                            "Tile " + (i + 1) +
                            " | Total Width: " + FormatInches(width) + "\"" +
                            " | Panel Width: " + FormatInches(panelWidth) + "\"");

                        label.Text.Story.Size = 18;
                        label.Text.Story.Font = "Arial";
                        label.Text.Story.Bold = true;

                        currentSourceX += panelWidth - overlap;
                        currentDestX += panelWidth + Gap;
                    }
                }
            }
            finally
            {
                document.EndCommandGroup();
            }

            MessageBox.Show(createdPanels + " panels created successfully." + Environment.NewLine + Environment.NewLine + suggestionText,
                "SMRI Panel Maker", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static dynamic GetRunningCorelDraw()
        {
            List<string> attemptedProgIds = new List<string>();
            List<string> errors = new List<string>();

            foreach (string progId in GetCorelDrawProgIds())
            {
                attemptedProgIds.Add(progId);

                try
                {
                    return Marshal.GetActiveObject(progId);
                }
                catch (COMException ex)
                {
                    errors.Add(progId + ": GetActiveObject failed 0x" + ex.ErrorCode.ToString("X8", CultureInfo.InvariantCulture));
                }

                try
                {
                    Type appType = Type.GetTypeFromProgID(progId, false);
                    if (appType == null)
                    {
                        continue;
                    }

                    return Activator.CreateInstance(appType);
                }
                catch (COMException ex)
                {
                    errors.Add(progId + ": CreateInstance failed 0x" + ex.ErrorCode.ToString("X8", CultureInfo.InvariantCulture));
                }
                catch (UnauthorizedAccessException ex)
                {
                    errors.Add(progId + ": " + ex.GetType().Name + " " + ex.Message);
                }
            }

            WriteComDiagnosticLog(attemptedProgIds, errors);

            throw new InvalidOperationException(
                "CorelDRAW is not running, its COM automation server is not registered, or Windows is blocking access because CorelDRAW and SMRI Panel Maker are running at different permission levels." +
                Environment.NewLine + Environment.NewLine +
                "Open CorelDRAW normally, then run SMRI Panel Maker normally from the CorelDRAW launcher macro or Start menu. Do not run one as administrator unless both are running as administrator." +
                Environment.NewLine + Environment.NewLine +
                "Diagnostic log: " + GetComDiagnosticLogPath());
        }

        private static IEnumerable<string> GetCorelDrawProgIds()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string progId in GetRegistryCorelDrawProgIds())
            {
                if (seen.Add(progId))
                {
                    yield return progId;
                }
            }

            string[] commonProgIds =
            {
                "CorelDRAW.Application",
                "CorelDRAW.Application.26",
                "CorelDRAW.Application.25",
                "CorelDRAW.Application.24"
            };

            foreach (string progId in commonProgIds)
            {
                if (seen.Add(progId))
                {
                    yield return progId;
                }
            }

            for (int version = 35; version >= 17; version--)
            {
                string progId = "CorelDRAW.Application." + version.ToString(CultureInfo.InvariantCulture);
                if (seen.Add(progId))
                {
                    yield return progId;
                }
            }
        }

        private static IEnumerable<string> GetRegistryCorelDrawProgIds()
        {
            List<string> progIds = new List<string>();

            try
            {
                foreach (string subKeyName in Registry.ClassesRoot.GetSubKeyNames())
                {
                    if (subKeyName.StartsWith("CorelDRAW.Application", StringComparison.OrdinalIgnoreCase))
                    {
                        progIds.Add(subKeyName);
                    }
                }
            }
            catch
            {
            }

            return progIds
                .OrderByDescending(GetProgIdVersion)
                .ThenByDescending(p => p, StringComparer.OrdinalIgnoreCase);
        }

        private static double GetProgIdVersion(string progId)
        {
            int lastDot = progId.LastIndexOf('.');
            if (lastDot < 0 || lastDot == progId.Length - 1)
            {
                return 0;
            }

            double version;
            return double.TryParse(progId.Substring(lastDot + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out version)
                ? version
                : 0;
        }

        private static void WriteComDiagnosticLog(IEnumerable<string> attemptedProgIds, IEnumerable<string> errors)
        {
            try
            {
                string path = GetComDiagnosticLogPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path,
                    "SMRI Panel Maker CorelDRAW COM diagnostic" + Environment.NewLine +
                    "Time: " + DateTime.Now.ToString("o", CultureInfo.InvariantCulture) + Environment.NewLine +
                    "Is64BitProcess: " + Environment.Is64BitProcess + Environment.NewLine +
                    "Is64BitOperatingSystem: " + Environment.Is64BitOperatingSystem + Environment.NewLine +
                    Environment.NewLine +
                    "Attempted ProgIDs:" + Environment.NewLine +
                    string.Join(Environment.NewLine, attemptedProgIds) + Environment.NewLine +
                    Environment.NewLine +
                    "Errors:" + Environment.NewLine +
                    string.Join(Environment.NewLine, errors));
            }
            catch
            {
            }
        }

        private static string GetComDiagnosticLogPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SMRI",
                "PanelMaker",
                "coreldraw-com-diagnostic.txt");
        }

        private static double[] PromptForMediaWidths()
        {
            string mediaText = PromptForMediaText();
            if (mediaText == null)
            {
                return null;
            }

            double[] mediaWidths;
            if (!TryParseMediaWidths(mediaText, out mediaWidths))
            {
                MessageBox.Show("Please enter valid media widths, like 39,49,59.", "SMRI Panel Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return mediaWidths;
        }

        private static string PromptForMediaText()
        {
            while (true)
            {
                string presetChoice = Interaction.InputBox(PresetMenuText(), "SMRI Panel Maker", "1");
                if (string.IsNullOrWhiteSpace(presetChoice))
                {
                    return null;
                }

                string choice = presetChoice.Trim().ToUpperInvariant();
                if (choice == "C" || choice == "CUSTOM")
                {
                    string custom = Interaction.InputBox("Enter available media widths in inches, separated by commas:",
                        "SMRI Panel Maker", "39,49,59");
                    return string.IsNullOrWhiteSpace(custom) ? null : custom;
                }

                if (choice == "N" || choice == "NEW")
                {
                    int slot = Convert.ToInt32(Val(Interaction.InputBox("Save preset to slot number 1-5:",
                        "SMRI Panel Maker", "1")));
                    if (slot < 1 || slot > 5)
                    {
                        MessageBox.Show("Please choose a slot from 1 to 5.", "SMRI Panel Maker",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    string presetName = Interaction.InputBox("Preset name:", "SMRI Panel Maker", "My Preset");
                    if (string.IsNullOrWhiteSpace(presetName))
                    {
                        return null;
                    }

                    string presetWidths = Interaction.InputBox("Preset widths in inches, separated by commas:",
                        "SMRI Panel Maker", "39,49,59");
                    if (string.IsNullOrWhiteSpace(presetWidths))
                    {
                        return null;
                    }

                    SavePreset(slot, presetName.Trim(), presetWidths.Trim());
                    return presetWidths;
                }

                int presetSlot = Convert.ToInt32(Val(choice));
                if (presetSlot >= 1 && presetSlot <= 5)
                {
                    string savedWidths = LoadPresetWidths(presetSlot);
                    if (string.IsNullOrWhiteSpace(savedWidths))
                    {
                        MessageBox.Show("That preset slot is empty. Create a preset first or choose Custom.",
                            "SMRI Panel Maker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    return savedWidths;
                }

                MessageBox.Show("Choose preset 1-5, N for new preset, or C for custom.",
                    "SMRI Panel Maker", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static bool? PromptForDirection()
        {
            string directionText = Interaction.InputBox(
                "Choose cut direction:" + Environment.NewLine +
                "V = Vertical panels, split by artwork width" + Environment.NewLine +
                "H = Horizontal panels, split by artwork height",
                "SMRI Panel Maker",
                "V");

            if (string.IsNullOrWhiteSpace(directionText))
            {
                return null;
            }

            return IsHorizontalCut(directionText);
        }

        private static double? PromptForOverlap(double smallestMediaWidth)
        {
            string overlapText = Interaction.InputBox("Enter overlap in inches:", "SMRI Panel Maker", "0.75");
            double overlap;
            if (!double.TryParse(overlapText, NumberStyles.Float, CultureInfo.CurrentCulture, out overlap) &&
                !double.TryParse(overlapText, NumberStyles.Float, CultureInfo.InvariantCulture, out overlap))
            {
                MessageBox.Show("Please enter a valid overlap, like 0.75.", "SMRI Panel Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            if (overlap < 0)
            {
                overlap = 0;
            }

            if (overlap >= smallestMediaWidth)
            {
                MessageBox.Show("Overlap must be smaller than the smallest media width.", "SMRI Panel Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return overlap;
        }

        private static bool? PromptForBleedMarkers()
        {
            string markerText = Interaction.InputBox("Add outside bleeding / overlap markers? Y/N",
                "SMRI Panel Maker", "Y");
            if (string.IsNullOrWhiteSpace(markerText))
            {
                return null;
            }

            return IsYes(markerText);
        }

        private static bool TryParseMediaWidths(string widthText, out double[] mediaWidths)
        {
            mediaWidths = null;
            if (string.IsNullOrWhiteSpace(widthText))
            {
                return false;
            }

            var widths = new List<double>();
            string[] parts = widthText.Split(',');
            foreach (string part in parts)
            {
                string trimmed = part.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                double width;
                if (!double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out width) &&
                    !double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out width))
                {
                    return false;
                }

                if (width <= 0)
                {
                    return false;
                }

                if (!widths.Any(w => Math.Abs(w - width) < 0.001))
                {
                    widths.Add(width);
                }
            }

            if (widths.Count == 0)
            {
                return false;
            }

            mediaWidths = widths.OrderBy(w => w).ToArray();
            return true;
        }

        private static string PresetMenuText()
        {
            var text = new System.Text.StringBuilder();
            text.AppendLine("Choose width preset:");
            for (int slot = 1; slot <= 5; slot++)
            {
                text.AppendLine(SavedPresetLabel(slot));
            }

            text.AppendLine("N = Create / update preset");
            text.Append("C = Custom one-time widths");
            return text.ToString();
        }

        private static string SavedPresetLabel(int slot)
        {
            string presetName = GetPresetValue(slot, "Name");
            string presetWidths = GetPresetValue(slot, "Widths");

            if (string.IsNullOrWhiteSpace(presetName))
            {
                presetName = "Preset " + slot.ToString(CultureInfo.InvariantCulture);
            }

            return string.IsNullOrWhiteSpace(presetWidths)
                ? slot.ToString(CultureInfo.InvariantCulture) + " = " + presetName + " (empty)"
                : slot.ToString(CultureInfo.InvariantCulture) + " = " + presetName + " (" + presetWidths + ")";
        }

        private static string LoadPresetWidths(int slot)
        {
            return GetPresetValue(slot, "Widths");
        }

        private static void SavePreset(int slot, string presetName, string presetWidths)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\VB and VBA Program Settings\SMRI\PanelMaker"))
            {
                key.SetValue("Preset" + slot.ToString(CultureInfo.InvariantCulture) + "Name", presetName);
                key.SetValue("Preset" + slot.ToString(CultureInfo.InvariantCulture) + "Widths", presetWidths);
            }
        }

        private static string GetPresetValue(int slot, string suffix)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\VB and VBA Program Settings\SMRI\PanelMaker"))
                {
                    object value = key != null
                        ? key.GetValue("Preset" + slot.ToString(CultureInfo.InvariantCulture) + suffix)
                        : null;
                    return value != null ? value.ToString() : "";
                }
            }
            catch
            {
                return "";
            }
        }

        private static bool IsHorizontalCut(string directionText)
        {
            string value = directionText.Trim().ToUpperInvariant();
            return value == "H" || value == "2" || value == "HORIZONTAL";
        }

        private static bool IsYes(string valueText)
        {
            string value = valueText.Trim().ToUpperInvariant();
            return value == "Y" || value == "YES" || value == "1";
        }

        private static double Val(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            string trimmed = text.Trim();
            int length = 0;
            while (length < trimmed.Length)
            {
                char c = trimmed[length];
                if (!(char.IsDigit(c) || c == '-' || c == '+' || c == '.' || c == ','))
                {
                    break;
                }

                length++;
            }

            if (length == 0)
            {
                return 0;
            }

            double value;
            return double.TryParse(trimmed.Substring(0, length), NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                   double.TryParse(trimmed.Substring(0, length), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value
                : 0;
        }

        private static bool BuildPanelSuggestions(double artworkWidth, double[] mediaWidths, double overlap, out double[] panelWidths)
        {
            panelWidths = null;
            double minMediaWidth = mediaWidths[0];
            double maxMediaWidth = mediaWidths[mediaWidths.Length - 1];

            if (overlap >= minMediaWidth)
            {
                return false;
            }

            int maxPanels = CeilD(artworkWidth / (minMediaWidth - overlap)) + 2;

            for (int tryPanels = 1; tryPanels <= maxPanels; tryPanels++)
            {
                if (maxMediaWidth + ((tryPanels - 1) * (maxMediaWidth - overlap)) < artworkWidth)
                {
                    continue;
                }

                double targetSum = artworkWidth + ((tryPanels - 1) * overlap);
                int[] counts = new int[mediaWidths.Length];
                int[] bestCounts = new int[mediaWidths.Length];
                double bestWaste = 0;
                bool bestFound = false;

                FindBestCounts(mediaWidths, 0, tryPanels, 0, targetSum, counts, bestCounts, ref bestWaste, ref bestFound);

                if (bestFound)
                {
                    var panels = new List<double>();
                    for (int i = mediaWidths.Length - 1; i >= 0; i--)
                    {
                        for (int n = 0; n < bestCounts[i]; n++)
                        {
                            panels.Add(mediaWidths[i]);
                        }
                    }

                    panelWidths = panels.ToArray();
                    return true;
                }
            }

            return false;
        }

        private static double MarkerLength(double overlap)
        {
            return overlap > 0 && overlap < BlendMarkerLength
                ? overlap
                : BlendMarkerLength;
        }

        private static void CreateBlendMarker(dynamic activeLayer, double x1, double y1, double x2, double y2)
        {
            dynamic marker = activeLayer.CreateLineSegment(x1, y1, x2, y2);
            marker.Name = "Blend_Marker";
            marker.Outline.Width = 0.01;
        }

        private static void CreateHorizontalTMarker(dynamic activeLayer, double markerX, double edgeY,
            double outsideDir, double alongDir, double markerLen)
        {
            double outerY = edgeY + (outsideDir * markerLen);
            CreateBlendMarker(activeLayer, markerX, edgeY, markerX, outerY);
            CreateBlendMarker(activeLayer, markerX, outerY, markerX + (alongDir * markerLen), outerY);
        }

        private static void CreateVerticalTMarker(dynamic activeLayer, double edgeX, double markerY,
            double outsideDir, double alongDir, double markerLen)
        {
            double outerX = edgeX + (outsideDir * markerLen);
            CreateBlendMarker(activeLayer, edgeX, markerY, outerX, markerY);
            CreateBlendMarker(activeLayer, outerX, markerY, outerX, markerY + (alongDir * markerLen));
        }

        private static void AddVerticalPanelSeamMarkers(dynamic activeLayer, double seamX, double startY,
            double panelHeight, double alongDir, double markerLen)
        {
            CreateHorizontalTMarker(activeLayer, seamX, startY, -1, alongDir, markerLen);

            if (panelHeight > 0)
            {
                CreateHorizontalTMarker(activeLayer, seamX, startY + panelHeight, 1, alongDir, markerLen);
            }
        }

        private static void AddHorizontalPanelSeamMarkers(dynamic activeLayer, double startX, double seamY,
            double panelWidth, double alongDir, double markerLen)
        {
            CreateVerticalTMarker(activeLayer, startX, seamY, -1, alongDir, markerLen);

            if (panelWidth > 0)
            {
                CreateVerticalTMarker(activeLayer, startX + panelWidth, seamY, 1, alongDir, markerLen);
            }
        }

        private static void AddBlendMarkers(dynamic activeLayer, double destX, double destY, double panelWidth,
            double panelHeight, int panelIndex, int panelCount, bool horizontalCut, double overlap)
        {
            double markerLen = MarkerLength(overlap);
            if (markerLen <= 0)
            {
                return;
            }

            if (horizontalCut)
            {
                if (panelIndex > 0)
                {
                    AddHorizontalPanelSeamMarkers(activeLayer, destX, destY, panelWidth, 1, markerLen);
                }

                if (panelIndex < panelCount - 1)
                {
                    AddHorizontalPanelSeamMarkers(activeLayer, destX, destY + panelHeight - markerLen, panelWidth, 1, markerLen);
                }
            }
            else
            {
                if (panelIndex > 0)
                {
                    AddVerticalPanelSeamMarkers(activeLayer, destX, destY, panelHeight, 1, markerLen);
                }

                if (panelIndex < panelCount - 1)
                {
                    AddVerticalPanelSeamMarkers(activeLayer, destX + panelWidth - markerLen, destY, panelHeight, 1, markerLen);
                }
            }
        }

        private static void FindBestCounts(
            double[] mediaWidths,
            int index,
            int remainingPanels,
            double currentSum,
            double targetSum,
            int[] counts,
            int[] bestCounts,
            ref double bestWaste,
            ref bool bestFound)
        {
            if (index == mediaWidths.Length - 1)
            {
                counts[index] = remainingPanels;
                double totalSum = currentSum + (remainingPanels * mediaWidths[index]);

                if (totalSum >= targetSum)
                {
                    double waste = totalSum - targetSum;
                    if (!bestFound || waste < bestWaste - 0.001)
                    {
                        bestFound = true;
                        bestWaste = waste;
                        Array.Copy(counts, bestCounts, counts.Length);
                    }
                }

                return;
            }

            for (int count = 0; count <= remainingPanels; count++)
            {
                counts[index] = count;
                FindBestCounts(mediaWidths, index + 1, remainingPanels - count,
                    currentSum + (count * mediaWidths[index]), targetSum,
                    counts, bestCounts, ref bestWaste, ref bestFound);
            }
        }

        private static int CeilD(double value)
        {
            return value <= Math.Floor(value)
                ? Convert.ToInt32(Math.Floor(value))
                : Convert.ToInt32(Math.Floor(value) + 1);
        }

        private static string FormatInches(double value)
        {
            return Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
