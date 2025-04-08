using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

public static class GpuDriver
{
	[DllImport("GpuDriver.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "get_gpu_driver")]
	private static extern IntPtr GetDriverVersion([MarshalAs(UnmanagedType.LPWStr)] string name);

	[DllImport("GpuDriver.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "free_driver")]
	private static extern void Free(IntPtr version);

	public static event Action<string> DriverLoaded;

	public static string DriverVersion
	{
		get
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return SystemInfo.graphicsDeviceVersion;
			}
			object dataLock = GpuDriver._dataLock;
			string text;
			lock (dataLock)
			{
				if (!string.IsNullOrWhiteSpace(GpuDriver._driverVersion))
				{
					text = GpuDriver._driverVersion;
				}
				else
				{
					try
					{
						string gpuName = SystemInfo.graphicsDeviceName;
						Thread thread = new Thread(delegate
						{
							object dataLock2 = GpuDriver._dataLock;
							lock (dataLock2)
							{
								IntPtr intPtr = GpuDriver.GetDriverVersion(gpuName);
								if (intPtr == IntPtr.Zero)
								{
									Debug.LogWarning("GPU Driver version for " + gpuName + " not found!");
									intPtr = GpuDriver.GetDriverVersion(null);
								}
								GpuDriver._driverVersion = Marshal.PtrToStringUni(intPtr) ?? "Loading failed";
								GpuDriver.Free(intPtr);
								Debug.Log("GPU Driver version: " + GpuDriver._driverVersion);
								MainThreadDispatcher.Dispatch(delegate
								{
									Action<string> driverLoaded = GpuDriver.DriverLoaded;
									if (driverLoaded == null)
									{
										return;
									}
									driverLoaded(GpuDriver._driverVersion);
								}, MainThreadDispatcher.DispatchTime.Update);
							}
						});
						thread.IsBackground = true;
						thread.SetApartmentState(ApartmentState.MTA);
						thread.Start();
						text = "Loading...";
					}
					catch (Exception ex)
					{
						Debug.Log(ex);
						text = SystemInfo.graphicsDeviceVersion;
					}
				}
			}
			return text;
		}
	}

	private const string Library = "GpuDriver.dll";

	private const CallingConvention CallingConv = CallingConvention.StdCall;

	private static string _driverVersion;

	private static readonly object _dataLock = new object();
}
