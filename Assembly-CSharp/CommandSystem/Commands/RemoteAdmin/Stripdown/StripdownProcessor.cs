using System;
using System.Collections;
using System.Reflection;
using System.Text;
using Org.BouncyCastle.Security;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown
{
	public static class StripdownProcessor
	{
		private static string PrintMembers(object obj, MemberInfo[] infos)
		{
			if (obj == null)
			{
				return "null";
			}
			StripdownProcessor.ReturnSb.Clear();
			foreach (MemberInfo memberInfo in infos)
			{
				StripdownProcessor.ReturnSb.Append(memberInfo.Name);
				StripdownProcessor.ReturnSb.Append("=\"");
				FieldInfo fieldInfo = memberInfo as FieldInfo;
				if (fieldInfo != null)
				{
					StripdownProcessor.ReturnSb.Append(StripdownProcessor.PrintObjToString(fieldInfo.GetValue(obj)));
				}
				else
				{
					PropertyInfo propertyInfo = memberInfo as PropertyInfo;
					if (propertyInfo == null)
					{
						throw new InvalidOperationException("Member " + memberInfo.Name + " is not a value provider!");
					}
					StripdownProcessor.ReturnSb.Append(StripdownProcessor.PrintObjToString(propertyInfo.GetValue(obj)));
				}
				StripdownProcessor.ReturnSb.Append("\"; ");
			}
			return StripdownProcessor.ReturnSb.ToString();
		}

		private static string PrintObjToString(object obj)
		{
			ICollection collection = obj as ICollection;
			if (collection == null)
			{
				return obj.ToString();
			}
			string text = "Collection {Cnt=" + collection.Count.ToString();
			foreach (object obj2 in collection)
			{
				text = text + ", " + obj2.ToString();
			}
			return text + "}";
		}

		private static MemberInfo GetValueMemberByName(string name)
		{
			MemberInfo memberInfo = StripdownProcessor._selectedType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? StripdownProcessor._selectedType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (memberInfo != null)
			{
				return memberInfo;
			}
			StripdownProcessor.ReturnSb.Clear();
			StripdownProcessor.ReturnSb.Append("Value " + name + " not found. Available values:");
			StripdownProcessor._selectedType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ForEach(delegate(FieldInfo x)
			{
				StripdownProcessor.ReturnSb.AppendLine(x.Name);
			});
			StripdownProcessor._selectedType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).ForEach(delegate(PropertyInfo x)
			{
				StripdownProcessor.ReturnSb.AppendLine(x.Name);
			});
			throw new InvalidParameterException(StripdownProcessor.ReturnSb.ToString());
		}

		public static int SelectUnityObjects(string typeName)
		{
			StripdownProcessor._selectedType = Type.GetType(typeName, true, true);
			object[] array = global::UnityEngine.Object.FindObjectsOfType(StripdownProcessor._selectedType);
			StripdownProcessor._selections = array;
			return StripdownProcessor._selections.Length;
		}

		public static void SelectValues(string valueName)
		{
			MemberInfo valueMemberByName = StripdownProcessor.GetValueMemberByName(valueName);
			int num = StripdownProcessor._selections.Length;
			object[] array = new object[num];
			Array.Copy(StripdownProcessor._selections, array, num);
			StripdownProcessor._selections = new object[num];
			PropertyInfo propertyInfo = valueMemberByName as PropertyInfo;
			if (propertyInfo != null)
			{
				StripdownProcessor._selectedType = propertyInfo.PropertyType;
				for (int i = 0; i < num; i++)
				{
					StripdownProcessor._selections[i] = propertyInfo.GetValue(array[i]);
				}
				return;
			}
			FieldInfo fieldInfo = valueMemberByName as FieldInfo;
			if (fieldInfo != null)
			{
				StripdownProcessor._selectedType = fieldInfo.FieldType;
				for (int j = 0; j < num; j++)
				{
					StripdownProcessor._selections[j] = fieldInfo.GetValue(array[j]);
				}
			}
		}

		public static void SelectComponent(string componentName)
		{
			int num = StripdownProcessor._selections.Length;
			object[] array = new object[num];
			Array.Copy(StripdownProcessor._selections, array, num);
			StripdownProcessor._selections = new object[num];
			for (int i = 0; i < num; i++)
			{
				object obj = array[i];
				GameObject gameObject = obj as GameObject;
				GameObject gameObject2;
				if (gameObject != null)
				{
					gameObject2 = gameObject;
				}
				else
				{
					Component component = obj as Component;
					if (component == null)
					{
						throw new InvalidOperationException("Currently selected object type (" + obj.GetType().FullName + ") does not support GetComponent.");
					}
					gameObject2 = component.gameObject;
				}
				Component component2 = gameObject2.GetComponent(componentName);
				if (component2 == null)
				{
					StripdownProcessor.ReturnSb.Clear();
					StripdownProcessor.ReturnSb.Append(string.Concat(new string[] { "GameObject ", gameObject2.name, " does not have a ", componentName, " component." }));
					StripdownProcessor.ReturnSb.AppendLine("List of compoenents:");
					gameObject2.GetComponents<Component>().ForEach(delegate(Component x)
					{
						StripdownProcessor.ReturnSb.AppendLine(x.GetType().Name);
					});
					throw new InvalidParameterException(StripdownProcessor.ReturnSb.ToString());
				}
				StripdownProcessor._selections[i] = component2;
			}
		}

		public static string[] Print(params string[] valueNames)
		{
			if (StripdownProcessor._selections == null || StripdownProcessor._selections.Length == 0)
			{
				throw new InvalidOperationException("No objects selected!");
			}
			int num = StripdownProcessor._selections.Length;
			int num2 = valueNames.Length;
			MemberInfo[] array = new MemberInfo[num2];
			for (int i = 0; i < num2; i++)
			{
				array[i] = StripdownProcessor.GetValueMemberByName(valueNames[i]);
			}
			string[] array2 = new string[num];
			for (int j = 0; j < num; j++)
			{
				string text;
				try
				{
					text = StripdownProcessor.PrintMembers(StripdownProcessor._selections[j], array);
				}
				catch (Exception ex)
				{
					text = ex.Message;
				}
				array2[j] = text;
			}
			return array2;
		}

		private static readonly StringBuilder ReturnSb = new StringBuilder();

		private static object[] _selections;

		private static Type _selectedType;

		private const BindingFlags SearchFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	}
}
