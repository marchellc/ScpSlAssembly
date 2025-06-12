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
	private const int SleepTime = 12;

	private static readonly HttpClient _client;

	private static readonly bool LockHttpMode;

	private static readonly HttpQueryMode HttpMode;

	static HttpQuery()
	{
		HttpQuery._client = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15.0),
			DefaultRequestHeaders = 
			{
				{ "User-Agent", "SCP SL" },
				{
					"Game-Version",
					GameCore.Version.VersionString
				}
			}
		};
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-lockhttpmode", StringComparison.OrdinalIgnoreCase)) || File.Exists(FileManager.GetAppFolder() + "LockHttpMode.txt"))
		{
			HttpQuery.LockHttpMode = true;
			GameCore.Console.AddLog("HTTP mode locked", Color.gray);
		}
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-httpproxy", StringComparison.OrdinalIgnoreCase)) || File.Exists(FileManager.GetAppFolder() + "HttpProxy.txt"))
		{
			HttpQuery.HttpMode = HttpQueryMode.HttpProxy;
			GameCore.Console.AddLog("HTTP mode switched to HttpProxy (startup argument)", Color.gray);
		}
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-unitywebrequest", StringComparison.OrdinalIgnoreCase)) || File.Exists(FileManager.GetAppFolder() + "UnityWebRequest.txt"))
		{
			HttpQuery.HttpMode = HttpQueryMode.UnityWebRequest;
			GameCore.Console.AddLog("HTTP mode switched to UnityWebRequest (startup argument)", Color.gray);
		}
		if (StartupArgs.Args.Any((string arg) => string.Equals(arg, "-unitywebrequestdispatcher", StringComparison.OrdinalIgnoreCase)) || File.Exists(FileManager.GetAppFolder() + "UnityWebRequestDispatcher.txt"))
		{
			HttpQuery.HttpMode = HttpQueryMode.UnityWebRequestDispatcher;
			GameCore.Console.AddLog("HTTP mode switched to UnityWebRequestDispatcher (startup argument)", Color.gray);
		}
	}

	public static string Get(string url)
	{
		bool success;
		HttpStatusCode code;
		string text = HttpQuery.Get(url, out success, out code);
		if (!success)
		{
			throw new Exception("Error " + code.ToString() + ".\n" + text);
		}
		return text;
	}

	public static string Get(string url, out bool success)
	{
		HttpStatusCode code;
		return HttpQuery.Get(url, out success, out code);
	}

	public static string Get(string url, out bool success, out HttpStatusCode code)
	{
		HashSet<HttpQueryMode> hashSet = HashSetPool<HttpQueryMode>.Shared.Rent();
		HttpQueryMode httpQueryMode = HttpQuery.HttpMode;
		try
		{
			while (true)
			{
				switch (httpQueryMode)
				{
				case HttpQueryMode.HttpClient:
					if (PlatformInfo.singleton == null || PlatformInfo.singleton.IsMainThread)
					{
						if (!hashSet.Add(HttpQueryMode.HttpClient) || HttpQuery.LockHttpMode)
						{
							throw new NotSupportedException();
						}
						httpQueryMode = HttpQueryMode.HttpProxy;
						break;
					}
					try
					{
						HttpResponseMessage result = HttpQuery._client.GetAsync(url).GetAwaiter().GetResult();
						code = result.StatusCode;
						success = result.IsSuccessStatusCode;
						return result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
					}
					catch (Exception ex2)
					{
						if (!hashSet.Add(HttpQueryMode.HttpClient) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.HttpProxy;
						GameCore.Console.AddLog("Switched to HttpProxy (exception \"" + ex2.Message + "\" occured) [GET Request].", Color.yellow);
					}
					break;
				case HttpQueryMode.HttpProxy:
					if (!HttpWorkaround.Enabled)
					{
						if (!hashSet.Add(HttpQueryMode.HttpProxy) || HttpQuery.LockHttpMode)
						{
							throw new NotSupportedException();
						}
						httpQueryMode = HttpQueryMode.UnityWebRequest;
						break;
					}
					try
					{
						return HttpWorkaround.Get(url, out success, out code);
					}
					catch (Exception ex4)
					{
						if (!hashSet.Add(HttpQueryMode.HttpProxy) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.UnityWebRequest;
						GameCore.Console.AddLog("Switched to UnityWebRequest (exception \"" + ex4.Message + "\" occured) [GET Request].", Color.yellow);
					}
					break;
				case HttpQueryMode.UnityWebRequest:
					if (PlatformInfo.singleton == null || !PlatformInfo.singleton.IsMainThread)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequest) || HttpQuery.LockHttpMode)
						{
							throw new NotSupportedException();
						}
						httpQueryMode = HttpQueryMode.UnityWebRequestDispatcher;
						break;
					}
					try
					{
						using UnityWebRequest unityWebRequest = UnityWebRequest.Get(url);
						UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = unityWebRequest.SendWebRequest();
						while (!unityWebRequestAsyncOperation.isDone)
						{
							Thread.Sleep(12);
						}
						UnityWebRequest.Result result2 = unityWebRequest.result;
						if ((uint)(result2 - 2) <= 1u)
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
					catch (Exception ex3)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequest) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.UnityWebRequestDispatcher;
						GameCore.Console.AddLog("Switched to UnityWebRequestDispatcher (exception \"" + ex3.Message + "\" occured) [GET Request].", Color.yellow);
					}
					break;
				case HttpQueryMode.UnityWebRequestDispatcher:
					if (PlatformInfo.singleton == null || PlatformInfo.singleton.IsMainThread)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequestDispatcher) || HttpQuery.LockHttpMode)
						{
							throw new NotSupportedException();
						}
						httpQueryMode = HttpQueryMode.HttpClient;
						break;
					}
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
					catch (Exception ex)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequestDispatcher) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.HttpClient;
						GameCore.Console.AddLog("Switched to HttpClient (exception \"" + ex.Message + "\" occured) [GET Request].", Color.yellow);
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
		}
		finally
		{
			HashSetPool<HttpQueryMode>.Shared.Return(hashSet);
		}
	}

	public static string Post(string url, string data)
	{
		bool success;
		HttpStatusCode code;
		string text = HttpQuery.Post(url, data, out success, out code);
		if (!success)
		{
			throw new Exception("Error " + code.ToString() + ".\n" + text);
		}
		return text;
	}

	public static string Post(string url, string data, out bool success)
	{
		HttpStatusCode code;
		return HttpQuery.Post(url, data, out success, out code);
	}

	public static string Post(string url, string data, out bool success, out HttpStatusCode code)
	{
		HashSet<HttpQueryMode> hashSet = HashSetPool<HttpQueryMode>.Shared.Rent();
		HttpQueryMode httpQueryMode = HttpQuery.HttpMode;
		try
		{
			while (true)
			{
				switch (httpQueryMode)
				{
				case HttpQueryMode.HttpClient:
					if (PlatformInfo.singleton == null || PlatformInfo.singleton.IsMainThread)
					{
						if (!hashSet.Add(HttpQueryMode.HttpClient) || HttpQuery.LockHttpMode)
						{
							throw new NotSupportedException();
						}
						httpQueryMode = HttpQueryMode.HttpProxy;
						break;
					}
					try
					{
						HttpResponseMessage result = HttpQuery._client.PostAsync(url, new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded")).GetAwaiter().GetResult();
						code = result.StatusCode;
						success = result.IsSuccessStatusCode;
						return result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
					}
					catch (Exception ex2)
					{
						if (!hashSet.Add(HttpQueryMode.HttpClient) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.HttpProxy;
						GameCore.Console.AddLog("Switched to HttpProxy (exception \"" + ex2.Message + "\" occured) [POST Request].", Color.yellow);
					}
					break;
				case HttpQueryMode.HttpProxy:
					if (!HttpWorkaround.Enabled)
					{
						if (!hashSet.Add(HttpQueryMode.HttpProxy) || HttpQuery.LockHttpMode)
						{
							throw new NotSupportedException();
						}
						httpQueryMode = HttpQueryMode.UnityWebRequest;
						break;
					}
					try
					{
						return HttpWorkaround.Post(url, data, out success, out code);
					}
					catch (Exception ex4)
					{
						if (!hashSet.Add(HttpQueryMode.HttpProxy) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.UnityWebRequest;
						GameCore.Console.AddLog("Switched to UnityWebRequest (exception \"" + ex4.Message + "\" occured) [POST Request].", Color.yellow);
					}
					break;
				case HttpQueryMode.UnityWebRequest:
					if (PlatformInfo.singleton == null || !PlatformInfo.singleton.IsMainThread)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequest) || HttpQuery.LockHttpMode)
						{
							throw new NotSupportedException();
						}
						httpQueryMode = HttpQueryMode.UnityWebRequestDispatcher;
						break;
					}
					try
					{
						using UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, HttpQuery.ToUnityForm(data));
						UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = unityWebRequest.SendWebRequest();
						while (!unityWebRequestAsyncOperation.isDone)
						{
							Thread.Sleep(12);
						}
						UnityWebRequest.Result result2 = unityWebRequest.result;
						if ((uint)(result2 - 2) <= 1u)
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
					catch (Exception ex3)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequest) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.UnityWebRequestDispatcher;
						GameCore.Console.AddLog("Switched to UnityWebRequestDispatcher (exception \"" + ex3.Message + "\" occured) [POST Request].", Color.yellow);
					}
					break;
				case HttpQueryMode.UnityWebRequestDispatcher:
					if (PlatformInfo.singleton == null || PlatformInfo.singleton.IsMainThread)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequestDispatcher) || HttpQuery.LockHttpMode)
						{
							throw new NotSupportedException();
						}
						httpQueryMode = HttpQueryMode.HttpClient;
						break;
					}
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
					catch (Exception ex)
					{
						if (!hashSet.Add(HttpQueryMode.UnityWebRequestDispatcher) || HttpQuery.LockHttpMode)
						{
							throw;
						}
						httpQueryMode = HttpQueryMode.HttpClient;
						GameCore.Console.AddLog("Switched to HttpClient (exception \"" + ex.Message + "\" occured) [POST Request].", Color.yellow);
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
		}
		finally
		{
			HashSetPool<HttpQueryMode>.Shared.Return(hashSet);
		}
	}

	public static string ToPostArgs(IEnumerable<string> data)
	{
		return data.Aggregate((string current, string a) => current + "&" + a.Replace("&", "[AMP]")).TrimStart('&');
	}

	public static WWWForm ToUnityForm(string data)
	{
		WWWForm wWWForm = new WWWForm();
		string[] array = data.Split('&');
		foreach (string text in array)
		{
			if (text.Contains("=", StringComparison.Ordinal))
			{
				string[] array2 = text.Split('=');
				wWWForm.AddField(array2[0], array2[1]);
			}
			else
			{
				wWWForm.AddField(text, string.Empty);
			}
		}
		return wWWForm;
	}
}
