using System;

namespace MicroEdge.Igam.Data
{
	/// <summary>
	/// Definition of a column in a database table; used by various 
	/// column/table tools in Data and DbTools
	/// </summary>
	public class ColumnDefinition
	{
		#region Constructors

		public ColumnDefinition(string name, Type type, int? size) :
			this(name, type, size, true, false)
		{ }

		public ColumnDefinition(string name, Type type, int? size, bool allowNull) :
			this(name, type, size, allowNull, false)
		{ }

		public ColumnDefinition(string name, Type type, int? size, bool allowNull, bool autoNumber)
		{
			Name = name;
			if (autoNumber)
			{
				IsAutoNumber = true;
				Type = typeof(int);
			}
			else
			{
				AllowsNull = allowNull;
				Type = type;
				Size = size;
			}
		}

		#endregion Constructors


		#region Properties

		public bool AllowsNull {get; private set;}

		public bool IsAutoNumber { get; private set; }

		public string Name {get; private set;}

		public Type Type { get; private set; }

		public int? Size { get; private set; }

		#endregion Properties
	}
}
