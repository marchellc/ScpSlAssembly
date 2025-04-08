using System;
using System.Collections.Generic;
using UnityEngine;
using Utf8Json.Formatters;

namespace Utf8Json.Unity
{
	internal static class UnityResolverGetFormatterHelper
	{
		internal static object GetFormatter(Type t)
		{
			int num;
			if (!UnityResolverGetFormatterHelper.lookup.TryGetValue(t, out num))
			{
				return null;
			}
			switch (num)
			{
			case 0:
				return new Vector2Formatter();
			case 1:
				return new Vector3Formatter();
			case 2:
				return new Vector4Formatter();
			case 3:
				return new QuaternionFormatter();
			case 4:
				return new ColorFormatter();
			case 5:
				return new BoundsFormatter();
			case 6:
				return new RectFormatter();
			case 7:
				return new ArrayFormatter<Vector2>();
			case 8:
				return new ArrayFormatter<Vector3>();
			case 9:
				return new ArrayFormatter<Vector4>();
			case 10:
				return new ArrayFormatter<Quaternion>();
			case 11:
				return new ArrayFormatter<Color>();
			case 12:
				return new ArrayFormatter<Bounds>();
			case 13:
				return new ArrayFormatter<Rect>();
			case 14:
				return new StaticNullableFormatter<Vector2>(new Vector2Formatter());
			case 15:
				return new StaticNullableFormatter<Vector3>(new Vector3Formatter());
			case 16:
				return new StaticNullableFormatter<Vector4>(new Vector4Formatter());
			case 17:
				return new StaticNullableFormatter<Quaternion>(new QuaternionFormatter());
			case 18:
				return new StaticNullableFormatter<Color>(new ColorFormatter());
			case 19:
				return new StaticNullableFormatter<Bounds>(new BoundsFormatter());
			case 20:
				return new StaticNullableFormatter<Rect>(new RectFormatter());
			default:
				return null;
			}
		}

		private static readonly Dictionary<Type, int> lookup = new Dictionary<Type, int>(7)
		{
			{
				typeof(Vector2),
				0
			},
			{
				typeof(Vector3),
				1
			},
			{
				typeof(Vector4),
				2
			},
			{
				typeof(Quaternion),
				3
			},
			{
				typeof(Color),
				4
			},
			{
				typeof(Bounds),
				5
			},
			{
				typeof(Rect),
				6
			},
			{
				typeof(Vector2[]),
				7
			},
			{
				typeof(Vector3[]),
				8
			},
			{
				typeof(Vector4[]),
				9
			},
			{
				typeof(Quaternion[]),
				10
			},
			{
				typeof(Color[]),
				11
			},
			{
				typeof(Bounds[]),
				12
			},
			{
				typeof(Rect[]),
				13
			},
			{
				typeof(Vector2?),
				14
			},
			{
				typeof(Vector3?),
				15
			},
			{
				typeof(Vector4?),
				16
			},
			{
				typeof(Quaternion?),
				17
			},
			{
				typeof(Color?),
				18
			},
			{
				typeof(Bounds?),
				19
			},
			{
				typeof(Rect?),
				20
			}
		};
	}
}
