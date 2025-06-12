using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using MEC;
using UnityEngine;
using UnityEngine.Networking;

internal class UnityWebRequestDispatcher : MonoBehaviour
{
	public abstract class Request
	{
		public readonly string Url;

		public bool Successful;

		public string Text;

		public HttpStatusCode Code;

		public volatile bool Done;

		protected Request(string url)
		{
			this.Url = url;
		}
	}

	public class GetRequest : Request
	{
		public GetRequest(string url)
			: base(url)
		{
		}
	}

	public class PostRequest : Request
	{
		public readonly string Data;

		public PostRequest(string url, string data)
			: base(url)
		{
			this.Data = data;
		}
	}

	private static readonly ConcurrentQueue<GetRequest> GetQueue = new ConcurrentQueue<GetRequest>();

	private static readonly ConcurrentQueue<PostRequest> PostQueue = new ConcurrentQueue<PostRequest>();

	public static GetRequest Get(string url)
	{
		GetRequest getRequest = new GetRequest(url);
		UnityWebRequestDispatcher.GetQueue.Enqueue(getRequest);
		return getRequest;
	}

	public static PostRequest Post(string url, string data)
	{
		PostRequest postRequest = new PostRequest(url, data);
		UnityWebRequestDispatcher.PostQueue.Enqueue(postRequest);
		return postRequest;
	}

	private void Update()
	{
		GetRequest result;
		while (UnityWebRequestDispatcher.GetQueue.TryDequeue(out result))
		{
			Timing.RunCoroutine(UnityWebRequestDispatcher.ProcessGet(result));
		}
		PostRequest result2;
		while (UnityWebRequestDispatcher.PostQueue.TryDequeue(out result2))
		{
			Timing.RunCoroutine(UnityWebRequestDispatcher.ProcessPost(result2));
		}
	}

	private static IEnumerator<float> ProcessGet(GetRequest request)
	{
		using UnityWebRequest uwr = UnityWebRequest.Get(request.Url);
		yield return Timing.WaitUntilDone(uwr.SendWebRequest());
		UnityWebRequest.Result result = uwr.result;
		if ((uint)(result - 2) <= 1u)
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

	private static IEnumerator<float> ProcessPost(PostRequest request)
	{
		using UnityWebRequest uwr = UnityWebRequest.Post(request.Url, HttpQuery.ToUnityForm(request.Data));
		yield return Timing.WaitUntilDone(uwr.SendWebRequest());
		UnityWebRequest.Result result = uwr.result;
		if ((uint)(result - 2) <= 1u)
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
}
