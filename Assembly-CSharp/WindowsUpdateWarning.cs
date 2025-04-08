using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using NorthwoodLib;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WindowsUpdateWarning : MonoBehaviour
{
	private void Start()
	{
		this.warning.SetActive(WindowsUpdateWarning.UpdateRequired());
		this.menu.SetActive(SceneManager.GetActiveScene().buildIndex == 3 || !this.warning.activeSelf);
	}

	public static bool UpdateRequired()
	{
		bool flag;
		try
		{
			flag = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows && global::NorthwoodLib.OperatingSystem.Version.Major < 10 && !File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.System) + Path.DirectorySeparatorChar.ToString() + "API-MS-WIN-CRT-MATH-L1-1-0.dll") && !WindowsUpdateWarning.CheckDll("API-MS-WIN-CRT-MATH-L1-1-0.dll");
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			flag = true;
		}
		return flag;
	}

	private static bool CheckDll(string name)
	{
		IntPtr intPtr = WindowsUpdateWarning.LoadLibrary(name);
		if (intPtr == IntPtr.Zero)
		{
			throw new Win32Exception();
		}
		if (!WindowsUpdateWarning.FreeLibrary(intPtr))
		{
			throw new Win32Exception();
		}
		return true;
	}

	[DllImport("Kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryW", SetLastError = true)]
	private static extern IntPtr LoadLibrary(string name);

	[DllImport("Kernel32.dll", SetLastError = true)]
	private static extern bool FreeLibrary(IntPtr library);

	public GameObject warning;

	public GameObject menu;
}
