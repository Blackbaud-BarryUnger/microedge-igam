using System;
using System.Xml;
using System.Text;

namespace MicroEdge
{
	/// <summary>
	/// These tools are specific to the manipulation of XML.
	/// </summary>
	public static class XmlTools
	{
		#region Methods

		/// <summary>
		/// Strips bum characters and replaces others with escape sequences or alternate characters
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string CleanString(string data)
		{
			for (int i = 0; i < 32; i++)
			{
				switch (i)
				{
					case 9:
					case 10:
					case 13:
						//Do nothing. These characters are allowed.
						break;

					default:
						//Remove this character.
						data = data.Replace(((char)i).ToString(), "");
						break;
				}
			}

			return data.Replace((char)147, '"').Replace((char)148, '"').Replace("&", "&amp;")
						.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;")
						.Replace("'", "&apos;");
		}

		/// <summary>
		/// Get the value of the xml attribute with the indicated name from the indicated xml node.
		/// </summary>
		/// <param name="node">
		/// The xml node whose attribute value to return.
		/// </param>
		/// <param name="name">
		/// The name of the attribute whose value to return.
		/// </param>
		/// <param name="dfltValue">
		/// The value to return if the attribute doesn't exist or the attribute has no value.
		/// </param>
		/// <returns>
		/// The value of the indicated attribute. An emptry string if the attribute does not exist.
		/// </returns>
		public static string GetAttributeValue(this XmlNode node, string name, string dfltValue)
		{
			string value;
			XmlAttribute attrib = null;

			if (node != null && node.Attributes != null)
                attrib = node.Attributes[name];

		    if (attrib == null)
				value = "";
			else
				value = attrib.InnerText;

			return value == string.Empty ? dfltValue : value;
		}
		public static string GetAttributeValue(this XmlNode node, string name)
		{
			return GetAttributeValue(node, name, "");
		}

		/// <summary>
		/// Set the value of the xml attribute with the indicated name to the indicated value for the indicated 
		/// xml node.
		/// </summary>
		/// <param name="node">
		/// The xml node whose attribute value to set.
		/// </param>
		/// <param name="name">
		/// The name of the attribute whose value to set.
		/// </param>
		/// <param name="value">
		/// The value of the attribute to set.
		/// </param>
		public static void SetAttributeValue(this XmlNode node, string name, string value)
		{
		    if (node.OwnerDocument == null || node.Attributes == null)
		        return;

			XmlAttribute attrib = node.Attributes[name];
			if (attrib == null)
			{
				//Attribute doesn't exist. Add it now.
				attrib = node.OwnerDocument.CreateAttribute(name);
				node.Attributes.Append(attrib);
			}

			attrib.InnerText = value;
		}

