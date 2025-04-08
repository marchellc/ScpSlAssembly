using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class HeadlessCallbacks : Attribute
{
	public static void FindCallbacks()
	{
		if (HeadlessCallbacks.callbackRegistry != null)
		{
			return;
		}
		try
		{
			HeadlessCallbacks.callbackRegistry = from t in Assembly.GetExecutingAssembly().GetTypes()
				let attributes = t.GetCustomAttributes(typeof(HeadlessCallbacks), true)
				where attributes != null && attributes.Length != 0
				select t;
		}
		catch (ReflectionTypeLoadException ex)
		{
			try
			{
				HeadlessCallbacks.callbackRegistry = ex.Types.Where((Type t) => t != null);
			}
			catch (Exception ex2)
			{
				Debug.Log("Headless Builder could not find callbacks (" + ex2.GetType().Name + "), but will still continue as planned");
				HeadlessCallbacks.callbackRegistry = Enumerable.Empty<Type>();
			}
		}
		catch (Exception ex3)
		{
			Debug.Log("Headless Builder could not find callbacks (" + ex3.GetType().Name + "), but will still continue as planned");
			HeadlessCallbacks.callbackRegistry = Enumerable.Empty<Type>();
		}
	}

	public static void InvokeCallbacks(string callbackName)
	{
		HeadlessCallbacks.FindCallbacks();
		foreach (object obj in HeadlessCallbacks.callbackRegistry)
		{
			Type type = (Type)obj;
			MethodInfo method = type.GetMethod(callbackName);
			if (method != null)
			{
				try
				{
					method.Invoke(type, null);
				}
				catch (Exception ex)
				{
					Debug.LogError(ex);
				}
			}
		}
	}

	private static IEnumerable callbackRegistry;
}
