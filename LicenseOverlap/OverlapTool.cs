using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualBasic;
using corel = Corel.Interop.VGCore;

namespace LicenseOverlap
{
    public static class OverlapTool
    {
        public static void RunOverlapTool(corel.Application app)
        {
            if (app == null)
            {
                System.Windows.MessageBox.Show("CorelDRAW application object is not available.");
                return;
            }

            var licenseManager = new LicenseManager();
            if (!licenseManager.EnsureActivated())
            {
                return;
            }

            if (app.ActiveSelectionRange.Count == 0)
            {
                app.MsgShow("Please select your artwork/image first.");
                return;
            }

            var oldUnit = app.ActiveDocument.Unit;
            app.ActiveDocument.Unit = corel.cdrUnit.cdrInch;
            bool commandGroupOpen = false;

            try
            {
                string mediaText = Interaction.InputBox(
                    "Enter available media widths in inches, separated by commas:",
                    "SMRI Panel Maker",
                    "39,49,59");

                List<double> mediaWidths;
                if (!TryParseMediaWidths(mediaText, out mediaWidths))
                {
                    app.MsgShow("Please enter valid media widths, like 39,49,59.");
                    return;
                }

                string overlapText = Interaction.InputBox(
                    "Enter overlap in inches:",
                    "SMRI Panel Maker",
                    "0.75");

                double overlap;
                if (!double.TryParse(overlapText, NumberStyles.Float, CultureInfo.CurrentCulture, out overlap) &&
                    !double.TryParse(overlapText, NumberStyles.Float, CultureInfo.InvariantCulture, out overlap))
                {
                    app.MsgShow("Please enter a valid overlap, like 0.75.");
                    return;
                }

                const double gap = 1.0;
                if (overlap < 0)
                {
                    overlap = 0;
                }

                if (overlap >= mediaWidths[0])
                {
                    app.MsgShow("Overlap must be smaller than the smallest media width.");
                    return;
                }

                corel.ShapeRange src = app.ActiveSelectionRange;
                double x;
                double y;
                double totalWidth;
                double height;
                src.GetBoundingBox(out x, out y, out totalWidth, out height, true);

                List<double> suggestedWidths = BuildPanelSuggestions(totalWidth, mediaWidths, overlap);
                if (suggestedWidths.Count == 0)
                {
                    app.MsgShow("Could not build panel suggestions from the entered media widths.");
                    return;
                }

                double startX = x;
                double startY = y - height - gap;
                double currentSourceX = x;
                double currentDestX = startX;
                int createdPanels = 0;
                string suggestionText = "Panel suggestions:" + Environment.NewLine;

                app.ActiveDocument.BeginCommandGroup("SMRI Auto Panel PowerClips");
                commandGroupOpen = true;

                for (int i = 0; i < suggestedWidths.Count; i++)
                {
                    double panelLeft = currentSourceX;
                    double panelWidth = suggestedWidths[i];
                    double remainingWidth = (x + totalWidth) - panelLeft;

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
                    suggestionText += "Panel " + (i + 1) + " " + FormatInches(panelWidth) + "\"" + Environment.NewLine;

                    corel.Shape box = app.ActiveLayer.CreateRectangle2(destX, startY, panelWidth, height);
                    box.Name = "Panel_" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                    box.Fill.ApplyNoFill();
                    box.Outline.Width = 0.01;

                    corel.ShapeRange dup = src.Duplicate();
                    dup.Move(destX - panelLeft, startY - y);
                    dup.AddToPowerClip(box, corel.cdrTriState.cdrFalse);

                    corel.Shape label = app.ActiveLayer.CreateArtisticText(
                        destX,
                        startY + height + 0.04,
                        "Tile " + (i + 1) +
                        " | Total Width: " + FormatInches(totalWidth) + "\"" +
                        " | Panel Width: " + FormatInches(panelWidth) + "\"");

                    label.Text.Story.Size = 18;
                    label.Text.Story.Font = "Arial";
                    label.Text.Story.Bold = true;

                    currentSourceX += panelWidth - overlap;
                    currentDestX += panelWidth + gap;
                }

                app.ActiveDocument.EndCommandGroup();
                commandGroupOpen = false;
                app.MsgShow(createdPanels + " panels created successfully." + Environment.NewLine + Environment.NewLine + suggestionText);
            }
            finally
            {
                if (commandGroupOpen)
                {
                    try
                    {
                        app.ActiveDocument.EndCommandGroup();
                    }
                    catch
                    {
                    }
                }

                app.ActiveDocument.Unit = oldUnit;
            }
        }

        private static int CeilD(double value)
        {
            return (int)Math.Ceiling(value);
        }

        private static string FormatInches(double value)
        {
            return Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static bool TryParseMediaWidths(string widthText, out List<double> widths)
        {
            widths = new List<double>();

            if (string.IsNullOrWhiteSpace(widthText))
            {
                return false;
            }

            foreach (string part in widthText.Split(','))
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

                if (!widths.Any(existing => Math.Abs(existing - width) < 0.001))
                {
                    widths.Add(width);
                }
            }

            widths.Sort();
            return widths.Count > 0;
        }

        private static List<double> BuildPanelSuggestions(double artworkWidth, List<double> mediaWidths, double overlap)
        {
            double minMediaWidth = mediaWidths[0];
            double maxMediaWidth = mediaWidths[mediaWidths.Count - 1];
            int maxPanels = CeilD(artworkWidth / (minMediaWidth - overlap)) + 2;

            for (int panelCount = 1; panelCount <= maxPanels; panelCount++)
            {
                if (maxMediaWidth + ((panelCount - 1) * (maxMediaWidth - overlap)) < artworkWidth)
                {
                    continue;
                }

                double targetSum = artworkWidth + ((panelCount - 1) * overlap);
                int[] counts = new int[mediaWidths.Count];
                int[] bestCounts = new int[mediaWidths.Count];
                bool bestFound = false;
                double bestWaste = 0;

                FindBestCounts(mediaWidths, 0, panelCount, 0, targetSum, counts, bestCounts, ref bestWaste, ref bestFound);

                if (bestFound)
                {
                    var panels = new List<double>();
                    for (int i = mediaWidths.Count - 1; i >= 0; i--)
                    {
                        for (int n = 0; n < bestCounts[i]; n++)
                        {
                            panels.Add(mediaWidths[i]);
                        }
                    }

                    return panels;
                }
            }

            return new List<double>();
        }

        private static void FindBestCounts(
            List<double> mediaWidths,
            int index,
            int remainingPanels,
            double currentSum,
            double targetSum,
            int[] counts,
            int[] bestCounts,
            ref double bestWaste,
            ref bool bestFound)
        {
            if (index == mediaWidths.Count - 1)
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
                FindBestCounts(
                    mediaWidths,
                    index + 1,
                    remainingPanels - count,
                    currentSum + (count * mediaWidths[index]),
                    targetSum,
                    counts,
                    bestCounts,
                    ref bestWaste,
                    ref bestFound);
            }
        }
    }
}
