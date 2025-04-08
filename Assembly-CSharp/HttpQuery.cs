using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using GameCore;
using NorthwoodLib.Pools;
using UnityEngine;
using UnityEngine.Networking;

public static class HttpQuery
{
	static HttpQuery()
	{
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-lockhttpmode", StringComparison.OrdinalIgnoreCase)) || File.Exists(FileManager.GetAppFolder(true, false, "") + "LockHttpMode.txt"))
		{
			HttpQuery.LockHttpMode = true;
			global::GameCore.Console.AddLog("HTTP mode locked", Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
		}
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-httpproxy", StringComparison.OrdinalIgnoreCase)) || File.Exists(FileManager.GetAppFolder(true, false, "") + "HttpProxy.txt"))
		{
			HttpQuery.HttpMode = HttpQueryMode.HttpProxy;
			global::GameCore.Console.AddLog("HTTP mode switched to HttpProxy (startup argument)", Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
		}
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-unitywebrequest", StringComparison.OrdinalIgnoreCase)) || File.Exists(FileManager.GetAppFolder(true, false, "") + "UnityWebRequest.txt"))
		{
			HttpQuery.HttpMode = HttpQueryMode.UnityWebRequest;
			global::GameCore.Console.AddLog("HTTP mode switched to UnityWebRequest (startup argument)", Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
		}
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-unitywebrequestdispatcher", StringComparison.OrdinalIgnoreCase)) || File.Exists(FileManager.GetAppFolder(true, false, "") + "UnityWebRequestDispatcher.txt"))
		{
			HttpQuery.HttpMode = HttpQueryMode.UnityWebRequestDispatcher;
			global::GameCore.Console.AddLog("HTTP mode switched to UnityWebRequestDispatcher (startup argument)", Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
		}
	}

	public static string Get(string url)
	{
		bool flag;
		HttpStatusCode httpStatusCode;
		string text = HttpQuery.Get(url, out flag, out httpStatusCode);
		if (!flag)
		{
			throw new Exception("Error " + httpStatusCode.ToString() + ".\n" + text);
		}
		return text;
	}

	public static string Get(string url, out bool success)
	{
		HttpStatusCode httpStatusCode;
		return HttpQuery.Get(url, out success, out httpStatusCode);
	}

	public static string Get(string url, out bool success, out HttpStatusCode code)
	{
		HashSet<HttpQueryMode> hashSet = HashSetPool<HttpQueryMode>.Shared.Rent();
		HttpQueryMode httpQueryMode = HttpQuery.HttpMode;
		try
		{
			for (;;)
			{
				switch (httpQueryMode)
				{
				case HttpQueryMode.HttpClient:
					if (!(PlatformInfo.singleton == null) && !PlatformInfo.singleton.IsMainThread)
					{
						try
						{
							HttpResponseMessage result = HttpQuery._client.GetAsync(url).GetAwaiter().GetResult();
							code = result.StatusCode;
							success = result.IsSuccessStatusCode;
							return result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
						}
						catch (Exception ex)
						{
							if (!hashSet.Add(HttpQueryMode.HttpClient) || HttpQuery.LockHttpMode)
							{
								throw;
							}
							httpQueryMode = HttpQueryMode.HttpProxy;
							global::GameCore.Console.AddLog("Switched to HttpProxy (exception \"" + ex.Message + "\" occured) [GET Request].", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
							continue;
						}
						goto IL_00E6;
					}
					if (!hashSet.Add(HttpQueryMode.HttpClient) || HttpQuery.LockHttpMode)
					{
						goto IL_0055;
					}
					httpQueryMode = HttpQueryMode.HttpProxy;
					continue;
				case HttpQueryMode.HttpProxy:
					goto IL_00E6;
				case HttpQueryMode.UnityWebRequest:
					goto IL_0157;
				case HttpQueryMode.UnityWebRequestDispatcher:
					goto IL_0246;
				}
				break;
				IL_00E6:
				if (!HttpWorkaround.Enabled)
				{
					if (!hashSet.Add(HttpQueryMode.HttpProxy) || HttpQuery.LockHttpMode)
					{
						goto IL_00FD;
					}
					httpQueryMode = HttpQueryMode.UnityWebRequest;
					continue;
				}
				else
				{
					try
					{
						return HttpWorkaround.Get(url, out success, out code);
					}
					catch (Exception ex2)
					{
						if (!hashSet.Add(HttpQueryMode.HttpProxy) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.UnityWebRequest;
						global::GameCore.Console.AddLog("Switched to UnityWebRequest (exception \"" + ex2.Message + "\" occured) [GET Request].", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
						continue;
					}
				}
				IL_0157:
				if (PlatformInfo.singleton == null || !PlatformInfo.singleton.IsMainThread)
				{
					if (!hashSet.Add(HttpQueryMode.UnityWebRequest) || HttpQuery.LockHttpMode)
					{
						goto IL_0180;
					}
					httpQueryMode = HttpQueryMode.UnityWebRequestDispatcher;
					continue;
				}
				else
				{
					try
					{
						using (UnityWebRequest unityWebRequest = UnityWebRequest.Get(url))
						{
							UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = unityWebRequest.SendWebRequest();
							while (!unityWebRequestAsyncOperation.isDone)
							{
								Thread.Sleep(12);
							}
							UnityWebRequest.Result result2 = unityWebRequest.result;
							if (result2 - UnityWebRequest.Result.ConnectionError <= 1)
							{
								success = false;
							}
							else
							{
								success = true;
							}
							code = (HttpStatusCode)unityWebRequest.responseCode;
							return string.IsNullOrEmpty(unityWebRequest.error) ? unityWebRequest.downloadHandler.text : unityWebRequest.error;
						}
					}
					catch (Exception ex3)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequest) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.UnityWebRequestDispatcher;
						global::GameCore.Console.AddLog("Switched to UnityWebRequestDispatcher (exception \"" + ex3.Message + "\" occured) [GET Request].", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
						continue;
					}
				}
				IL_0246:
				if (!(PlatformInfo.singleton == null) && !PlatformInfo.singleton.IsMainThread)
				{
					try
					{
						UnityWebRequestDispatcher.GetRequest getRequest = UnityWebRequestDispatcher.Get(url);
						while (!getRequest.Done)
						{
							Thread.Sleep(12);
						}
						success = getRequest.Successful;
						code = getRequest.Code;
						return getRequest.Text;
					}
					catch (Exception ex4)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequestDispatcher) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.HttpClient;
						global::GameCore.Console.AddLog("Switched to HttpClient (exception \"" + ex4.Message + "\" occured) [GET Request].", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
						continue;
					}
					break;
				}
				if (!hashSet.Add(HttpQueryMode.UnityWebRequestDispatcher) || HttpQuery.LockHttpMode)
				{
					goto IL_026F;
				}
				httpQueryMode = HttpQueryMode.HttpClient;
			}
			goto IL_02F3;
			IL_0055:
			throw new NotSupportedException();
			IL_00FD:
			throw new NotSupportedException();
			IL_0180:
			throw new NotSupportedException();
			IL_026F:
			throw new NotSupportedException();
			IL_02F3:
			throw new ArgumentOutOfRangeException();
		}
		finally
		{
			HashSetPool<HttpQueryMode>.Shared.Return(hashSet);
		}
		string text;
		return text;
	}

	public static string Post(string url, string data)
	{
		bool flag;
		HttpStatusCode httpStatusCode;
		string text = HttpQuery.Post(url, data, out flag, out httpStatusCode);
		if (!flag)
		{
			throw new Exception("Error " + httpStatusCode.ToString() + ".\n" + text);
		}
		return text;
	}

	public static string Post(string url, string data, out bool success)
	{
		HttpStatusCode httpStatusCode;
		return HttpQuery.Post(url, data, out success, out httpStatusCode);
	}

	public static string Post(string url, string data, out bool success, out HttpStatusCode code)
	{
		HashSet<HttpQueryMode> hashSet = HashSetPool<HttpQueryMode>.Shared.Rent();
		HttpQueryMode httpQueryMode = HttpQuery.HttpMode;
		try
		{
			for (;;)
			{
				switch (httpQueryMode)
				{
				case HttpQueryMode.HttpClient:
					if (!(PlatformInfo.singleton == null) && !PlatformInfo.singleton.IsMainThread)
					{
						try
						{
							HttpResponseMessage result = HttpQuery._client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded")).GetAwaiter().GetResult();
							code = result.StatusCode;
							success = result.IsSuccessStatusCode;
							return result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
						}
						catch (Exception ex)
						{
							if (!hashSet.Add(HttpQueryMode.HttpClient) || HttpQuery.LockHttpMode)
							{
								throw;
							}
							httpQueryMode = HttpQueryMode.HttpProxy;
							global::GameCore.Console.AddLog("Switched to HttpProxy (exception \"" + ex.Message + "\" occured) [POST Request].", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
							continue;
						}
						goto IL_00F6;
					}
					if (!hashSet.Add(HttpQueryMode.HttpClient) || HttpQuery.LockHttpMode)
					{
						goto IL_0055;
					}
					httpQueryMode = HttpQueryMode.HttpProxy;
					continue;
				case HttpQueryMode.HttpProxy:
					goto IL_00F6;
				case HttpQueryMode.UnityWebRequest:
					goto IL_0168;
				case HttpQueryMode.UnityWebRequestDispatcher:
					goto IL_025D;
				}
				break;
				IL_00F6:
				if (!HttpWorkaround.Enabled)
				{
					if (!hashSet.Add(HttpQueryMode.HttpProxy) || HttpQuery.LockHttpMode)
					{
						goto IL_010D;
					}
					httpQueryMode = HttpQueryMode.UnityWebRequest;
					continue;
				}
				else
				{
					try
					{
						return HttpWorkaround.Post(url, data, out success, out code);
					}
					catch (Exception ex2)
					{
						if (!hashSet.Add(HttpQueryMode.HttpProxy) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.UnityWebRequest;
						global::GameCore.Console.AddLog("Switched to UnityWebRequest (exception \"" + ex2.Message + "\" occured) [POST Request].", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
						continue;
					}
				}
				IL_0168:
				if (PlatformInfo.singleton == null || !PlatformInfo.singleton.IsMainThread)
				{
					if (!hashSet.Add(HttpQueryMode.UnityWebRequest) || HttpQuery.LockHttpMode)
					{
						goto IL_0191;
					}
					httpQueryMode = HttpQueryMode.UnityWebRequestDispatcher;
					continue;
				}
				else
				{
					try
					{
						using (UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, HttpQuery.ToUnityForm(data)))
						{
							UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = unityWebRequest.SendWebRequest();
							while (!unityWebRequestAsyncOperation.isDone)
							{
								Thread.Sleep(12);
							}
							UnityWebRequest.Result result2 = unityWebRequest.result;
							if (result2 - UnityWebRequest.Result.ConnectionError <= 1)
							{
								success = false;
							}
							else
							{
								success = true;
							}
							code = (HttpStatusCode)unityWebRequest.responseCode;
							return string.IsNullOrEmpty(unityWebRequest.error) ? unityWebRequest.downloadHandler.text : unityWebRequest.error;
						}
					}
					catch (Exception ex3)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequest) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.UnityWebRequestDispatcher;
						global::GameCore.Console.AddLog("Switched to UnityWebRequestDispatcher (exception \"" + ex3.Message + "\" occured) [POST Request].", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
						continue;
					}
				}
				IL_025D:
				if (!(PlatformInfo.singleton == null) && !PlatformInfo.singleton.IsMainThread)
				{
					try
					{
						UnityWebRequestDispatcher.PostRequest postRequest = UnityWebRequestDispatcher.Post(url, data);
						while (!postRequest.Done)
						{
							Thread.Sleep(12);
						}
						success = postRequest.Successful;
						code = postRequest.Code;
						return postRequest.Text;
					}
					catch (Exception ex4)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequestDispatcher) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.HttpClient;
						global::GameCore.Console.AddLog("Switched to HttpClient (exception \"" + ex4.Message + "\" occured) [POST Request].", Color.yellow, false, global::GameCore.Console.ConsoleLogType.Log);
						continue;
					}
					break;
				}
				if (!hashSet.Add(HttpQueryMode.UnityWebRequestDispatcher) || HttpQuery.LockHttpMode)
				{
					goto IL_0286;
				}
				httpQueryMode = HttpQueryMode.HttpClient;
			}
			goto IL_030B;
			IL_0055:
			throw new NotSupportedException();
			IL_010D:
			throw new NotSupportedException();
			IL_0191:
			throw new NotSupportedException();
			IL_0286:
			throw new NotSupportedException();
			IL_030B:
			throw new ArgumentOutOfRangeException();
		}
		finally
		{
			HashSetPool<HttpQueryMode>.Shared.Return(hashSet);
		}
		string text;
		return text;
	}

	public static string ToPostArgs(IEnumerable<string> data)
	{
		return data.Aggregate((string current, string a) => current + "&" + a.Replace("&", "[AMP]")).TrimStart('&');
	}

	public static WWWForm ToUnityForm(string data)
	{
		WWWForm wwwform = new WWWForm();
		foreach (string text in data.Split('&', StringSplitOptions.None))
		{
			if (text.Contains("=", StringComparison.Ordinal))
			{
				string[] array2 = text.Split('=', StringSplitOptions.None);
				wwwform.AddField(array2[0], array2[1]);
			}
			else
			{
				wwwform.AddField(text, string.Empty);
			}
		}
		return wwwform;
	}

	private const int SleepTime = 12;

	private static readonly HttpClient _client = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15.0),
		DefaultRequestHeaders = 
		{
			{ "User-Agent", "SCP SL" },
			{
				"Game-Version",
				global::GameCore.Version.VersionString
			}
		}
	};

	private static readonly bool LockHttpMode;

	private static readonly HttpQueryMode HttpMode;
}
