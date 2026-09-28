using System;
using System.Windows.Forms;

namespace SMRI.Exporter
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                var license = new LicenseManager();
                if (!license.EnsureActivated())
                {
                    return;
                }

                new CorelExporter().Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SMRI Exporter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
