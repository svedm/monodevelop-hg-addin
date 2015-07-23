using System;
using System.Xml;

namespace Hg.Net.Models
{
	public class CommandServerRevision
	{
		public string RevisionId { get; private set; }

		public DateTime Date { get; private set; }

		public string Author { get; private set; }

		public string Email { get; private set; }

		public string Message { get; private set; }

		public CommandServerRevision(string revision, DateTime date, string author, string email, string message)
		{
			RevisionId = revision;
			Date = date;
			Author = author;
			Message = message;
			Email = email;
		}

		public CommandServerRevision(XmlNode node)
		{
			if (node.Attributes != null)
			{
				RevisionId = node.Attributes["revision"].Value;

				var date = node.SelectSingleNode("date");
				if (date != null)
				{
					Date = DateTime.Parse(date.InnerText);
				}

				var author = node.SelectSingleNode("author");
				if (author != null)
				{
					Email = author.InnerText;
					if (author.Attributes != null)
					{
						Author = author.Attributes["email"].Value;
					}
				}
			}
			var selectSingleNode = node.SelectSingleNode("msg");
			if (selectSingleNode != null)
			{
				Message = selectSingleNode.InnerText;
			}
		}
	}
}

