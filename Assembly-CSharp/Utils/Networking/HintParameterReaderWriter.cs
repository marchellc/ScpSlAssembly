using System;
using Hints;
using Mirror;
using UnityEngine;

namespace Utils.Networking
{
	public static class HintParameterReaderWriter
	{
		public static HintParameter ReadHintParameter(this NetworkReader reader)
		{
			byte b = reader.ReadByte();
			Func<NetworkReader, HintParameter> func;
			switch (b)
			{
			case 0:
				func = new Func<NetworkReader, HintParameter>(StringHintParameter.FromNetwork);
				break;
			case 1:
				func = new Func<NetworkReader, HintParameter>(TimespanHintParameter.FromNetwork);
				break;
			case 2:
				func = new Func<NetworkReader, HintParameter>(AmmoHintParameter.FromNetwork);
				break;
			case 3:
				func = new Func<NetworkReader, HintParameter>(ItemHintParameter.FromNetwork);
				break;
			case 4:
				func = new Func<NetworkReader, HintParameter>(ItemCategoryHintParameter.FromNetwork);
				break;
			case 5:
				func = new Func<NetworkReader, HintParameter>(ByteHintParameter.FromNetwork);
				break;
			case 6:
				func = new Func<NetworkReader, HintParameter>(SByteHintParameter.FromNetwork);
				break;
			case 7:
				func = new Func<NetworkReader, HintParameter>(ShortHintParameter.FromNetwork);
				break;
			case 8:
				func = new Func<NetworkReader, HintParameter>(UShortHintParameter.FromNetwork);
				break;
			case 9:
				func = new Func<NetworkReader, HintParameter>(IntHintParameter.FromNetwork);
				break;
			case 10:
				func = new Func<NetworkReader, HintParameter>(UIntHintParameter.FromNetwork);
				break;
			case 11:
				func = new Func<NetworkReader, HintParameter>(LongHintParameter.FromNetwork);
				break;
			case 12:
				func = new Func<NetworkReader, HintParameter>(ULongHintParameter.FromNetwork);
				break;
			case 13:
				func = new Func<NetworkReader, HintParameter>(FloatHintParameter.FromNetwork);
				break;
			case 14:
				func = new Func<NetworkReader, HintParameter>(DoubleHintParameter.FromNetwork);
				break;
			case 15:
				func = new Func<NetworkReader, HintParameter>(PackedLongHintParameter.FromNetwork);
				break;
			case 16:
				func = new Func<NetworkReader, HintParameter>(PackedULongHintParameter.FromNetwork);
				break;
			case 17:
				func = new Func<NetworkReader, HintParameter>(Scp330HintParameter.FromNetwork);
				break;
			default:
				Debug.LogWarning(string.Format("Received malformed hint parameter (type {0}).", b));
				return null;
			}
			return func(reader);
		}

		public static void WriteHintParameter(this NetworkWriter writer, HintParameter parameter)
		{
			if (parameter == null)
			{
				throw new ArgumentNullException("parameter");
			}
			HintParameterReaderWriter.HintParameterType hintParameterType;
			if (!(parameter is StringHintParameter))
			{
				if (!(parameter is TimespanHintParameter))
				{
					if (!(parameter is AmmoHintParameter))
					{
						if (!(parameter is ItemHintParameter))
						{
							if (!(parameter is ItemCategoryHintParameter))
							{
								if (!(parameter is ByteHintParameter))
								{
									if (!(parameter is SByteHintParameter))
									{
										if (!(parameter is ShortHintParameter))
										{
											if (!(parameter is UShortHintParameter))
											{
												if (!(parameter is IntHintParameter))
												{
													if (!(parameter is UIntHintParameter))
													{
														if (!(parameter is LongHintParameter))
														{
															if (!(parameter is ULongHintParameter))
															{
																if (!(parameter is FloatHintParameter))
																{
																	if (!(parameter is DoubleHintParameter))
																	{
																		if (!(parameter is PackedLongHintParameter))
																		{
																			if (!(parameter is PackedULongHintParameter))
																			{
																				if (!(parameter is Scp330HintParameter))
																				{
																					throw new ArgumentException("Hint parameter was of an unknown type. This type should be added to the pattern switch (needed for polymorphism to work).", "parameter");
																				}
																				hintParameterType = HintParameterReaderWriter.HintParameterType.Scp330Hint;
																			}
																			else
																			{
																				hintParameterType = HintParameterReaderWriter.HintParameterType.PackedULong;
																			}
																		}
																		else
																		{
																			hintParameterType = HintParameterReaderWriter.HintParameterType.PackedLong;
																		}
																	}
																	else
																	{
																		hintParameterType = HintParameterReaderWriter.HintParameterType.Double;
																	}
																}
																else
																{
																	hintParameterType = HintParameterReaderWriter.HintParameterType.Float;
																}
															}
															else
															{
																hintParameterType = HintParameterReaderWriter.HintParameterType.ULong;
															}
														}
														else
														{
															hintParameterType = HintParameterReaderWriter.HintParameterType.Long;
														}
													}
													else
													{
														hintParameterType = HintParameterReaderWriter.HintParameterType.UInt;
													}
												}
												else
												{
													hintParameterType = HintParameterReaderWriter.HintParameterType.Int;
												}
											}
											else
											{
												hintParameterType = HintParameterReaderWriter.HintParameterType.UShort;
											}
										}
										else
										{
											hintParameterType = HintParameterReaderWriter.HintParameterType.Short;
										}
									}
									else
									{
										hintParameterType = HintParameterReaderWriter.HintParameterType.SByte;
									}
								}
								else
								{
									hintParameterType = HintParameterReaderWriter.HintParameterType.Byte;
								}
							}
							else
							{
								hintParameterType = HintParameterReaderWriter.HintParameterType.ItemCategory;
							}
						}
						else
						{
							hintParameterType = HintParameterReaderWriter.HintParameterType.Item;
						}
					}
					else
					{
						hintParameterType = HintParameterReaderWriter.HintParameterType.Ammo;
					}
				}
				else
				{
					hintParameterType = HintParameterReaderWriter.HintParameterType.Timespan;
				}
			}
			else
			{
				hintParameterType = HintParameterReaderWriter.HintParameterType.Text;
			}
			writer.WriteByte((byte)hintParameterType);
			parameter.Serialize(writer);
		}

		public enum HintParameterType : byte
		{
			Text,
			Timespan,
			Ammo,
			Item,
			ItemCategory,
			Byte,
			SByte,
			Short,
			UShort,
			Int,
			UInt,
			Long,
			ULong,
			Float,
			Double,
			PackedLong,
			PackedULong,
			Scp330Hint
		}
	}
}
