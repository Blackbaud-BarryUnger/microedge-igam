using System;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;

namespace MicroEdge
{
	/// <summary>
	/// This class wraps an HttpWebRequest and HttpWebResponse object and helps in http communication.
	/// </summary>
	public class HttpComm : IDisposable
	{
		#region Fields

		//Wrapped HttpWebRequest and HttpWebResponse.
		private HttpWebRequest _webRequest;
		private HttpWebResponse _webResponse;

		//Post data as a memory stream.
		private BinaryWriter _postData;
		private MemoryStream _postStream;

		private const string MultipartBoundary = "-----------------------------7cf2a327f01ae";

		#endregion Fields

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="uri">
		/// The uri to send a request to.
		/// </param>
		public HttpComm(string uri)
		{
			_webRequest = (HttpWebRequest)WebRequest.Create(uri);
		}
		public HttpComm()
		{ }

		#endregion Constructors

		#region Properties

		/// <summary>
		/// The HttpWebRequest contained in this object.
		/// </summary>
		public HttpWebRequest HttpWebRequest
		{
			get { return _webRequest; }
			set { _webRequest = value; }
		}

		/// <summary>
		/// The HttpWebResponse contained in this object.
		/// </summary>
		public HttpWebResponse HttpWebResponse
		{
			get { return _webResponse; }
			set { _webResponse = value; }
		}

		/// <summary>
		/// The timeout for the request.
		/// </summary>
		public int Timeout
		{
			get { return _webRequest.Timeout; }
			set { _webRequest.Timeout = value; }
		}

		#endregion Properties

		#region Methods

		/// <summary>
		/// Add a form variable to the request.
		/// </summary>
		/// <param name="name">
		/// The name of the form variable.
		/// </param>
		/// <param name="value">
		/// Value of the form variable
		/// </param>
		public void AddFormVariable(string name, string value)
		{
			try
			{
				//Initialize post stream if not yet set.
				if (_postData == null)
				{
					_postStream = new MemoryStream();
					_postData = new BinaryWriter(_postStream);
				}

				// trb 7/22/14: adjusted for igam v6. Also explicity use utf-8 and not default. default doesn't necessarily mean utf-8 depending on system application
				// is being run on.  Note some changes have been to the spacing of the variables.
				_postData.Write(Encoding.UTF8.GetBytes("--" + MultipartBoundary + "\r\n" + "Content-Disposition: form-data; name=\"" + name + "\"\r\n"));
				_postData.Write(Encoding.UTF8.GetBytes("Content-Type:text/plain;charset=utf-8;" + "\r\n\r\n"));
				_postData.Write(Encoding.UTF8.GetBytes(value));
				_postData.Write(Encoding.UTF8.GetBytes("\r\n"));

			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while adding a form variable to an http request.", ex);
			}
		}

		/// <summary>
		/// Add a file to the request.
		/// </summary>
		/// <param name="name">
		/// The name of the file variable.
		/// </param>
		/// <param name="fileName">
		/// The name of the file.
		/// </param>
		/// <param name="fileContents">
		/// The file contents.
		/// </param>
		public void AddFile(string name, string fileName, byte[] fileContents)
		{
			try
			{
				//Initialize post stream if not yet set.
				if (_postData == null)
				{
					_postStream = new MemoryStream();
					_postData = new BinaryWriter(_postStream);
				}

				//The Content-Type header below shouldn't be necessary but because of the way the IGAM server parses a multi-part request we need to have a 
				//Content-Type header appearing in the request stream after the filename header.
				_postData.Write(Encoding.UTF8.GetBytes("--" + MultipartBoundary + "\r\nContent-Disposition: form-data; name=\"" + name + "\" filename=\"" + fileName + "\"" + "\r\nContent-Type: application/octet-stream" + "\r\n\r\n"));
				_postData.Write(fileContents);				
				_postData.Write(Encoding.UTF8.GetBytes("\r\n"));
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while adding a file to an http request.", ex);
			}
		}
		public void AddFile(string name, string fileName, string fileData)
		{
			AddFile(name, fileName, Encoding.UTF8.GetBytes(fileData));
		}

