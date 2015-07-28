using System;
using System.Collections.Generic;
using System.Linq;
using Hg.Net.Models;

namespace Hg.Net
{
    public class ArgumentHelper
    {
        private readonly List<string> _argumentsList;

        public ArgumentHelper()
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
            if (arguments.Any(string.IsNullOrEmpty))
            {
                if (throwExceptonIfFalse)
                {
                    throw new ArgumentException("can not be bull or empty", arguments.First(string.IsNullOrEmpty));
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

		public static string ArgumentForStatus (Status status)
		{
			switch (status) {
				case Status.Added:
					return "--added";
				case Status.Clean:
					return "--clean";
				case Status.Ignored:
					return "--ignored";
				case Status.Modified:
					return "--modified";
				case Status.Removed:
					return "--removed";
				case Status.Unknown:
					return "--unknown";
				case Status.Missing:
					return "--deleted";
				case Status.All:
					return "--all";
				default:
					return string.Empty;
			}
        }
    }
}

