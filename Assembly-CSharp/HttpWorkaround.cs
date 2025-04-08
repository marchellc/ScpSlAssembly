using System;
using System.Net;
using System.Runtime.InteropServices;
using GameCore;
using UnityEngine;

internal static class HttpWorkaround
{
	[DllImport("HttpProxy", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	private static extern bool Initialize(string ptr, out IntPtr message);

	[DllImport("HttpProxy", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	private static extern IntPtr Get(string url, out bool success, out int code, out IntPtr exception);

	[DllImport("HttpProxy", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	private static extern IntPtr Post(string url, string data, out bool success, out int code, out IntPtr exception);

	[DllImport("HttpProxy", CallingConvention = CallingConvention.StdCall)]
	private static extern void Free(IntPtr ptr);

	static HttpWorkaround()
	{
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			HttpWorkaround.Enabled = false;
			return;
		}
		try
		{
			IntPtr intPtr;
			HttpWorkaround.Enabled = HttpWorkaround.Initialize(global::GameCore.Version.VersionString, out intPtr);
			Debug.Log(Marshal.PtrToStringUni(intPtr));
			HttpWorkaround.Free(intPtr);
		}
		catch (Exception ex)
		{
			HttpWorkaround.Enabled = false;
			Debug.LogException(ex);
		}
	}

	internal static string Get(string url, out bool success, out HttpStatusCode code)
	{
		int num;
		IntPtr intPtr2;
		IntPtr intPtr = HttpWorkaround.Get(url, out success, out num, out intPtr2);
		if (intPtr2 != IntPtr.Zero)
		{
			string text = Marshal.PtrToStringUni(intPtr2);
			HttpWorkaround.Free(intPtr2);
			throw new HttpWorkaround.HttpProxyException(text);
		}
		code = (HttpStatusCode)num;
		string text2 = Marshal.PtrToStringUni(intPtr);
		HttpWorkaround.Free(intPtr);
		return text2;
	}

	internal static string Post(string url, string data, out bool success, out HttpStatusCode code)
	{
		int num;
		IntPtr intPtr2;
		IntPtr intPtr = HttpWorkaround.Post(url, data, out success, out num, out intPtr2);
		if (intPtr2 != IntPtr.Zero)
		{
			string text = Marshal.PtrToStringUni(intPtr2);
			HttpWorkaround.Free(intPtr2);
			throw new HttpWorkaround.HttpProxyException(text);
		}
		code = (HttpStatusCode)num;
		string text2 = Marshal.PtrToStringUni(intPtr);
		HttpWorkaround.Free(intPtr);
		return text2;
	}

	public static readonly bool Enabled;

	private const string HttpProxy = "HttpProxy";

	private const CallingConvention Convention = CallingConvention.StdCall;

	private const CharSet Encoding = CharSet.Unicode;

	private class HttpProxyException : Exception
	{
		public HttpProxyException(string message)
			: base(message)
		{
		}
	}
}
