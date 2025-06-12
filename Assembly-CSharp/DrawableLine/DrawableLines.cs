using System;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace DrawableLine;

public static class DrawableLines
{
	private const int RecommendedSegments = 8;

	public static float ClientDefaultDuration { get; set; } = 0.25f;

	public static Color ClientDefaultColor { get; set; } = Color.green;

	public static bool IsDebugModeEnabled { get; set; } = false;

	public static float? DurationOverride { get; set; }

	public static float DefaultDuration
	{
		get
		{
			if (!NetworkServer.active)
			{
				return DrawableLines.ClientDefaultDuration;
			}
			return DrawableLines.ServerDefaultDuration;
		}
	}

	public static Color DefaultColor
	{
		get
		{
			if (!NetworkServer.active)
			{
				return DrawableLines.ClientDefaultColor;
			}
			return DrawableLines.ServerDefaultColor;
		}
	}

	public static float ServerDefaultDuration { get; set; } = 0.3f;

	public static Color ServerDefaultColor { get; set; } = Color.red;

	public static void ClientGenerateLine(params Vector3[] positions)
	{
		DrawableLines.ClientGenerateLine(DrawableLines.ClientDefaultDuration, DrawableLines.ClientDefaultColor, positions);
	}

	public static void ClientGenerateLine(float? duration, params Vector3[] positions)
	{
		DrawableLines.ClientGenerateLine(duration, DrawableLines.ClientDefaultColor, positions);
	}

	public static void ClientGenerateLine(Color? color, params Vector3[] positions)
	{
		DrawableLines.ClientGenerateLine(DrawableLines.ClientDefaultDuration, color, positions);
	}

	public static void ClientGenerateLine(float? duration, Color? color, params Vector3[] positions)
	{
		if (!color.HasValue)
		{
			color = DrawableLines.ClientDefaultColor;
		}
		if (DrawableLines.DurationOverride.HasValue)
		{
			duration = DrawableLines.DurationOverride.Value;
		}
		else if (!duration.HasValue)
		{
			duration = DrawableLines.ClientDefaultDuration;
		}
		for (int i = 0; i < positions.Length - 1; i++)
		{
			Vector3 start = positions[i];
			Vector3 end = positions[i + 1];
			DrawableLinesManager.DrawLine(start, end, color.Value, duration.Value);
		}
	}

	public static void GenerateLine(params Vector3[] positions)
	{
		DrawableLines.GenerateLine(DrawableLines.DefaultDuration, DrawableLines.DefaultColor, positions);
	}

	public static void GenerateLine(Color? color, params Vector3[] positions)
	{
		DrawableLines.GenerateLine(DrawableLines.DefaultDuration, color, positions);
	}

	public static void GenerateLine(float? duration, params Vector3[] positions)
	{
		DrawableLines.GenerateLine(duration, DrawableLines.DefaultColor, positions);
	}

	public static void GenerateLine(float? duration, Color? color, params Vector3[] positions)
	{
		if (DrawableLines.IsDebugModeEnabled)
		{
			if (NetworkServer.active)
			{
				DrawableLines.ServerGenerateLine(duration, color, positions);
			}
			else
			{
				DrawableLines.ClientGenerateLine(duration, color, positions);
			}
		}
	}

	public static void GenerateSphere(Vector3 origin, float radius, int segments = 8)
	{
		DrawableLines.GenerateSphere(origin, radius, DrawableLines.DefaultDuration, segments);
	}

	public static void GenerateSphere(Vector3 origin, float radius, float? duration, int segments = 8)
	{
		DrawableLines.GenerateSphere(origin, radius, duration, DrawableLines.DefaultColor, segments);
	}

	public static void GenerateSphere(Vector3 origin, float radius, Color? color, int segments = 8)
	{
		DrawableLines.GenerateSphere(origin, radius, DrawableLines.DefaultDuration, color, segments);
	}

	public static void GenerateSphere(Vector3 origin, float radius, float? duration, Color? color, int segments = 8)
	{
		DrawableLines.GenerateLine(duration, color, DrawableLines.GetCircle(origin, radius, horizontalAxis: true, segments));
		DrawableLines.GenerateLine(duration, color, DrawableLines.GetCircle(origin, radius, horizontalAxis: false, segments));
	}

	public static Vector3[] GetCircle(Vector3 origin, float radius, bool horizontalAxis = true, int segments = 8)
	{
		if (segments <= 0)
		{
			segments = 8;
		}
		if (segments % 2 != 0)
		{
			segments++;
		}
		Vector3[] array = new Vector3[segments + 1];
		float num = MathF.PI * 2f / (float)segments;
		for (int i = 0; i < segments; i++)
		{
			float f = (float)i * num;
			float num2 = Mathf.Cos(f) * radius;
			float num3 = Mathf.Sin(f) * radius;
			array[i] = (horizontalAxis ? new Vector3(origin.x + num2, origin.y + num3, origin.z) : new Vector3(origin.x + num2, origin.y, origin.z + num3));
		}
		array[^1] = array[0];
		return array;
	}

	public static void ServerGenerateLine(params Vector3[] positions)
	{
		DrawableLines.ServerGenerateLine(DrawableLines.ServerDefaultDuration, DrawableLines.ServerDefaultColor, positions);
	}

	public static void ServerGenerateLine(float? duration, params Vector3[] positions)
	{
		DrawableLines.ServerGenerateLine(duration, DrawableLines.ServerDefaultColor, positions);
	}

	public static void ServerGenerateLine(Color? color, params Vector3[] positions)
	{
		DrawableLines.ServerGenerateLine(DrawableLines.ServerDefaultDuration, color, positions);
	}

	public static void ServerGenerateLine(float? duration, Color? color, params Vector3[] positions)
	{
		if (NetworkServer.active && DrawableLines.IsDebugModeEnabled)
		{
			DrawableLines.ServerGenerateMessage(duration, color, positions).SendToAuthenticated();
		}
	}

	public static void ServerGenerateLine(ReferenceHub hub, params Vector3[] positions)
	{
		DrawableLines.ServerGenerateLine(hub, DrawableLines.ServerDefaultDuration, DrawableLines.ServerDefaultColor, positions);
	}

	public static void ServerGenerateLine(ReferenceHub hub, float? duration, params Vector3[] positions)
	{
		DrawableLines.ServerGenerateLine(hub, duration, DrawableLines.ServerDefaultColor, positions);
	}

	public static void ServerGenerateLine(ReferenceHub hub, Color? color, params Vector3[] positions)
	{
		DrawableLines.ServerGenerateLine(hub, DrawableLines.ServerDefaultDuration, color, positions);
	}

	public static void ServerGenerateLine(ReferenceHub hub, float? duration, Color? color, params Vector3[] positions)
	{
		if (NetworkServer.active && DrawableLines.IsDebugModeEnabled)
		{
			hub.connectionToServer.Send(DrawableLines.ServerGenerateMessage(duration, color, positions));
		}
	}

	private static DrawableLineMessage ServerGenerateMessage(float? duration, Color? color, Vector3[] positions)
	{
		if (DrawableLines.DurationOverride.HasValue)
		{
			duration = DrawableLines.DurationOverride.Value;
		}
		else if (!duration.HasValue)
		{
			duration = DrawableLines.ServerDefaultDuration;
		}
		if (!color.HasValue)
		{
			color = DrawableLines.ServerDefaultColor;
		}
		return new DrawableLineMessage(duration, color, positions);
	}
}
