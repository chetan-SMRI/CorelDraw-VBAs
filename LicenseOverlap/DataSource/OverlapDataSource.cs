using System;
using System.Runtime.InteropServices;
using Corel.Interop.VGCore;

namespace LicenseOverlap.DataSource
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class OverlapDataSource : BaseDataSource
    {
        private string caption = "SMRI Overlap Panels";
        private string icon = "";

        public OverlapDataSource(DataSourceProxy proxy) : base(proxy)
        {
        }

        public string Caption
        {
            get { return caption; }
            set { caption = value; NotifyPropertyChanged(); }
        }

        public string Icon
        {
            get { return icon; }
            set { icon = value; NotifyPropertyChanged(); }
        }

        public void MenuItemCommand()
        {
            try
            {
                OverlapTool.RunOverlapTool(ControlUI.CorelApp);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("SMRI Overlap Panels failed: " + ex.Message);
            }
        }
    }
}
