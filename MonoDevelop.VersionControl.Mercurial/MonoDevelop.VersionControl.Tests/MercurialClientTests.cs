using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hg.Net;
using Hg.Net.Models;
using NUnit.Framework;
using System.Linq;
using System.Text.RegularExpressions;

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
		private List<string> ToDelete = new List<string>();
		private const string RemoteRepo = "https://svedm@bitbucket.org/svedm/hgtestrepo";
		private string MercurialPath = "";


		[TestFixtureSetUp]
		public void Init()
		{
			Console.InputEncoding = new UTF8Encoding(false);

			_currentPath = Directory.GetCurrentDirectory();
			_testRepoPath = GetTmpPath();

			Directory.CreateDirectory(_testRepoPath);

			var os = Environment.OSVersion.VersionString.ToLower();
			var MercurialPath = (os.Contains("win")) ? "hg" : (TestsUtils.IsRunningOnMac()) ? "/usr/local/bin/hg" : "/usr/bin/hg";

			MercurialClient.Init(_testRepoPath, MercurialPath);

			_mc = new MercurialClient(_testRepoPath, MercurialPath);

			_testFilePath = Path.Combine(_testRepoPath, "qwe.txt");
			File.AppendAllText(_testFilePath, TestFileContent);

			_mc.Add(new[] { _testFilePath });
			_mc.Commit("first commit");
		}

		private string GetTmpPath()
		{
			var path = Path.Combine(_currentPath, DateTime.UtcNow.Ticks.ToString());
			if (Directory.Exists(_testRepoPath))
			{
				Directory.Delete(_testRepoPath, true);
			}
			ToDelete.Add(path);
			return path;
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

		[Test]
		public void TestInitialize()
		{
			string path = GetTmpPath();
			MercurialClient.Init(path, null);
			Assert.That(Directory.Exists(Path.Combine(path, ".hg")), string.Format("Repository was not created at {0}", path));
		}

		[Test]
		public void TestCloneRemote()
		{
			string path = GetTmpPath();
			MercurialClient.Clone(RemoteRepo, path, true, null, null, null, false, true, null);
			Assert.That(Directory.Exists(Path.Combine(path, ".hg")), string.Format("Repository was not cloned from {0} to {1}", RemoteRepo, path));
		}

		[Test]
		public void TestCloneLocal()
		{
			string firstPath = GetTmpPath();
			string secondPath = GetTmpPath();
			string file = Path.Combine(firstPath, "foo");
			MercurialClient.Init(firstPath, null);

			using (var client = new MercurialClient(firstPath, MercurialPath))
			{
				File.WriteAllText(file, "1");
				client.Add(new [] { file });
				client.Commit("1");
			}
			try
			{
				MercurialClient.Clone(source: firstPath, destination: secondPath, mercurialPath: null);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				Assert.That(false, ex.Message);
			}
			Assert.That(Directory.Exists(Path.Combine(secondPath, ".hg")), string.Format("Repository was not cloned from {0} to {1}", firstPath, secondPath));
			Assert.That(File.Exists(Path.Combine(secondPath, "foo")), "foo doesn't exist in cloned working copy");

			using (var client = new MercurialClient(secondPath, MercurialPath))
			{
				var log = client.Log(null, new List<string>());
				Assert.AreEqual(1, log.Count, "Unexpected number of log entries");
			}
		}

		[Test]
		public void TestAdd()
		{
			string path = GetTmpPath();
			IDictionary<string,Status > statuses = null;

			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(Path.Combine(path, "foo"), string.Empty);
				File.WriteAllText(Path.Combine(path, "bar"), string.Empty);
				client.Add(new [] { Path.Combine(path, "foo"), Path.Combine(path, "bar") });
				statuses = client.Status(null);
			}

			Assert.IsNotNull(statuses);
			Assert.That(statuses.ContainsKey("foo"), "No status received for foo");
			Assert.That(statuses.ContainsKey("bar"), "No status received for bar");
			Assert.AreEqual(Status.Added, statuses["foo"]);
			Assert.AreEqual(Status.Added, statuses["bar"]);
		}

		[Test]
		public void TestCommit()
		{
			string path = GetTmpPath();
			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(Path.Combine(path, "foo"), string.Empty);
				File.WriteAllText(Path.Combine(path, "bar"), string.Empty);
				client.Add(new [] { Path.Combine(path, "foo") });
				client.Commit("Commit all");
				Assert.That(!client.Status(new List<string>()).ContainsKey("foo"), "Default commit failed for foo");

				File.WriteAllText(Path.Combine(path, "foo"), "foo");
				client.Add(new [] { Path.Combine(path, "bar") });
				client.Commit("Commit only bar", new List<string> { Path.Combine(path, "bar") });
				Assert.That(!client.Status(new List<string>()).ContainsKey("bar"), "Commit failed for bar");
				Assert.That(client.Status(new List<string>()).ContainsKey("foo"), "Committed unspecified file!");
				Assert.AreEqual(Status.Modified, client.Status(new List<string>())["foo"], "Committed unspecified file!");
				Assert.AreEqual(2, client.Log(null, new List<string>()).Count, "Unexpected revision count");
			}
		}

		[Test]
		public void TestLog()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			MercurialClient.Init(path, MercurialPath);

			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, "1");
				client.Add(new [] { file });
				client.Commit("1");
				File.WriteAllText(file, "2");
				client.Commit("2");
				Assert.AreEqual(2, client.Log(null, new List<string>()).Count, "Unexpected revision count");
			}
		}

		[Test]
		public void TestDiff()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			string diffText = string.Empty;
			MercurialClient.Init(path, MercurialPath);

			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, "1\n");
				client.Add(new [] { file });
				client.Commit("1", null, false, false, null, null, null, DateTime.MinValue, "user");
				File.WriteAllText(file, "2\n");
				diffText = client.Diff(null, new List<string> { file });
			}

			string[] lines = diffText.Split(new[]{ "\n" }, StringSplitOptions.RemoveEmptyEntries);
			Assert.AreEqual(6, lines.Length, "Unexpected diff length");
			Assert.AreEqual("@@ -1,1 +1,1 @@", lines[3]);
			Assert.AreEqual("-1", lines[4]);
			Assert.AreEqual("+2", lines[5]);
		}

		[Test]
		public void TestPull()
		{
			string firstPath = GetTmpPath();
			string secondPath = GetTmpPath();
			string file = Path.Combine(firstPath, "foo");
			MercurialClient.Init(firstPath, MercurialPath);
			MercurialClient firstClient = null,
			secondClient = null;

			try
			{
				// Create repo with one commit
				firstClient = new MercurialClient(firstPath, MercurialPath);
				File.WriteAllText(file, "1");
				firstClient.Add(new [] { file });
				firstClient.Commit("1");

				// Clone repo
				MercurialClient.Clone(source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new MercurialClient(secondPath, MercurialPath);
				Assert.AreEqual(1, secondClient.Log(null, new List<string>()).Count, "Unexpected number of log entries");

				// Add changeset to original repo
				File.WriteAllText(file, "2");
				firstClient.Commit("2");

				// Pull from clone
				Assert.IsTrue(secondClient.Pull(null), "Pull unexpectedly resulted in unresolved files");
				Assert.AreEqual(2, secondClient.Log(null, new List<string>()).Count, "Unexpected number of log entries");
			}
			finally
			{
				if (null != firstClient)
					firstClient.Dispose();
				if (null != secondClient)
					secondClient.Dispose();
			}
		}


		[Test]
		[Ignore("popup")]
		public void TestMerge()
		{
			string firstPath = GetTmpPath();
			string secondPath = GetTmpPath();
			string file = Path.Combine(firstPath, "foo");
			MercurialClient.Init(firstPath, MercurialPath);
			MercurialClient firstClient = null,
			secondClient = null;

			try
			{
				// Create repo with one commit
				firstClient = new MercurialClient(firstPath, MercurialPath);
				File.WriteAllText(file, "1\n");
				firstClient.Add(new [] { file });
				firstClient.Commit("1");

				// Clone repo
				MercurialClient.Clone(source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new MercurialClient(secondPath, MercurialPath);
				Assert.AreEqual(1, secondClient.Log(null, new List<string>()).Count, "Unexpected number of log entries");

				// Add changeset to original repo
				File.WriteAllText(file, "2\n");
				firstClient.Commit("2");

				// Add non-conflicting changeset to child repo
				File.WriteAllText(Path.Combine(secondPath, "foo"), "1\na\n");
				secondClient.Commit("a");

				// Pull from clone
				Assert.IsTrue(secondClient.Pull(null), "Pull unexpectedly resulted in unresolved files");
				Assert.AreEqual(3, secondClient.Log(null, new List<string>()).Count, "Unexpected number of log entries");

				Assert.AreEqual(2, secondClient.Heads(null).Count(), "Unexpected number of heads");

				Assert.IsTrue(secondClient.Merge(null), "Merge unexpectedly resulted in unresolved files");
			}
			finally
			{
				if (null != firstClient)
					firstClient.Dispose();
				if (null != secondClient)
					secondClient.Dispose();
			}
		}

		[Test]
		public void TestHeads()
		{
			string firstPath = GetTmpPath();
			string secondPath = GetTmpPath();
			string file = Path.Combine(firstPath, "foo");
			MercurialClient.Init(firstPath, MercurialPath);
			MercurialClient firstClient = null,
			secondClient = null;

			try
			{
				// Create repo with one commit
				firstClient = new MercurialClient(firstPath, MercurialPath);
				File.WriteAllText(file, "1\n");
				firstClient.Add(new [] { file });
				firstClient.Commit("1");

				// Clone repo
				MercurialClient.Clone(source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new MercurialClient(secondPath, MercurialPath);
				Assert.AreEqual(1, secondClient.Log(null, new List<string>()).Count, "Unexpected number of log entries");

				// Add changeset to original repo
				File.WriteAllText(file, "2\n");
				firstClient.Commit("2");

				// Add non-conflicting changeset to child repo
				File.WriteAllText(Path.Combine(secondPath, "foo"), "1\na\n");
				secondClient.Commit("a");

				// Pull from clone
				Assert.IsTrue(secondClient.Pull(null), "Pull unexpectedly resulted in unresolved files");
				Assert.AreEqual(3, secondClient.Log(null, new List<string>()).Count, "Unexpected number of log entries");

				Assert.AreEqual(2, secondClient.Heads(null).Count(), "Unexpected number of heads");
			}
			finally
			{
				if (null != firstClient)
					firstClient.Dispose();
				if (null != secondClient)
					secondClient.Dispose();
			}
		}

		[Test]
		public void TestPush()
		{
			string firstPath = GetTmpPath();
			string secondPath = GetTmpPath();
			string file = Path.Combine(firstPath, "foo");
			MercurialClient.Init(firstPath, MercurialPath);
			MercurialClient firstClient = null,
			secondClient = null;

			try
			{
				// Create repo with one commit
				firstClient = new MercurialClient(firstPath, MercurialPath);
				File.WriteAllText(file, "1\n");
				firstClient.Add(new [] { file });
				firstClient.Commit("1");

				// Clone repo
				MercurialClient.Clone(source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new MercurialClient(secondPath, MercurialPath);
				Assert.AreEqual(1, secondClient.Log(null, new List<string>()).Count, "Unexpected number of log entries");

				// Add changeset to child repo
				File.WriteAllText(Path.Combine(secondPath, "foo"), "1\na\n");
				secondClient.Commit("a");

				// Push to parent
				Assert.IsTrue(secondClient.Push(firstPath, null), "Nothing to push");

				// Assert that the first repo now has two revisions in the log
				Assert.AreEqual(2, firstClient.Log(null, new List<string> { firstPath }).Count, "Known commandserver bug: server is out of sync");
			}
			finally
			{
				if (null != firstClient)
					firstClient.Dispose();
				if (null != secondClient)
					secondClient.Dispose();
			}
		}

		[Test]
		[Ignore("Not implemented yet")]
		public void TestSummary()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			string summary = string.Empty;
			MercurialClient.Init(path, MercurialPath);

			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, "1");
				client.Add(new [] { file });
				client.Commit("1", null, false, false, null, null, null, DateTime.MinValue, "user");
				summary = client.Summary(false);
			}

			Assert.IsTrue(summary.Contains("branch: default"));
		}

		[Test]
		public void TestIncoming()
		{
			string firstPath = GetTmpPath();
			string secondPath = GetTmpPath();
			string file = Path.Combine(firstPath, "foo");
			MercurialClient.Init(firstPath, MercurialPath);
			MercurialClient firstClient = null,
			secondClient = null;

			try
			{
				// Create repo with one commit
				firstClient = new MercurialClient(firstPath, MercurialPath);
				File.WriteAllText(file, "1");
				firstClient.Add(new [] { file });
				firstClient.Commit("1");

				// Clone repo
				MercurialClient.Clone(source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new MercurialClient(secondPath, MercurialPath);
				Assert.AreEqual(1, secondClient.Log(null, new List<string>()).Count, "Unexpected number of log entries");

				// Add changesets to original repo
				File.WriteAllText(file, "2");
				firstClient.Commit("2");
				File.WriteAllText(file, "3");
				firstClient.Commit("3");

				var incoming = secondClient.Incoming(null, null);
				Assert.AreEqual(2, incoming.Count, "Unexpected number of incoming changesets");
			}
			finally
			{
				if (null != firstClient)
					firstClient.Dispose();
				if (null != secondClient)
					secondClient.Dispose();
			}
		}

		[Test]
		public void TestOutgoing()
		{
			string firstPath = GetTmpPath();
			string secondPath = GetTmpPath();
			string file = Path.Combine(firstPath, "foo");
			MercurialClient.Init(firstPath, MercurialPath);
			MercurialClient firstClient = null,
			secondClient = null;

			try
			{
				// Create repo with one commit
				firstClient = new MercurialClient(firstPath, MercurialPath);
				File.WriteAllText(file, "1");
				firstClient.Add(new [] { file });
				firstClient.Commit("1");

				// Clone repo
				MercurialClient.Clone(source: firstPath, destination: secondPath, mercurialPath: MercurialPath);
				secondClient = new MercurialClient(secondPath, MercurialPath);
				Assert.AreEqual(1, secondClient.Log(null, null).Count, "Unexpected number of log entries");

				// Add changeset to original repo
				File.WriteAllText(file, "2");
				firstClient.Commit("2");
				File.WriteAllText(file, "3");
				firstClient.Commit("3");

				var outgoing = firstClient.Outgoing(secondPath, null);
				Assert.AreEqual(2, outgoing.Count, "Unexpected number of outgoing changesets");
			}
			finally
			{
				if (null != firstClient)
					firstClient.Dispose();
				if (null != secondClient)
					secondClient.Dispose();
			}
		}

		[Test]
		public void TestRevert()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, string.Empty);
				client.Add(new [] { file });
				client.Commit("Commit all");
				Assert.That(!client.Status(null).ContainsKey("foo"), "Default commit failed for foo");

				File.WriteAllText(file, "Modified!");
				Assert.That(client.Status(null).ContainsKey("foo"), "Failed to modify file");
				client.Revert(null, new List<string> { file });
				Assert.That(!client.Status(null).ContainsKey("foo"), "Revert failed for foo");
			}
		}

		[Test]
		[Ignore("Not implemented yet")]
		public void TestRename()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, string.Empty);
				client.Add(new [] { file });
				client.Commit("Commit all");
				Assert.That(!client.Status(null).ContainsKey("foo"), "Default commit failed for foo");

				client.Rename("foo", "foo2");
				IDictionary<string,Status > statuses = client.Status(null);
				statuses = client.Status(new[]{ path }, quiet: false);
				Assert.AreEqual(Status.Removed, statuses["foo"], string.Format("Incorrect status for foo: {0}", statuses["foo"]));
				Assert.AreEqual(Status.Added, statuses["foo2"], string.Format("Incorrect status for foo2: {0}", statuses["foo2"]));

				client.Commit("Commit rename");
				Assert.That(!client.Status(null).ContainsKey("foo"), "Failed to rename file");
				Assert.That(!client.Status(null).ContainsKey("foo2"), "Failed to rename file");
				Assert.That(!File.Exists(file));
				Assert.That(File.Exists(Path.Combine(path, "foo2")));
			}
		}

		[Test]
		public void TestCat()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, "foo\n");
				client.Add(new []{ Path.Combine(path, "foo") });
				client.Commit("Commit all");
				Assert.That(!client.Status(null).ContainsKey("foo"), "Default commit failed for foo");

				var contents = client.Cat(file, null);
				Assert.AreEqual("foo\n", contents);
			}
		}

		[Test]
		public void TestRemove()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, string.Empty);
				client.Add(new [] { file });
				client.Commit("Commit all");
				Assert.That(!client.Status(null).ContainsKey(file), "Default commit failed for foo");

				client.Remove(new List<string> { file });
				Assert.That(!File.Exists(file));

				IDictionary<string,Status > statuses = client.Status(null);
				Assert.That(statuses.ContainsKey("foo"), "No status for foo");
				Assert.AreEqual(Status.Removed, statuses["foo"], string.Format("Incorrect status for foo: {0}", statuses["foo"]));
			}
		}

		[Test]
		public void TestStatus()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			string unknownFile = Path.Combine(path, "bar");
			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, string.Empty);
				File.WriteAllText(unknownFile, string.Empty);
				client.Add(new [] { file });
				IDictionary<string,Status> statuses = client.Status(new List<string> { path });
				Assert.That(statuses.ContainsKey("foo"), "foo not found in status");
				Assert.That(statuses.ContainsKey("bar"), "bar not found in status");
				Assert.AreEqual(Status.Added, statuses["foo"], "Incorrect status for foo");
				Assert.AreEqual(statuses["bar"], Status.Unknown, "Incorrect status for bar");

				statuses = client.Status(new[]{ path }, quiet: true);
				Assert.That(statuses.ContainsKey("foo"), "foo not found in status");
				Assert.AreEqual(Status.Added, statuses["foo"], "Incorrect status for foo");
				Assert.That(!statuses.ContainsKey("bar"), "bar listed in quiet status output");

				statuses = client.Status(new[]{ path }, onlyFilesWithThisStatus: Status.Added);
				Assert.That(statuses.ContainsKey("foo"), "foo not found in status");
				Assert.AreEqual(Status.Added, statuses["foo"], "Incorrect status for foo");
				Assert.That(!statuses.ContainsKey("bar"), "bar listed in added-only status output");
			}
		}

		[Test]
		public void TestRollback()
		{
			string path = GetTmpPath();
			string file = Path.Combine(path, "foo");
			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, string.Empty);
				client.Add(new [] { file });
				client.Commit(file);
				File.WriteAllText(file, file);
				client.Commit(file);
				Assert.AreEqual(2, client.Log(null, null).Count, "Unexpected history length");
				Assert.That(client.Rollback());
				Assert.AreEqual(1, client.Log(null, null).Count, "Unexpected history length after rollback");
				Assert.AreEqual(Status.Modified, client.Status(new List<string> { file })["foo"], "Unexpected file status after rollback");
			}
		}

		[Test]
		public void TestArchive()
		{
			string path = GetTmpPath();
			string archivePath = GetTmpPath();
			string file = Path.Combine(path, "foo");
			MercurialClient.Init(path, MercurialPath);
			using (var client = new MercurialClient(path, MercurialPath))
			{
				File.WriteAllText(file, string.Empty);
				client.Add(new [] { file });
				client.Commit(file);
				client.Archive(archivePath);
				Assert.That(Directory.Exists(archivePath));
				Assert.That(!Directory.Exists(Path.Combine(archivePath, ".hg")));
				Assert.That(File.Exists(Path.Combine(archivePath, "foo")));
			}
		}


		[TestFixtureTearDown]
		public void CleanUp()
		{
			Directory.Delete(_testRepoPath, true);
			foreach (var dir in ToDelete)
			{
				Directory.Delete(dir, true);
			}
		}
	}
}

