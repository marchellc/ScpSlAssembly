using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace _Scripts.Utils;

public static class StartExternalProcess
{
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
		NONE = 0u,
		CREATE_BREAKAWAY_FROM_JOB = 0x1000000u,
		CREATE_DEFAULT_ERROR_MODE = 0x4000000u,
		CREATE_NEW_CONSOLE = 0x10u,
		CREATE_NEW_PROCESS_GROUP = 0x200u,
		CREATE_NO_WINDOW = 0x8000000u,
		CREATE_PROTECTED_PROCESS = 0x40000u,
		CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x2000000u,
		CREATE_SECURE_PROCESS = 0x400000u,
		CREATE_SEPARATE_WOW_VDM = 0x800u,
		CREATE_SHARED_WOW_VDM = 0x1000u,
		CREATE_SUSPENDED = 4u,
		CREATE_UNICODE_ENVIRONMENT = 0x400u,
		DEBUG_ONLY_THIS_PROCESS = 2u,
		DEBUG_PROCESS = 1u,
		DETACHED_PROCESS = 8u,
		EXTENDED_STARTUPINFO_PRESENT = 0x80000u,
		INHERIT_PARENT_AFFINITY = 0x10000u
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool CreateProcessW(string lpApplicationName, [In] string lpCommandLine, IntPtr procSecAttrs, IntPtr threadSecAttrs, bool bInheritHandles, ProcessCreationFlags dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, ref PROCESS_INFORMATION lpProcessInformation);

	public static uint Start(string path, string dir, bool hidden = false)
	{
		ProcessCreationFlags dwCreationFlags = (hidden ? ProcessCreationFlags.CREATE_NO_WINDOW : ProcessCreationFlags.NONE);
		STARTUPINFO sTARTUPINFO = default(STARTUPINFO);
		sTARTUPINFO.cb = (uint)Marshal.SizeOf<STARTUPINFO>();
		STARTUPINFO lpStartupInfo = sTARTUPINFO;
		PROCESS_INFORMATION lpProcessInformation = default(PROCESS_INFORMATION);
		if (!CreateProcessW(null, path, IntPtr.Zero, IntPtr.Zero, bInheritHandles: false, dwCreationFlags, IntPtr.Zero, dir, ref lpStartupInfo, ref lpProcessInformation))
		{
			throw new Win32Exception();
		}
		return lpProcessInformation.dwProcessId;
	}
}
