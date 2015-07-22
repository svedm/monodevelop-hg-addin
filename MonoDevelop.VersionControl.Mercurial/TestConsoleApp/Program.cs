using System;
using MonoDevelop.VersionControl.Mercurial;
using System.IO;
using System.Runtime.InteropServices;
using Hg.Net;

namespace TestConsoleApp
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var currentPath = Directory.GetCurrentDirectory();
			var testRepoPath = Path.Combine(currentPath, "testRepo");
			if (Directory.Exists(testRepoPath))
			{
				Directory.Delete(testRepoPath, true);
			}

			Directory.CreateDirectory(testRepoPath);

			var os = Environment.OSVersion.VersionString.ToLower();
			var hgPath = (os.Contains("win")) ? "hg" : (IsRunningOnMac()) ? "/usr/local/bin/hg" : "/usr/bin/hg";

			MercurialClient.Init(testRepoPath, hgPath);

			var mc = new MercurialClient(testRepoPath, hgPath);

			var testFilePath = Path.Combine(testRepoPath, "qwe.txt");
			File.AppendAllText(testFilePath, "qweqwe");

			mc.Add(new string[] { testFilePath });
			mc.Commit("first commit");
		}

		[DllImport ("libc")]
		static extern int uname (IntPtr buf);

		public static bool IsRunningOnMac ()
		{
			IntPtr buf = IntPtr.Zero;
			try {
				buf = Marshal.AllocHGlobal (8192);
				// This is a hacktastic way of getting sysname from uname ()
				if (uname (buf) == 0) {
					string os = Marshal.PtrToStringAnsi (buf);
					if (os == "Darwin")
						return true;
				}
			} catch {
			} finally {
				if (buf != IntPtr.Zero)
					Marshal.FreeHGlobal (buf);
			}
			return false;
		}
	}
}