		/// <summary>
		/// Add a child node to the current node with the indicated name.
		/// </summary>
		/// <param name="node">
		/// The node to which to add the child node.
		/// </param>
		/// <param name="name">
		/// The name of the child node to add.
		/// </param>
		/// <param name="value">
		/// The value to assign to the new child node.
		/// </param>
		/// <param name="type">
		/// The type of node to create. Either XmlNodeType.CDATA, XmlNodeType.Text, or XmlNodeType.Element.
		/// </param>
		/// <param name="position">
		/// The position where the child should be placed in the parent node. This will be -1 if the child should be last.
		/// </param>
		/// <returns>
		/// The child node added.
		/// </returns>
		public static XmlNode AddChildNode(this XmlNode node, string name, string value, XmlNodeType type, int position)
		{
		    if (node.OwnerDocument == null)
		        return null;

            XmlNode returnNode;
            XmlNode child = node.OwnerDocument.CreateElement(name);
			switch (type)
			{
			    case XmlNodeType.CDATA:
			        XmlCDataSection cdataNode = node.OwnerDocument.CreateCDataSection(value);
			        child.AppendChild(cdataNode);

			        returnNode = cdataNode;
			        break;

			    case XmlNodeType.Text:
			        XmlText textNode = node.OwnerDocument.CreateTextNode(value);
			        child.AppendChild(textNode);
			        node.AppendChild(child);
			        returnNode = textNode;
			        break;

			    default:
			        //For XmlNodeType.Element, the value will become the inner text of the node.
			        child.InnerText = value;
			        node.AppendChild(child);
			        returnNode = child;
			        break;
			}

			if (position == -1)
				node.AppendChild(child);
			else
			{
				XmlNode referenceNode = node.ChildNodes[position];
				if (referenceNode == null)
					node.AppendChild(child);
				else
					node.InsertBefore(child, referenceNode);
			}

			return returnNode;
		}
		public static XmlNode AddChildNode(this XmlNode node, string name, string value, XmlNodeType type)
		{
			return AddChildNode(node, name, value, type, -1);
		}
		public static XmlNode AddChildNode(this XmlNode node, string name, string value, int position)
		{
			return AddChildNode(node, name, value, XmlNodeType.Element, position);
		}
		public static XmlNode AddChildNode(this XmlNode node, string name, string value)
		{
			return AddChildNode(node, name, value, XmlNodeType.Element, -1);
		}
		public static XmlNode AddChildNode(this XmlNode node, string name, XmlNodeType type, int position)
		{
			return AddChildNode(node, name, "", type, position);
		}
		public static XmlNode AddChildNode(this XmlNode node, string name, XmlNodeType type)
		{
			return AddChildNode(node, name, "", type, -1);
		}
		public static XmlNode AddChildNode(this XmlNode node, string name, int position)
		{
			return AddChildNode(node, name, "", XmlNodeType.Element, position);
		}
		public static XmlNode AddChildNode(this XmlNode node, string name)
		{
			return AddChildNode(node, name, "", XmlNodeType.Element, -1);
		}

		/// <summary>
		/// Add a child node to the current node consisting of the indicated xml.
		/// </summary>
		/// <param name="node">
		/// The node to which to add the child node.
		/// </param>
		/// <param name="xml">
		/// The xml which will be used to create the new node.
		/// </param>
		/// <returns>
		/// The child node added.
		/// </returns>
		public static XmlNode AddChildNodeXml(this XmlNode node, string xml)
		{
			//We need to load the xml into a separate xml document first.
			XmlDocument childDoc = new XmlDocument();
			childDoc.LoadXml(xml);

			//Add as a foreign node.
			return AddForeignChildNode(node, childDoc.DocumentElement);
		}

		/// <summary>
		/// Add a child node to the indicated node when the child node is from a different document.
		/// </summary>
		/// <param name="node">
		/// The node to which to add the child.
		/// </param>
		/// <param name="foreignNode">
		/// The node from a different document to add. Note we will add a clone of this node so that the node is not removed from its current document.
		/// </param>
		/// <returns>
		/// The child node added.
		/// </returns>
		public static XmlNode AddForeignChildNode(this XmlNode node, XmlNode foreignNode)
		{
		    if (node.OwnerDocument == null)
		        return null;

			//Now import the node into the target document.
			XmlNode childNode = node.OwnerDocument.ImportNode(foreignNode.Clone(), true);

			//And append.
			node.AppendChild(childNode);

			return childNode;
		}

