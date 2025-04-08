using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Utf8Json.Internal
{
	internal static class ReflectionExtensions
	{
		public static bool IsNullable(this TypeInfo type)
		{
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static bool IsPublic(this TypeInfo type)
		{
			return type.IsPublic;
		}

		public static bool IsAnonymous(this TypeInfo type)
		{
			return type.GetCustomAttribute<CompilerGeneratedAttribute>() != null && type.Name.Contains("AnonymousType") && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")) && (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
		}

		public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
		{
			return ReflectionExtensions.GetAllPropertiesCore(type, new HashSet<string>());
		}

		private static IEnumerable<PropertyInfo> GetAllPropertiesCore(Type type, HashSet<string> nameCheck)
		{
			foreach (PropertyInfo propertyInfo in type.GetRuntimeProperties())
			{
				if (nameCheck.Add(propertyInfo.Name))
				{
					yield return propertyInfo;
				}
			}
			IEnumerator<PropertyInfo> enumerator = null;
			if (type.BaseType != null)
			{
				foreach (PropertyInfo propertyInfo2 in ReflectionExtensions.GetAllPropertiesCore(type.BaseType, nameCheck))
				{
					yield return propertyInfo2;
				}
				enumerator = null;
			}
			yield break;
			yield break;
		}

		public static IEnumerable<FieldInfo> GetAllFields(this Type type)
		{
			return ReflectionExtensions.GetAllFieldsCore(type, new HashSet<string>());
		}

		private static IEnumerable<FieldInfo> GetAllFieldsCore(Type type, HashSet<string> nameCheck)
		{
			foreach (FieldInfo fieldInfo in type.GetRuntimeFields())
			{
				if (nameCheck.Add(fieldInfo.Name))
				{
					yield return fieldInfo;
				}
			}
			IEnumerator<FieldInfo> enumerator = null;
			if (type.BaseType != null)
			{
				foreach (FieldInfo fieldInfo2 in ReflectionExtensions.GetAllFieldsCore(type.BaseType, nameCheck))
				{
					yield return fieldInfo2;
				}
				enumerator = null;
			}
			yield break;
			yield break;
		}
	}
}
