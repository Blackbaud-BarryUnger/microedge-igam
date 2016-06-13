using System;
using System.Xml.Linq;

namespace MicroEdge.Igam.Business
{
    public static class Tools
    {
        public static char FieldMarker = (char) 255;

        /// <summary>
        /// Get the value of the indicated child element of this XElement or an empty string if the child element doesn't exist.
        /// </summary>
        /// <param name="value">
        /// An XElement.
        /// </param>
        /// <param name="name">
        /// The name of a child XElement. 
        /// </param>
        /// <returns>
        /// The value of the indicated child element. If a child element with the indicated name doesn't exist, an empty string is returned.
        /// </returns>
        public static string GetElementValueOrEmptyString(this XElement value, string name)
        {
            if (value == null)
                return string.Empty;

            XElement element = value.Element(name);
            return element == null 
                ? string.Empty 
                : element.Value;
        }

        /// <summary>
        /// Get the value of the indicated child element of this XElement or a specified default string if the child element doesn't exist.
        /// </summary>
        /// <param name="value">
        /// An XElement.
        /// </param>
        /// <param name="name">
        /// The name of a child XElement. 
        /// </param>
        /// <param name="dflt"></param>
        /// <returns>
        /// The value of the indicated child element. If a child element with the indicated name doesn't exist, the specified default string is returned.
        /// </returns>
        public static string GetElementValueOrDefaultString(this XElement value, string name, string dflt)
        {
            XElement element = value.Element(name);
            if (element == null)
                return dflt;
            return element.Value;
        }

        /// <summary>
        /// Get the value of the indicated attribute of this XElement or an empty string if the attribute doesn't exist.
        /// </summary>
        /// <param name="value">
        /// An XElement.
        /// </param>
        /// <param name="name">
        /// The name of an attribute. 
        /// </param>
        /// <returns>
        /// The value of the indicated attribute. If an attribute with the indicated name doesn't exist, an empty string is returned.
        /// </returns>
        public static string GetAttributeValueOrEmptyString(this XElement value, string name)
        {
            XAttribute attribute = value.Attribute(name);
            if (attribute == null)
                return String.Empty;
            return attribute.Value;
        }

        /// <summary>
        /// Get the value of the indicated attribute of this XElement or a specified default string if the attribute doesn't exist.
        /// </summary>
        /// <param name="value">
        /// An XElement.
        /// </param>
        /// <param name="name">
        /// The name of an attribute. 
        /// </param>
        /// <param name="dflt"></param>
        /// <returns>
        /// The value of the indicated attribute. If an attribute with the indicated name doesn't exist, the specified default string is returned.
        /// </returns>
        public static string GetAttributeValueOrDefaultString(this XElement value, string name, string dflt)
        {
            XAttribute attribute = value.Attribute(name);
            if (attribute == null)
                return dflt;
            return attribute.Value;
        }
    }
}
