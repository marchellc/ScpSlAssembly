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
	private static readonly string[] _rolevars = new string[6] { "color", "badge", "cover", "hidden", "kick_power", "required_kick_power" };

	private static readonly string[] _deprecatedconfigs = new string[1] { "administrator_password" };

	private bool _afteradding;

	private bool _virtual;

	private string[] _rawDataUnfiltered;

	private string[] _rawData;

	private static readonly List<string> DataBuffer = new List<string>();

	public string Path;

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
			}
			else
			{
				this._rawData = value;
			}
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
			if (value && !this._virtual)
			{
				this._virtual = true;
				this._rawDataUnfiltered = this.RawData;
			}
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
		if (!(configEntry is ConfigEntry<bool> configEntry2))
		{
			if (!(configEntry is ConfigEntry<byte> configEntry3))
			{
				if (!(configEntry is ConfigEntry<char> configEntry4))
				{
					if (!(configEntry is ConfigEntry<decimal> configEntry5))
					{
						if (!(configEntry is ConfigEntry<double> configEntry6))
						{
							if (!(configEntry is ConfigEntry<float> configEntry7))
							{
								if (!(configEntry is ConfigEntry<int> configEntry8))
								{
									if (!(configEntry is ConfigEntry<long> configEntry9))
									{
										if (!(configEntry is ConfigEntry<sbyte> configEntry10))
										{
											if (!(configEntry is ConfigEntry<short> configEntry11))
											{
												if (!(configEntry is ConfigEntry<string> configEntry12))
												{
													if (!(configEntry is ConfigEntry<uint> configEntry13))
													{
														if (!(configEntry is ConfigEntry<ulong> configEntry14))
														{
															if (!(configEntry is ConfigEntry<ushort> configEntry15))
															{
																if (!(configEntry is ConfigEntry<List<bool>> configEntry16))
																{
																	if (!(configEntry is ConfigEntry<List<byte>> configEntry17))
																	{
																		if (!(configEntry is ConfigEntry<List<char>> configEntry18))
																		{
																			if (!(configEntry is ConfigEntry<List<decimal>> configEntry19))
																			{
																				if (!(configEntry is ConfigEntry<List<double>> configEntry20))
																				{
																					if (!(configEntry is ConfigEntry<List<float>> configEntry21))
																					{
																						if (!(configEntry is ConfigEntry<List<int>> configEntry22))
																						{
																							if (!(configEntry is ConfigEntry<List<long>> configEntry23))
																							{
																								if (!(configEntry is ConfigEntry<List<sbyte>> configEntry24))
																								{
																									if (!(configEntry is ConfigEntry<List<short>> configEntry25))
																									{
																										if (!(configEntry is ConfigEntry<List<string>> configEntry26))
																										{
																											if (!(configEntry is ConfigEntry<List<uint>> configEntry27))
																											{
																												if (!(configEntry is ConfigEntry<List<ulong>> configEntry28))
																												{
																													if (!(configEntry is ConfigEntry<List<ushort>> configEntry29))
																													{
																														if (!(configEntry is ConfigEntry<Dictionary<string, string>> configEntry30))
																														{
																															if (!(configEntry is ConfigEntry<Scp914Mode> configEntry31))
																															{
																																throw new Exception("Config type unsupported (Config: Key = \"" + (configEntry.Key ?? "Null") + "\" Type = \"" + (configEntry.ValueType.FullName ?? "Null") + "\" Name = \"" + (configEntry.Name ?? "Null") + "\" Description = \"" + (configEntry.Description ?? "Null") + "\").");
																															}
																															string text = this.GetString(configEntry31.Key);
																															if (text == "default" || !Enum.TryParse<Scp914Mode>(text, out var result))
																															{
																																configEntry31.Value = configEntry31.Default;
																															}
																															else
																															{
																																configEntry31.Value = result;
																															}
																														}
																														else
																														{
																															configEntry30.Value = this.GetStringDictionary(configEntry30.Key);
																															if (configEntry30.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry30.Key), "default", StringComparison.OrdinalIgnoreCase))
																															{
																																configEntry30.Value = configEntry30.Default;
																															}
																														}
																													}
																													else
																													{
																														configEntry29.Value = this.GetUShortList(configEntry29.Key);
																														if (configEntry29.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry29.Key), "default", StringComparison.OrdinalIgnoreCase))
																														{
																															configEntry29.Value = configEntry29.Default;
																														}
																													}
																												}
																												else
																												{
																													configEntry28.Value = this.GetULongList(configEntry28.Key);
																													if (configEntry28.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry28.Key), "default", StringComparison.OrdinalIgnoreCase))
																													{
																														configEntry28.Value = configEntry28.Default;
																													}
																												}
																											}
																											else
																											{
																												configEntry27.Value = this.GetUIntList(configEntry27.Key);
																												if (configEntry27.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry27.Key), "default", StringComparison.OrdinalIgnoreCase))
																												{
																													configEntry27.Value = configEntry27.Default;
																												}
																											}
																										}
																										else
																										{
																											configEntry26.Value = this.GetStringList(configEntry26.Key);
																											if (configEntry26.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry26.Key), "default", StringComparison.OrdinalIgnoreCase))
																											{
																												configEntry26.Value = configEntry26.Default;
																											}
																										}
																									}
																									else
																									{
																										configEntry25.Value = this.GetShortList(configEntry25.Key);
																										if (configEntry25.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry25.Key), "default", StringComparison.OrdinalIgnoreCase))
																										{
																											configEntry25.Value = configEntry25.Default;
																										}
																									}
																								}
																								else
																								{
																									configEntry24.Value = this.GetSByteList(configEntry24.Key);
																									if (configEntry24.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry24.Key), "default", StringComparison.OrdinalIgnoreCase))
																									{
																										configEntry24.Value = configEntry24.Default;
																									}
																								}
																							}
																							else
																							{
																								configEntry23.Value = this.GetLongList(configEntry23.Key);
																								if (configEntry23.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry23.Key), "default", StringComparison.OrdinalIgnoreCase))
																								{
																									configEntry23.Value = configEntry23.Default;
																								}
																							}
																						}
																						else
																						{
																							configEntry22.Value = this.GetIntList(configEntry22.Key);
																							if (configEntry22.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry22.Key), "default", StringComparison.OrdinalIgnoreCase))
																							{
																								configEntry22.Value = configEntry22.Default;
																							}
																						}
																					}
																					else
																					{
																						configEntry21.Value = this.GetFloatList(configEntry21.Key);
																						if (configEntry21.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry21.Key), "default", StringComparison.OrdinalIgnoreCase))
																						{
																							configEntry21.Value = configEntry21.Default;
																						}
																					}
																				}
																				else
																				{
																					configEntry20.Value = this.GetDoubleList(configEntry20.Key);
																					if (configEntry20.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry20.Key), "default", StringComparison.OrdinalIgnoreCase))
																					{
																						configEntry20.Value = configEntry20.Default;
																					}
																				}
																			}
																			else
																			{
																				configEntry19.Value = this.GetDecimalList(configEntry19.Key);
																				if (configEntry19.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry19.Key), "default", StringComparison.OrdinalIgnoreCase))
																				{
																					configEntry19.Value = configEntry19.Default;
																				}
																			}
																		}
																		else
																		{
																			configEntry18.Value = this.GetCharList(configEntry18.Key);
																			if (configEntry18.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry18.Key), "default", StringComparison.OrdinalIgnoreCase))
																			{
																				configEntry18.Value = configEntry18.Default;
																			}
																		}
																	}
																	else
																	{
																		configEntry17.Value = this.GetByteList(configEntry17.Key);
																		if (configEntry17.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry17.Key), "default", StringComparison.OrdinalIgnoreCase))
																		{
																			configEntry17.Value = configEntry17.Default;
																		}
																	}
																}
																else
																{
																	configEntry16.Value = this.GetBoolList(configEntry16.Key);
																	if (configEntry16.Value.Count <= 0 && string.Equals(this.GetRawString(configEntry16.Key), "default", StringComparison.OrdinalIgnoreCase))
																	{
																		configEntry16.Value = configEntry16.Default;
																	}
																}
															}
															else
															{
																configEntry15.Value = this.GetUShort(configEntry15.Key, configEntry15.Default);
															}
														}
														else
														{
															configEntry14.Value = this.GetULong(configEntry14.Key, configEntry14.Default);
														}
													}
													else
													{
														configEntry13.Value = this.GetUInt(configEntry13.Key, configEntry13.Default);
													}
												}
												else
												{
													configEntry12.Value = this.GetString(configEntry12.Key, configEntry12.Default);
												}
											}
											else
											{
												configEntry11.Value = this.GetShort(configEntry11.Key, configEntry11.Default);
											}
										}
										else
										{
											configEntry10.Value = this.GetSByte(configEntry10.Key, configEntry10.Default);
										}
									}
									else
									{
										configEntry9.Value = this.GetLong(configEntry9.Key, configEntry9.Default);
									}
								}
								else
								{
									configEntry8.Value = this.GetInt(configEntry8.Key, configEntry8.Default);
								}
							}
							else
							{
								configEntry7.Value = this.GetFloat(configEntry7.Key, configEntry7.Default);
							}
						}
						else
						{
							configEntry6.Value = this.GetDouble(configEntry6.Key, configEntry6.Default);
						}
					}
					else
					{
						configEntry5.Value = this.GetDecimal(configEntry5.Key, configEntry5.Default);
					}
				}
				else
				{
					configEntry4.Value = this.GetChar(configEntry4.Key, configEntry4.Default);
				}
			}
			else
			{
				configEntry3.Value = this.GetByte(configEntry3.Key, configEntry3.Default);
			}
		}
		else
		{
			configEntry2.Value = this.GetBool(configEntry2.Key, configEntry2.Default);
		}
	}

	private static string[] Filter(IEnumerable<string> lines)
	{
		return lines.Where((string line) => !string.IsNullOrEmpty(line) && !line.StartsWith("#") && (line.StartsWith(" - ") || line.Contains(':'))).ToArray();
	}

	public void LoadConfigFile(string path)
	{
		if (!string.IsNullOrEmpty(path))
		{
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
	}

	private static void RemoveDeprecated(string path)
	{
		List<string> list = FileManager.ReadAllLinesList(path);
		for (int num = list.Count - 1; num >= 0; num--)
		{
			for (int i = 0; i < YamlConfig._deprecatedconfigs.Length; i++)
			{
				if (list[num].StartsWith(YamlConfig._deprecatedconfigs[i] + ":") && (num == 0 || list[num - 1] != "#REMOVED FROM GAME - REDUNDANT"))
				{
					list.Insert(num, "#REMOVED FROM GAME - REDUNDANT");
				}
			}
		}
		FileManager.WriteToFile(list, path);
	}

	private static void AddMissingPerms(string path, ref bool _afteradding)
	{
		string[] perms = YamlConfig.GetStringList("Permissions", path).ToArray();
		string[] array = EnumUtils<PlayerPermissions>.Names.ToArray();
		if (perms.Length == array.Length)
		{
			return;
		}
		List<string> collection = (from permtype in array
			select new
			{
				permtype = permtype,
				inconfig = perms.Any((string perm) => perm.StartsWith(permtype))
			} into t
			where !t.inconfig
			select " - " + t.permtype + ": []").ToList();
		List<string> list = FileManager.ReadAllLinesList(path);
		for (int num = 0; num < list.Count; num++)
		{
			if (list[num] == "Permissions:")
			{
				list.InsertRange(num + 1, collection);
			}
		}
		FileManager.WriteToFile(list, path);
		_afteradding = true;
	}

	private static void AddMissingRoleVars(string path)
	{
		string time = TimeBehaviour.FormatTime("yyyy/MM/dd HH:mm:ss");
		List<string> stringList = YamlConfig.GetStringList("Roles", path);
		List<string> list = ListPool<string>.Shared.Rent();
		string config = FileManager.ReadAllText(path);
		foreach (string role in stringList)
		{
			list.AddRange(from rolevar in YamlConfig._rolevars
				where !config.Contains(role + "_" + rolevar + ":")
				select role + "_" + rolevar + ": default");
		}
		if (list.Count > 0)
		{
			YamlConfig.Write(list, path, ref time);
		}
		ListPool<string>.Shared.Return(list);
	}

	private static void AddMissingTemplateKeys(string templatepath, string path, ref bool _afteradding)
	{
		string time = TimeBehaviour.FormatTime("yyyy/MM/dd HH:mm:ss");
		string text = FileManager.ReadAllText(path);
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
		foreach (string item in list2)
		{
			if (!text.Contains(item))
			{
				list3.Add(item + " default");
			}
		}
		ListPool<string>.Shared.Return(list2);
		YamlConfig.Write(list3, path, ref time);
		ListPool<string>.Shared.Return(list3);
		foreach (string item2 in list)
		{
			if (text.Contains(item2))
			{
				continue;
			}
			bool flag = false;
			List<string> list4 = new List<string> { "#LIST", item2 };
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (text2.StartsWith(item2) && text2.EndsWith("[]"))
				{
					list4.Clear();
					list4.AddRange(new string[2] { "#LIST - [] equals to empty", text2 });
					break;
				}
				if (text2.StartsWith(item2))
				{
					flag = true;
				}
				else if (flag)
				{
					if (text2.StartsWith(" - "))
					{
						list4.Add(text2);
					}
					else if (!text2.StartsWith("#"))
					{
						break;
					}
				}
			}
			YamlConfig.Write(list4, path, ref time);
		}
		ListPool<string>.Shared.Return(list);
		_afteradding = true;
	}

	private static void Write(IEnumerable<string> text, string path, ref string time)
	{
		string[] array = text.ToArray();
		if (array.Length != 0)
		{
			YamlConfig.Write(string.Join("\r\n", array), path, ref time);
		}
	}

	private static void Write(string text, string path, ref string time)
	{
		using StreamWriter streamWriter = File.AppendText(path);
		streamWriter.Write("\r\n\r\n#ADDED BY CONFIG VALIDATOR - " + time + " Game version: " + GameCore.Version.VersionString + "\r\n" + text);
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
			FileManager.WriteToFile(array, path);
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
			FileManager.WriteToFile(this._rawDataUnfiltered, this.Path);
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
		string[] rawData = this.RawData;
		foreach (string text in rawData)
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
		if (!this.TryGetString(key, out var value))
		{
			return "default";
		}
		return value;
	}

	public void SetString(string key, string value = null)
	{
		this.Reload();
		int num = 0;
		List<string> list = null;
		for (int i = 0; i < this._rawDataUnfiltered.Length; i++)
		{
			if (this._rawDataUnfiltered[i].StartsWith(key + ": "))
			{
				if (value == null)
				{
					list = this._rawDataUnfiltered.ToList();
					list.RemoveAt(i);
					num = 2;
				}
				else
				{
					this._rawDataUnfiltered[i] = key + ": " + value;
					num = 1;
				}
				break;
			}
		}
		if (this.IsVirtual)
		{
			return;
		}
		switch (num)
		{
		case 0:
			list = this._rawDataUnfiltered.ToList();
			list.Insert(list.Count, key + ": " + value);
			FileManager.WriteToFile(list, this.Path);
			break;
		case 1:
			FileManager.WriteToFile(this._rawDataUnfiltered, this.Path);
			break;
		case 2:
			if (list != null)
			{
				FileManager.WriteToFile(list, this.Path);
			}
			break;
		}
		this.Reload();
	}

	private static List<string> GetStringList(string key, string path)
	{
		bool flag = false;
		List<string> list = new List<string>();
		string[] array = FileManager.ReadAllLines(path);
		foreach (string text in array)
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
					return YamlConfig.ParseCommaSeparatedString(text2).ToList();
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
			else
			{
				if (!flag)
				{
					continue;
				}
				if (value != null && text == " - " + value)
				{
					if (newValue == null)
					{
						list = this._rawDataUnfiltered.ToList();
						list.RemoveAt(i);
						num = 2;
					}
					else
					{
						this._rawDataUnfiltered[i] = " - " + newValue;
						num = 1;
					}
					break;
				}
				if (!text.StartsWith(" - ") && !text.StartsWith("#"))
				{
					if (value != null)
					{
						list = this._rawDataUnfiltered.ToList();
						list.Insert(i, " - " + newValue);
						num = 2;
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
		case 1:
			FileManager.WriteToFile(this._rawDataUnfiltered, this.Path);
			break;
		case 2:
			if (list != null)
			{
				FileManager.WriteToFile(list, this.Path);
			}
			break;
		}
		this.Reload();
	}

	public IEnumerable<string> StringListToText(string key, List<string> list)
	{
		yield return key + ":";
		foreach (string item in list)
		{
			yield return " - " + item;
		}
	}

	public Dictionary<string, string> GetStringDictionary(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (string item in YamlConfig.DataBuffer)
		{
			if (!item.Contains(": "))
			{
				ServerConsole.AddLog("Invalid entry \"" + item + "\" in dictionary " + key + " in the config file - missing \": \".", ConsoleColor.Red);
				continue;
			}
			int num = item.IndexOf(": ", StringComparison.Ordinal);
			string text = item.Substring(0, num);
			if (!dictionary.ContainsKey(text))
			{
				dictionary.Add(text, item.Substring(num + 2));
				continue;
			}
			ServerConsole.AddLog("Ignoring duplicated subkey " + text + " in dictionary " + key + " in the config file.", ConsoleColor.Yellow);
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
			else
			{
				if (!flag)
				{
					continue;
				}
				if (text.StartsWith(" - " + subkey + ": "))
				{
					if (value == null)
					{
						list = this._rawDataUnfiltered.ToList();
						list.RemoveAt(i);
						num = 2;
					}
					else
					{
						this._rawDataUnfiltered[i] = " - " + subkey + ": " + value;
						num = 1;
					}
					break;
				}
				if (!text.StartsWith(" - ") && !text.StartsWith("#"))
				{
					if (value != null)
					{
						list = this._rawDataUnfiltered.ToList();
						list.Insert(i, " - " + subkey + ": " + value);
						num = 2;
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
			list = this._rawDataUnfiltered.ToList();
			list.Insert(list.Count, " - " + subkey + ": " + value);
			FileManager.WriteToFile(list, this.Path);
			break;
		case 1:
			FileManager.WriteToFile(this._rawDataUnfiltered, this.Path);
			break;
		case 2:
			if (list != null)
			{
				FileManager.WriteToFile(list, this.Path);
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
		return data.Split(',');
	}

	public IEnumerable<string> GetKeys()
	{
		return from line in this.RawData
			where line.Contains(":")
			select line.Split(':')[0];
	}

	public bool IsList(string key)
	{
		bool flag = false;
		string[] rawData = this.RawData;
		foreach (string text in rawData)
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
		string[] array = this.GetKeys().ToArray();
		this.IsVirtual = true;
		foreach (string key in toMerge.GetKeys())
		{
			if (array.Contains<string>(key))
			{
				continue;
			}
			if (toMerge.IsList(key))
			{
				foreach (string item in toMerge.StringListToText(key, toMerge.GetStringList(key)))
				{
					this.RawData.Append(item);
				}
			}
			else
			{
				this.SetString(key, toMerge.GetRawString(key));
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
		if (bool.TryParse(text, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has invalid value, " + text + " is not a valid bool!");
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
		if (byte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid byte!");
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
		if (sbyte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid signed byte!");
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
		if (char.TryParse(rawString, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + rawString + " is not a valid char!");
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
		if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has invalid value, " + text + " is not a valid decimal!");
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
		if (double.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has invalid value, " + text + " is not a valid double!");
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
		if (float.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has invalid value, " + text + " is not a valid float!");
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
		if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid integer!");
		this.CommentInvalid(key, "INT");
		return def;
	}

	public uint GetUInt(string key, uint def = 0u)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		if (uint.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid unsigned integer!");
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
		if (long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid long!");
		this.CommentInvalid(key, "LONG");
		return def;
	}

	public ulong GetULong(string key, ulong def = 0uL)
	{
		string text = this.GetRawString(key).ToLower();
		if (text == "default")
		{
			return def;
		}
		if (ulong.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid unsigned long!");
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
		if (short.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid short!");
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
		if (ushort.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
		{
			return result;
		}
		ServerConsole.AddLog(key + " has an invalid value, " + text + " is not a valid unsigned short!");
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
		return YamlConfig.DataBuffer.Select(bool.Parse).ToList();
	}

	public List<byte> GetByteList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(byte.Parse).ToList();
	}

	public List<sbyte> GetSByteList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(sbyte.Parse).ToList();
	}

	public List<char> GetCharList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(char.Parse).ToList();
	}

	public List<decimal> GetDecimalList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(decimal.Parse).ToList();
	}

	public List<double> GetDoubleList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(double.Parse).ToList();
	}

	public List<float> GetFloatList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(float.Parse).ToList();
	}

	public List<int> GetIntList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(int.Parse).ToList();
	}

	public List<uint> GetUIntList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(uint.Parse).ToList();
	}

	public List<long> GetLongList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(long.Parse).ToList();
	}

	public List<ulong> GetULongList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(ulong.Parse).ToList();
	}

	public List<short> GetShortList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(short.Parse).ToList();
	}

	public List<ushort> GetUShortList(string key)
	{
		this.GetStringCollection(key, YamlConfig.DataBuffer);
		return YamlConfig.DataBuffer.Select(ushort.Parse).ToList();
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
		string[] rawData = this.RawData;
		foreach (string text in rawData)
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
						if (collection is List<string> list)
						{
							list.AddRange(YamlConfig.ParseCommaSeparatedString(text2));
							break;
						}
						string[] array = YamlConfig.ParseCommaSeparatedString(text2);
						foreach (string item in array)
						{
							collection.Add(item);
						}
						break;
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
}
