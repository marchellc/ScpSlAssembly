using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class DebugLog
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Log(string text)
	{
		global::UnityEngine.Debug.Log(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Log(object text)
	{
		global::UnityEngine.Debug.Log(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogWarning(string text)
	{
		global::UnityEngine.Debug.LogWarning(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogWarning(object text)
	{
		global::UnityEngine.Debug.LogWarning(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogError(string text)
	{
		global::UnityEngine.Debug.LogError(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogError(object text)
	{
		global::UnityEngine.Debug.LogError(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogException(Exception exception)
	{
		global::UnityEngine.Debug.LogException(exception);
	}

	[Conditional("UNITY_EDITOR")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogEditor(string text)
	{
		global::UnityEngine.Debug.Log(text);
	}

	[Conditional("UNITY_EDITOR")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogEditor(object text)
	{
		global::UnityEngine.Debug.Log(text);
	}

	[Conditional("UNITY_EDITOR")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogWarningEditor(string text)
	{
		global::UnityEngine.Debug.LogWarning(text);
	}

	[Conditional("UNITY_EDITOR")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogWarningEditor(object text)
	{
		global::UnityEngine.Debug.LogWarning(text);
	}

	[Conditional("UNITY_EDITOR")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogErrorEditor(string text)
	{
		global::UnityEngine.Debug.LogError(text);
	}

	[Conditional("UNITY_EDITOR")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogErrorEditor(object text)
	{
		global::UnityEngine.Debug.LogError(text);
	}

	[Conditional("UNITY_EDITOR")]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogExceptionEditor(Exception exception)
	{
		global::UnityEngine.Debug.LogException(exception);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogBuild(string text)
	{
		global::UnityEngine.Debug.Log(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogBuild(object text)
	{
		global::UnityEngine.Debug.Log(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogWarningBuild(string text)
	{
		global::UnityEngine.Debug.LogWarning(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogWarningBuild(object text)
	{
		global::UnityEngine.Debug.LogWarning(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogErrorBuild(string text)
	{
		global::UnityEngine.Debug.LogError(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogErrorBuild(object text)
	{
		global::UnityEngine.Debug.LogError(text);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void LogExceptionBuild(Exception exception)
	{
		global::UnityEngine.Debug.LogException(exception);
	}
}
