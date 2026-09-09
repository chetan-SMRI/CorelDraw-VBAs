# License checks (Windows)

From a Visual Studio Developer Command Prompt at the repository root:

```bat
csc /nologo /out:"%TEMP%\SMRI.LicenseTests.exe" /r:Microsoft.VisualBasic.dll /r:System.Windows.Forms.dll /r:System.Security.dll SMRI.PanelMaker\LicenseManager.cs Tests\LicenseManagerTests.cs
"%TEMP%\SMRI.LicenseTests.exe"
```

The executable checks the actual production code against Adobe generator vectors, code windows, input validation, annual expiry, leap years, and clock rollback.

Also test the built application on a Windows test account:

1. Cancel activation; confirm the panel tool does not open.
2. Enter an incorrect code, then a current code from `python3 Adobe/TOTP_gen.py`; confirm only the valid code opens the tool.
3. Relaunch while disconnected from the internet; confirm there is no activation prompt.
4. Back up `%LOCALAPPDATA%\SMRI\PanelMaker\license.dat`, alter its bytes, and relaunch; confirm activation is required. Restore the backup afterward.
5. Copy that file to another Windows test account/computer; confirm it requires activation.
6. Deny write access to the license folder in the test account; confirm startup reports an error and the tool does not open. Restore permissions afterward.

The code generator is for the operator only; do not include it or these tests in the customer installer.
