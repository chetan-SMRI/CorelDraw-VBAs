using System;
using System.Windows.Controls;
using corel = Corel.Interop.VGCore;

namespace LicenseOverlap
{
    public partial class ControlUI : UserControl
    {
        public static corel.Application CorelApp;

        public ControlUI(object app)
        {
            InitializeComponent();

            try
            {
                CorelApp = app as corel.Application;

                var dsf = new DataSource.DataSourceFactory();
                dsf.AddDataSource("LicenseOverlapDS", typeof(DataSource.OverlapDataSource));
                dsf.Register();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("LicenseOverlap failed to initialize: " + ex.Message);
            }
        }
    }
}
