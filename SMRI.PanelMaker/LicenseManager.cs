using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace SMRI.PanelMaker
{
    internal sealed class LicenseManager
    {
        private const string ProductName = "SMRI Panel Maker";
        private static readonly string LicenseDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SMRI", "PanelMaker");
        private static readonly string LicensePath = Path.Combine(LicenseDirectory, "license.json");

        public bool EnsureActivated()
        {
            string machineId = GetMachineId();
            ActivationFile existing = ReadActivationFile();

            if (existing != null &&
                existing.Valid &&
                string.Equals(existing.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string licenseKey = Interaction.InputBox("Enter your SMRI Panel Maker license key:", ProductName, "");
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                MessageBox.Show("A valid license key is required.", ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            ActivationResponse response = ActivateLicense(licenseKey.Trim(), machineId);
            if (response == null || !response.Valid)
            {
                string message = response != null && !string.IsNullOrWhiteSpace(response.Message)
                    ? response.Message
                    : "License activation failed.";
                MessageBox.Show(message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            Directory.CreateDirectory(LicenseDirectory);
            WriteJson(LicensePath, new ActivationFile
            {
                Valid = true,
                LicenseKey = licenseKey.Trim(),
                MachineId = machineId,
                ActivationToken = response.ActivationToken,
                Message = response.Message,
                ActivatedUtc = DateTime.UtcNow.ToString("o")
            });

            return true;
        }

        private static ActivationResponse ActivateLicense(string licenseKey, string machineId)
        {
            string apiUrl = ConfigurationManager.AppSettings["LicenseApiUrl"];
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                throw new InvalidOperationException("Set LicenseApiUrl in App.config before distributing SMRI Panel Maker.");
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(BuildActivationUrl(apiUrl, licenseKey, machineId));
            request.Method = "POST";
            request.Accept = "application/json";
            request.Timeout = 30000;
            request.ContentLength = 0;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    return ReadActivationResponse(stream);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                {
                    throw;
                }

                using (Stream stream = ex.Response.GetResponseStream())
                {
                    ActivationResponse response = ReadActivationResponse(stream);
                    return response ?? new ActivationResponse { Valid = false, Message = "License activation failed." };
                }
            }
        }

        private static string BuildActivationUrl(string configuredUrl, string licenseKey, string machineId)
        {
            int queryStart = configuredUrl.IndexOf('?');
            string baseUrl = queryStart >= 0 ? configuredUrl.Substring(0, queryStart) : configuredUrl;
            string separator = baseUrl.Contains("?") ? "&" : "?";

            return baseUrl + separator +
                "machineId=" + Uri.EscapeDataString(machineId) +
                "&licenseKey=" + Uri.EscapeDataString(licenseKey);
        }

        private static ActivationResponse ReadActivationResponse(Stream stream)
        {
            ActivationEnvelope envelope;
            try
            {
                envelope = ReadJson<ActivationEnvelope>(stream);
            }
            catch (SerializationException)
            {
                envelope = null;
            }

            if (envelope == null || envelope.Message == null)
            {
                return new ActivationResponse
                {
                    Valid = false,
                    Message = "Invalid license key."
                };
            }

            return envelope.Message;
        }

        private static ActivationFile ReadActivationFile()
        {
            if (!File.Exists(LicensePath))
            {
                return null;
            }

            try
            {
                using (FileStream stream = File.OpenRead(LicensePath))
                {
                    return ReadJson<ActivationFile>(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        private static string GetMachineId()
        {
            string machineGuid = ReadMachineGuid();
            string raw = Environment.MachineName + "|" + Environment.UserDomainName + "|" + machineGuid;

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private static string ReadMachineGuid()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    object value = key != null ? key.GetValue("MachineGuid") : null;
                    return value != null ? value.ToString() : "";
                }
            }
            catch
            {
                return "";
            }
        }

        private static T ReadJson<T>(Stream stream) where T : class
        {
            if (stream == null)
            {
                return null;
            }

            var serializer = new DataContractJsonSerializer(typeof(T));
            return serializer.ReadObject(stream) as T;
        }

        private static void WriteJson<T>(string path, T value)
        {
            using (FileStream stream = File.Create(path))
            {
                WriteJson(stream, value);
            }
        }

        private static void WriteJson<T>(Stream stream, T value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(stream, value);
        }

        [DataContract]
        private sealed class ActivationEnvelope
        {
            [DataMember(Name = "message")]
            public ActivationResponse Message { get; set; }
        }

        [DataContract]
        private sealed class ActivationResponse
        {
            [DataMember(Name = "valid")]
            public bool Valid { get; set; }

            [DataMember(Name = "message")]
            public string Message { get; set; }

            [DataMember(Name = "activationToken")]
            public string ActivationToken { get; set; }
        }

        [DataContract]
        private sealed class ActivationFile
        {
            [DataMember(Name = "valid")]
            public bool Valid { get; set; }

            [DataMember(Name = "licenseKey")]
            public string LicenseKey { get; set; }

            [DataMember(Name = "machineId")]
            public string MachineId { get; set; }

            [DataMember(Name = "activationToken")]
            public string ActivationToken { get; set; }

            [DataMember(Name = "message")]
            public string Message { get; set; }

            [DataMember(Name = "activatedUtc")]
            public string ActivatedUtc { get; set; }
        }
    }
}
