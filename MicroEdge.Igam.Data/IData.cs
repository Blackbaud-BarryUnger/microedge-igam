namespace MicroEdge.Igam.Data
{
	/// <summary>
	/// This interface defines the lowest level properties/methods that must be common 
	/// any class representing a data store (such as the Row class)
	/// </summary>
	public interface IData
	{
		void Update();
	}

	/// <summary>
	/// This version establishes the type of the id/key property
	/// </summary>
	public interface IData<K> : IData
	{
		K Id { get; set; }
	}
}
