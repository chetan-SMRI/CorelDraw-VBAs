using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SMRI.Exporter
{
    internal static class ModernFolderPicker
    {
        private const uint FosPickFolders = 0x00000020;
        private const uint FosForceFileSystem = 0x00000040;
        private const uint FosPathMustExist = 0x00000800;
        private const uint FosDontAddToRecent = 0x02000000;
        private const uint SigdnFileSystemPath = 0x80058000;
        private const int CancelledHResult = unchecked((int)0x800704C7);

        internal static string Show(IWin32Window owner, string title, string initialFolder)
        {
            IFileOpenDialog dialog = null;
            IShellItem initialItem = null;
            IShellItem result = null;

            try
            {
                dialog = (IFileOpenDialog)new FileOpenDialogComObject();
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | FosPickFolders | FosForceFileSystem |
                    FosPathMustExist | FosDontAddToRecent);
                dialog.SetTitle(title);
                dialog.SetOkButtonLabel("Select Folder");

                if (!string.IsNullOrWhiteSpace(initialFolder))
                {
                    Guid shellItemId = typeof(IShellItem).GUID;
                    if (SHCreateItemFromParsingName(initialFolder, IntPtr.Zero,
                        ref shellItemId, out initialItem) == 0)
                    {
                        dialog.SetFolder(initialItem);
                    }
                }

                int resultCode = dialog.Show(owner == null ? IntPtr.Zero : owner.Handle);
                if (resultCode == CancelledHResult) return null;
                Marshal.ThrowExceptionForHR(resultCode);

                dialog.GetResult(out result);
                IntPtr pathPointer;
                result.GetDisplayName(SigdnFileSystemPath, out pathPointer);
                try
                {
                    return Marshal.PtrToStringUni(pathPointer);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pathPointer);
                }
            }
            catch (COMException ex)
            {
                if (ex.HResult == CancelledHResult) return null;
                return ShowLegacy(owner, title, initialFolder);
            }
            finally
            {
                Release(result);
                Release(initialItem);
                Release(dialog);
            }
        }

        private static string ShowLegacy(IWin32Window owner, string title, string initialFolder)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = title;
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrWhiteSpace(initialFolder)) dialog.SelectedPath = initialFolder;
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        private static void Release(object value)
        {
            if (value != null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value);
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(
            string path, IntPtr bindContext, ref Guid shellItemId,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem shellItem);

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class FileOpenDialogComObject { }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr bindContext, ref Guid handlerId, ref Guid interfaceId, out IntPtr result);
            void GetParent(out IShellItem parent);
            void GetDisplayName(uint displayNameType, out IntPtr name);
            void GetAttributes(uint mask, out uint attributes);
            void Compare(IShellItem shellItem, uint hint, out int order);
        }

        [ComImport]
        [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr owner);
            void SetFileTypes(uint count, IntPtr filterSpecifications);
            void SetFileTypeIndex(uint fileType);
            void GetFileTypeIndex(out uint fileType);
            void Advise(IntPtr events, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder(IShellItem shellItem);
            void SetFolder(IShellItem shellItem);
            void GetFolder(out IShellItem shellItem);
            void GetCurrentSelection(out IShellItem shellItem);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void GetResult(out IShellItem shellItem);
            void AddPlace(IShellItem shellItem, uint placement);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
            void Close(int errorCode);
            void SetClientGuid(ref Guid clientGuid);
            void ClearClientData();
            void SetFilter(IntPtr filter);
            void GetResults(out IntPtr shellItems);
            void GetSelectedItems(out IntPtr shellItems);
        }
    }
}
