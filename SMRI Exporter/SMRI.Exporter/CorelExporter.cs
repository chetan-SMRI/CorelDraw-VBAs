using Microsoft.VisualBasic;
using Microsoft.Win32;
using VGCore = Corel.Interop.VGCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SMRI.Exporter
{
    internal sealed class CorelExporter
    {
        private const string ProductName = "SMRI Exporter";
        private const int CdrInch = 1;
        private const int PdfSelection = 2;
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMRI", "Exporter");
        private static readonly string ExportFolderSettingPath = Path.Combine(SettingsDirectory, "export-folder.txt");

        private sealed class ExportChoice
        {
            public string Name;
            public string Extension;
            public VGCore.cdrFilter Filter;
            public bool IsPdf;
        }

        public void Run()
        {
            dynamic app = GetRunningCorelDraw();
            dynamic document = app.ActiveDocument;
            if (document == null)
            {
                MessageBox.Show("Please open a CorelDRAW document first.", ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            dynamic selection = app.ActiveSelectionRange;
            if (selection == null || selection.Count == 0)
            {
                MessageBox.Show("Select the objects you want to export first.", ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string sequenceStyle;
            ExportChoice exportChoice;
            int dpi;
            int jpegQuality;
            string prefix;
            string suffix;
            string folderPath;

            using (var form = new ExportOptionsForm(DocumentBaseName(document), ReadSavedExportFolder()))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                sequenceStyle = form.SequenceStyle;
                exportChoice = ExportChoiceFor(form.ExportFormat);
                dpi = form.Dpi;
                jpegQuality = form.JpegQuality;
                prefix = CleanFilePart(form.FilePrefix);
                suffix = CleanFilePart(form.FileSuffix);
                SaveExportFolder(form.RememberFolder ? form.BaseFolder : null);
                folderPath = CreateTimestampedExportFolder(form.BaseFolder);
            }

            ExportObjects(app, document, selection, exportChoice, sequenceStyle, dpi,
                jpegQuality, prefix, suffix, folderPath);
        }

        private static ExportChoice ExportChoiceFor(string format)
        {
            switch (format)
            {
                case "PNG":
                    return new ExportChoice { Name = "PNG", Extension = ".png", Filter = VGCore.cdrFilter.cdrPNG };
                case "TIFF":
                    return new ExportChoice { Name = "TIFF", Extension = ".tif", Filter = VGCore.cdrFilter.cdrTIFF };
                case "PDF":
                    return new ExportChoice { Name = "PDF", Extension = ".pdf", IsPdf = true };
                default:
                    return new ExportChoice { Name = "JPG", Extension = ".jpg", Filter = VGCore.cdrFilter.cdrJPEG };
            }
        }

        private static void SaveExportFolder(string baseFolder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseFolder))
                {
                    if (File.Exists(ExportFolderSettingPath)) File.Delete(ExportFolderSettingPath);
                    return;
                }

                Directory.CreateDirectory(SettingsDirectory);
                File.WriteAllText(ExportFolderSettingPath, baseFolder, Encoding.UTF8);
            }
            catch { }
        }

        private static string CreateTimestampedExportFolder(string baseFolder)
        {
            Directory.CreateDirectory(baseFolder);
            string requestedFolder = Path.Combine(baseFolder,
                DateTime.Now.ToString("dd-MM-yy HH-mm", CultureInfo.InvariantCulture));
            string exportFolder = UniqueDirectoryPath(requestedFolder);
            Directory.CreateDirectory(exportFolder);
            return exportFolder;
        }

        private static string ReadSavedExportFolder()
        {
            try
            {
                return File.Exists(ExportFolderSettingPath)
                    ? File.ReadAllText(ExportFolderSettingPath, Encoding.UTF8).Trim()
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static string UniqueDirectoryPath(string requestedPath)
        {
            if (!Directory.Exists(requestedPath)) return requestedPath;
            for (int copy = 2; ; copy++)
            {
                string candidate = requestedPath + " (" + copy.ToString(CultureInfo.InvariantCulture) + ")";
                if (!Directory.Exists(candidate)) return candidate;
            }
        }

        private static void ExportObjects(dynamic app, dynamic document, dynamic selection,
            ExportChoice exportChoice, string sequenceStyle, int dpi, int jpegQuality,
            string prefix, string suffix, string folderPath)
        {
            dynamic originals = selection.All();
            int count = originals.Count;
            object oldUnit = document.Unit;
            object oldPdfRange = null;
            bool pdfRangeSaved = false;
            bool progressStarted = false;
            int exportedCount = 0;
            int failedCount = 0;
            bool cancelled = false;
            var failures = new StringBuilder();

            try
            {
                document.Unit = CdrInch;
                if (exportChoice.IsPdf)
                {
                    oldPdfRange = document.PDFSettings.PublishRange;
                    pdfRangeSaved = true;
                    document.PDFSettings.PublishRange = PdfSelection;
                }

                app.Status.BeginProgress("Exporting 0/" + count, true);
                progressStarted = true;

                for (int i = 1; i <= count; i++)
                {
                    if (app.Status.Aborted)
                    {
                        cancelled = true;
                        break;
                    }

                    dynamic shape = originals.Item(i);
                    double x = 0;
                    double y = 0;
                    double width = 0;
                    double height = 0;
                    shape.GetBoundingBox(ref x, ref y, ref width, ref height, true);

                    string sequence = SequenceLabel(i, sequenceStyle);
                    string fileName = BuildFileName(sequence, prefix, width, height,
                        suffix, exportChoice.Extension);
                    string filePath = UniqueFilePath(Path.Combine(folderPath, fileName));

                    try
                    {
                        shape.CreateSelection();
                        if (exportChoice.IsPdf)
                        {
                            document.PublishToPDF(filePath);
                        }
                        else
                        {
                            ExportRaster(document, filePath, exportChoice, dpi, jpegQuality);
                        }

                        exportedCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        failures.AppendLine(i + ": " + fileName + " - " + ex.Message);
                    }

                    app.Status.SetProgressMessage(i + "/" + count + " exported");
                    app.Status.Progress = (i * 100) / count;
                    Application.DoEvents();
                }
            }
            finally
            {
                try
                {
                    if (progressStarted) app.Status.EndProgress();
                }
                catch { }

                try
                {
                    if (pdfRangeSaved) document.PDFSettings.PublishRange = oldPdfRange;
                }
                catch { }

                try { document.Unit = oldUnit; } catch { }
                try { originals.CreateSelection(); } catch { }
            }

            var summary = new StringBuilder();
            summary.AppendLine(exportedCount + " of " + count + " objects exported to:");
            summary.Append(folderPath);

            if (failedCount > 0)
            {
                summary.AppendLine().AppendLine();
                summary.AppendLine(failedCount + " failed:");
                summary.Append(failures);
            }

            if (cancelled)
            {
                summary.AppendLine().AppendLine();
                summary.Append("Export was cancelled.");
            }

            MessageBox.Show(summary.ToString(), ProductName, MessageBoxButtons.OK,
                failedCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private static void ExportRaster(dynamic document, string filePath,
            ExportChoice choice, int dpi, int jpegQuality)
        {
            VGCore.IVGDocument typedDocument = (VGCore.IVGDocument)document;
            dynamic exportFilter = typedDocument.ExportBitmap(filePath, choice.Filter,
                VGCore.cdrExportRange.cdrSelection, VGCore.cdrImageType.cdrRGBColorImage,
                0, 0, dpi, dpi);

            if (choice.Name == "JPG")
            {
                exportFilter.Compression = 100 - jpegQuality;
                exportFilter.Smoothing = 0;
                exportFilter.Optimized = true;
                exportFilter.Progressive = false;
            }

            exportFilter.Finish();
        }

        private static string PromptForSequenceStyle()
        {
            string choice = Interaction.InputBox(
                "Choose the filename sequence:\n\n" +
                "1 = 1, 2, 3...\n" +
                "a = a, b, c...\n" +
                "A = A, B, C...", ProductName, "a").Trim();

            if (choice.Length == 0) return null;
            if (choice == "1" || choice == "a" || choice == "A") return choice;

            MessageBox.Show("Enter 1, a, or A.", ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        private static ExportChoice PromptForFormat()
        {
            string choice = Interaction.InputBox(
                "Choose export format:\n\n" +
                "1 = JPG\n" +
                "2 = PNG\n" +
                "3 = TIFF\n" +
                "4 = PDF", ProductName, "1").Trim().ToUpperInvariant();

            switch (choice)
            {
                case "1":
                case "JPG":
                case "JPEG":
                    return new ExportChoice { Name = "JPG", Extension = ".jpg", Filter = VGCore.cdrFilter.cdrJPEG };
                case "2":
                case "PNG":
                    return new ExportChoice { Name = "PNG", Extension = ".png", Filter = VGCore.cdrFilter.cdrPNG };
                case "3":
                case "TIF":
                case "TIFF":
                    return new ExportChoice { Name = "TIFF", Extension = ".tif", Filter = VGCore.cdrFilter.cdrTIFF };
                case "4":
                case "PDF":
                    return new ExportChoice { Name = "PDF", Extension = ".pdf", IsPdf = true };
                case "":
                    return null;
                default:
                    MessageBox.Show("Choose 1, 2, 3, or 4.", ProductName,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
            }
        }

        private static bool PromptForWholeNumber(string prompt, int defaultValue,
            int minimum, int maximum, out int result)
        {
            result = defaultValue;
            string value = Interaction.InputBox(prompt, ProductName,
                defaultValue.ToString(CultureInfo.InvariantCulture)).Trim();
            if (value.Length == 0) return false;

            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result) ||
                result < minimum || result > maximum)
            {
                MessageBox.Show("Enter a whole number from " + minimum + " to " + maximum + ".",
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private static string SequenceLabel(int sequence, string style)
        {
            if (style == "1") return sequence.ToString(CultureInfo.InvariantCulture);

            var result = new StringBuilder();
            int value = sequence;
            while (value > 0)
            {
                value--;
                result.Insert(0, (char)('a' + (value % 26)));
                value /= 26;
            }

            string text = result.ToString();
            return style == "A" ? text.ToUpperInvariant() : text;
        }

        private static string BuildFileName(string sequence, string prefix,
            double width, double height, string suffix, string extension)
        {
            var parts = new List<string> { sequence };
            if (!string.IsNullOrWhiteSpace(prefix)) parts.Add(prefix);
            parts.Add(FormatInches(width) + "x" + FormatInches(height) + " Inch");
            if (!string.IsNullOrWhiteSpace(suffix)) parts.Add(suffix);
            return string.Join(" ", parts) + extension;
        }

        private static string FormatInches(double value)
        {
            return Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string DocumentBaseName(dynamic document)
        {
            string name = Convert.ToString(document.Name, CultureInfo.CurrentCulture);
            return CleanFilePart(Path.GetFileNameWithoutExtension(name));
        }

        private static string CleanFilePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string result = value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalid, '-');
            }

            while (result.Contains("  ")) result = result.Replace("  ", " ");
            return result;
        }

        private static string UniqueFilePath(string requestedPath)
        {
            if (!File.Exists(requestedPath)) return requestedPath;

            string directory = Path.GetDirectoryName(requestedPath);
            string name = Path.GetFileNameWithoutExtension(requestedPath);
            string extension = Path.GetExtension(requestedPath);

            for (int copy = 2; ; copy++)
            {
                string candidate = Path.Combine(directory, name + " (" + copy + ")" + extension);
                if (!File.Exists(candidate)) return candidate;
            }
        }

        private static dynamic GetRunningCorelDraw()
        {
            var attemptedProgIds = new List<string>();
            var errors = new List<string>();

            foreach (string progId in GetCorelDrawProgIds())
            {
                attemptedProgIds.Add(progId);
                try
                {
                    return Marshal.GetActiveObject(progId);
                }
                catch (COMException ex)
                {
                    errors.Add(progId + ": GetActiveObject failed 0x" +
                        ex.ErrorCode.ToString("X8", CultureInfo.InvariantCulture));
                }

                try
                {
                    Type appType = Type.GetTypeFromProgID(progId, false);
                    if (appType != null) return Activator.CreateInstance(appType);
                }
                catch (Exception ex)
                {
                    errors.Add(progId + ": " + ex.GetType().Name + " " + ex.Message);
                }
            }

            WriteComDiagnosticLog(attemptedProgIds, errors);
            throw new InvalidOperationException(
                "CorelDRAW is not running, its COM automation server is not registered, or Windows is blocking access because CorelDRAW and SMRI Exporter are running at different permission levels." +
                Environment.NewLine + Environment.NewLine +
                "Open CorelDRAW normally, then run SMRI Exporter normally. Do not run one as administrator unless both are running as administrator." +
                Environment.NewLine + Environment.NewLine +
                "Diagnostic log: " + GetComDiagnosticLogPath());
        }

        private static IEnumerable<string> GetCorelDrawProgIds()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string progId in GetRegistryCorelDrawProgIds())
                if (seen.Add(progId)) yield return progId;

            string[] commonProgIds =
            {
                "CorelDRAW.Application",
                "CorelDRAW.Application.26",
                "CorelDRAW.Application.25",
                "CorelDRAW.Application.24"
            };

            foreach (string progId in commonProgIds)
                if (seen.Add(progId)) yield return progId;

            for (int version = 35; version >= 17; version--)
            {
                string progId = "CorelDRAW.Application." + version.ToString(CultureInfo.InvariantCulture);
                if (seen.Add(progId)) yield return progId;
            }
        }

        private static IEnumerable<string> GetRegistryCorelDrawProgIds()
        {
            var progIds = new List<string>();
            try
            {
                foreach (string subKeyName in Registry.ClassesRoot.GetSubKeyNames())
                    if (subKeyName.StartsWith("CorelDRAW.Application", StringComparison.OrdinalIgnoreCase))
                        progIds.Add(subKeyName);
            }
            catch { }

            return progIds.OrderByDescending(GetProgIdVersion)
                .ThenByDescending(p => p, StringComparer.OrdinalIgnoreCase);
        }

        private static double GetProgIdVersion(string progId)
        {
            int lastDot = progId.LastIndexOf('.');
            if (lastDot < 0 || lastDot == progId.Length - 1) return 0;
            double version;
            return double.TryParse(progId.Substring(lastDot + 1), NumberStyles.Float,
                CultureInfo.InvariantCulture, out version) ? version : 0;
        }

        private static void WriteComDiagnosticLog(IEnumerable<string> attemptedProgIds,
            IEnumerable<string> errors)
        {
            try
            {
                string path = GetComDiagnosticLogPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path,
                    "SMRI Exporter CorelDRAW COM diagnostic" + Environment.NewLine +
                    "Time: " + DateTime.Now.ToString("o", CultureInfo.InvariantCulture) + Environment.NewLine +
                    "Is64BitProcess: " + Environment.Is64BitProcess + Environment.NewLine +
                    "Is64BitOperatingSystem: " + Environment.Is64BitOperatingSystem + Environment.NewLine +
                    Environment.NewLine + "Attempted ProgIDs:" + Environment.NewLine +
                    string.Join(Environment.NewLine, attemptedProgIds) + Environment.NewLine +
                    Environment.NewLine + "Errors:" + Environment.NewLine +
                    string.Join(Environment.NewLine, errors));
            }
            catch { }
        }

        private static string GetComDiagnosticLogPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SMRI", "Exporter", "coreldraw-com-diagnostic.txt");
        }
    }
}
