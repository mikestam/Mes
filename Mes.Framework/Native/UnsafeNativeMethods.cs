namespace Mes.Native
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;

    [SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("Kernel32.dll")]
        internal static extern bool AttachConsole(int processId);
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);
        [DllImport("kernel32.dll")]
        internal static extern int WerRegisterMemoryBlock(IntPtr pvAddress, uint dwSize);
    }
}

