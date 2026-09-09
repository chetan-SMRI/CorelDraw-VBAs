using System;
using SMRI.PanelMaker;

internal static class LicenseManagerTests
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("Failed: " + name);
    }

    private static void Main()
    {
        // Expected codes generated independently by Adobe/TOTP_gen.py.
        Check(LicenseManager.ActivationCode(0) == "60890780", "epoch code");
        Check(LicenseManager.ActivationCode(29813760) == "24694940", "Adobe compatibility");
        Check(LicenseManager.ActivationCode(99999999) == "00215489", "leading zeroes");
        Check(LicenseManager.ActivationCode(100000000) == "60890780", "modulus boundary");
        DateTime now = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(29813760);
        Check(LicenseManager.IsActivationCodeValid("24694940", now), "current minute");
        Check(LicenseManager.IsActivationCodeValid("64019649", now), "previous minute");
        Check(LicenseManager.IsActivationCodeValid("85370231", now), "next minute");
        Check(!LicenseManager.IsActivationCodeValid("24694940", now.AddMinutes(2)), "stale code");
        Check(!LicenseManager.IsActivationCodeValid(null, now), "cancelled input");
        Check(!LicenseManager.IsActivationCodeValid("2469494", now), "short input");
        Check(!LicenseManager.IsActivationCodeValid("abcdefgh", now), "non-numeric input");

        DateTime activated = new DateTime(2024, 2, 29, 12, 0, 0, DateTimeKind.Utc);
        DateTime expires = activated.AddYears(1);
        DateTime seen = activated.AddDays(30);
        Check(expires == new DateTime(2025, 2, 28, 12, 0, 0, DateTimeKind.Utc), "leap-year term");
        Check(LicenseManager.IsLicenseValid(activated, expires, activated, activated), "new activation");
        Check(LicenseManager.IsLicenseValid(activated, expires, seen, expires.AddTicks(-1)), "before expiry");
        Check(!LicenseManager.IsLicenseValid(activated, expires, seen, expires), "exact expiry");
        Check(!LicenseManager.IsLicenseValid(activated, expires, seen, expires.AddDays(1)), "expired");
        Check(LicenseManager.IsLicenseValid(activated, expires, seen, seen.AddMinutes(-5)), "clock tolerance");
        Check(!LicenseManager.IsLicenseValid(activated, expires, seen, seen.AddMinutes(-6)), "clock rollback");
        Check(!LicenseManager.IsLicenseValid(activated, expires.AddDays(1), seen, seen), "extended term");
        Check(!LicenseManager.IsLicenseValid(activated, expires, activated.AddDays(-1), seen), "invalid last use");
        Console.WriteLine("All license logic checks passed.");
    }
}
