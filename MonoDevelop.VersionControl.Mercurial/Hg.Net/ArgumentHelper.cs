using System;
using System.Collections.Generic;

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
			foreach (var argument in arguments)
			{
				if (!string.IsNullOrEmpty(argument))
				{
					_argumentsList.Add(argument);
				}
				else if (throwExceptonIfFalse)
				{
					throw new ArgumentException("can not be bull or empty", argument);
				}
			}
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
			return string.Concat(_argumentsList, ' ');
		}
	}
}

