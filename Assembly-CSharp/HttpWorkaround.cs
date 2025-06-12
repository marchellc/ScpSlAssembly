using System;
using System.Net;
using System.Runtime.InteropServices;
using GameCore;
using UnityEngine;

internal static class HttpWorkaround
{
	private class HttpProxyException : Exception
	{
		public HttpProxyException(string message)
			: base(message)
		{
		}
	}

	public static readonly bool Enabled;

	private const string HttpProxy = "HttpProxy";

	private const CallingConvention Convention = CallingConvention.StdCall;

	private const CharSet Encoding = CharSet.Unicode;

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
			HttpWorkaround.Enabled = HttpWorkaround.Initialize(GameCore.Version.VersionString, out var message);
			Debug.Log(Marshal.PtrToStringUni(message));
			HttpWorkaround.Free(message);
		}
		catch (Exception exception)
		{
			HttpWorkaround.Enabled = false;
			Debug.LogException(exception);
		}
	}

	internal static string Get(string url, out bool success, out HttpStatusCode code)
	{
		int code2;
		IntPtr exception;
		IntPtr ptr = HttpWorkaround.Get(url, out success, out code2, out exception);
		if (exception != IntPtr.Zero)
		{
			string message = Marshal.PtrToStringUni(exception);
			HttpWorkaround.Free(exception);
			throw new HttpProxyException(message);
		}
		code = (HttpStatusCode)code2;
		string result = Marshal.PtrToStringUni(ptr);
		HttpWorkaround.Free(ptr);
		return result;
	}

	internal static string Post(string url, string data, out bool success, out HttpStatusCode code)
	{
		int code2;
		IntPtr exception;
		IntPtr ptr = HttpWorkaround.Post(url, data, out success, out code2, out exception);
		if (exception != IntPtr.Zero)
		{
			string message = Marshal.PtrToStringUni(exception);
			HttpWorkaround.Free(exception);
			throw new HttpProxyException(message);
		}
		code = (HttpStatusCode)code2;
		string result = Marshal.PtrToStringUni(ptr);
		HttpWorkaround.Free(ptr);
		return result;
	}
}
