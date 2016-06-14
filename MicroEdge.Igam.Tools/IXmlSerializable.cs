using System.Collections.Generic;
using System.Xml.Linq;

namespace MicroEdge
{
	/// <summary>
	/// Interface that any class that wants to be serializable must implement.
	/// </summary>
	public interface IXmlSerializable
	{
		#region Properties

		/// <summary>
		/// The this will have in its Xml representation.
		/// </summary>
		string XmlElementName { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets a list of which data to serialize and how to do it.
		/// </summary>
		/// <returns>
		/// The list of XmlSerializationInfo.
		/// </returns>
		List<XmlSerializationInfo> GetXmlSerializationData();

		/// <summary>
		/// Creates a new child object which is compatible with a particular property.
		/// </summary>
		/// <param name="propertyName">Name of the property which contains child object(s).</param>
		/// <param name="childXml">
		/// xml element for the child object(s)
		/// </param>
		/// <returns>The new instance of a child object.</returns>
		IXmlSerializable CreateChildObjectForProperty(string propertyName, XElement childXml);
		
		#endregion
	}
}