		/// <summary>
		/// Create a node below this node with the indicated path and return the node. If the node
		/// already exists, simply return the node.
		/// </summary>
		/// <param name="parentNode">
		/// The node below which to create the new node.
		/// </param>
		/// <param name="path">
		/// The path of the node to create.
		/// </param>
		/// <param name="type">
		/// Type indicates the type of node to create.
		/// </param>
		/// <returns>
		/// The node below the indicated node with the indicated path. If the indicated node already exists,
		/// this node is simply returned.
		/// </returns>
		public static XmlNode CreateNode(this XmlNode parentNode, string path, XmlNodeType type)
		{
			//Get the desired element.
			XmlNode node;
			if (path == "")
				node = parentNode;
			else
				node = parentNode.SelectSingleNode(path);

			if (node != null)
			{
				//If type is CData or Text check for a child node of this type. If found return it, else
				//create and return.
				if (type == XmlNodeType.CDATA)
				{
					XmlCDataSection cdataNode = GetCDataSection(node);
				    if (cdataNode != null)
                        return cdataNode;

                    if (parentNode.OwnerDocument == null)
                        return null;

                    cdataNode = parentNode.OwnerDocument.CreateCDataSection("");
				    node.AppendChild(cdataNode);
				    return cdataNode;
				}

			    if (type != XmlNodeType.Text)
                    return node;

			    XmlText textNode = GetTextSection(node);
			    if (textNode != null)
                    return textNode;

			    if (parentNode.OwnerDocument == null)
			        return null;

                textNode = parentNode.OwnerDocument.CreateTextNode("");
			    node.AppendChild(textNode);
			    return textNode;
			}

			//If not found, use CreateNode to get the immediate parent of this element and
			//create the child on this element.
			string nodeName;
			string[] nodes = path.Split('/');
			if (nodes.Length == 1)
			{
				nodeName = path;
				node = parentNode;
			}
			else
			{
				nodeName = nodes[nodes.Length - 1];
				path = nodes[nodes.Length - 2];
				node = CreateNode(parentNode, path, XmlNodeType.Element);
			}

			return AddChildNode(node, nodeName, type);
		}
		public static XmlNode CreateNode(this XmlNode parentNode, string path)
		{
			return CreateNode(parentNode, path, XmlNodeType.Element);
		}

		/// <summary>
		/// Get the first CData section child of the indicated node.
		/// </summary>
		/// <param name="node">
		/// The node whose CData section should be returned.
		/// </param>
		/// <returns>
		/// The first CData section child of the indicated node.
		/// </returns>
		private static XmlCDataSection GetCDataSection(this XmlNode node)
		{
			XmlNode child = node.FirstChild;
			while (child != null)
			{
				if (child.NodeType == XmlNodeType.CDATA) 
					return (XmlCDataSection) child;

				child = child.NextSibling;
			}

			return null;
		}

		/// <summary>
		/// Get the first Text section child of the indicated node.
		/// </summary>
		/// <param name="node">
		/// The node whose Text section should be returned.
		/// </param>
		/// <returns>
		/// The first Text section child of the indicated node.
		/// </returns>
		private static XmlText GetTextSection(this XmlNode node)
		{
			XmlNode child = node.FirstChild;
			while (child != null)
			{
				if (child.NodeType == XmlNodeType.Text) 
					return (XmlText)child;

				child = child.NextSibling;
			}

			return null;
		}

		/// <summary>
		/// Remove those characters in the xml string which are not valid xml characters.
		/// </summary>
		/// <param name="xml">
		/// The xml string from which to remove the invalid characters.
		/// </param>
		/// <returns>
		/// The xml string with the invalid characters removed.
		/// </returns>
		public static string RemoveInvalidCharacters(string xml)
		{
			//Invalid xml characters are those below hexadecimal value 0x20 with the exception of horizontal tab 
			//(0x9), line feed (0xA), and carriage return (0xD).
			StringBuilder result = new StringBuilder(xml.Length);
			foreach (char c in xml)
			{
				try
				{
					byte asc = Convert.ToByte(c);
					if (asc < 32 && (asc != 9 && asc != 10 && asc != 13))
						result.Append(" "); //Replace with a space.
					else
						result.Append(c);
				}
				catch
				{
					result.Append(c);
				}
			}

			return result.ToString();
		}

