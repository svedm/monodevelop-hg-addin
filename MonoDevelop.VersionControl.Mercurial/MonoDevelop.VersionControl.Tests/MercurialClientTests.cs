using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hg.Net;
using Hg.Net.Models;
using NUnit.Framework;

namespace MonoDevelop.VersionControl.Tests
{
	[TestFixture]
	public class MercurialClientTests
	{
		private string _currentPath;
		private string _testRepoPath;
		private MercurialClient _mc;
		private string _testFilePath;
		private const string TestFileContent = "qweqwe";


		[TestFixtureSetUp]
		public void Init()
		{
			Console.InputEncoding = new UTF8Encoding(false);

			_currentPath = Directory.GetCurrentDirectory();
			_testRepoPath = Path.Combine(_currentPath, "testRepo");
			if (Directory.Exists(_testRepoPath))
			{
				Directory.Delete(_testRepoPath, true);
			}

			Directory.CreateDirectory(_testRepoPath);

			var os = Environment.OSVersion.VersionString.ToLower();
			var hgPath = (os.Contains("win")) ? "hg" : (TestsUtils.IsRunningOnMac()) ? "/usr/local/bin/hg" : "/usr/bin/hg";

			MercurialClient.Init(_testRepoPath, hgPath);

			_mc = new MercurialClient(_testRepoPath, hgPath);

			_testFilePath = Path.Combine(_testRepoPath, "qwe.txt");
			File.AppendAllText(_testFilePath, TestFileContent);

			_mc.Add(new[] { _testFilePath });
			_mc.Commit("first commit");
		}

		[Test]
		public void CatWithoutRevision()
		{
			var res = _mc.Cat(_testFilePath, null);
			Assert.AreEqual(TestFileContent, res);
		}

		[Test]
		public void GetHistory()
		{
			var res = _mc.Log(null, new List<string> { _testFilePath });
			Assert.AreEqual(res[0].Message, "first commit");
		}

		[Test]
		public void CheckStatus()
		{
			File.WriteAllText(_testFilePath, "asdasd");
			var res = _mc.Status(new List<string> { _testFilePath });
			Assert.IsTrue(res.Values.Contains(Status.Modified));
		}

		[TestFixtureTearDown]
		public void CleanUp()
		{
			Directory.Delete(_testRepoPath, true);
		}
	}
}

