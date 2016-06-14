using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MicroEdge
{
	/// <summary>
	/// IO class for reading and writing to files on disk
	/// </summary>
	public static class IoTools
	{
		#region Fields
			private static Boolean _waitforexit;   // used to store behavior flag when extending ProcessStartInfo.
			private static Boolean _waitforidle;    // used to store behavior flag when extending ProcessStartInfo.
		#endregion

		#region Enumerations

		/// <summary>
		/// Enumeration for the result of running an external process
		/// </summary>
		public enum ProcessResult
		{
			Fail = 0,
			Success = 1,
			ProcessNotFound = 2
		}

		#endregion

		#region Writing

		/// <summary>
		/// Creates the writer.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		public static StreamWriter CreateWriter(string fileName)
		{
			return CreateWriter(fileName, false);
		}

		/// <summary>
		/// Creates the writer.
		/// </summary>
		public static StreamWriter CreateWriter(string fileName, bool overwriteExisting)
		{
			if (string.IsNullOrEmpty(fileName))
				return null;
			
			return CreateWriter(OpenFile(fileName, overwriteExisting, false));
		}

		/// <summary>
		/// Creates the writer.
		/// </summary>
		public static StreamWriter CreateWriter(Stream streamToWriteTo)
		{
			if (streamToWriteTo == null) 
				return null;

			StreamWriter sw = null;
			try
			{
				sw = new StreamWriter(streamToWriteTo);
				return sw;
			}
			catch (System.Exception)
			{
				if (sw != null)
				{
					sw.Close();
				}
			}

			return null;
		}

		/// <summary>
		/// Creates the writer.
		/// </summary>
		public static BinaryWriter CreateBinaryWriter(Stream streamToWriteTo)
		{
			BinaryWriter bw = null;
			if (streamToWriteTo != null)
			{
				try
				{
					bw = new BinaryWriter(streamToWriteTo);
					return bw;
				}
				catch (System.Exception)
				{
					if (bw != null)
					{
						bw.Close();
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Closes the writer.
		/// </summary>
		public static void CloseWriter(StreamWriter sw)
		{
			if (sw != null)
			{
				try
				{
					sw.Close();
					CloseFile(sw.BaseStream);
				}
				catch
				{

				}
			}
		}

		#endregion

		#region Reading


		/// <summary>
		/// Creates the file reader.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		public static StreamReader CreateReader(string fileName)
		{
			return CreateReader(fileName, false);
		}

		/// <summary>
		/// Creates the file reader.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="overwriteExisting">if set to <c>true</c> [overwrite existing].</param>
		/// <returns></returns>
		public static StreamReader CreateReader(string fileName, bool overwriteExisting)
		{
			if (string.IsNullOrEmpty(fileName))
				return null;

			return CreateReader(OpenFile(fileName, overwriteExisting, true));
		}

		/// <summary>
		/// Creates the reader.
		/// </summary>
		/// <param name="streamToReadFrom">The stream to read from.</param>
		/// <returns></returns>
		public static BinaryReader CreateBinaryReader(Stream streamToReadFrom)
		{
			BinaryReader br = null;
			if (streamToReadFrom != null)
			{
				try
				{
					br = new BinaryReader(streamToReadFrom);
					return br;
				}
				catch (System.Exception)
				{
					if (br != null)
					{
						br.Close();
					}
				}
			}

			return null;
		}


		/// <summary>
		/// Creates the reader
		/// </summary>
		/// <param name="streamToReadFrom">The stream to read from.</param>
		/// <returns></returns>
		public static StreamReader CreateReader(Stream streamToReadFrom)
		{
			return CreateReader(streamToReadFrom, Encoding.UTF8);
		}

		/// <summary>
		/// Creates the reader.
		/// </summary>
		/// <param name="streamToReadFrom">The stream to read from.</param>
		/// <param name="encodingFrom">The Encoding to expect</param>
		/// <returns></returns>
		public static StreamReader CreateReader(Stream streamToReadFrom, Encoding encodingFrom)
		{
			StreamReader sr = null;
			if (streamToReadFrom != null)
			{
				try
				{
					sr = new StreamReader(streamToReadFrom, encodingFrom);
					return sr;
				}
				catch (System.Exception)
				{
					if (sr != null)
					{
						sr.Close();
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Closes the reader.
		/// </summary>
		/// <param name="sr">The sr.</param>
		public static void CloseReader(StreamReader sr)
		{
			if (sr != null)
			{
				try
				{
					sr.Close();
					CloseFile(sr.BaseStream);
				}
				catch
				{

				}
			}
		}

		/// <summary>
		/// Closes the reader.
		/// </summary>
		/// <param name="br">The br.</param>
		public static void CloseBinaryReader(BinaryReader br)
		{
			if (br != null)
			{
				try
				{
					br.Close();
					CloseFile(br.BaseStream);
				}
				catch
				{

				}
			}
		}

		#endregion

		#region Support
		
		/// <summary>
		/// Opens the file.
		/// </summary>
		public static FileStream OpenFile(string fileName, bool overwriteExisting, bool readOnly)
		{
			FileStream fs = null;
			if (!string.IsNullOrEmpty(fileName))
			{
				try
				{
					fs = File.Open(
						fileName, overwriteExisting ? FileMode.Create : FileMode.OpenOrCreate, 
						readOnly ? FileAccess.Read : FileAccess.ReadWrite, 
						FileShare.ReadWrite);
					return fs;
				}
				catch (System.Exception)
				{
					if (fs != null)
					{
						CloseFile(fs);
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Closes the file.
		/// </summary>
		public static void CloseFile(Stream stream)
		{
			try
			{
				if (stream != null)
				{
					stream.Close();
				}
			}
			catch (System.Exception)
			{
			}
		}

		public static bool IsFileUtf8Format(string srcFileName)
		{
			const int chunkSize = 3;

			if (!File.Exists(srcFileName)) 
				return false;

			using (FileStream fs = OpenFile(srcFileName,false,true))
			{
				using (BinaryReader br = CreateBinaryReader(fs))
				{
					byte[] chunk = br.ReadBytes(chunkSize);
					if (chunk.Length != 3) 
						return false;

					string fileContent = Encoding.Default.GetString(chunk);  // use utf8 encoding.
					return (Tools.StringToHex(fileContent) == "EFBBBF");
				}
			}
		}	
	
		#endregion

		#region ExtensionMethods

		/// <summary>
		/// Gets whether a ProcessStartInfo object should be used for a process launch
		/// that will wait for an idle state
		/// </summary>
		public static Boolean GetWaitforIdle(this ProcessStartInfo thisProcess)
		{
			return _waitforidle;
		}

		/// <summary>
		/// Sets whether a ProcessStartInfo object should be used for a process launch
		/// that will wait for an idle state
		/// </summary>
		public static void SetWaitforIdle(this ProcessStartInfo thisProcess, Boolean value)
		{
			_waitforidle = value;
		}

		/// <summary>
		/// Gets whether a ProcessStartInfo object should be used for a process launch
		/// that will wait for exit
		/// </summary>
		/// <param name="thisProcess"></param>
		/// <returns>Boolean value determing if process should 'wait'</returns>
		public static Boolean GetWaitforExit(this ProcessStartInfo thisProcess)
		{
			return _waitforexit;
		}

		/// <summary>
		/// Sets whether a ProcessStartInfo object should be used for a process launch
		/// that will wait for exit
		/// </summary>
		/// <param name="thisProcess"></param>
		/// <param name="value">set value </param>
		public static void SetWaitforExit(this ProcessStartInfo thisProcess, Boolean value)
		{
			_waitforexit = value;
		}

		#endregion
		
		#region ProcessSupport

		public static ProcessResult LaunchProcess(ProcessStartInfo startInfo)
		{
			return LaunchProcess(startInfo, null);
		}

		/// <summary>
		/// Launches the process.  
		/// To launch the process asyncronously, pass in handler.
		/// </summary>
		/// <param name="startInfo"></param>
		/// <param name="exitedHandler"></param>
		/// <returns></returns>
		public static ProcessResult LaunchProcess(ProcessStartInfo startInfo, EventHandler exitedHandler)
		{
			if (string.IsNullOrEmpty(startInfo.FileName))
				throw new ArgumentNullException();

			// shell 'false' in order to launch 'exe' applications without the addition of a IE zone security check.
			// for any other file types, must use shell.  When using shell, 'explorer' is used, which goes through 
			// process of using IEZone Check (which will bring up 'security warnings' if item is located on what 
			// is considered 'internet zone'/file share.)
			if (startInfo.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				startInfo.UseShellExecute = false;

			if (File.Exists(startInfo.FileName))
			{
				Process proc = Process.Start(startInfo);
				if (proc == null)
					return ProcessResult.Fail;

				if (exitedHandler != null)
				{
					proc.EnableRaisingEvents = true;
					proc.Exited += exitedHandler;
				}

				if (startInfo.GetWaitforExit())
				{
					proc.WaitForExit();
					startInfo.SetWaitforExit(false);  // turn off option (since it's held in static scope)
				}
				else if (startInfo.GetWaitforIdle())
				{
					proc.WaitForInputIdle();
					startInfo.SetWaitforIdle(false);  // turn off option (since it's held in static scope)
				}
			}
			else
			{
				return ProcessResult.ProcessNotFound;
			}

			return ProcessResult.Success;

		}

		/// <summary>
		/// Launches the process.
		/// </summary>
		/// <param name="filename">The filename.</param>
		public static ProcessResult LaunchProcess(string filename)
		{
			return LaunchProcess(filename, false, false);
		}

		/// <summary>
		/// Launches the process.
		/// </summary>
		/// <param name="filename">The filename.</param>
		/// <param name="waitForIdle">
		/// whether or not the calling application should wait for the application being launched to go idle before continuing
		/// </param>
		/// <param name="waitForExit">
		/// whether or not the calling application should wait for the application being launched to close before continuing
		/// </param>
		public static ProcessResult LaunchProcess(string filename, bool waitForIdle, bool waitForExit)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = filename;
			
			if (waitForExit)
				startInfo.SetWaitforExit(true);
			
			if (waitForIdle)
				startInfo.SetWaitforIdle(true);

			return LaunchProcess(startInfo, null);
		}

		/// <summary>
		/// Launches the process.
		/// </summary>
		/// <param name="filename">
		/// The filename of the application to launch
		/// </param>
		/// <param name="waitForIdle">
		/// Whether or not we should wait for the application being launched to go idle before returning
		/// </param>
		/// <param name="waitForExit">
		/// Whether or not we should wait for the application being launched to exit before returning
		/// </param>
		/// <param name="args">The args.</param>
		public static ProcessResult LaunchProcess(string filename, Boolean waitForIdle, Boolean waitForExit, params string[] args)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = filename.Trim();
			startInfo.Arguments = string.Join(",", args);
			startInfo.SetWaitforExit(waitForExit);
			startInfo.SetWaitforIdle(waitForIdle);

			return LaunchProcess(startInfo, null);
		}

		/// <summary>
		/// Launches the process.  
		/// To launch the process asyncronously, pass in handler.
		/// </summary>
		/// <param name="filename">
		/// The filename.
		/// </param>
		/// <param name="args">
		/// The args.
		/// </param>
		/// <param name="waitForIdle">
		/// Whether or not we should wait for the process to go idle before returning
		/// </param>
		/// <param name="waitForExit">
		/// Whether or not we should wait for the process to exit before returning
		/// </param>
		/// <param name="exitedHandler">
		/// The asyncronous handler.
		/// </param>
		public static ProcessResult LaunchProcess(string filename, Boolean waitForIdle, Boolean waitForExit, EventHandler exitedHandler, params string[] args)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = filename;
			startInfo.Arguments = string.Join(",", args);

			return LaunchProcess(startInfo, exitedHandler);
		}

		#endregion

		#region .ini Support

		/// <summary>
		/// Gets all property and their attributes from an .ini file
		/// </summary>
		public static Dictionary<string, Dictionary<string, string>> GetAllPropertyAttributes(string fileName)
		{
			StreamReader reader = null;
			try
			{
				reader = CreateReader(fileName);
				return GetAllPropertyAttributes(reader);
			}
			finally
			{
				CloseReader(reader);
			}
		}

		/// <summary>
		/// Gets all property and their attributes from an .ini file
		/// </summary>
		/// <param name="reader">The reader.</param>
		/// <returns></returns>
		public static Dictionary<string, Dictionary<string, string>> GetAllPropertyAttributes(StreamReader reader)
		{
			if (reader == null)
			{
				return null;
			}

			Dictionary<string, Dictionary<string, string>> dictProperties = new Dictionary<string, Dictionary<string, string>>();

			const string propPattern = @"[[](?<Property>.*?)[]]\s*?\r?\n(?<Attributes>(.*?=.*\s*?\r?\n?)+)";
			const string attrPattern = @"(?<AttrName>.*?)=(?<AttrValue>.*?)\s*?\r?\n";

			Regex regexProp = new Regex(propPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
			Regex regexAttr = new Regex(attrPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);

			// read all of ini file
			string contents = reader.ReadToEnd();
			MatchCollection allProperties = regexProp.Matches(contents);

			// get all properties
			foreach (Match prop in allProperties)
			{
				string propertyName = prop.Groups["Property"].Value;
				if (!dictProperties.ContainsKey(propertyName))
				{
					dictProperties.Add(propertyName, new Dictionary<string, string>());
				}

				// get all attributes of current property
				MatchCollection allAttributes = regexAttr.Matches(prop.Groups["Attributes"].Value);
				foreach (Match attr in allAttributes)
				{
					string attrName = attr.Groups["AttrName"].Value;
					string attrValue = attr.Groups["AttrValue"].Value;

					// add the pair to the list
					if (!dictProperties[propertyName].ContainsKey(attrName))
					{
						dictProperties[propertyName].Add(attrName, attrValue);
					}
				}
			}

			return dictProperties;
		}

		#endregion
	}
}
