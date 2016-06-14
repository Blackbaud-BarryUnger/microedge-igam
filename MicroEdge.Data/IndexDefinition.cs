using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MicroEdge.Igam.Data
{
	/// <summary>
	/// Definition of an index on one or more columns in a database table; used by various 
	/// column/table tools in Data and DbTools
	/// </summary>
	public class IndexDefinition
	{
		public IndexDefinition(string name, bool isPrimary, bool isUnique)
		{
			IsPrimary = isPrimary;
			IsUnique = isUnique;
			Name = name;
			Columns = new List<string>();
		}
		
		public IndexDefinition(string name, bool isPrimary, bool isUnique, string columnName)
			: this(name, isPrimary, isUnique )
		{
			Columns.Add(columnName);
		}

		public IndexDefinition(string name, bool isPrimary, bool isUnique, IEnumerable<string> columnNames)
			: this(name, isPrimary, isUnique)
		{
			Columns.AddRange(columnNames);
		}

		public bool IsPrimary { get; private set; }
		public bool IsUnique { get; private set; }
		public string Name { get; private set; }
		public List<string> Columns { get; private set; }
	}
}