		/// <summary>
		/// Get the value of the node beneath the current node indicated by the indicated path.
		/// </summary>
		/// <param name="node">
		/// Node is the ancestor of the node whose value we are to get.
		/// </param>
		/// <param name="path">
		/// This is a path of the form Element/Child/GrandChild... indicating the node whose value to return.
		/// </param>
		/// <returns>
		/// The string contained in the node indicated by path. If the node contains a cdata node, the value
		/// of this cdata node will be returned. If the node contains a text node the value of this text node
		/// will be returned. Otherwise an empty string will be returned.
		/// </returns>
		public static string GetValue(this XmlNode node, string path)
		{
			if (node != null)
			{
			    if (path == "")
					return GetValue(node);
			    return GetValue(node.SelectSingleNode(path));
			}

		    return "";
		}

		/// <summary>
		/// Get the value of the indicated node.
		/// </summary>
		/// <param name="node">
		/// The node whose value to get.
		/// </param>
		/// <returns>
		/// The string contained in the node. If the node contains a cdata node, the value
		/// of this cdata node will be returned. If the node contains a text node the value of this text node
		/// will be returned. Otherwise an empty string will be returned.
		/// </returns>
		public static string GetValue(this XmlNode node)
		{
			if (node != null)
			{
				XmlCDataSection cdataNode = GetCDataSection(node);
				if (cdataNode != null)
					return cdataNode.Value;

				XmlText textNode = GetTextSection(node);
				if (textNode != null)
					return textNode.Value;
			}

			return "";
		}

		/// <summary>
		/// Get the value of the node beneath the current node indicated by the indicated path. Return a default value if the child node 
		/// could not be found or if the node has no value.
		/// </summary>
		/// <param name="node">
		/// Node is the ancestor of the node whose value we are to get.
		/// </param>
		/// <param name="path">
		/// This is a path of the form Element/Child/GrandChild... indicating the node whose value to return.
		/// </param>
		/// <param name="defaultValue">
		/// This is the string that should be returned if the indicated path is not found
		/// </param>
		/// <returns>
		/// The string contained in the node indicated by path. If the node contains a cdata node, the value
		/// of this cdata node will be returned. If the node contains a text node the value of this text node
		/// will be returned. Otherwise an empty string will be returned.
		/// </returns>
		public static string GetValue(this XmlNode node, string path, string defaultValue)
		{
			if (node != null)
			{
				XmlNode child;
				if (path == "")
					child = node;
				else
					child = node.SelectSingleNode(path);
				
				if (child != null)
				{
					XmlCDataSection cdataNode = GetCDataSection(child);
					if (cdataNode != null)
						return cdataNode.Value;

					XmlText textNode = GetTextSection(child);
					if (textNode != null)
						return textNode.Value;
				}
			}

			return defaultValue;
		}

		/// <summary>
		/// Set the value of the node beneath the current node indicated by the indicated path.
		/// </summary>
		/// <param name="node">
		/// Node is the ancestor of the node whose value we are to set.
		/// </param>
		/// <param name="path">
		/// This is a path of the form Element/Child/GrandChild... indicating the node whose value to set.
		/// </param>
		/// <param name="value">
		/// The value to set in the node.
		/// </param>
		/// <param name="cdata">
		/// True if the value should be stored in a cdata node beneath the indicated node.
		/// </param>
		public static void SetValue(this XmlNode node, string path, string value, bool cdata)
		{
			if (cdata)
			{
				XmlCDataSection cdataNode = CreateNode(node, path, XmlNodeType.CDATA) as XmlCDataSection;
				if (cdataNode != null) cdataNode.Value = value;
			}
			else
			{
				XmlText textNode = CreateNode(node, path, XmlNodeType.Text) as XmlText;
				if (textNode != null) textNode.Value = value;
			}
		}
		public static void SetValue(this XmlNode node, string path, string value)
		{
			SetValue(node, path, value, false);
		}
		public static void SetValue(this XmlDocument doc, string path, string value, bool cdata)
		{
			SetValue(doc.DocumentElement, path, value, cdata);
		}
		public static void SetValue(this XmlDocument doc, string path, string value)
		{
			SetValue(doc.DocumentElement, path, value, false);
		}

		#endregion Methods
	}
}
