using System;
using System.Windows.Forms;

namespace SMRI.PanelMaker
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

                new CorelPanelMaker().Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SMRI Panel Maker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
