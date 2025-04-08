using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace _Scripts.Utils
{
	public static class StartExternalProcess
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool CreateProcessW(string lpApplicationName, [In] string lpCommandLine, IntPtr procSecAttrs, IntPtr threadSecAttrs, bool bInheritHandles, StartExternalProcess.ProcessCreationFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref StartExternalProcess.STARTUPINFO lpStartupInfo, ref StartExternalProcess.PROCESS_INFORMATION lpProcessInformation);

		public static uint Start(string path, string dir, bool hidden = false)
		{
			StartExternalProcess.ProcessCreationFlags processCreationFlags = (hidden ? StartExternalProcess.ProcessCreationFlags.CREATE_NO_WINDOW : StartExternalProcess.ProcessCreationFlags.NONE);
			StartExternalProcess.STARTUPINFO startupinfo = new StartExternalProcess.STARTUPINFO
			{
				cb = (uint)Marshal.SizeOf<StartExternalProcess.STARTUPINFO>()
			};
			StartExternalProcess.PROCESS_INFORMATION process_INFORMATION = default(StartExternalProcess.PROCESS_INFORMATION);
			if (!StartExternalProcess.CreateProcessW(null, path, IntPtr.Zero, IntPtr.Zero, false, processCreationFlags, IntPtr.Zero, dir, ref startupinfo, ref process_INFORMATION))
			{
				throw new Win32Exception();
			}
			return process_INFORMATION.dwProcessId;
		}

		private struct PROCESS_INFORMATION
		{
			internal IntPtr hProcess;

			internal IntPtr hThread;

			internal uint dwProcessId;

			internal uint dwThreadId;
		}

		private struct STARTUPINFO
		{
			internal uint cb;

			internal IntPtr lpReserved;

			internal IntPtr lpDesktop;

			internal IntPtr lpTitle;

			internal uint dwX;

			internal uint dwY;

			internal uint dwXSize;

			internal uint dwYSize;

			internal uint dwXCountChars;

			internal uint dwYCountChars;

			internal uint dwFillAttribute;

			internal uint dwFlags;

			internal ushort wShowWindow;

			internal ushort cbReserved2;

			internal IntPtr lpReserved2;

			internal IntPtr hStdInput;

			internal IntPtr hStdOutput;

			internal IntPtr hStdError;
		}

		[Flags]
		private enum ProcessCreationFlags : uint
		{
			NONE = 0U,
			CREATE_BREAKAWAY_FROM_JOB = 16777216U,
			CREATE_DEFAULT_ERROR_MODE = 67108864U,
			CREATE_NEW_CONSOLE = 16U,
			CREATE_NEW_PROCESS_GROUP = 512U,
			CREATE_NO_WINDOW = 134217728U,
			CREATE_PROTECTED_PROCESS = 262144U,
			CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 33554432U,
			CREATE_SECURE_PROCESS = 4194304U,
			CREATE_SEPARATE_WOW_VDM = 2048U,
			CREATE_SHARED_WOW_VDM = 4096U,
			CREATE_SUSPENDED = 4U,
			CREATE_UNICODE_ENVIRONMENT = 1024U,
			DEBUG_ONLY_THIS_PROCESS = 2U,
			DEBUG_PROCESS = 1U,
			DETACHED_PROCESS = 8U,
			EXTENDED_STARTUPINFO_PRESENT = 524288U,
			INHERIT_PARENT_AFFINITY = 65536U
		}
	}
}
