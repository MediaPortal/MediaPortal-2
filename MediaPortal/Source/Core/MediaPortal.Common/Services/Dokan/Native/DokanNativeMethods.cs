using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Common.Services.Dokan.Native
{
  internal static class DokanNativeMethods
  {
    private const string DOKAN_DLL = "dokan1.dll";

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern int DokanMain(ref DOKAN_OPTIONS options, ref DOKAN_OPERATIONS operations);

    [DllImport(DOKAN_DLL, ExactSpelling = true, CharSet = CharSet.Auto)]
    public static extern int DokanUnmount(char driveLetter);

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern uint DokanVersion();

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern uint DokanDriveVersion();

    [DllImport(DOKAN_DLL, ExactSpelling = true, CharSet = CharSet.Auto)]
    public static extern int DokanRemoveMountPoint([MarshalAs(UnmanagedType.LPWStr)] string mountPoint);

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DokanResetTimeout(uint timeout, DokanFileInfo rawFileInfo);

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern IntPtr DokanOpenRequestorToken(DokanFileInfo rawFileInfo);

    [DllImport(DOKAN_DLL, ExactSpelling = true)]
    public static extern void DokanMapKernelToUserCreateFileFlags(uint fileAttributes, uint createOptions, uint createDisposition, ref int outFileAttributesAndFlags, ref int outCreationDisposition);
  }
}
