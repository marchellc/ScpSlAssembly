using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Utf8Json.Formatters;
using Utf8Json.Internal;

namespace Utf8Json.Resolvers.Internal
{
	internal static class DynamicGenericResolverGetFormatterHelper
	{
		internal static object GetFormatter(Type t)
		{
			TypeInfo typeInfo = t.GetTypeInfo();
			if (!t.IsArray)
			{
				if (typeInfo.IsGenericType)
				{
					Type genericTypeDefinition = typeInfo.GetGenericTypeDefinition();
					bool flag = genericTypeDefinition.GetTypeInfo().IsNullable();
					Type type = (flag ? typeInfo.GenericTypeArguments[0] : null);
					if (genericTypeDefinition == typeof(KeyValuePair<, >))
					{
						return DynamicGenericResolverGetFormatterHelper.CreateInstance(typeof(KeyValuePairFormatter<, >), typeInfo.GenericTypeArguments, Array.Empty<object>());
					}
					if (flag && type.GetTypeInfo().IsConstructedGenericType() && type.GetGenericTypeDefinition() == typeof(KeyValuePair<, >))
					{
						return DynamicGenericResolverGetFormatterHelper.CreateInstance(typeof(NullableFormatter<>), new Type[] { type }, Array.Empty<object>());
					}
					if (genericTypeDefinition == typeof(ArraySegment<>))
					{
						if (typeInfo.GenericTypeArguments[0] == typeof(byte))
						{
							return ByteArraySegmentFormatter.Default;
						}
						return DynamicGenericResolverGetFormatterHelper.CreateInstance(typeof(ArraySegmentFormatter<>), typeInfo.GenericTypeArguments, Array.Empty<object>());
					}
					else if (flag && type.GetTypeInfo().IsConstructedGenericType() && type.GetGenericTypeDefinition() == typeof(ArraySegment<>))
					{
						if (type == typeof(ArraySegment<byte>))
						{
							return new StaticNullableFormatter<ArraySegment<byte>>(ByteArraySegmentFormatter.Default);
						}
						return DynamicGenericResolverGetFormatterHelper.CreateInstance(typeof(NullableFormatter<>), new Type[] { type }, Array.Empty<object>());
					}
					else
					{
						Type type2;
						if (DynamicGenericResolverGetFormatterHelper.formatterMap.TryGetValue(genericTypeDefinition, out type2))
						{
							return DynamicGenericResolverGetFormatterHelper.CreateInstance(type2, typeInfo.GenericTypeArguments, Array.Empty<object>());
						}
						if (typeInfo.GenericTypeArguments.Length == 1)
						{
							if (typeInfo.ImplementedInterfaces.Any((Type x) => x.GetTypeInfo().IsConstructedGenericType() && x.GetGenericTypeDefinition() == typeof(ICollection<>)))
							{
								if (typeInfo.DeclaredConstructors.Any((ConstructorInfo x) => x.GetParameters().Length == 0))
								{
									Type type3 = typeInfo.GenericTypeArguments[0];
									return DynamicGenericResolverGetFormatterHelper.CreateInstance(typeof(GenericCollectionFormatter<, >), new Type[] { type3, t }, Array.Empty<object>());
								}
							}
						}
						if (typeInfo.GenericTypeArguments.Length == 2)
						{
							if (typeInfo.ImplementedInterfaces.Any((Type x) => x.GetTypeInfo().IsConstructedGenericType() && x.GetGenericTypeDefinition() == typeof(IDictionary<, >)))
							{
								if (typeInfo.DeclaredConstructors.Any((ConstructorInfo x) => x.GetParameters().Length == 0))
								{
									Type type4 = typeInfo.GenericTypeArguments[0];
									Type type5 = typeInfo.GenericTypeArguments[1];
									return DynamicGenericResolverGetFormatterHelper.CreateInstance(typeof(GenericDictionaryFormatter<, , >), new Type[] { type4, type5, t }, Array.Empty<object>());
								}
							}
						}
					}
				}
				else
				{
					if (t == typeof(IEnumerable))
					{
						return NonGenericInterfaceEnumerableFormatter.Default;
					}
					if (t == typeof(ICollection))
					{
						return NonGenericInterfaceCollectionFormatter.Default;
					}
					if (t == typeof(IList))
					{
						return NonGenericInterfaceListFormatter.Default;
					}
					if (t == typeof(IDictionary))
					{
						return NonGenericInterfaceDictionaryFormatter.Default;
					}
					if (typeof(IList).GetTypeInfo().IsAssignableFrom(typeInfo))
					{
						if (typeInfo.DeclaredConstructors.Any((ConstructorInfo x) => x.GetParameters().Length == 0))
						{
							return Activator.CreateInstance(typeof(NonGenericListFormatter<>).MakeGenericType(new Type[] { t }));
						}
					}
					if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(typeInfo))
					{
						if (typeInfo.DeclaredConstructors.Any((ConstructorInfo x) => x.GetParameters().Length == 0))
						{
							return Activator.CreateInstance(typeof(NonGenericDictionaryFormatter<>).MakeGenericType(new Type[] { t }));
						}
					}
				}
				return null;
			}
			int arrayRank = t.GetArrayRank();
			if (arrayRank == 1)
			{
				if (t.GetElementType() == typeof(byte))
				{
					return ByteArrayFormatter.Default;
				}
				return Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(new Type[] { t.GetElementType() }));
			}
			else
			{
				if (arrayRank == 2)
				{
					return Activator.CreateInstance(typeof(TwoDimentionalArrayFormatter<>).MakeGenericType(new Type[] { t.GetElementType() }));
				}
				if (arrayRank == 3)
				{
					return Activator.CreateInstance(typeof(ThreeDimentionalArrayFormatter<>).MakeGenericType(new Type[] { t.GetElementType() }));
				}
				if (arrayRank == 4)
				{
					return Activator.CreateInstance(typeof(FourDimentionalArrayFormatter<>).MakeGenericType(new Type[] { t.GetElementType() }));
				}
				return null;
			}
		}

		private static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
		{
			return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
		}

		private static readonly Dictionary<Type, Type> formatterMap = new Dictionary<Type, Type>
		{
			{
				typeof(List<>),
				typeof(ListFormatter<>)
			},
			{
				typeof(LinkedList<>),
				typeof(LinkedListFormatter<>)
			},
			{
				typeof(Queue<>),
				typeof(QeueueFormatter<>)
			},
			{
				typeof(Stack<>),
				typeof(StackFormatter<>)
			},
			{
				typeof(HashSet<>),
				typeof(HashSetFormatter<>)
			},
			{
				typeof(ReadOnlyCollection<>),
				typeof(ReadOnlyCollectionFormatter<>)
			},
			{
				typeof(IList<>),
				typeof(InterfaceListFormatter<>)
			},
			{
				typeof(ICollection<>),
				typeof(InterfaceCollectionFormatter<>)
			},
			{
				typeof(IEnumerable<>),
				typeof(InterfaceEnumerableFormatter<>)
			},
			{
				typeof(Dictionary<, >),
				typeof(DictionaryFormatter<, >)
			},
			{
				typeof(IDictionary<, >),
				typeof(InterfaceDictionaryFormatter<, >)
			},
			{
				typeof(SortedDictionary<, >),
				typeof(SortedDictionaryFormatter<, >)
			},
			{
				typeof(SortedList<, >),
				typeof(SortedListFormatter<, >)
			},
			{
				typeof(ILookup<, >),
				typeof(InterfaceLookupFormatter<, >)
			},
			{
				typeof(IGrouping<, >),
				typeof(InterfaceGroupingFormatter<, >)
			}
		};
	}
}