		/// <summary>
		/// Add a file to the request.
		/// </summary>
		/// <param name="name">
		/// The name of the file variable.
		/// </param>
		/// <param name="fileName">
		/// The name of the file.
		/// </param>
		/// <param name="fileStream">
		/// The file contents as a stream.
		/// </param>
		public void AddFile(string name, string fileName, Stream fileStream)
		{
			try
			{
				//Initialize post stream if not yet set.
				if (_postData == null)
				{
					_postStream = new MemoryStream();
					_postData = new BinaryWriter(_postStream);
				}

				//The Content-Type header below shouldn't be necessary but because of the way the IGAM server parses a multi-part request we need to have a 
				//Content-Type header appearing in the request stream after the filename header.
				_postData.Write(Encoding.UTF8.GetBytes("--" + MultipartBoundary + "\r\nContent-Disposition: form-data; name=\"" + name + "\" filename=\"" + fileName + "\"" + "\r\nContent-Type: application/octet-stream" + "\r\n\r\n"));

				byte[] buffer = new byte[8096];
				int num;

				//Chunk the unzipped response into a memory stream which we can then convert to a string.
				while ((num = fileStream.Read(buffer, 0, 8096)) > 0)
					_postData.Write(buffer, 0, num);

				_postData.Write(Encoding.UTF8.GetBytes("\r\n"));
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while adding a file to an http request.", ex);
			}
		}

		/// <summary>
		/// Send the web request and receive the response.
		/// </summary>
		public void Send()
		{
			try
			{
				if (_webRequest == null)
					throw new SystemException("No HttpWebRequest is associated with this MeWebRequest object.");

				_webRequest.Method = "POST";

				//Send any post data to request stream.
				if (_postData != null)
				{
					_webRequest.ContentType = "multipart/form-data; boundary=" + MultipartBoundary;
					_postData.Write(Encoding.UTF8.GetBytes("--" + MultipartBoundary + "\r\n"));

					Stream requestStream = _webRequest.GetRequestStream();
					_postStream.WriteTo(requestStream);

					_postStream.Close();
					_postStream = null;

					_postData.Close();
					_postData = null;

					requestStream.Close();
				}

				_webResponse = (HttpWebResponse)_webRequest.GetResponse();
			}
			catch (System.Exception ex)
			{
				throw new SystemException(string.Format("Error occurred while sending an HTTP request to {0}.",
					_webRequest== null ? string.Empty : _webRequest.RequestUri.AbsoluteUri), ex);
			}
		}

		/// <summary>
		/// Get the response as a string.
		/// </summary>
		/// <returns>
		/// The response as a string.
		/// </returns>
		public string GetResponseText()
		{
			try
			{
				if (_webRequest == null)
					return null;

				Stream stream = _webResponse.GetResponseStream();
				if (stream == null)
					return null;

				StreamReader sr = new StreamReader(stream, Encoding.UTF8);
				return sr.ReadToEnd();
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while getting the http response text.", ex);
			}
		}

		/// <summary>
		/// Get the response as a stream.
		/// </summary>
		/// <returns>
		/// The response as a stream.
		/// </returns>
		public Stream GetResponseStream()
		{
			try
			{
				return _webResponse.GetResponseStream();
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while getting the http response stream.", ex);
			}
		}

		/// <summary>
		/// Get the response as an xml document.
		/// </summary>
		/// <returns>
		/// The response as an xml document.
		/// </returns>
		public XmlDocument GetResponseXml()
		{
			string text = GetResponseText();
			XmlDocument xml = new XmlDocument();

			try
			{
				xml.LoadXml(text);
			}
			catch (System.Exception ex)
			{
				throw new SystemException("Error occurred while getting the http response. The response is not valid xml.", ex);
			}

			return xml;
		}

		#endregion Methods

		#region IDisposable Implementation

		private bool _isDisposed;
   
		/// <summary>
		/// Since this class uses classes which use unmanaged resources, we implement the finalize/dispose pattern to dispose of these resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed) 
				return;
			
			if (_webResponse != null) 
			{
				_webResponse.Close();
				_webResponse = null;
			}

			_webRequest = null;

			_isDisposed = true;
		}
   
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~HttpComm()
		{
			Dispose(false);
		}

		#endregion IDisposable Implementation
	}
}
