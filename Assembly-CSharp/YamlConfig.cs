using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GameCore;
using NorthwoodLib.Pools;
using Scp914;
using Utils.ConfigHandler;

public class YamlConfig : ConfigRegister
{
	public string[] RawData
	{
		get
		{
			if (!this._virtual)
			{
				return this._rawData;
			}
			return this._rawDataUnfiltered;
		}
		set
		{
			if (this._virtual)
			{
				this._rawDataUnfiltered = value;
				return;
			}
			this._rawData = value;
		}
	}

	public bool IsVirtual
	{
		get
		{
			return this._virtual;
		}
		set
		{
			if (!value || this._virtual)
			{
				return;
			}
			this._virtual = true;
			this._rawDataUnfiltered = this.RawData;
		}
	}

	public YamlConfig()
	{
		this.RawData = new string[0];
	}

	public YamlConfig(string path)
	{
		this.Path = path;
		this.LoadConfigFile(path);
	}

	public override void UpdateConfigValue(ConfigEntry configEntry)
	{
		if (configEntry == null)
		{
			throw new NullReferenceException("Config type unsupported (Config: Null).");
		}
		ConfigEntry<bool> configEntry2 = configEntry as ConfigEntry<bool>;
		if (configEntry2 != null)
		{
			configEntry2.Value = this.GetBool(configEntry2.Key, configEntry2.Default);
			return;
		}
		ConfigEntry<byte> configEntry3 = configEntry as ConfigEntry<byte>;
		if (configEntry3 != null)
		{
			configEntry3.Value = this.GetByte(configEntry3.Key, configEntry3.Default);
			return;
		}
		ConfigEntry<char> configEntry4 = configEntry as ConfigEntry<char>;
		if (configEntry4 != null)
		{
			configEntry4.Value = this.GetChar(configEntry4.Key, configEntry4.Default);
			return;
		}
		ConfigEntry<decimal> configEntry5 = configEntry as ConfigEntry<decimal>;
		if (configEntry5 != null)
		{
			configEntry5.Value = this.GetDecimal(configEntry5.Key, configEntry5.Default);
			return;
		}
		ConfigEntry<double> configEntry6 = configEntry as ConfigEntry<double>;
		if (configEntry6 != null)
		{
			configEntry6.Value = this.GetDouble(configEntry6.Key, configEntry6.Default);
			return;
		}
		ConfigEntry<float> configEntry7 = configEntry as ConfigEntry<float>;
		if (configEntry7 != null)
		{
			configEntry7.Value = this.GetFloat(configEntry7.Key, configEntry7.Default);
			return;
		}
		ConfigEntry<int> configEntry8 = configEntry as ConfigEntry<int>;
		if (configEntry8 != null)
		{
			configEntry8.Value = this.GetInt(configEntry8.Key, configEntry8.Default);
			return;
		}
		ConfigEntry<long> configEntry9 = configEntry as ConfigEntry<long>;
		if (configEntry9 != null)
		{
			configEntry9.Value = this.GetLong(configEntry9.Key, configEntry9.Default);
			return;
		}
		ConfigEntry<sbyte> configEntry10 = configEntry as ConfigEntry<sbyte>;
		if (configEntry10 != null)
		{
			configEntry10.Value = this.GetSByte(configEntry10.Key, configEntry10.Default);
			return;
		}
		ConfigEntry<short> configEntry11 = configEntry as ConfigEntry<short>;
		if (configEntry11 != null)
		{
			configEntry11.Value = this.GetShort(configEntry11.Key, configEntry11.Default);
			return;
		}
		ConfigEntry<string> configEntry12 = configEntry as ConfigEntry<string>;
		if (configEntry12 != null)
		{
			configEntry12.Value = this.GetString(configEntry12.Key, configEntry12.Default);
			return;
		}
		ConfigEntry<uint> configEntry13 = configEntry as ConfigEntry<uint>;
		if (configEntry13 != null)
		{
			configEntry13.Value = this.GetUInt(configEntry13.Key, configEntry13.Default);
			return;
		}
		ConfigEntry<ulong> configEntry14 = configEntry as ConfigEntry<ulong>;
		if (configEntry14 != null)
		{
			configEntry14.Value = this.GetULong(configEntry14.Key, configEntry14.Default);
			return;
		}
		ConfigEntry<ushort> configEntry15 = configEntry as ConfigEntry<ushort>;
		if (configEntry15 == null)
		{
			ConfigEntry<List<bool>> configEntry16 = configEntry as ConfigEntry<List<bool>>;
			if (configEntry16 == null)
			{
				ConfigEntry<List<byte>> configEntry17 = configEntry as ConfigEntry<List<byte>>;
				if (configEntry17 == null)
				{
					ConfigEntry<List<char>> configEntry18 = configEntry as ConfigEntry<List<char>>;
					if (configEntry18 == null)
					{
						ConfigEntry<List<decimal>> configEntry19 = configEntry as ConfigEntry<List<decimal>>;
						if (configEntry19 == null)
						{
							ConfigEntry<List<double>> configEntry20 = configEntry as ConfigEntry<List<double>>;
							if (configEntry20 == null)
							{
								ConfigEntry<List<float>> configEntry21 = configEntry as ConfigEntry<List<float>>;
								if (configEntry21 == null)
								{
									ConfigEntry<List<int>> configEntry22 = configEntry as ConfigEntry<List<int>>;
									if (configEntry22 == null)
									{
										ConfigEntry<List<long>> configEntry23 = configEntry as ConfigEntry<List<long>>;
										if (configEntry23 == null)
										{
											ConfigEntry<List<sbyte>> configEntry24 = configEntry as ConfigEntry<List<sbyte>>;
											if (configEntry24 == null)
											{
												ConfigEntry<List<short>> configEntry25 = configEntry as ConfigEntry<List<short>>;
												if (configEntry25 == null)
												{
													ConfigEntry<List<string>> configEntry26 = configEntry as ConfigEntry<List<string>>;
													if (configEntry26 == null)
													{
														ConfigEntry<List<uint>> configEntry27 = configEntry as ConfigEntry<List<uint>>;
														if (configEntry27 == null)
														{
															ConfigEntry<List<ulong>> configEntry28 = configEntry as ConfigEntry<List<ulong>>;
															if (configEntry28 == null)
															{
																ConfigEntry<List<ushort>> configEntry29 = configEntry as ConfigEntry<List<ushort>>;
																if (configEntry29 == null)
																{
																	ConfigEntry<Dictionary<string, string>> configEntry30 = configEntry as ConfigEntry<Dictionary<string, string>>;
																	if (configEntry30 == null)
																	{
																		ConfigEntry<Scp914Mode> configEntry31 = configEntry as ConfigEntry<Scp914Mode>;
																		if (configEntry31 == null)
																		{
																			throw new Exception(string.Concat(new string[]
																			{
																				"Config type unsupported (Config: Key = \"",
																				configEntry.Key ?? "Null",
																				"\" Type = \"",
																				configEntry.ValueType.FullName ?? "Null",
																				"\" Name = \"",
																				configEntry.Name ?? "Null",
																				"\" Description = \"",
																				configEntry.Description ?? "Null",
																				"\")."
																			}));
																		}
																		string @string = this.GetString(configEntry31.Key, "");
																		Scp914Mode scp914Mode;
																		if (@string == "default" || !Enum.TryParse<Scp914Mode>(@string, out scp914Mode))
																		{
																			configEntry31.Value = configEntry31.Default;
																			return;
																		}
																		configEntry31.Value = scp914Mode;
																		return;
																	}
																	else
																	{
																		configEntry30.Value = this.GetStringDictionary(configEntry30.Key);
																		if (configEntry30.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry30.Key), "default", StringComparison.OrdinalIgnoreCase))
																		{
																			configEntry30.Value = configEntry30.Default;
																			return;
																		}
																	}
																}
																else
																{
																	configEntry29.Value = this.GetUShortList(configEntry29.Key);
																	if (configEntry29.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry29.Key), "default", StringComparison.OrdinalIgnoreCase))
																	{
																		configEntry29.Value = configEntry29.Default;
																		return;
																	}
																}
															}
															else
															{
																configEntry28.Value = this.GetULongList(configEntry28.Key);
																if (configEntry28.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry28.Key), "default", StringComparison.OrdinalIgnoreCase))
																{
																	configEntry28.Value = configEntry28.Default;
																	return;
																}
															}
														}
														else
														{
															configEntry27.Value = this.GetUIntList(configEntry27.Key);
															if (configEntry27.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry27.Key), "default", StringComparison.OrdinalIgnoreCase))
															{
																configEntry27.Value = configEntry27.Default;
																return;
															}
														}
													}
													else
													{
														configEntry26.Value = this.GetStringList(configEntry26.Key);
														if (configEntry26.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry26.Key), "default", StringComparison.OrdinalIgnoreCase))
														{
															configEntry26.Value = configEntry26.Default;
															return;
														}
													}
												}
												else
												{
													configEntry25.Value = this.GetShortList(configEntry25.Key);
													if (configEntry25.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry25.Key), "default", StringComparison.OrdinalIgnoreCase))
													{
														configEntry25.Value = configEntry25.Default;
														return;
													}
												}
											}
											else
											{
												configEntry24.Value = this.GetSByteList(configEntry24.Key);
												if (configEntry24.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry24.Key), "default", StringComparison.OrdinalIgnoreCase))
												{
													configEntry24.Value = configEntry24.Default;
													return;
												}
											}
										}
										else
										{
											configEntry23.Value = this.GetLongList(configEntry23.Key);
											if (configEntry23.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry23.Key), "default", StringComparison.OrdinalIgnoreCase))
											{
												configEntry23.Value = configEntry23.Default;
												return;
											}
										}
									}
									else
									{
										configEntry22.Value = this.GetIntList(configEntry22.Key);
										if (configEntry22.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry22.Key), "default", StringComparison.OrdinalIgnoreCase))
										{
											configEntry22.Value = configEntry22.Default;
											return;
										}
									}
								}
								else
								{
									configEntry21.Value = this.GetFloatList(configEntry21.Key);
									if (configEntry21.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry21.Key), "default", StringComparison.OrdinalIgnoreCase))
									{
										configEntry21.Value = configEntry21.Default;
										return;
									}
								}
							}
							else
							{
								configEntry20.Value = this.GetDoubleList(configEntry20.Key);
								if (configEntry20.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry20.Key), "default", StringComparison.OrdinalIgnoreCase))
								{
									configEntry20.Value = configEntry20.Default;
									return;
								}
							}
						}
						else
						{
							configEntry19.Value = this.GetDecimalList(configEntry19.Key);
							if (configEntry19.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry19.Key), "default", StringComparison.OrdinalIgnoreCase))
							{
								configEntry19.Value = configEntry19.Default;
								return;
							}
						}
					}
					else
					{
						configEntry18.Value = this.GetCharList(configEntry18.Key);
						if (configEntry18.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry18.Key), "default", StringComparison.OrdinalIgnoreCase))
						{
							configEntry18.Value = configEntry18.Default;
							return;
						}
					}
				}
				else
				{
					configEntry17.Value = this.GetByteList(configEntry17.Key);
					if (configEntry17.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry17.Key), "default", StringComparison.OrdinalIgnoreCase))
					{
						configEntry17.Value = configEntry17.Default;
						return;
					}
				}
			}
			else
			{
				configEntry16.Value = this.GetBoolList(configEntry16.Key);
				if (configEntry16.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry16.Key), "default", StringComparison.OrdinalIgnoreCase))
				{
					configEntry16.Value = configEntry16.Default;
					return;
				}
			}
			return;
		}
		configEntry15.Value = this.GetUShort(configEntry15.Key, configEntry15.Default);
	}

	private static string[] Filter(IEnumerable<string> lines)
	{
		return lines.Where((string line) => !string.IsNullOrEmpty(line) && !line.StartsWith("#") && (line.StartsWith(" - ") || line.Contains(':'))).ToArray<string>();
	}

	public void LoadConfigFile(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		this.Path = path;
		if (!ServerStatic.DisableConfigValidation)
		{
			YamlConfig.RemoveInvalid(path);
		}
		if (!ServerStatic.DisableConfigValidation && this.Path.EndsWith("config_gameplay.txt") && !this._afteradding && FileManager.FileExists("ConfigTemplates/config_gameplay.template.txt"))
		{
			YamlConfig.AddMissingTemplateKeys("ConfigTemplates/config_gameplay.template.txt", path, ref this._afteradding);
		}
		else if (!ServerStatic.DisableConfigValidation && this.Path.EndsWith("config_remoteadmin.txt") && !this._afteradding)
		{
			YamlConfig.AddMissingRoleVars(path);
			YamlConfig.AddMissingPerms(path, ref this._afteradding);
		}
		this._rawDataUnfiltered = FileManager.ReadAllLines(path);
		this.RawData = YamlConfig.Filter(this._rawDataUnfiltered);
		base.UpdateRegisteredConfigValues();
	}

	private static void RemoveDeprecated(string path)
	{
		List<string> list = FileManager.ReadAllLinesList(path);
		for (int i = list.Count - 1; i >= 0; i--)
		{
			for (int j = 0; j < YamlConfig._deprecatedconfigs.Length; j++)
			{
				if (list[i].StartsWith(YamlConfig._deprecatedconfigs[j] + ":") && (i == 0 || list[i - 1] != "#REMOVED FROM GAME - REDUNDANT"))
				{
					list.Insert(i, "#REMOVED FROM GAME - REDUNDANT");
				}
			}
		}
		FileManager.WriteToFile(list, path, false);
	}

	private static void AddMissingPerms(string path, ref bool _afteradding)
	{
		string[] perms = YamlConfig.GetStringList("Permissions", path).ToArray();
		string[] array = EnumUtils<PlayerPermissions>.Names.ToArray<string>();
		if (perms.Length == array.Length)
		{
			return;
		}
		List<string> list = (from permtype in array
			select new
			{
				permtype = permtype,
				inconfig = perms.Any((string perm) => perm.StartsWith(permtype))
			} into t
			where !t.inconfig
			select " - " + t.permtype + ": []").ToList<string>();
		List<string> list2 = FileManager.ReadAllLinesList(path);
		for (int i = 0; i < list2.Count; i++)
		{
			if (list2[i] == "Permissions:")
			{
				list2.InsertRange(i + 1, list);
			}
		}
		FileManager.WriteToFile(list2, path, false);
		_afteradding = true;
	}

	private static void AddMissingRoleVars(string path)
	{
		string text = TimeBehaviour.FormatTime("yyyy/MM/dd HH:mm:ss");
		List<string> stringList = YamlConfig.GetStringList("Roles", path);
		List<string> list = ListPool<string>.Shared.Rent();
		string config = FileManager.ReadAllText(path);
		using (List<string>.Enumerator enumerator = stringList.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				string role = enumerator.Current;
				list.AddRange(from rolevar in YamlConfig._rolevars
					where !config.Contains(role + "_" + rolevar + ":")
					select role + "_" + rolevar + ": default");
			}
		}
		if (list.Count > 0)
		{
			YamlConfig.Write(list, path, ref text);
		}
		ListPool<string>.Shared.Return(list);
	}

	private static void AddMissingTemplateKeys(string templatepath, string path, ref bool _afteradding)
	{
		string text = TimeBehaviour.FormatTime("yyyy/MM/dd HH:mm:ss");
		string text2 = FileManager.ReadAllText(path);
		string[] array = FileManager.ReadAllLines(templatepath);
		List<string> list = ListPool<string>.Shared.Rent();
		List<string> list2 = ListPool<string>.Shared.Rent();
		List<string> list3 = ListPool<string>.Shared.Rent();
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].StartsWith("#") && !array[i].StartsWith(" -") && array[i].Contains(":") && ((i + 1 < array.Length && array[i + 1].StartsWith(" -")) || array[i].EndsWith("[]")))
			{
				list.Add(array[i]);
			}
			else if (!array[i].StartsWith("#") && array[i].Contains(":") && !array[i].StartsWith(" -"))
			{
				list2.Add(array[i].Substring(0, array[i].IndexOf(':') + 1));
			}
		}
		foreach (string text3 in list2)
		{
			if (!text2.Contains(text3))
			{
				list3.Add(text3 + " default");
			}
		}
		ListPool<string>.Shared.Return(list2);
		YamlConfig.Write(list3, path, ref text);
		ListPool<string>.Shared.Return(list3);
		foreach (string text4 in list)
		{
			if (!text2.Contains(text4))
			{
				bool flag = false;
				List<string> list4 = new List<string> { "#LIST", text4 };
				foreach (string text5 in array)
				{
					if (text5.StartsWith(text4) && text5.EndsWith("[]"))
					{
						list4.Clear();
						list4.AddRange(new string[] { "#LIST - [] equals to empty", text5 });
						break;
					}
					if (text5.StartsWith(text4))
					{
						flag = true;
					}
					else if (flag)
					{
						if (text5.StartsWith(" - "))
						{
							list4.Add(text5);
						}
						else if (!text5.StartsWith("#"))
						{
							break;
						}
					}
				}
				YamlConfig.Write(list4, path, ref text);
			}
		}
		ListPool<string>.Shared.Return(list);
		_afteradding = true;
	}

	private static void Write(IEnumerable<string> text, string path, ref string time)
	{
		string[] array = text.ToArray<string>();
		if (array.Length != 0)
		{
			YamlConfig.Write(string.Join("\r\n", array), path, ref time);
		}
	}

	private static void Write(string text, string path, ref string time)
	{
		using (StreamWriter streamWriter = File.AppendText(path))
		{
			streamWriter.Write(string.Concat(new string[]
			{
				"\r\n\r\n#ADDED BY CONFIG VALIDATOR - ",
				time,
				" Game version: ",
				global::GameCore.Version.VersionString,
				"\r\n",
				text
			}));
		}
	}

	private static void RemoveInvalid(string path)
	{
		string[] array = FileManager.ReadAllLines(path);
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].StartsWith("#") && !array[i].StartsWith(" -") && !array[i].Contains(":") && !string.IsNullOrEmpty(array[i].Replace(" ", "")))
			{
				flag = true;
				array[i] = "#INVALID - " + array[i];
			}
		}
		if (flag)
		{
			FileManager.WriteToFile(array, path, false);
		}
	}

	private void CommentInvalid(string key, string type)
	{
		if (this.IsVirtual)
		{
			return;
		}
		for (int i = 0; i < this._rawDataUnfiltered.Length; i++)
		{
			if (this._rawDataUnfiltered[i].StartsWith(key + ": ", StringComparison.Ordinal))
			{
				this._rawDataUnfiltered[i] = "#INVALID " + type + " - " + this._rawDataUnfiltered[i];
			}
		}
		if (!ServerStatic.DisableConfigValidation)
		{
			FileManager.WriteToFile(this._rawDataUnfiltered, this.Path, false);
		}
	}

	public bool Reload()
	{
		if (this.IsVirtual)
		{
			return false;
		}
		if (string.IsNullOrEmpty(this.Path))
		{
			return false;
		}
		this.LoadConfigFile(this.Path);
		return true;
	}

	public bool TryGetString(string key, out string value)
	{
		foreach (string text in this.RawData)
		{
			if (text.StartsWith(key + ": "))
			{
				value = text.Substring(key.Length + 2);
				return true;
			}
		}
		value = "default";
		return false;
	}

	private string GetRawString(string key)
	{
		string text;
		if (!this.TryGetString(key, out text))
		{
			return "default";
		}
		return text;
	}

	public void SetString(string key, string value = null)
	{
		this.Reload();
		int num = 0;
		List<string> list = null;
		int i = 0;
		while (i < this._rawDataUnfiltered.Length)
		{
			if (this._rawDataUnfiltered[i].StartsWith(key + ": "))
			{
				if (value == null)
				{
					list = this._rawDataUnfiltered.ToList<string>();
					list.RemoveAt(i);
					num = 2;
					break;
				}
				this._rawDataUnfiltered[i] = key + ": " + value;
				num = 1;
				break;
			}
			else
			{
				i++;
			}
		}
		if (this.IsVirtual)
		{
			return;
		}
		switch (num)
		{
		case 0:
			list = this._rawDataUnfiltered.ToList<string>();
			list.Insert(list.Count, key + ": " + value);
			FileManager.WriteToFile(list, this.Path, false);
			break;
		case 1:
			FileManager.WriteToFile(this._rawDataUnfiltered, this.Path, false);
			break;
		case 2:
			if (list != null)
			{
				FileManager.WriteToFile(list, this.Path, false);
			}
			break;
		}
		this.Reload();
	}

	private static List<string> GetStringList(string key, string path)
	{
		bool flag = false;
		List<string> list = new List<string>();
		foreach (string text in FileManager.ReadAllLines(path))
		{
			if (text.StartsWith(key) && text.EndsWith("[]"))
			{
				break;
			}
			if (text.StartsWith(key + ":"))
			{
				string text2 = text.Substring(key.Length + 1);
				if (text2.Contains("[") && text2.Contains("]"))
				{
					return YamlConfig.ParseCommaSeparatedString(text2).ToList<string>();
				}
				flag = true;
			}
			else if (flag)
			{
				if (text.StartsWith(" - "))
				{
					list.Add(text.Substring(3));
				}
				else if (!text.StartsWith("#"))
				{
					break;
				}
			}
		}
		return list;
	}

	public void SetStringListItem(string key, string value, string newValue)
	{
		this.Reload();
		bool flag = false;
		int num = 0;
		List<string> list = null;
		for (int i = 0; i < this._rawDataUnfiltered.Length; i++)
		{
			string text = this._rawDataUnfiltered[i];
			if (text.StartsWith(key + ":"))
			{
				flag = true;
			}
			else if (flag)
			{
				if (value != null && text == " - " + value)
				{
					if (newValue == null)
					{
						list = this._rawDataUnfiltered.ToList<string>();
						list.RemoveAt(i);
						num = 2;
						break;
					}
					this._rawDataUnfiltered[i] = " - " + newValue;
					num = 1;
					break;
				}
				else if (!text.StartsWith(" - ") && !text.StartsWith("#"))
				{
					if (value != null)
					{
						list = this._rawDataUnfiltered.ToList<string>();
						list.Insert(i, " - " + newValue);
						num = 2;
						break;
					}
					break;
				}
			}
		}
		if (this.IsVirtual)
		{
			return;
		}
		int num2 = num;
		if (num2 != 1)
		{
			if (num2 == 2)
			{
				if (list != null)
				{
					FileManager.WriteToFile(list, this.Path, false);
				}
			}
		}
		else
		{
			FileManager.WriteToFile(this._rawDataUnfiltered, this.Path, false);
		}
		this.Reload();
	}

	public IEnumerable<string> StringListToText(string key, List<string> list)
	{
		yield return key + ":";
		foreach (string text in list)
		{
			yield return " - " + text;
		}
		List<string>.Enumerator enumerator = default(List<string>.Enumerator);
		yield break;
		yield break;
	}

	public Dictionary<string, string> GetStringDictionary(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (string text in YamlConfig.DataBuffer)
		{
			if (!text.Contains(": "))
			{
				ServerConsole.AddLog(string.Concat(new string[] { "Invalid entry \"", text, "\" in dictionary ", key, " in the config file - missing \": \"." }), ConsoleColor.Red, false);
			}
			else
			{
				int num = text.IndexOf(": ", StringComparison.Ordinal);
				string text2 = text.Substring(0, num);
				if (!dictionary.ContainsKey(text2))
				{
					dictionary.Add(text2, text.Substring(num + 2));
				}
				else
				{
					ServerConsole.AddLog(string.Concat(new string[] { "Ignoring duplicated subkey ", text2, " in dictionary ", key, " in the config file." }), ConsoleColor.Yellow, false);
				}
			}
		}
		return dictionary;
	}

	public void SetStringDictionaryItem(string key, string subkey, string value)
	{
		this.Reload();
		bool flag = false;
		int num = 0;
		List<string> list = null;
		for (int i = 0; i < this._rawDataUnfiltered.Length; i++)
		{
			string text = this._rawDataUnfiltered[i];
			if (text.StartsWith(key + ":"))
			{
				flag = true;
			}
			else if (flag)
			{
				if (text.StartsWith(" - " + subkey + ": "))
				{
					if (value == null)
					{
						list = this._rawDataUnfiltered.ToList<string>();
						list.RemoveAt(i);
						num = 2;
						break;
					}
					this._rawDataUnfiltered[i] = " - " + subkey + ": " + value;
					num = 1;
					break;
				}
				else if (!text.StartsWith(" - ") && !text.StartsWith("#"))
				{
					if (value != null)
					{
						list = this._rawDataUnfiltered.ToList<string>();
						list.Insert(i, " - " + subkey + ": " + value);
						num = 2;
						break;
					}
					break;
				}
			}
		}
		if (this.IsVirtual)
		{
			return;
		}
		switch (num)
		{
		case 0:
			list = this._rawDataUnfiltered.ToList<string>();
			list.Insert(list.Count, " - " + subkey + ": " + value);
			FileManager.WriteToFile(list, this.Path, false);
			break;
		case 1:
			FileManager.WriteToFile(this._rawDataUnfiltered, this.Path, false);
			break;
		case 2:
			if (list != null)
			{
				FileManager.WriteToFile(list, this.Path, false);
			}
			break;
		}
		this.Reload();
	}

	public static string[] ParseCommaSeparatedString(string data)
	{
		data = data.Trim();
		if (!data.StartsWith("[", StringComparison.Ordinal) || !data.EndsWith("]", StringComparison.Ordinal))
		{
			return null;
		}
		data = data.Substring(1, data.Length - 2).Replace(", ", ",");
		return data.Split(',', StringSplitOptions.None);
	}

	public IEnumerable<string> GetKeys()
	{
		return from line in this.RawData
			where line.Contains(":")
			select line.Split(':', StringSplitOptions.None)[0];
	}

	public bool IsList(string key)
	{
		bool flag = false;
		foreach (string text in this.RawData)
		{
			if (text.StartsWith(key + ":"))
			{
				flag = true;
			}
			else if (flag)
			{
				if (text.StartsWith(" - "))
				{
					return true;
				}
				if (!text.StartsWith("#"))
				{
					break;
				}
			}
		}
		return false;
	}

	public void Merge(ref YamlConfig toMerge)
	{
		string[] array = this.GetKeys().ToArray<string>();
		this.IsVirtual = true;
		foreach (string text in toMerge.GetKeys())
		{
			if (!array.Contains(text))
			{
				if (toMerge.IsList(text))
				{
					using (IEnumerator<string> enumerator2 = toMerge.StringListToText(text, toMerge.GetStringList(text)).GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							string text2 = enumerator2.Current;
							this.RawData.Append(text2);
						}
						continue;
					}
				}
				this.SetString(text, toMerge.GetRawString(text));
			}
		}
	}

	public bool GetBool(string key, bool def = false)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		bool flag;
		if (bool.TryParse(text, out flag))
		{
			return flag;
		}
		ServerConsole.AddLog(key + " has invalid value, " + text + " is not a valid bool!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "BOOL");
		return def;
	}

	public byte GetByte(string key, byte def = 0)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		byte b;
		if (byte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out b))
		{
			return b;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid byte!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "BYTE");
		return def;
	}

	public sbyte GetSByte(string key, sbyte def = 0)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		sbyte b;
		if (sbyte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out b))
		{
			return b;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid signed byte!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "SBYTE");
		return def;
	}

	public char GetChar(string key, char def = ' ')
	{
		string rawString = this.GetRawString(key);
		if (rawString == "default")
		{
			return def;
		}
		char c;
		if (char.TryParse(rawString, out c))
		{
			return c;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + rawString + " is not a valid char!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "CHAR");
		return def;
	}

	public decimal GetDecimal(string key, decimal def = 0m)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		decimal num;
		if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has invalid value, " + text + " is not a valid decimal!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "DECIMAL");
		return def;
	}

	public double GetDouble(string key, double def = 0.0)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		double num;
		if (double.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has invalid value, " + text + " is not a valid double!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "DOUBLE");
		return def;
	}

	public float GetFloat(string key, float def = 0f)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		float num;
		if (float.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has invalid value, " + text + " is not a valid float!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "FLOAT");
		return def;
	}

	public int GetInt(string key, int def = 0)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		int num;
		if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid integer!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "INT");
		return def;
	}

	public uint GetUInt(string key, uint def = 0U)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		uint num;
		if (uint.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid unsigned integer!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "UINT");
		return def;
	}

	public long GetLong(string key, long def = 0L)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		long num;
		if (long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid long!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "LONG");
		return def;
	}

	public ulong GetULong(string key, ulong def = 0UL)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		ulong num;
		if (ulong.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid unsigned long!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "ULONG");
		return def;
	}

	public short GetShort(string key, short def = 0)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		short num;
		if (short.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid short!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "SHORT");
		return def;
	}

	public ushort GetUShort(string key, ushort def = 0)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		ushort num;
		if (ushort.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
		{
			return num;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid unsigned short!", ConsoleColor.Gray, false);
		this.CommentInvalid(key, "USHORT");
		return def;
	}

	public string GetString(string key, string def = "")
	{
		string rawString = this.GetRawString(key);
		if (!(rawString == "default"))
		{
			return rawString;
		}
		return def;
	}

	public List<bool> GetBoolList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, bool>(bool.Parse)).ToList<bool>();
	}

	public List<byte> GetByteList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, byte>(byte.Parse)).ToList<byte>();
	}

	public List<sbyte> GetSByteList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, sbyte>(sbyte.Parse)).ToList<sbyte>();
	}

	public List<char> GetCharList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, char>(char.Parse)).ToList<char>();
	}

	public List<decimal> GetDecimalList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, decimal>(decimal.Parse)).ToList<decimal>();
	}

	public List<double> GetDoubleList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, double>(double.Parse)).ToList<double>();
	}

	public List<float> GetFloatList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, float>(float.Parse)).ToList<float>();
	}

	public List<int> GetIntList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, int>(int.Parse)).ToList<int>();
	}

	public List<uint> GetUIntList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, uint>(uint.Parse)).ToList<uint>();
	}

	public List<long> GetLongList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, long>(long.Parse)).ToList<long>();
	}

	public List<ulong> GetULongList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, ulong>(ulong.Parse)).ToList<ulong>();
	}

	public List<short> GetShortList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, short>(short.Parse)).ToList<short>();
	}

	public List<ushort> GetUShortList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(new Func<string, ushort>(ushort.Parse)).ToList<ushort>();
	}

	public List<string> GetStringList(string key)
	{
		List<string> list = new List<string>();
		this.GetStringCollection(key, list);
		return list;
	}

	public void GetStringCollection(string key, ICollection<string> collection)
	{
		bool flag = false;
		collection.Clear();
		foreach (string text in this.RawData)
		{
			if (text.StartsWith(key) && text.TrimEnd().EndsWith("[]", StringComparison.Ordinal))
			{
				break;
			}
			if (text.StartsWith(key + ":", StringComparison.Ordinal))
			{
				if (text.StartsWith(key + ": ", StringComparison.Ordinal))
				{
					string text2 = text.Substring(key.Length + 2);
					if (text2.Contains("[") && text2.Contains("]"))
					{
						List<string> list = collection as List<string>;
						if (list != null)
						{
							list.AddRange(YamlConfig.ParseCommaSeparatedString(text2));
							return;
						}
						foreach (string text3 in YamlConfig.ParseCommaSeparatedString(text2))
						{
							collection.Add(text3);
						}
						return;
					}
				}
				flag = true;
			}
			else if (flag)
			{
				if (text.StartsWith(" - ", StringComparison.Ordinal))
				{
					collection.Add(text.Substring(3).TrimEnd());
				}
				else if (!text.StartsWith("#", StringComparison.Ordinal))
				{
					break;
				}
			}
		}
	}

	private static readonly string[] _rolevars = new string[] { "color", "badge", "cover", "hidden", "kick_power", "required_kick_power" };

	private static readonly string[] _deprecatedconfigs = new string[] { "administrator_password" };

	private bool _afteradding;

	private bool _virtual;

	private string[] _rawDataUnfiltered;

	private string[] _rawData;

	private static readonly List<string> DataBuffer = new List<string>();

	public string Path;
}
