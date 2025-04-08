using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Utf8Json.Internal.Emit
{
	internal class MetaType
	{
		public Type Type { get; private set; }

		public bool IsClass { get; private set; }

		public bool IsStruct
		{
			get
			{
				return !this.IsClass;
			}
		}

		public bool IsConcreteClass { get; private set; }

		public ConstructorInfo BestmatchConstructor { get; internal set; }

		public MetaMember[] ConstructorParameters { get; internal set; }

		public MetaMember[] Members { get; internal set; }

		public MetaType(Type type, Func<string, string> nameMutetor, bool allowPrivate)
		{
			TypeInfo typeInfo = type.GetTypeInfo();
			bool flag = typeInfo.IsClass || typeInfo.IsInterface || typeInfo.IsAbstract;
			this.Type = type;
			Dictionary<string, MetaMember> dictionary = new Dictionary<string, MetaMember>();
			foreach (PropertyInfo propertyInfo in type.GetAllProperties())
			{
				if (propertyInfo.GetIndexParameters().Length == 0 && propertyInfo.GetCustomAttribute(true) == null)
				{
					DataMemberAttribute customAttribute = propertyInfo.GetCustomAttribute(true);
					string text = ((customAttribute != null && customAttribute.Name != null) ? customAttribute.Name : nameMutetor(propertyInfo.Name));
					MetaMember metaMember = new MetaMember(propertyInfo, text, allowPrivate);
					if (metaMember.IsReadable || metaMember.IsWritable)
					{
						if (dictionary.ContainsKey(metaMember.Name))
						{
							throw new InvalidOperationException("same (custom)name is in type. Type:" + type.Name + " Name:" + metaMember.Name);
						}
						dictionary.Add(metaMember.Name, metaMember);
					}
				}
			}
			foreach (FieldInfo fieldInfo in type.GetAllFields())
			{
				if (fieldInfo.GetCustomAttribute(true) == null && fieldInfo.GetCustomAttribute(true) == null && !fieldInfo.IsStatic && !fieldInfo.Name.StartsWith("<"))
				{
					DataMemberAttribute customAttribute2 = fieldInfo.GetCustomAttribute(true);
					string text2 = ((customAttribute2 != null && customAttribute2.Name != null) ? customAttribute2.Name : nameMutetor(fieldInfo.Name));
					MetaMember metaMember2 = new MetaMember(fieldInfo, text2, allowPrivate);
					if (metaMember2.IsReadable || metaMember2.IsWritable)
					{
						if (dictionary.ContainsKey(metaMember2.Name))
						{
							throw new InvalidOperationException("same (custom)name is in type. Type:" + type.Name + " Name:" + metaMember2.Name);
						}
						dictionary.Add(metaMember2.Name, metaMember2);
					}
				}
			}
			ConstructorInfo constructorInfo = typeInfo.DeclaredConstructors.Where((ConstructorInfo x) => x.IsPublic).SingleOrDefault((ConstructorInfo x) => x.GetCustomAttribute(false) != null);
			List<MetaMember> list = new List<MetaMember>();
			IEnumerator<ConstructorInfo> enumerator3 = null;
			if (constructorInfo == null)
			{
				enumerator3 = (from x in typeInfo.DeclaredConstructors
					where x.IsPublic
					orderby x.GetParameters().Length descending
					select x).GetEnumerator();
				if (enumerator3.MoveNext())
				{
					constructorInfo = enumerator3.Current;
				}
			}
			if (constructorInfo != null)
			{
				ILookup<string, KeyValuePair<string, MetaMember>> lookup = dictionary.ToLookup((KeyValuePair<string, MetaMember> x) => x.Key, (KeyValuePair<string, MetaMember> x) => x, StringComparer.OrdinalIgnoreCase);
				for (;;)
				{
					list.Clear();
					int num = 0;
					foreach (ParameterInfo parameterInfo in constructorInfo.GetParameters())
					{
						IEnumerable<KeyValuePair<string, MetaMember>> enumerable = lookup[parameterInfo.Name];
						int num2 = enumerable.Count<KeyValuePair<string, MetaMember>>();
						if (num2 != 0)
						{
							if (num2 != 1)
							{
								if (enumerator3 == null)
								{
									goto IL_037E;
								}
								constructorInfo = null;
							}
							else
							{
								MetaMember value = enumerable.First<KeyValuePair<string, MetaMember>>().Value;
								if (parameterInfo.ParameterType == value.Type && value.IsReadable)
								{
									list.Add(value);
									num++;
								}
								else
								{
									constructorInfo = null;
								}
							}
						}
						else
						{
							constructorInfo = null;
						}
					}
					if (!MetaType.TryGetNextConstructor(enumerator3, ref constructorInfo))
					{
						goto IL_0431;
					}
				}
				IL_037E:
				ParameterInfo parameterInfo;
				throw new InvalidOperationException(string.Concat(new string[]
				{
					"duplicate matched constructor parameter name:",
					type.FullName,
					" parameterName:",
					parameterInfo.Name,
					" paramterType:",
					parameterInfo.ParameterType.Name
				}));
			}
			IL_0431:
			this.IsClass = flag;
			this.IsConcreteClass = flag && !typeInfo.IsAbstract && !typeInfo.IsInterface;
			this.BestmatchConstructor = constructorInfo;
			this.ConstructorParameters = list.ToArray();
			this.Members = dictionary.Values.ToArray<MetaMember>();
		}

		private static bool TryGetNextConstructor(IEnumerator<ConstructorInfo> ctorEnumerator, ref ConstructorInfo ctor)
		{
			if (ctorEnumerator == null || ctor != null)
			{
				return false;
			}
			if (ctorEnumerator.MoveNext())
			{
				ctor = ctorEnumerator.Current;
				return true;
			}
			ctor = null;
			return false;
		}
	}
}
