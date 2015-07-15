using System.Collections.Generic;
using System.Linq;

namespace Hg.Net
{
	public class ArgumentHelper
	{
		private readonly List<string> _argumentsList;
 
		public ArgumentHelper ()
		{
			_argumentsList = new List<string>();
		}

		public void AddIf(bool condition, params string[] arguments)
		{
			if (condition)
			{
				_argumentsList.AddRange(arguments);
			}
		}

		public void AddIfNotNullOrEmpty(params string[] arguments)
		{
			foreach (var argument in arguments.Where(argument => !string.IsNullOrEmpty(argument)))
			{
				_argumentsList.Add(argument);
			}
		}
	}
}

