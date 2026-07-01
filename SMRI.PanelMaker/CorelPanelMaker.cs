using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SMRI.PanelMaker
{
    internal sealed class CorelPanelMaker
    {
        private const int CdrInch = 3;
        private const bool CdrFalse = false;
        private const double Gap = 1.0;

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

            double? overlapValue = PromptForOverlap(mediaWidths[0]);
            if (!overlapValue.HasValue)
            {
                return;
            }

            double overlap = overlapValue.Value;
            object oldUnit = document.Unit;

            try
            {
                document.Unit = CdrInch;
                CreatePanels(document, selection, mediaWidths, overlap);
            }
            finally
            {
                document.Unit = oldUnit;
            }
        }

        private static void CreatePanels(dynamic document, dynamic source, double[] mediaWidths, double overlap)
        {
            double x = 0;
            double y = 0;
            double width = 0;
            double height = 0;
            source.GetBoundingBox(ref x, ref y, ref width, ref height, true);

            double[] panelWidths;
            if (!BuildPanelSuggestions(width, mediaWidths, overlap, out panelWidths))
            {
                MessageBox.Show("Could not build panel suggestions from the entered media widths.", "SMRI Panel Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double startX = x;
            double startY = y - height - Gap;
            double currentSourceX = x;
            double currentDestX = startX;
            int createdPanels = 0;
            var suggestionText = new System.Text.StringBuilder();
            suggestionText.AppendLine("Panel suggestions:");
            dynamic activeLayer = document.ActiveLayer;

            document.BeginCommandGroup("SMRI Auto Panel PowerClips");

            try
            {
                for (int i = 0; i < panelWidths.Length; i++)
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

                    dynamic label = activeLayer.CreateArtisticText(destX, startY + height + 0.04,
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
            finally
            {
                document.EndCommandGroup();
            }

            MessageBox.Show(createdPanels + " panels created successfully." + Environment.NewLine + Environment.NewLine + suggestionText,
                "SMRI Panel Maker", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static dynamic GetRunningCorelDraw()
        {
            string[] progIds =
            {
                "CorelDRAW.Application.25",
                "CorelDRAW.Application.24",
                "CorelDRAW.Application"
            };

            foreach (string progId in progIds)
            {
                try
                {
                    return Marshal.GetActiveObject(progId);
                }
                catch (COMException)
                {
                }
            }

            throw new InvalidOperationException("CorelDRAW 2024 is not running. Open CorelDRAW 2024 and try again.");
        }

        private static double[] PromptForMediaWidths()
        {
            string mediaText = Interaction.InputBox(
                "Enter available media widths in inches, separated by commas:",
                "SMRI Panel Maker",
                "39,49,59");

            double[] mediaWidths;
            if (!TryParseMediaWidths(mediaText, out mediaWidths))
            {
                MessageBox.Show("Please enter valid media widths, like 39,49,59.", "SMRI Panel Maker",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return mediaWidths;
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
