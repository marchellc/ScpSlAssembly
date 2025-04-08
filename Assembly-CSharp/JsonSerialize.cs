﻿using System;
using System.IO;
using Utf8Json;
using Utf8Json.Resolvers;
using Utf8Json.Unity;

public static class JsonSerialize
{
	static JsonSerialize()
	{
		CompositeResolver.RegisterAndSetAsDefault(new IJsonFormatterResolver[]
		{
			GeneratedResolver.Instance,
			BuiltinResolver.Instance,
			EnumResolver.Default,
			UnityResolver.Instance,
			StandardResolver.Default
		});
	}

	public static string ToJson<T>(T value) where T : IJsonSerializable
	{
		return JsonSerializer.ToJsonString<T>(value);
	}

	public static T FromJson<T>(Stream value) where T : IJsonSerializable
	{
		return JsonSerializer.Deserialize<T>(value);
	}

	public static T FromJson<T>(byte[] value) where T : IJsonSerializable
	{
		return JsonSerializer.Deserialize<T>(value);
	}

	public static T FromJson<T>(byte[] value, int offset) where T : IJsonSerializable
	{
		return JsonSerializer.Deserialize<T>(value, offset);
	}

	public static T FromJson<T>(string value) where T : IJsonSerializable
	{
		return JsonSerializer.Deserialize<T>(value);
	}

	public static T FromFile<T>(string path) where T : IJsonSerializable
	{
		T t;
		using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
		{
			t = JsonSerializer.Deserialize<T>(fileStream);
		}
		return t;
	}
}
