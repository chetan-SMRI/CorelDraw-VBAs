using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Globalization;
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
        private const string ApiBaseUrl = "https://shrimayanand.com/api/method/coreldraw_utility.api.";
        private const int ValidationGraceDays = 30;
        private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);
        private static readonly byte[] CryptoSalt = Encoding.UTF8.GetBytes("SMRI.PanelMaker.LocalLicense.v1");
        private static readonly string LicenseDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SMRI", "PanelMaker");
        private static readonly string LicensePath = Path.Combine(LicenseDirectory, "license.dat");
        private static readonly string LegacyLicensePath = Path.Combine(LicenseDirectory, "license.json");

        public bool EnsureActivated()
        {
            string machineId = GetMachineId();
            LocalLicenseReadStatus status;
            LocalLicense existing = ReadLocalLicense(out status);

            if (existing == null)
            {
                if (status == LocalLicenseReadStatus.InvalidSignature || status == LocalLicenseReadStatus.Corrupt)
                {
                    MessageBox.Show("The saved license could not be verified. Please activate again.",
                        ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                return PromptAndActivate(machineId);
            }

            if (!string.Equals(existing.MachineId, machineId, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("This license file is bound to another computer. Please activate this device with your license key.",
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return PromptAndActivate(machineId);
            }

            DateTime utcNow = DateTime.UtcNow;

            if (IsClockRollback(existing, utcNow))
            {
                return RequireForceReauthentication(existing, machineId,
                    "Your computer clock appears to be behind the last verified server time. Internet revalidation is required.");
            }

            if (IsValidationOverdue(existing, utcNow))
            {
                return RequireForceReauthentication(existing, machineId,
                    "License validation is overdue. Please connect to the internet to continue.");
            }

            if (IsSubscriptionPastLocalEnd(existing, utcNow))
            {
                return RequireForceReauthentication(existing, machineId,
                    "Your saved subscription date has expired. Please connect to the internet to revalidate your license.");
            }

            ValidationAttempt validation = TryValidateLicense(existing, machineId);
            if (validation.Success)
            {
                SaveFromValidation(existing, validation.Response, machineId);
                return true;
            }

            if (validation.NetworkUnavailable)
            {
                return true;
            }

            MessageBox.Show(BuildFailureMessage(validation.Message,
                    "License validation failed. Please activate again. If this license is already active on another PC, deactivate it there or from the admin side first."),
                ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

            return PromptAndActivate(machineId);
        }

        public bool Deactivate()
        {
            string machineId = GetMachineId();
            LocalLicenseReadStatus status;
            LocalLicense existing = ReadLocalLicense(out status);
            if (existing == null)
            {
                DeleteLocalLicense();
                return true;
            }

            ApiCallResult<DeactivateResponse> result = Post<DeactivateRequest, DeactivateResponse>("deactivate_license",
                new DeactivateRequest
                {
                    LicenseKey = existing.LicenseKey,
                    SessionToken = existing.SessionToken,
                    MachineId = machineId
                });

            if (!result.Success || result.Response == null || !result.Response.Success)
            {
                string detail = result.Response != null ? result.Response.Message : result.Message;
                MessageBox.Show(BuildFailureMessage(detail, "License deactivation failed."),
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            DeleteLocalLicense();
            return true;
        }

        public LicenseDetails GetLicenseDetails()
        {
            string machineId = GetMachineId();
            LocalLicenseReadStatus status;
            LocalLicense existing = ReadLocalLicense(out status);
            if (existing == null)
            {
                return null;
            }

            ApiCallResult<LicenseDetails> result = Post<LicenseDetailsRequest, LicenseDetails>("get_license_details",
                new LicenseDetailsRequest
                {
                    LicenseKey = existing.LicenseKey,
                    SessionToken = existing.SessionToken,
                    MachineId = machineId
                });

            return result.Success ? result.Response : null;
        }

        private static bool PromptAndActivate(string machineId)
        {
            string licenseKey = Interaction.InputBox("Enter your SMRI Panel Maker license key:", ProductName, "");
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                MessageBox.Show("A valid license key is required.", ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            ApiCallResult<ActivationResponse> result = ActivateOrForce("activate_license", licenseKey.Trim(), machineId);
            if (!result.Success || result.Response == null || !result.Response.Success)
            {
                string detail = result.Response != null ? result.Response.Message : result.Message;
                MessageBox.Show(BuildFailureMessage(detail, "License activation failed."),
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            SaveFromActivation(licenseKey.Trim(), machineId, result.Response);
            return true;
        }

        private static bool RequireForceReauthentication(LocalLicense existing, string machineId, string reason)
        {
            MessageBox.Show(reason, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);

            ApiCallResult<ActivationResponse> result = ActivateOrForce("force_reauthentication", existing.LicenseKey, machineId);
            if (!result.Success || result.Response == null || !result.Response.Success)
            {
                string detail = result.Response != null ? result.Response.Message : result.Message;
                MessageBox.Show(BuildFailureMessage(detail, "Online reauthentication failed. Full use is blocked until the license is revalidated."),
                    ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            SaveFromActivation(existing.LicenseKey, machineId, result.Response);
            return true;
        }

        private static ValidationAttempt TryValidateLicense(LocalLicense existing, string machineId)
        {
            ApiCallResult<ValidationResponse> result = Post<ValidateRequest, ValidationResponse>("validate_license",
                new ValidateRequest
                {
                    LicenseKey = existing.LicenseKey,
                    SessionToken = existing.SessionToken,
                    MachineId = machineId
                });

            if (result.Success && result.Response != null && result.Response.Valid)
            {
                return new ValidationAttempt { Success = true, Response = result.Response };
            }

            return new ValidationAttempt
            {
                Success = false,
                NetworkUnavailable = result.NetworkUnavailable,
                Message = result.Response != null ? result.Response.Message : result.Message
            };
        }

        private static ApiCallResult<ActivationResponse> ActivateOrForce(string method, string licenseKey, string machineId)
        {
            return Post<ActivationRequest, ActivationResponse>(method, new ActivationRequest
            {
                LicenseKey = licenseKey,
                MachineId = machineId,
                MachineName = Environment.MachineName,
                MachineInfo = MachineInfo.Create()
            });
        }

        private static ApiCallResult<TResponse> Post<TRequest, TResponse>(string method, TRequest request)
            where TResponse : class
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string requestJson = SerializeJson(request);
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
            var httpRequest = (HttpWebRequest)WebRequest.Create(ApiBaseUrl + method);
            httpRequest.Method = "POST";
            httpRequest.Accept = "application/json";
            httpRequest.ContentType = "application/json";
            httpRequest.Timeout = 30000;
            httpRequest.ReadWriteTimeout = 30000;
            httpRequest.ContentLength = requestBytes.Length;

            try
            {
                using (Stream stream = httpRequest.GetRequestStream())
                {
                    stream.Write(requestBytes, 0, requestBytes.Length);
                }

                using (var response = (HttpWebResponse)httpRequest.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    string responseJson = ReadAllText(stream);
                    TResponse responseBody = DeserializeApiResponse<TResponse>(responseJson);
                    return new ApiCallResult<TResponse> { Success = responseBody != null, Response = responseBody };
                }
            }
            catch (WebException ex)
            {
                string message = ReadWebExceptionMessage(ex);
                return new ApiCallResult<TResponse>
                {
                    Success = false,
                    NetworkUnavailable = IsNetworkUnavailable(ex),
                    Message = message
                };
            }
            catch (Exception ex)
            {
                return new ApiCallResult<TResponse> { Success = false, Message = ex.Message };
            }
        }

        private static bool IsClockRollback(LocalLicense license, DateTime utcNow)
        {
            DateTime lastServerTime;
            if (!TryParseDateTime(license.LastServerTime, out lastServerTime))
            {
                return true;
            }

            return utcNow.Add(ClockTolerance) < lastServerTime;
        }

        private static bool IsValidationOverdue(LocalLicense license, DateTime utcNow)
        {
            DateTime nextValidationDue;
            if (TryParseDate(license.NextValidationDue, out nextValidationDue))
            {
                return utcNow.Date > nextValidationDue.Date;
            }

            DateTime lastServerTime;
            if (!TryParseDateTime(license.LastServerTime, out lastServerTime))
            {
                return true;
            }

            return utcNow.Date > lastServerTime.Date.AddDays(ValidationGraceDays);
        }

        private static bool IsSubscriptionPastLocalEnd(LocalLicense license, DateTime utcNow)
        {
            DateTime subscriptionEnd;
            return TryParseDate(license.SubscriptionEnd, out subscriptionEnd) && utcNow.Date > subscriptionEnd.Date;
        }

        private static void SaveFromActivation(string licenseKey, string machineId, ActivationResponse response)
        {
            SaveLocalLicense(new LocalLicense
            {
                LicenseKey = licenseKey,
                MachineId = machineId,
                SessionToken = response.SessionToken,
                LastServerTime = response.ServerTime,
                NextValidationDue = response.NextValidationDue,
                SubscriptionEnd = response.SubscriptionEnd
            });
        }

        private static void SaveFromValidation(LocalLicense existing, ValidationResponse response, string machineId)
        {
            SaveLocalLicense(new LocalLicense
            {
                LicenseKey = existing.LicenseKey,
                MachineId = machineId,
                SessionToken = existing.SessionToken,
                LastServerTime = response.ServerTime,
                NextValidationDue = response.NextValidationDue,
                SubscriptionEnd = response.SubscriptionEnd
            });
        }

        private static LocalLicense ReadLocalLicense(out LocalLicenseReadStatus status)
        {
            status = LocalLicenseReadStatus.NotFound;
            if (!File.Exists(LicensePath))
            {
                return null;
            }

            try
            {
                SecureLicenseFile file;
                using (FileStream stream = File.OpenRead(LicensePath))
                {
                    file = ReadJson<SecureLicenseFile>(stream);
                }

                if (file == null || string.IsNullOrWhiteSpace(file.Iv) ||
                    string.IsNullOrWhiteSpace(file.CipherText) || string.IsNullOrWhiteSpace(file.Signature))
                {
                    status = LocalLicenseReadStatus.Corrupt;
                    return null;
                }

                string machineId = GetMachineId();
                byte[] encryptionKey;
                byte[] signingKey;
                BuildLocalKeys(machineId, out encryptionKey, out signingKey);

                string expectedSignature = SignLicenseFile(file, signingKey);
                if (!FixedTimeEquals(expectedSignature, file.Signature))
                {
                    status = LocalLicenseReadStatus.InvalidSignature;
                    return null;
                }

                byte[] plainBytes = Decrypt(Convert.FromBase64String(file.CipherText), Convert.FromBase64String(file.Iv), encryptionKey);
                using (var stream = new MemoryStream(plainBytes))
                {
                    LocalLicense license = ReadJson<LocalLicense>(stream);
                    status = license == null ? LocalLicenseReadStatus.Corrupt : LocalLicenseReadStatus.Valid;
                    return license;
                }
            }
            catch (CryptographicException)
            {
                status = LocalLicenseReadStatus.InvalidSignature;
                return null;
            }
            catch
            {
                status = LocalLicenseReadStatus.Corrupt;
                return null;
            }
        }

        private static void SaveLocalLicense(LocalLicense license)
        {
            Directory.CreateDirectory(LicenseDirectory);

            string machineId = GetMachineId();
            byte[] encryptionKey;
            byte[] signingKey;
            BuildLocalKeys(machineId, out encryptionKey, out signingKey);

            byte[] plainBytes = Encoding.UTF8.GetBytes(SerializeJson(license));
            byte[] iv;
            byte[] cipherBytes = Encrypt(plainBytes, encryptionKey, out iv);

            var file = new SecureLicenseFile
            {
                Version = 1,
                Iv = Convert.ToBase64String(iv),
                CipherText = Convert.ToBase64String(cipherBytes)
            };
            file.Signature = SignLicenseFile(file, signingKey);

            using (FileStream stream = File.Create(LicensePath))
            {
                WriteJson(stream, file);
            }

            DeleteFileIfExists(LegacyLicensePath);
        }

        private static void DeleteLocalLicense()
        {
            DeleteFileIfExists(LicensePath);
            DeleteFileIfExists(LegacyLicensePath);
        }

        private static void DeleteFileIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static byte[] Encrypt(byte[] plainBytes, byte[] key, out byte[] iv)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.GenerateIV();
                iv = aes.IV;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }
            }
        }

        private static byte[] Decrypt(byte[] cipherBytes, byte[] iv, byte[] key)
        {
            using (AesManaged aes = new AesManaged())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = key;
                aes.IV = iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    return decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                }
            }
        }

        private static void BuildLocalKeys(string machineId, out byte[] encryptionKey, out byte[] signingKey)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(
                ProductName + "|" + machineId + "|CorelDrawUtility",
                CryptoSalt,
                10000))
            {
                encryptionKey = deriveBytes.GetBytes(32);
                signingKey = deriveBytes.GetBytes(32);
            }
        }

        private static string SignLicenseFile(SecureLicenseFile file, byte[] signingKey)
        {
            string signedText = file.Version.ToString(CultureInfo.InvariantCulture) + "|" + file.Iv + "|" + file.CipherText;
            using (var hmac = new HMACSHA256(signingKey))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedText)));
            }
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            byte[] leftBytes;
            byte[] rightBytes;
            try
            {
                leftBytes = Convert.FromBase64String(left ?? "");
                rightBytes = Convert.FromBase64String(right ?? "");
            }
            catch
            {
                return false;
            }

            if (leftBytes.Length != rightBytes.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < leftBytes.Length; i++)
            {
                diff |= leftBytes[i] ^ rightBytes[i];
            }

            return diff == 0;
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

        private static bool TryParseDateTime(string value, out DateTime utcValue)
        {
            utcValue = DateTime.MinValue;
            DateTimeOffset parsed;
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed))
            {
                return false;
            }

            utcValue = parsed.UtcDateTime;
            return true;
        }

        private static bool TryParseDate(string value, out DateTime date)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date);
        }

        private static string BuildFailureMessage(string detail, string fallback)
        {
            return string.IsNullOrWhiteSpace(detail) ? fallback : fallback + Environment.NewLine + Environment.NewLine + detail;
        }

        private static bool IsNetworkUnavailable(WebException ex)
        {
            var httpResponse = ex.Response as HttpWebResponse;
            if (httpResponse != null && (int)httpResponse.StatusCode >= 500)
            {
                return true;
            }

            return ex.Status == WebExceptionStatus.ConnectFailure ||
                ex.Status == WebExceptionStatus.NameResolutionFailure ||
                ex.Status == WebExceptionStatus.ProxyNameResolutionFailure ||
                ex.Status == WebExceptionStatus.ReceiveFailure ||
                ex.Status == WebExceptionStatus.SendFailure ||
                ex.Status == WebExceptionStatus.Timeout ||
                ex.Status == WebExceptionStatus.TrustFailure ||
                ex.Response == null;
        }

        private static string ReadWebExceptionMessage(WebException ex)
        {
            if (ex.Response == null)
            {
                return ex.Message;
            }

            try
            {
                using (Stream stream = ex.Response.GetResponseStream())
                {
                    string responseJson = ReadAllText(stream);
                    ErrorEnvelope error = DeserializeJson<ErrorEnvelope>(responseJson);
                    if (error != null && !string.IsNullOrWhiteSpace(error.MessageText))
                    {
                        return error.MessageText;
                    }

                    ErrorDirect direct = DeserializeJson<ErrorDirect>(responseJson);
                    if (direct != null && !string.IsNullOrWhiteSpace(direct.Exception))
                    {
                        return direct.Exception;
                    }

                    return responseJson;
                }
            }
            catch
            {
                return ex.Message;
            }
        }

        private static T DeserializeApiResponse<T>(string json) where T : class
        {
            ApiEnvelope<T> envelope = DeserializeJson<ApiEnvelope<T>>(json);
            if (envelope != null && envelope.Message != null)
            {
                return envelope.Message;
            }

            return DeserializeJson<T>(json);
        }

        private static T DeserializeJson<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return ReadJson<T>(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        private static string SerializeJson<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                WriteJson(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static string ReadAllText(Stream stream)
        {
            if (stream == null)
            {
                return "";
            }

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
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

        private static void WriteJson<T>(Stream stream, T value)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            serializer.WriteObject(stream, value);
        }

        private enum LocalLicenseReadStatus
        {
            NotFound,
            Valid,
            InvalidSignature,
            Corrupt
        }

        private sealed class ApiCallResult<T> where T : class
        {
            public bool Success { get; set; }
            public bool NetworkUnavailable { get; set; }
            public string Message { get; set; }
            public T Response { get; set; }
        }

        private sealed class ValidationAttempt
        {
            public bool Success { get; set; }
            public bool NetworkUnavailable { get; set; }
            public string Message { get; set; }
            public ValidationResponse Response { get; set; }
        }

        [DataContract]
        private sealed class ApiEnvelope<T> where T : class
        {
            [DataMember(Name = "message")]
            public T Message { get; set; }
        }

        [DataContract]
        private sealed class ErrorEnvelope
        {
            [DataMember(Name = "message")]
            public string MessageText { get; set; }
        }

        [DataContract]
        private sealed class ErrorDirect
        {
            [DataMember(Name = "exception")]
            public string Exception { get; set; }
        }

        [DataContract]
        private sealed class ActivationRequest
        {
            [DataMember(Name = "license_key")]
            public string LicenseKey { get; set; }

            [DataMember(Name = "machine_id")]
            public string MachineId { get; set; }

            [DataMember(Name = "machine_name")]
            public string MachineName { get; set; }

            [DataMember(Name = "machine_info")]
            public MachineInfo MachineInfo { get; set; }
        }

        [DataContract]
        private sealed class ValidateRequest
        {
            [DataMember(Name = "license_key")]
            public string LicenseKey { get; set; }

            [DataMember(Name = "session_token")]
            public string SessionToken { get; set; }

            [DataMember(Name = "machine_id")]
            public string MachineId { get; set; }
        }

        [DataContract]
        private sealed class DeactivateRequest
        {
            [DataMember(Name = "license_key")]
            public string LicenseKey { get; set; }

            [DataMember(Name = "session_token")]
            public string SessionToken { get; set; }

            [DataMember(Name = "machine_id")]
            public string MachineId { get; set; }
        }

        [DataContract]
        private sealed class LicenseDetailsRequest
        {
            [DataMember(Name = "license_key")]
            public string LicenseKey { get; set; }

            [DataMember(Name = "session_token")]
            public string SessionToken { get; set; }

            [DataMember(Name = "machine_id")]
            public string MachineId { get; set; }
        }

        [DataContract]
        private sealed class ActivationResponse
        {
            [DataMember(Name = "success")]
            public bool Success { get; set; }

            [DataMember(Name = "message")]
            public string Message { get; set; }

            [DataMember(Name = "session_token")]
            public string SessionToken { get; set; }

            [DataMember(Name = "server_time")]
            public string ServerTime { get; set; }

            [DataMember(Name = "subscription_end")]
            public string SubscriptionEnd { get; set; }

            [DataMember(Name = "next_validation_due")]
            public string NextValidationDue { get; set; }
        }

        [DataContract]
        private sealed class ValidationResponse
        {
            [DataMember(Name = "valid")]
            public bool Valid { get; set; }

            [DataMember(Name = "message")]
            public string Message { get; set; }

            [DataMember(Name = "server_time")]
            public string ServerTime { get; set; }

            [DataMember(Name = "subscription_end")]
            public string SubscriptionEnd { get; set; }

            [DataMember(Name = "next_validation_due")]
            public string NextValidationDue { get; set; }
        }

        [DataContract]
        private sealed class DeactivateResponse
        {
            [DataMember(Name = "success")]
            public bool Success { get; set; }

            [DataMember(Name = "message")]
            public string Message { get; set; }
        }

        [DataContract]
        public sealed class LicenseDetails
        {
            [DataMember(Name = "customer_name")]
            public string CustomerName { get; set; }

            [DataMember(Name = "subscription_end")]
            public string SubscriptionEnd { get; set; }

            [DataMember(Name = "days_left")]
            public int DaysLeft { get; set; }
        }

        [DataContract]
        private sealed class MachineInfo
        {
            [DataMember(Name = "os_version")]
            public string OsVersion { get; set; }

            [DataMember(Name = "user_domain")]
            public string UserDomain { get; set; }

            [DataMember(Name = "is_64bit_os")]
            public bool Is64BitOperatingSystem { get; set; }

            public static MachineInfo Create()
            {
                return new MachineInfo
                {
                    OsVersion = Environment.OSVersion.VersionString,
                    UserDomain = Environment.UserDomainName,
                    Is64BitOperatingSystem = Environment.Is64BitOperatingSystem
                };
            }
        }

        [DataContract]
        private sealed class SecureLicenseFile
        {
            [DataMember(Name = "version")]
            public int Version { get; set; }

            [DataMember(Name = "iv")]
            public string Iv { get; set; }

            [DataMember(Name = "ciphertext")]
            public string CipherText { get; set; }

            [DataMember(Name = "signature")]
            public string Signature { get; set; }
        }

        [DataContract]
        private sealed class LocalLicense
        {
            [DataMember(Name = "license_key")]
            public string LicenseKey { get; set; }

            [DataMember(Name = "machine_id")]
            public string MachineId { get; set; }

            [DataMember(Name = "session_token")]
            public string SessionToken { get; set; }

            [DataMember(Name = "last_server_time")]
            public string LastServerTime { get; set; }

            [DataMember(Name = "next_validation_due")]
            public string NextValidationDue { get; set; }

            [DataMember(Name = "subscription_end")]
            public string SubscriptionEnd { get; set; }
        }
    }
}
