using System;
using System.IO;
using System.Text;

namespace Reflix.Worker.Utility
{
	/// <summary>
	/// Summary description for Log.
	/// </summary>
	public class Log
	{
		string sFile;
		bool bOpen;
		bool bTimeStamp;
		bool bConsole = false;
		bool bEnabled = true;
		bool bDebug = false;
		StreamWriter writer;

		public bool Enabled
		{
			get { return bEnabled; }
			set { bEnabled = value; }
		}

		public bool Debug
		{
			get { return bDebug; }
			set { bDebug = value; }
		}

		public bool ConsoleOutput
		{
			get { return bConsole; }
			set { bConsole = value; }
		}

		public Log()
		{
			bEnabled = true;
			bOpen = false;
			bTimeStamp = false;
			bConsole = false;
		}

		public void Write(string sText)
		{
			if(!bEnabled) return;

			if(!bOpen)
				throw(new InvalidOperationException("Log file has not been opened.")); 

			if(!bTimeStamp)
			{
				writer.Write(System.DateTime.Now);
				writer.Write(" ");
				bTimeStamp = true;

				if(bConsole)
				{
					Console.Write(System.DateTime.Now);
					Console.Write(" ");
				}
			}

			writer.Write(sText);
			writer.Flush();
			if(bConsole) Console.Write(sText);
		}

		public void WriteLine(string sText)
		{
			Write(sText);
			writer.WriteLine();
			writer.Flush();
			if(bConsole) Console.WriteLine();
			bTimeStamp = false;
		}

		public void WriteLine(string format, params string[] args)
		{
			WriteLine(string.Format(format, args));
		}

		public void WriteDebug(string sText)
		{
			if(!bDebug) return;

			WriteLine(sText);
		}

		public void WriteDebug(string format, params string[] args)
		{
			if(!bDebug) return;

			WriteLine(format, args);
		}

		public void Open(string sFileName)
		{
			if(!bEnabled) return;

			if(bOpen)
				throw(new InvalidOperationException("Log file already opened.")); 

			sFile = sFileName;

			FileInfo fi = new FileInfo(sFile);
			writer = fi.AppendText();
			bOpen = true;
		}

		public void Close()
		{
			if(!bEnabled) return;

			writer.Flush();
			writer.Close();
			bOpen = false;
		}
	}
}
