using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Windows
{
	public class HeadlessConsole
	{
		public void Initialize()
		{
			if (!HeadlessConsole.AttachConsole(4294967295U))
			{
				HeadlessConsole.AllocConsole();
			}
			this.oldOutput = Console.Out;
			try
			{
				Stream stream = new FileStream(HeadlessConsole.GetStdHandle(-11), FileAccess.Write);
				Encoding ascii = Encoding.ASCII;
				Console.SetOut(new StreamWriter(stream, ascii)
				{
					AutoFlush = true
				});
			}
			catch (Exception ex)
			{
				Debug.Log("Couldn't redirect output: " + ex.Message);
			}
		}

		public void Shutdown()
		{
			Console.SetOut(this.oldOutput);
			HeadlessConsole.FreeConsole();
		}

		public void SetTitle(string strName)
		{
			HeadlessConsole.SetConsoleTitle(strName);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AllocConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		[DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		[DllImport("kernel32.dll")]
		private static extern bool SetConsoleTitle(string lpConsoleTitle);

		private TextWriter oldOutput;

		private const int STD_OUTPUT_HANDLE = -11;
	}
}
