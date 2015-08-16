using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Hg.Net.Models;

namespace Hg.Net
{
	public class XmlHelper
	{
		public static IList<CommandServerRevision> GetRevisions(string text)
		{
			var document = new XmlDocument();
			document.LoadXml(text);

			var revisions = new List<CommandServerRevision>();
			var xmlNodeList = document.SelectNodes("/log/logentry");
			if (xmlNodeList != null)
			{
				revisions.AddRange(xmlNodeList.Cast<XmlNode>().Select(n => new CommandServerRevision(n)));
			}

			return revisions;
		}
	}
}

