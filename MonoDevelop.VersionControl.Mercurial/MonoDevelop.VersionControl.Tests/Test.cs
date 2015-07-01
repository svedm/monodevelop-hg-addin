using NUnit.Framework;
using System;
using MonoDevelop.VersionControl.Mercurial;
using System.Diagnostics;

namespace MonoDevelop.VersionControl.Tests
{
	[TestFixture()]
	public class Test
	{
		[Test()]
		public void TestCase()
		{
			var md = new MercurialClient("/Users/VeNOm/Documents/testRepo", "/usr/local/bin/hg");

			var res = md.Cat("/Users/VeNOm/Documents/testRepo/qwe.txt", null);

			Debug.WriteLine(res);
		}
	}
}

