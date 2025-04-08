using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using MEC;
using UnityEngine;
using UnityEngine.Networking;

internal class UnityWebRequestDispatcher : MonoBehaviour
{
	public static UnityWebRequestDispatcher.GetRequest Get(string url)
	{
		UnityWebRequestDispatcher.GetRequest getRequest = new UnityWebRequestDispatcher.GetRequest(url);
		UnityWebRequestDispatcher.GetQueue.Enqueue(getRequest);
		return getRequest;
	}

	public static UnityWebRequestDispatcher.PostRequest Post(string url, string data)
	{
		UnityWebRequestDispatcher.PostRequest postRequest = new UnityWebRequestDispatcher.PostRequest(url, data);
		UnityWebRequestDispatcher.PostQueue.Enqueue(postRequest);
		return postRequest;
	}

	private void Update()
	{
		UnityWebRequestDispatcher.GetRequest getRequest;
		while (UnityWebRequestDispatcher.GetQueue.TryDequeue(out getRequest))
		{
			Timing.RunCoroutine(UnityWebRequestDispatcher.ProcessGet(getRequest));
		}
		UnityWebRequestDispatcher.PostRequest postRequest;
		while (UnityWebRequestDispatcher.PostQueue.TryDequeue(out postRequest))
		{
			Timing.RunCoroutine(UnityWebRequestDispatcher.ProcessPost(postRequest));
		}
	}

	private static IEnumerator<float> ProcessGet(UnityWebRequestDispatcher.GetRequest request)
	{
		using (UnityWebRequest uwr = UnityWebRequest.Get(request.Url))
		{
			yield return Timing.WaitUntilDone(uwr.SendWebRequest());
			UnityWebRequest.Result result = uwr.result;
			if (result - UnityWebRequest.Result.ConnectionError <= 1)
			{
				request.Successful = false;
			}
			else
			{
				request.Successful = true;
			}
			request.Code = (HttpStatusCode)uwr.responseCode;
			request.Text = (string.IsNullOrEmpty(uwr.error) ? uwr.downloadHandler.text : uwr.error);
			request.Done = true;
		}
		UnityWebRequest uwr = null;
		yield break;
		yield break;
	}

	private static IEnumerator<float> ProcessPost(UnityWebRequestDispatcher.PostRequest request)
	{
		using (UnityWebRequest uwr = UnityWebRequest.Post(request.Url, HttpQuery.ToUnityForm(request.Data)))
		{
			yield return Timing.WaitUntilDone(uwr.SendWebRequest());
			UnityWebRequest.Result result = uwr.result;
			if (result - UnityWebRequest.Result.ConnectionError <= 1)
			{
				request.Successful = false;
			}
			else
			{
				request.Successful = true;
			}
			request.Code = (HttpStatusCode)uwr.responseCode;
			request.Text = (string.IsNullOrEmpty(uwr.error) ? uwr.downloadHandler.text : uwr.error);
			request.Done = true;
		}
		UnityWebRequest uwr = null;
		yield break;
		yield break;
	}

	private static readonly ConcurrentQueue<UnityWebRequestDispatcher.GetRequest> GetQueue = new ConcurrentQueue<UnityWebRequestDispatcher.GetRequest>();

	private static readonly ConcurrentQueue<UnityWebRequestDispatcher.PostRequest> PostQueue = new ConcurrentQueue<UnityWebRequestDispatcher.PostRequest>();

	public abstract class Request
	{
		protected Request(string url)
		{
			this.Url = url;
		}

		public readonly string Url;

		public bool Successful;

		public string Text;

		public HttpStatusCode Code;

		public volatile bool Done;
	}

	public class GetRequest : UnityWebRequestDispatcher.Request
	{
		public GetRequest(string url)
			: base(url)
		{
		}
	}

	public class PostRequest : UnityWebRequestDispatcher.Request
	{
		public PostRequest(string url, string data)
			: base(url)
		{
			this.Data = data;
		}

		public readonly string Data;
	}
}
