using System;
using System.Runtime.InteropServices;

namespace MonoDevelop.VersionControl.Tests
{
    public class TestsUtils
    {
        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        public static bool IsRunningOnMac()
        {
            var buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                if (uname(buf) == 0)
                {
                    var os = Marshal.PtrToStringAnsi(buf);
                    if (os == "Darwin")
                        return true;
                }
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }
            return false;
        }
    }
}

