namespace MicroEdge
{
	/// <summary>
	/// These tools are specific to the manipulation of HTML.
	/// </summary>
	public static class HtmlTools
	{
		#region Methods

		/// <summary>
		/// Convert the crlf, cr, and lf strings in the text to <br/>.
		/// </summary>
		/// <param name="text">
		/// The text to convert.
		/// </param>
		/// <returns>
		/// The text with all line breaks converted to <br/>.
		/// </returns>
		public static string ConvertLineBreaks(string text)
		{
			if (text != null)
			{
				text = text.Replace("\r\n", "<br/>");
				text = text.Replace("\r", "<br/>");
				text = text.Replace("\n", "<br/>");
			}

			return text;
		}

		#endregion Methods
	}
}
