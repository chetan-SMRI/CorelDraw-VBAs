using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Corel.Interop.VGCore;

namespace LicenseOverlap.DataSource
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BaseDataSource : INotifyPropertyChanged
    {
        protected readonly DataSourceProxy AppProxy;

        public BaseDataSource(DataSourceProxy proxy)
        {
            AppProxy = proxy;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            try
            {
                AppProxy.UpdateListeners(propertyName);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch
            {
            }
        }
    }
}
