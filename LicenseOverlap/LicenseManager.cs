using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.Win32;

namespace LicenseOverlap
{
    public class LicenseManager
    {
        private const string ValidOfflineLicenseKey = "12345";

        // Later, paste your real API URL here and call ValidateWithServer instead
        // of ValidateOfflineOnly. Expected payload: licenseKey + machineId.
        private const string LicenseApiUrl = "https://your-domain.com/api/license/activate";

        private readonly string activationPath;

        public LicenseManager()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SMRI",
                "LicenseOverlap");
            Directory.CreateDirectory(dir);
            activationPath = Path.Combine(dir, "activation.dat");
        }

        public bool EnsureActivated()
        {
            string machineId = GetMachineId();

            if (HasValidLocalActivation(machineId))
            {
                return true;
            }

            string licenseKey = Interaction.InputBox(
                "Enter your LicenseOverlap license key:",
                "LicenseOverlap Activation",
                "");

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                return false;
            }

            licenseKey = licenseKey.Trim();

            if (!ValidateOfflineOnly(licenseKey, machineId))
            {
                System.Windows.MessageBox.Show("Invalid license key.");
                return false;
            }

            SaveLocalActivation(licenseKey, machineId);
            return true;
        }

        private bool ValidateOfflineOnly(string licenseKey, string machineId)
        {
            return licenseKey == ValidOfflineLicenseKey;
        }

        /*
        private bool ValidateWithServer(string licenseKey, string machineId)
        {
            // Paste your real license API URL into LicenseApiUrl above.
            // POST JSON like:
            // { "licenseKey": "...", "machineId": "..." }
            // Return true only when your server responds with an active/valid result.
            throw new NotImplementedException();
        }
        */

        private bool HasValidLocalActivation(string machineId)
        {
            try
            {
                if (!File.Exists(activationPath))
                {
                    return false;
                }

                byte[] protectedBytes = File.ReadAllBytes(activationPath);
                byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                string stored = Encoding.UTF8.GetString(plainBytes);
                string expected = BuildActivationToken(ValidOfflineLicenseKey, machineId);
                return SlowEquals(stored, expected);
            }
            catch
            {
                return false;
            }
        }

        private void SaveLocalActivation(string licenseKey, string machineId)
        {
            string token = BuildActivationToken(licenseKey, machineId);
            byte[] plainBytes = Encoding.UTF8.GetBytes(token);
            byte[] protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(activationPath, protectedBytes);
        }

        private string BuildActivationToken(string licenseKey, string machineId)
        {
            return Sha256(licenseKey + "|" + machineId + "|LicenseOverlap-v1");
        }

        private string GetMachineId()
        {
            string machineGuid = "";

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    machineGuid = Convert.ToString(key?.GetValue("MachineGuid")) ?? "";
                }
            }
            catch
            {
            }

            string raw = Environment.MachineName + "|" + Environment.UserName + "|" + machineGuid;
            return Sha256(raw);
        }

        private static string Sha256(string input)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static bool SlowEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }
}
