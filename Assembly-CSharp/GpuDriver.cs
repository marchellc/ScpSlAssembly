using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public static class GpuDriver
{
	private const string Library = "GpuDriver.dll";

	private const CallingConvention CallingConv = CallingConvention.StdCall;

	private static string _driverVersion;

	private static readonly object _dataLock = new object();

	public static string DriverVersion
	{
		get
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return SystemInfo.graphicsDeviceVersion;
			}
			lock (GpuDriver._dataLock)
			{
				if (!string.IsNullOrWhiteSpace(GpuDriver._driverVersion))
				{
					return GpuDriver._driverVersion;
				}
				try
				{
					string gpuName = SystemInfo.graphicsDeviceName;
					Thread thread = new Thread((ThreadStart)delegate
					{
						lock (GpuDriver._dataLock)
						{
							IntPtr driverVersion = GpuDriver.GetDriverVersion(gpuName);
							if (driverVersion == IntPtr.Zero)
							{
								Debug.LogWarning("GPU Driver version for " + gpuName + " not found!");
								driverVersion = GpuDriver.GetDriverVersion(null);
							}
							GpuDriver._driverVersion = Marshal.PtrToStringUni(driverVersion) ?? "Loading failed";
							GpuDriver.Free(driverVersion);
							Debug.Log("GPU Driver version: " + GpuDriver._driverVersion);
							MainThreadDispatcher.Dispatch(delegate
							{
								GpuDriver.DriverLoaded?.Invoke(GpuDriver._driverVersion);
							});
						}
					});
					thread.IsBackground = true;
					thread.SetApartmentState(ApartmentState.MTA);
					thread.Start();
					return "Loading...";
				}
				catch (Exception message)
				{
					Debug.Log(message);
					return SystemInfo.graphicsDeviceVersion;
				}
			}
		}
	}

	public static event Action<string> DriverLoaded;

	[DllImport("GpuDriver.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "get_gpu_driver")]
	private static extern IntPtr GetDriverVersion([MarshalAs(UnmanagedType.LPWStr)] string name);

	[DllImport("GpuDriver.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "free_driver")]
	private static extern void Free(IntPtr version);
}
