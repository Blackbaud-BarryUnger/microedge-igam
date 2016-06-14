using System;
using System.Collections;

namespace MicroEdge
{
	/// <summary>
	/// Class that represents a single piece of datum (of many) for a
	/// serializable class. (Although this could be a data point for a collection or a whole 'nother object)
	/// </summary>
	public class XmlSerializationInfo
	{
		#region Fields

	    //private readonly bool _isCollection;

	    #endregion

		#region Constructors

	    /// <summary>
	    /// Constructor.
	    /// </summary>
	    /// <param name="propertyName">
	    /// Name of the property to get/set data from.
	    /// </param>
	    public XmlSerializationInfo(string propertyName) : this(propertyName, propertyName, null, null)
		{}

	    /// <summary>
	    /// Constructor.
	    /// </summary>
	    /// <param name="propertyName">
	    /// Name of the property to get/set data from.
	    /// </param>
	    /// <param name="elementName">
	    /// Name of the element for the data in the XML.
	    /// </param>
	    /// <param name="collectionType"></param>
	    /// <param name="itemsInCollectionType"></param>
	    public XmlSerializationInfo(string propertyName, string elementName, Type collectionType, Type itemsInCollectionType)
		{
			if (collectionType != null)
			{
				// Make sure if a collection type is specified it can be added to (IList)
				if (collectionType.GetInterfaces().Contains(i => i is IList))
				{
					throw new System.Exception("Collection must be able to be added to.");
				}

				// Make sure if the collection type isn't null, the item type isn't
				if (itemsInCollectionType == null)
				{
					throw new ArgumentNullException(nameof(itemsInCollectionType), "Can't be null when a collection type is specified.");
				}
			}

			PropertyName = propertyName;
			ElementName = elementName;
			CollectionType = collectionType;
			ItemsInCollectionType = itemsInCollectionType;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Name of the property to get/set data from.
		/// </summary>
		public string PropertyName { get; }

	    /// <summary>
		/// Name of the element for the data in the XML.
		/// </summary>
		public string ElementName { get; }

	    /// <summary>
		/// Indicates if the property is a collection.
		/// </summary>
		public bool IsPropertyCollection
		{
			get
			{
			    return CollectionType != null;
			}
		}

		/// <summary>
		/// Type of collection.
		/// </summary>
		public Type CollectionType { get; }

	    /// <summary>
		/// Type of items in the collection.
		/// </summary>
		public Type ItemsInCollectionType { get; }

	    #endregion
	}
}