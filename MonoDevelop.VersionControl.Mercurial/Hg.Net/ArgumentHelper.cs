using System;
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

		public void AddIfNotNullOrEmpty(bool throwExceptonIfFalse, params string[] arguments)
		{
			if (arguments.Any(x => string.IsNullOrEmpty(x)))
			{
				if (throwExceptonIfFalse)
				{
					throw new ArgumentException("can not be bull or empty", arguments.First(x => string.IsNullOrEmpty(x)));
				}
				return;
			}
			_argumentsList.AddRange(arguments);
		}

		public void Add(params string[] argumets)
		{
			_argumentsList.AddRange(argumets);
		}

		public List<string> GetList()
		{
			return _argumentsList;
		}

		public override string ToString()
		{
			return string.Join(" ", _argumentsList);
		}
	}
}

