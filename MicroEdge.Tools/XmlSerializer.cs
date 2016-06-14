using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace MicroEdge
{
	/// <summary>
	/// Utility class for serializing an instance of an object into XML. The object must
	/// implement the MicroEdge.IXmlSerializable interface.
	/// </summary>
	public static class XmlSerializer
	{
		#region Fields

		private static Dictionary<Tuple<string, Type>, System.Xml.Serialization.XmlSerializer> _cacheSerializers = new Dictionary<Tuple<string, Type>, System.Xml.Serialization.XmlSerializer>();

		#endregion

		#region Serialization Methods

		/// <summary>
		/// Converts an IXmlSerializable object into an XElement using
		/// it's XmlSerializationData
		/// </summary>
		/// <param name="instance">
		/// Object to convert
		/// </param>
		/// <returns>
		/// Populated xelement
		/// </returns>
		public static XElement ToXElement(IXmlSerializable instance)
		{
			return ToXDocument(instance).Root;
		}

		/// <summary>
		/// Converts a serializable object into an XML string.
		/// </summary>
		/// <param name="instance">Object to serialize.</param>
		/// <returns>XML string of object.</returns>
		public static string ToXml(IXmlSerializable instance)
		{
			XDocument xml = ToXDocument(instance);
			return xml.ToString();
		}

		/// <summary>
		/// Converts a serializable object into an XML document.
		/// </summary>
		/// <param name="instance">Object to serialize.</param>
		/// <returns>XML document of object.</returns>
		public static XDocument ToXDocument(IXmlSerializable instance)
		{
			XDocument xml = new XDocument(new XElement(instance.XmlElementName));

			List<XmlSerializationInfo> infoList = instance.GetXmlSerializationData();

			foreach (XmlSerializationInfo serializationInfo in infoList)
			{
				// Turn the value into a suitable string.
				XElement element = GetXmlData(instance, serializationInfo);

			    // ReSharper disable once PossibleNullReferenceException
				xml.Root.Add(element);
			}

			return xml;
		}

		/// <summary>
		/// Builds XML element for a particular property on an instance.
		/// </summary>
		/// <param name="instance">The instance to build XML element for.</param>
		/// <param name="serializationInfo">Info about the property to serialize.</param>
		/// <returns>XML element for the property.</returns>
		public static XElement GetXmlData(IXmlSerializable instance, XmlSerializationInfo serializationInfo)
		{
			// Get the raw value out of the property.
			Type valueType;
			object rawValue = ReflectionTools.GetProperty(instance, serializationInfo.PropertyName, out valueType);

			if (rawValue is IList)
			{
				// Create the element with the correct name.
				XElement element = new XElement(serializationInfo.ElementName);

				IList valuesEnumerable = rawValue as IList;
				// Make sure that this is an enumerable of items that implement IXmlSerializable.
				Debug.Assert(valuesEnumerable.Count == 0 || valuesEnumerable[0] is IXmlSerializable);
				// Serialize each item individually and add it to the xml element.
				foreach (IXmlSerializable rawValueItem in valuesEnumerable)
				{
					XDocument itemXml = ToXDocument(rawValueItem);
					element.Add(itemXml.Root);
				}

				return element;
			}
			else
			{
				// Single value, use set it directly.
				XElement element = AddXmlValueToElement(valueType, serializationInfo, rawValue);

				return element;
			}
		}

	    /// <summary>
	    /// Gets the XML compatible string value of a piece of data.
	    /// </summary>
	    /// <param name="valueType">Type of the data.</param>
	    /// <param name="serializationInfo"></param>
	    /// <param name="rawValue">The data itself.</param>
	    /// <returns>XML appropriate string for the data.</returns>
	    private static XElement AddXmlValueToElement(Type valueType, XmlSerializationInfo serializationInfo, object rawValue)
		{
			// Create the element with the correct name.
			XElement element = new XElement(serializationInfo.ElementName);

			if (valueType == typeof(string) || valueType.IsPrimitive || valueType.IsEnum)
			{
				// Must just need to put it to a string.
				string value = Tools.ToString(rawValue);
				element.Value = value;
			}
			else if (valueType.IsClass && valueType.GetInterfaces().Contains(i => i == typeof(IXmlSerializable)))
			{
				// Must be a serializable class; it'll do it's own mojo (but don't bother if null)
				if (rawValue != null)
				{
					string value = ToXml(rawValue as IXmlSerializable);
					element.Value = value;
				}
			}
			else  //if (!valueType.IsClass && !valueType.IsPrimitive)
			{
				// Anything else, we should try to serialize with the system serializer.
				// (This should handle struct values like Color and KeyValuePair)

				// Create special setting to omit name space stuff.
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.OmitXmlDeclaration = true;
				System.Xml.Serialization.XmlSerializerNamespaces ns = new System.Xml.Serialization.XmlSerializerNamespaces();
				ns.Add("", "");

				// Create writer to write into.
				StringBuilder xmlClause = new StringBuilder();
				XmlWriter writer = XmlWriter.Create(xmlClause, settings);

				// Create serializer.
				System.Xml.Serialization.XmlSerializer systemSerializer = CreateSystemXmlSerializer(valueType, serializationInfo);
				systemSerializer.Serialize(writer, rawValue, ns);

				XDocument serializedValue = XDocument.Parse(xmlClause.ToString());
				// Use the entire element itself as the element to represent the property.
				// (The element name should be built into it's root)
				XElement valueElement = serializedValue.Elements().First();
				element = valueElement;
			}

			return element;
		}

        #endregion

        #region Deserialization Methods

        /// <summary>
        /// Populates an instance of a serializable object from XML.
        /// </summary>
        /// <param name="xmlElement">
        /// XElement containing the xml to use to populate the instance.
        /// </param>
        /// <param name="newInstance">
        /// Instance to put data in.
        /// </param>
        public static void PopulateInstance(XElement xmlElement, IXmlSerializable newInstance)
		{
			//Null object?  Nothing to do.
			if (newInstance == null)
				return;

			List<XmlSerializationInfo> infoList = newInstance.GetXmlSerializationData();

			foreach (XmlSerializationInfo serializationInfo in infoList)
			{
				XElement element = xmlElement.Element(serializationInfo.ElementName);
				if (element != null)
				{
					PopulateProperty(element, serializationInfo, newInstance);
				}
			}
		}
		public static void PopulateInstance(XDocument xmlDoc, IXmlSerializable newInstance)
		{
			PopulateInstance(xmlDoc.Elements().First(), newInstance);
		}
		public static void PopulateInstance(string xml, IXmlSerializable newInstance)
		{
			PopulateInstance(XDocument.Parse(xml), newInstance);
		}

		/// <summary>
		/// Populates data from XML onto a target property.
		/// </summary>
		/// <param name="xml">Xml for the property.</param>
		/// <param name="serializationInfo">Serialization info about the property.</param>
		/// <param name="instance">The instance to set the target property for.</param>
		private static void PopulateProperty(XElement xml, XmlSerializationInfo serializationInfo, IXmlSerializable instance)
		{
			if (serializationInfo.IsPropertyCollection)
			{
				// This is a list/collection, we must need to populate it.

				// Get the type for the items in it.
				Type itemType = serializationInfo.ItemsInCollectionType;

				Type propertyType;
				IList list = ReflectionTools.GetProperty(instance, serializationInfo.PropertyName, out propertyType) as IList;
			    if (list == null)
                    return;

			    foreach (XElement subElement in xml.Elements())
			    {
			        object childItem = ConvertXmlToDataForProperty(subElement, instance, itemType, serializationInfo);

			        //CreateChildObjectForProperty may have already added the child to the list property, so test to see if it's there before adding it.
			        if (!list.Contains(childItem))
			            list.Add(childItem);
			    }
			}
			else
			{
				// This is a regular property we need to directly set the value on.

				// Get the property name and type.
				Type propertyType = ReflectionTools.GetPropertyInfo(instance, serializationInfo.PropertyName).PropertyType;

				// Get the data for the property.
				object value = ConvertXmlToDataForProperty(xml, instance, propertyType, serializationInfo);

				// Put the data on the instance.
				ReflectionTools.SetProperty(instance, serializationInfo.PropertyName, value);
			}
		}

		/// <summary>
		/// Turns xml for a property into data.
		/// </summary>
		/// <param name="xml">The xml representing the property.</param>
		/// <param name="parentInstance">The instance the data is for.</param>
		/// <param name="propertyType">The type of the target property.</param>
		/// <param name="serializationInfo">
		/// Serialization info for the property.
		/// </param>
		/// <returns>The data for the property.</returns>
		private static object ConvertXmlToDataForProperty(XElement xml, IXmlSerializable parentInstance, Type propertyType, XmlSerializationInfo serializationInfo)
		{
		    // Convert out of the XML based on the type.
			// Note that these checks are order dependant.
			if (propertyType == typeof(string) || propertyType.IsPrimitive)
			{
				return Tools.ConvertType(xml.Value, propertyType);
			}

		    if (propertyType.IsClass && propertyType.GetInterfaces().Contains(i => i == typeof(IXmlSerializable)))
		    {
		        // Let the parent create the instance.
		        IXmlSerializable childInstance = parentInstance.CreateChildObjectForProperty(serializationInfo.PropertyName, xml);

		        // Populate the instance 
		        PopulateInstance(xml, childInstance);

		        return childInstance;
		    }

		    if (propertyType.IsEnum)
		    {
		        // Enumerated type, make sure the value is defined in the enumeration.
		        if (!propertyType.IsEnumDefined(xml.Value))
		        {
		            throw new SystemException("Enum value not defined.");
		        }

		        // Parse the value into the enumerated type.
		        object enumValue = Enum.Parse(propertyType, xml.Value);
		        return enumValue;
		    }

		    // Anything else, we should try to deserialize with the system serializer.
		    // Get the serializer with the appropriate settings for this property.
		    System.Xml.Serialization.XmlSerializer serializer = CreateSystemXmlSerializer(propertyType, serializationInfo);

		    object value = serializer.Deserialize(new StringReader(xml.ToString()));

		    return value;
		}

	    #endregion

		#region Methods


		/// <summary>
		/// Creates a system serializer with the appropriate setting to (de)serialize
		/// a property with the correct element name etc.
		/// </summary>
		/// <param name="propertyType">Type of the property being serialized.</param>
		/// <param name="serializationInfo">Info dictating how to serialize the property.</param>
		/// <returns>The system serializer.</returns>
		private static System.Xml.Serialization.XmlSerializer CreateSystemXmlSerializer(Type propertyType, XmlSerializationInfo serializationInfo)
		{
			//// Create Xml attribute override info, so we can force the serializer to use the element name.
			//System.Xml.Serialization.XmlElementAttribute elementAttribute = new System.Xml.Serialization.XmlElementAttribute();
			//elementAttribute.ElementName = serializationInfo.ElementName;
			//elementAttribute.Type = propertyType;
			//// Add the element attribute to attributes
			//System.Xml.Serialization.XmlAttributes attributes = new System.Xml.Serialization.XmlAttributes();
			//attributes.XmlElements.Add(elementAttribute);
			//// Add the attributes to the overrides.
			//System.Xml.Serialization.XmlAttributeOverrides attributeOverides = new System.Xml.Serialization.XmlAttributeOverrides();
			//attributeOverides.Add(propertyType, attributes);

			System.Xml.Serialization.XmlRootAttribute rootAttribute = new System.Xml.Serialization.XmlRootAttribute();
			rootAttribute.ElementName = serializationInfo.ElementName;
			rootAttribute.Namespace = "";

			Tuple<string, Type> serializerKey = new Tuple<string, Type>(serializationInfo.ElementName, propertyType);

			// Create the serializer with the overrides.
			if (!_cacheSerializers.ContainsKey(serializerKey))
			{
				_cacheSerializers.Add(serializerKey, new System.Xml.Serialization.XmlSerializer(propertyType, rootAttribute));
			}

			System.Xml.Serialization.XmlSerializer serializer = _cacheSerializers[serializerKey];

			return serializer;
		}

		#endregion
	}
}