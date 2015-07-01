using System;
using MonoDevelop.VersionControl.Mercurial;

namespace TestConsoleApp
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var md = new MercurialClient("/Users/VeNOm/Documents/testRepo", "/usr/local/bin/hg");

			//var res = md.Cat("/Users/VeNOm/Documents/testRepo/qwe.txt", null);

			Console.WriteLine("qwe");
		}
	}
}
