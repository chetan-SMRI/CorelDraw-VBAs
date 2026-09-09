using Microsoft.VisualBasic;
using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace SMRI.PanelMaker
{
    internal sealed class LicenseManager
    {
        private const string ProductName = "SMRI Panel Maker";
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SMRI.PanelMaker.OfflineLicense.v1");
        private static readonly string LicenseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMRI", "PanelMaker");
        private static readonly string LicensePath = Path.Combine(LicenseDirectory, "license.dat");

        public bool EnsureActivated()
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                DateTime activated, expires, lastSeen;
                if (TryReadLicense(out activated, out expires, out lastSeen) &&
                    IsLicenseValid(activated, expires, lastSeen, now))
                {
                    SaveLicense(activated, expires, now > lastSeen ? now : lastSeen);
                    return true;
                }

                string code = Interaction.InputBox(
                    "This computer is not activated, or its one-year activation has expired.\n" +
                    "Contact SMRI for the current 8-digit activation code, then enter it here:",
                    ProductName + " Activation", "");
                if (string.IsNullOrWhiteSpace(code)) return false;

                now = DateTime.UtcNow;
                if (!IsActivationCodeValid(code.Trim(), now))
                {
                    MessageBox.Show("Invalid or expired activation code. Check the computer clock and request a new code.",
                        ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                expires = now.AddYears(1);
                SaveLicense(now, expires, now);
                MessageBox.Show("Activation successful. Licensed until " +
                    expires.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) + ".",
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not read or save local activation.\n" + ex.Message,
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Matches Adobe/TOTP_gen.py exactly; this is the existing custom time-code
        // scheme, not RFC 6238 TOTP. Use long arithmetic to avoid 32-bit overflow.
        internal static string ActivationCode(long minute)
        {
            long value = minute % 100000000L;
            value = (value * 48271L + 917263L) % 100000000L;
            value = (value * 69621L + 123457L) % 100000000L;
            return value.ToString("D8", CultureInfo.InvariantCulture);
        }

        internal static bool IsActivationCodeValid(string code, DateTime now)
        {
            if (code == null || code.Length != 8 || now < Epoch) return false;
            for (int i = 0; i < code.Length; i++)
                if (code[i] < '0' || code[i] > '9') return false;
            long minute = (long)(now - Epoch).TotalMinutes;
            return code == ActivationCode(minute) || code == ActivationCode(minute - 1) ||
                code == ActivationCode(minute + 1);
        }

        internal static bool IsLicenseValid(DateTime activated, DateTime expires, DateTime lastSeen, DateTime now)
        {
            return activated >= Epoch && activated.Year < 9999 &&
                expires == activated.AddYears(1) && lastSeen >= activated && lastSeen < expires &&
                now >= activated.AddMinutes(-5) && now < expires && now >= lastSeen.AddMinutes(-5);
        }

        private static bool TryReadLicense(out DateTime activated, out DateTime expires, out DateTime lastSeen)
        {
            activated = expires = lastSeen = DateTime.MinValue;
            try
            {
                byte[] data = ProtectedData.Unprotect(File.ReadAllBytes(LicensePath), Entropy,
                    DataProtectionScope.CurrentUser);
                string[] fields = Encoding.UTF8.GetString(data).Split('|');
                return fields.Length == 4 && fields[0] == "SMRI-OFFLINE-1" &&
                    DateTime.TryParseExact(fields[1], "O", CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out activated) &&
                    DateTime.TryParseExact(fields[2], "O", CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out expires) &&
                    DateTime.TryParseExact(fields[3], "O", CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out lastSeen) &&
                    activated.Kind == DateTimeKind.Utc && expires.Kind == DateTimeKind.Utc &&
                    lastSeen.Kind == DateTimeKind.Utc;
            }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
            catch (CryptographicException) { return false; }
        }

        private static void SaveLicense(DateTime activated, DateTime expires, DateTime lastSeen)
        {
            string text = "SMRI-OFFLINE-1|" + activated.ToString("O", CultureInfo.InvariantCulture) + "|" +
                expires.ToString("O", CultureInfo.InvariantCulture) + "|" +
                lastSeen.ToString("O", CultureInfo.InvariantCulture);
            byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(text), Entropy,
                DataProtectionScope.CurrentUser);
            Directory.CreateDirectory(LicenseDirectory);
            string temporaryPath = Path.Combine(LicenseDirectory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllBytes(temporaryPath, data);
                if (File.Exists(LicensePath)) File.Replace(temporaryPath, LicensePath, null);
                else File.Move(temporaryPath, LicensePath);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
    }
}
