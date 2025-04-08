using System;
using System.IO;
using System.Linq;
using Cryptography;
using GameCore;
using Org.BouncyCastle.Crypto;
using UnityEngine;

public static class CentralServerKeyCache
{
	public static string ReadCache()
	{
		string text3;
		try
		{
			string appFolder = FileManager.GetAppFolder(true, false, "");
			string text = appFolder + "internal/KeyCache";
			string text2 = appFolder + "internal/KeySignatureCache";
			if (!File.Exists(text))
			{
				ServerConsole.AddLog("Central server public key not found in cache.", ConsoleColor.Gray, false);
				text3 = null;
			}
			else if (!File.Exists(text2))
			{
				ServerConsole.AddLog("Central server public key signature not found in cache.", ConsoleColor.Gray, false);
				text3 = null;
			}
			else
			{
				string[] array = FileManager.ReadAllLines(text);
				string[] array2 = FileManager.ReadAllLines(text2);
				if (array2.Length == 0)
				{
					ServerConsole.AddLog("Can't load central server public key from cache - empty signature.", ConsoleColor.Gray, false);
					text3 = null;
				}
				else
				{
					string text4 = array.Aggregate("", (string current, string line) => current + line + "\r\n").Trim();
					try
					{
						if (ECDSA.Verify(text4, array2[0], CentralServerKeyCache.MasterKey))
						{
							text3 = text4;
						}
						else
						{
							global::GameCore.Console.AddLog("Invalid signature of Central Server Key in cache!", Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
							text3 = null;
						}
					}
					catch (Exception ex)
					{
						if (ServerStatic.IsDedicated)
						{
							ServerConsole.AddLog("Can't load central server public key from cache - " + ex.Message, ConsoleColor.Gray, false);
						}
						else
						{
							global::GameCore.Console.AddLog("Can't load central server public key from cache - " + ex.Message, Color.magenta, false, global::GameCore.Console.ConsoleLogType.Log);
						}
						text3 = null;
					}
				}
			}
		}
		catch (Exception ex2)
		{
			ServerConsole.AddLog("Can't read public key cache - " + ex2.Message, ConsoleColor.Gray, false);
			text3 = null;
		}
		return text3;
	}

	public static void SaveCache(string key, string signature)
	{
		try
		{
			if (!ECDSA.Verify(key, signature, CentralServerKeyCache.MasterKey))
			{
				global::GameCore.Console.AddLog("Invalid signature of Central Server Key!", Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			}
			else
			{
				string appFolder = FileManager.GetAppFolder(true, false, "");
				string text = appFolder + "internal/KeyCache";
				if (!Directory.Exists(FileManager.GetAppFolder(true, false, "") + "internal/"))
				{
					Directory.CreateDirectory(FileManager.GetAppFolder(true, false, "") + "internal/");
				}
				if (File.Exists(text))
				{
					if (key == CentralServerKeyCache.ReadCache())
					{
						ServerConsole.AddLog("Key cache is up to date.", ConsoleColor.Gray, false);
						return;
					}
					File.Delete(text);
				}
				ServerConsole.AddLog("Updating key cache...", ConsoleColor.Gray, false);
				FileManager.WriteStringToFile(key, text);
				FileManager.WriteStringToFile(signature, appFolder + "internal/KeySignatureCache");
				ServerConsole.AddLog("Key cache updated.", ConsoleColor.Gray, false);
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog("Can't write public key cache - " + ex.Message, ConsoleColor.Gray, false);
		}
	}

	private const string CacheLocation = "internal/KeyCache";

	private const string CacheSignatureLocation = "internal/KeySignatureCache";

	private const string InternalDir = "internal/";

	internal static readonly AsymmetricKeyParameter MasterKey = ECDSA.PublicKeyFromString("-----BEGIN PUBLIC KEY-----\r\nMIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQAbL0YvrhVB2meqCq5XzjAJD8Ii0hb\r\nBHdIQ587N583cP8twjDhcITjZhBHJPJDuA85XdpgG04HwT0SD3WcAvoQXBUAUsG1\r\nLS9TR4urHwfgfroq4tH2HAQE6ZxFZeIFSglLO8nxySim4yKBj96HLG624lzKvzoD\r\nId+GOwjcd3XskOq9Dwc=\r\n-----END PUBLIC KEY-----");

	private const string MasterPublicKey = "-----BEGIN PUBLIC KEY-----\r\nMIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQAbL0YvrhVB2meqCq5XzjAJD8Ii0hb\r\nBHdIQ587N583cP8twjDhcITjZhBHJPJDuA85XdpgG04HwT0SD3WcAvoQXBUAUsG1\r\nLS9TR4urHwfgfroq4tH2HAQE6ZxFZeIFSglLO8nxySim4yKBj96HLG624lzKvzoD\r\nId+GOwjcd3XskOq9Dwc=\r\n-----END PUBLIC KEY-----";
}
