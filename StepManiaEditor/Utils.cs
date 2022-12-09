﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Fumen;
using Fumen.Converters;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StepManiaEditor
{
	public class Utils
	{
		// TODO: Rename / Reorganize. Currently dumping a lot of rendering-related constants in here.

		public const int MaxLogFiles = 10;

		public const uint UITempoColorRGBA = 0x8A297A79;			// yellow
		public const uint UITimeSignatureColorRGBA = 0x8A297A29;	// green
		public const uint UIStopColorRGBA = 0x8A29297A;				// red
		public const uint UIDelayColorRGBA = 0x8A295E7A;			// light orange
		public const uint UIWarpColorRGBA = 0x8A7A7929;				// cyan
		public const uint UIScrollsColorRGBA = 0x8A7A2929;			// blue
		public const uint UISpeedsColorRGBA = 0x8A7A294D;			// purple

		public const uint UITicksColorRGBA = 0x8A295E7A;			// orange
		public const uint UIMultipliersColorRGBA = 0x8A297A63;		// lime
		public const uint UIFakesColorRGBA = 0x8A29467A;			// dark orange
		public const uint UILabelColorRGBA = 0x8A68297A;			// pink

		public const uint UIPreviewColorRGBA = 0x8A7A7A7A;          // grey

		public const uint UIDifficultyBeginnerColorRGBA = 0xFF808040;
		public const uint UIDifficultyEasyColorRGBA = 0xFF4D804D;
		public const uint UIDifficultyMediumColorRGBA = 0xFF408080;
		public const uint UIDifficultyHardColorRGBA = 0xFF404080;
		public const uint UIDifficultyChallengeColorRGBA = 0xFF804080;
		public const uint UIDifficultyEditColorRGBA = 0xFF807D7B;

		/// <summary>
		/// Color for sparse area of waveform. BGR565. Red.
		/// The waveform-color shader expects this color to perform recoloring.
		/// </summary>
		public const ushort WaveFormColorSparse = 0xF800;
		/// <summary>
		/// Color for dense area of waveform. BGR565. Green.
		/// The waveform-color shader expects this color to perform recoloring.
		/// </summary>
		public const ushort WaveFormColorDense = 0x7E0;

		public const int MarkerTextureWidth = 128;

		public const int WaveFormTextureWidth = 1024;

		public const float BeatMarkerScaleToStartingFading = 0.15f;
		public const float BeatMarkerMinScale = 0.10f;
		public const float MeasureMarkerScaleToStartingFading = 0.10f;
		public const float MeasureMarkerMinScale = 0.05f;
		public const float MeasureNumberScaleToStartFading = 0.20f;
		public const float MeasureNumberMinScale = 0.10f;
		public const float MiscEventScaleToStartingFading = 0.05f;
		public const float MiscEventMinScale = 0.04f;

		public const float ActiveEditEventAlpha = 0.8f;

		public const float HelpWidth = 18.0f;
		public const int CloseWidth = 18;

		public const int BackgroundWidth = 640;
		public const int BackgroundHeight = 480;
		public const int BannerWidth = 418;
		public const int BannerHeight = 164;
		public const int CDTitleWidth = 164;
		public const int CDTitleHeight = 164;

		public const int MaxMarkersToDraw = 256;
		public const int MaxEventsToDraw = 2048;

		public const int MiniMapMaxNotesToDraw = 6144;
		public const int MiniMapYPaddingFromTop = 30;		// This takes into account a 20 pixel padding for the main menu bar.
		public const int MiniMapYPaddingFromBottom = 10;
		public const int ChartPositionUIYPAddingFromBottom = 10;

		public const string TextureIdMeasureMarker = "measure_marker";
		public const string TextureIdBeatMarker = "beat_marker";
		public const string TextureIdRegionRect = "region_rect";

		public static Color StopRegionColor = new Color(0x7A, 0x29, 0x29, 0x7F);
		public static Color DelayRegionColor = new Color(0x7A, 0x5E, 0x29, 0x7F);
		public static Color FakeRegionColor = new Color(0x7A, 0x46, 0x29, 0x7F);
		public static Color WarpRegionColor = new Color(0x29, 0x79, 0x7A, 0x7F);
		public static Color PreviewRegionColor = new Color(0x7A, 0x7A, 0x7A, 0x7F);

		public static readonly string[] ExpectedAudioFormats = { "mp3", "oga", "ogg", "wav" };
		public static readonly string[] ExpectedImageFormats = { "bmp", "gif", "jpeg", "jpg", "png", "tif", "tiff", "webp" };
		public static readonly string[] ExpectedVideoFormats = { "avi", "f4v", "flv", "mkv", "mp4", "mpeg", "mpg", "mov", "ogv", "webm", "wmv" };
		public static readonly string[] ExpectedLyricsFormats = { "lrc" };

		private static readonly Dictionary<Type, string[]> EnumStringsCacheByType = new Dictionary<Type, string[]>();
		private static readonly Dictionary<string, string[]> EnumStringsCacheByCustomKey = new Dictionary<string, string[]>();

		private static List<bool> EnabledStack = new List<bool>();

		public enum HorizontalAlignment
		{
			Left,
			Center,
			Right
		}

		public enum VerticalAlignment
		{
			Top,
			Center,
			Bottom
		}

		public enum TextureLayoutMode
		{
			/// <summary>
			/// Draw the texture at its original size, centered in the destination area. If the texture is larger
			/// than the destination area then it will be cropped as needed to fit. If it is smaller then it will
			/// be rendered smaller.
			/// </summary>
			OriginalSize,

			/// <summary>
			/// The texture will fill the destination area exactly. It will shrink or grow as needed and the aspect
			/// ratio will change to match the destination.
			/// </summary>
			Stretch,

			/// <summary>
			/// Maintain the texture's original aspect ratio and fill the destination area. If the texture aspect
			/// ratio does not match the destination area's aspect ratio, then the texture will be cropped.
			/// </summary>
			Fill,

			/// <summary>
			/// Letterbox or pillarbox as needed such that texture's original aspect ratio is maintained and it fills
			/// the destination area as much as possible.
			/// </summary>
			Box
		}

		public static uint ColorRGBAInterpolate(uint startColor, uint endColor, float endPercent)
		{
			var startPercent = 1.0f - endPercent;
			return (uint)((startColor & 0xFF) * startPercent + (endColor & 0xFF) * endPercent)
			       | ((uint)(((startColor >> 8) & 0xFF) * startPercent + ((endColor >> 8) & 0xFF) * endPercent) << 8)
			       | ((uint)(((startColor >> 16) & 0xFF) * startPercent + ((endColor >> 16) & 0xFF) * endPercent) << 16)
			       | ((uint)(((startColor >> 24) & 0xFF) * startPercent + ((endColor >> 24) & 0xFF) * endPercent) << 24);
		}

		public static uint ColorRGBAInterpolateBGR(uint startColor, uint endColor, float endPercent)
		{
			var startPercent = 1.0f - endPercent;
			return (uint)((startColor & 0xFF) * startPercent + (endColor & 0xFF) * endPercent)
			       | ((uint)(((startColor >> 8) & 0xFF) * startPercent + ((endColor >> 8) & 0xFF) * endPercent) << 8)
			       | ((uint)(((startColor >> 16) & 0xFF) * startPercent + ((endColor >> 16) & 0xFF) * endPercent) << 16)
			       | (endColor & 0xFF000000);
		}

		public static uint ColorRGBAMultiply(uint color, float multiplier)
		{
			return (uint)(Math.Min((color & 0xFF) * multiplier, byte.MaxValue))
			       | ((uint)Math.Min(((color >> 8) & 0xFF) * multiplier, byte.MaxValue) << 8)
			       | ((uint)Math.Min(((color >> 16) & 0xFF) * multiplier, byte.MaxValue) << 16)
			       | (color & 0xFF000000);
		}

		public static ushort ToBGR565(float r, float g, float b)
		{
			return (ushort)(((ushort)(r * 31) << 11) + ((ushort)(g * 63) << 5) + (ushort)(b * 31));
		}

		public static ushort ToBGR565(Color c)
		{
			return ToBGR565((float)c.R / byte.MaxValue, (float)c.G / byte.MaxValue, (float)c.B / byte.MaxValue);
		}

		public static ushort ToBGR565(uint RGBA)
		{
			return ToBGR565(
				(byte)((RGBA & 0x00FF0000) >> 16) / (float)byte.MaxValue,
				(byte)((RGBA & 0x0000FF00) >> 8) / (float)byte.MaxValue,
				(byte)(RGBA & 0x000000FF) / (float)byte.MaxValue);
		}

		public static uint ToRGBA(float r, float g, float b, float a)
		{
			return (((uint)(byte)(a * byte.MaxValue)) << 24)
				+ (((uint)(byte)(b * byte.MaxValue)) << 16)
				+ (((uint)(byte)(g * byte.MaxValue)) << 8)
				+ ((byte)(r * byte.MaxValue));
		}

		public static (float, float, float, float) ToFloats(uint RGBA)
		{
			return ((byte)(RGBA & 0x000000FF) / (float)byte.MaxValue,
				(byte)((RGBA & 0x0000FF00) >> 8) / (float)byte.MaxValue,
				(byte)((RGBA & 0x00FF0000) >> 16) / (float)byte.MaxValue,
				(byte)((RGBA & 0xFF000000) >> 24) / (float)byte.MaxValue);
		}

		public static Vector2 GetDrawPos(
			SpriteFont font,
			string text,
			Vector2 anchorPos,
			float scale,
			HorizontalAlignment hAlign = HorizontalAlignment.Left,
			VerticalAlignment vAlign = VerticalAlignment.Top)
		{
			var x = anchorPos.X;
			var y = anchorPos.Y;
			var size = font.MeasureString(text);
			switch (hAlign)
			{
				case HorizontalAlignment.Center:
					x -= size.X * 0.5f * scale;
					break;
				case HorizontalAlignment.Right:
					x -= size.X * scale;
					break;
			}
			switch (vAlign)
			{
				case VerticalAlignment.Center:
					y -= size.Y * 0.5f * scale;
					break;
				case VerticalAlignment.Bottom:
					y -= size.Y * scale;
					break;
			}
			return new Vector2(x, y);
		}

		public static uint GetColorForDifficultyType(SMCommon.ChartDifficultyType difficulty)
		{
			switch (difficulty)
			{
				case SMCommon.ChartDifficultyType.Beginner: return UIDifficultyBeginnerColorRGBA;
				case SMCommon.ChartDifficultyType.Easy: return UIDifficultyEasyColorRGBA;
				case SMCommon.ChartDifficultyType.Medium: return UIDifficultyMediumColorRGBA;
				case SMCommon.ChartDifficultyType.Hard: return UIDifficultyHardColorRGBA;
				case SMCommon.ChartDifficultyType.Challenge: return UIDifficultyChallengeColorRGBA;
				case SMCommon.ChartDifficultyType.Edit: return UIDifficultyEditColorRGBA;
			}
			return UIDifficultyEditColorRGBA;
		}

		/// <summary>
		/// Gets the average color of the given texture.
		/// The average color is calculated from the texture's HSV values.
		/// Hue is averaged.
		/// Value and saturation are averaged with root mean square.
		/// </summary>
		public static uint GetTextureColor(Texture2D texture)
		{
			var colorData = GetRGBAColorData(texture);
			double hueXSum = 0.0f;
			double hueYSum = 0.0f;
			double saturationSumOfSquares = 0.0f;
			double valueSumOfSquares = 0.0f;
			float r, g, b, a, h, s, v;
			double hx, hy;
			foreach (var color in colorData)
			{
				// Convert the color to HSV values.
				(r, g, b, a) = ToFloats(color);
				(h, s, v) = RgbToHsv(r, g, b);

				saturationSumOfSquares += (s * s);
				valueSumOfSquares += (v * v);

				// Hue values are angles around a circle. We need to determine the average x and y
				// and then compute the average angle from those values.
				hx = Math.Cos(h);
				hy = Math.Sin(h);
				hueXSum += hx;
				hueYSum += hy;
			}

			// Determine the average hue by determining the angle of the average hue x and y values.
			hx = (hueXSum / colorData.Length);
			hy = (hueYSum / colorData.Length);
			double avgHue = Math.Atan2(hy, hx);
			if (avgHue < 0.0)
				avgHue = 2.0 * Math.PI + avgHue;

			// Convert back to RGB.
			(r, g, b) = HsvToRgb(
				(float)avgHue,
				(float)Math.Sqrt(saturationSumOfSquares / colorData.Length),
				(float)Math.Sqrt(valueSumOfSquares / colorData.Length));

			return ToRGBA(r, g, b, 1.0f);
		}

		public static uint[] GetRGBAColorData(Texture2D texture)
		{
			var data = new uint[texture.Width * texture.Height];
			switch (texture.Format)
			{
				case SurfaceFormat.Color:
				{
					texture.GetData(data);
					break;
				}
				default:
					break;
			}
			return data;
		}

		/// <summary>
		/// Given a color represented by red, green, and blue floating point values ranging from 0.0f to 1.0f,
		/// return the hue, saturation, and value of the color.
		/// Hue is represented as a degree in radians between 0.0 and 2*pi.
		/// For pure grey colors the returned hue will be 0.0.
		/// </summary>
		public static (float, float, float) RgbToHsv(float r, float g, float b)
		{
			float h = 0.0f, s, v;
			var min = Math.Min(Math.Min(r, g), b);
			var max = Math.Max(Math.Max(r, g), b);
			
			v = max;
			s = max.FloatEquals(0.0f) ? 0.0f : (max - min) / max;
			if (!s.FloatEquals(0.0f))
			{
				var d = max - min;
				if (r.FloatEquals(max))
				{
					h = (g - b) / d;
				}
				else if (g.FloatEquals(max))
				{
					h = 2 + (b - r) / d;
				}
				else
				{
					h = 4 + (r - g) / d;
				}
				h *= (float)(Math.PI / 3.0f);
				if (h < 0.0f)
				{
					h += (float)(2.0f * Math.PI);
				}
			}

			return (h, s, v);
		}

		/// <summary>
		/// Given a color represented by hue, saturation, and value return the red, blue, and green
		/// values of the color. The saturation and value parameters are expected to be in the range
		/// of 0.0 to 1.0. The hue value is expected to be between 0.0 and 2*pi. The returned color
		/// values will be between 0.0 and 1.0.
		/// </summary>
		public static (float, float, float) HsvToRgb(float h, float s, float v)
		{
			float r, g, b;

			if (s.FloatEquals(0.0f))
			{
				r = v;
				g = v;
				b = v;
			}
			else
			{
				if (h.FloatEquals((float)(Math.PI * 2.0f)))
					h = 0.0f;
				else
					h = (float)((h * 3.0f)/Math.PI);
				var sextant = (float)Math.Floor(h);
				var f = h - sextant;
				var p = v * (1.0f - s);
				var q = v * (1.0f - (s * f));
				var t = v * (1.0f - (s * (1.0f - f)));
				switch (sextant)
				{
					default:
					case 0: r = v; g = t; b = p; break;
					case 1: r = q; g = v; b = p; break;
					case 2: r = p; g = v; b = t; break;
					case 3: r = p; g = q; b = v; break;
					case 4: r = t; g = p; b = v; break;
					case 5: r = v; g = p; b = q; break;
				}

			}

			return (r, g, b);
		}


		#region ImGui Helpers

		[DllImport("msvcrt.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		private static extern int _snwprintf_s([MarshalAs(UnmanagedType.LPWStr)] StringBuilder sb, IntPtr sizeOfBuffer, IntPtr count, string format, int p);

		public static string FormatImGuiInt(string fmt, int i, int sizeOfBuffer = 64, int count = 32)
		{
			StringBuilder sb = new StringBuilder(sizeOfBuffer);
			_snwprintf_s(sb, (IntPtr)sizeOfBuffer, (IntPtr)count, fmt, i);
			return sb.ToString();
		}

		[DllImport("msvcrt.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
		private static extern int _snwprintf_s([MarshalAs(UnmanagedType.LPWStr)] StringBuilder sb, IntPtr sizeOfBuffer, IntPtr count, string format, double p);

		public static string FormatImGuiDouble(string fmt, double d, int sizeOfBuffer = 64, int count = 32)
		{
			StringBuilder sb = new StringBuilder(sizeOfBuffer);
			_snwprintf_s(sb, (IntPtr)sizeOfBuffer, (IntPtr)count, fmt, d);
			return sb.ToString();
		}

		public static bool ComboFromEnum<T>(string name, ref T enumValue) where T : Enum
		{
			var strings = GetCachedEnumStrings<T>();
			var intValue = (int)(object)enumValue;
			var result = ImGui.Combo(name, ref intValue, strings, strings.Length);
			enumValue = (T)(object)intValue;
			return result;
		}

		public static bool ComboFromEnum<T>(string name, ref T enumValue, T[] allowedValues, string cacheKey) where T : Enum
		{
			if (!EnumStringsCacheByCustomKey.ContainsKey(cacheKey))
			{
				var numEnumValues = allowedValues.Length;
				var enumStrings = new string[numEnumValues];
				for (var i = 0; i < numEnumValues; i++)
					enumStrings[i] = FormatEnumForUI(allowedValues[i].ToString());
				EnumStringsCacheByCustomKey[cacheKey] = enumStrings;
			}

			var strings = EnumStringsCacheByCustomKey[cacheKey];
			var intValue = (int)(object)enumValue;
			var result = ImGui.Combo(name, ref intValue, strings, strings.Length);
			enumValue = (T)(object)intValue;
			return result;
		}

		/// <summary>
		/// Draws a TreeNode in ImGui with Selectable elements for each value in the
		/// Enum specified by the given type parameter T.
		/// </summary>
		/// <typeparam name="T">Enum type for drawing elements.</typeparam>
		/// <param name="label">Label for the TreeNode.</param>
		/// <param name="values">
		/// Array of booleans represented the selected state of each value in the Enum.
		/// Assumed to be the same length as the Enum type param.
		/// </param>
		/// <returns>
		/// Tuple. First value is whether any Selectable was changed.
		/// Second value is an array of bools represented the previous state. This is only
		/// set if the state changes. This is meant is a convenience for undo/redo so the
		/// caller can avoid creating a before state unnecessarily.
		/// </returns>
		public static (bool, bool[]) SelectableTree<T>(string label, ref bool[] values) where T : Enum
		{
			var strings = GetCachedEnumStrings<T>();
			var index = 0;
			var ret = false;
			bool[] originalValues = null;
			if (ImGui.TreeNode(label))
			{
				foreach (var enumString in strings)
				{
					if (ImGui.Selectable(enumString, values[index]))
					{
						if (!ret)
						{
							originalValues = (bool[])values.Clone();
						}
						ret = true;

						// Unset other selections if not holding control.
						if (!ImGui.GetIO().KeyCtrl)
						{
							for (var i = 0; i < values.Length; i++)
							{
								values[i] = false;
							}
						}

						// Toggle selected element.
						values[index] = !values[index];
					}

					index++;
				}

				ImGui.TreePop();
			}

			return (ret, originalValues);
		}

		public static string GetPrettyEnumString<T>(T value)
		{
			var strings = GetCachedEnumStrings<T>();
			var intValue = (int)(object)value;
			return strings[intValue];
		}

		private static string[] GetCachedEnumStrings<T>()
		{
			var typeOfT = typeof(T);
			if (EnumStringsCacheByType.ContainsKey(typeOfT))
				return EnumStringsCacheByType[typeOfT];
			
			var enumValues = Enum.GetValues(typeOfT);
			var numEnumValues = enumValues.Length;
			var enumStrings = new string[numEnumValues];
			for (var i = 0; i < numEnumValues; i++)
				enumStrings[i] = FormatEnumForUI(enumValues.GetValue(i).ToString());
			EnumStringsCacheByType[typeOfT] = enumStrings;
			return EnumStringsCacheByType[typeOfT];
		}

		/// <summary>
		/// Formats an enum string value for by returning a string value
		/// with space-separated capitalized words.
		/// </summary>
		/// <param name="enumValue">String representation of enum value.</param>
		/// <returns>Formatting string representation of enum value.</returns>
		private static string FormatEnumForUI(string enumValue)
		{
			StringBuilder sb = new StringBuilder(enumValue.Length * 2);
			var capitalizeNext = true;
			var previousWasCapital = false;
			var first = true;
			foreach (var character in enumValue)
			{
				// Treat dashes as spaces. Capitalize the letter after a space.
				if (character == '_' || character == '-')
				{
					sb.Append(' ');
					capitalizeNext = true;
					first = false;
					previousWasCapital = false;
					continue;
				}

				// Lowercase character. Use this character unless we are supposed to
				// capitalize it due to it following a space.
				if (char.IsLower(character))
				{
					if (capitalizeNext)
					{
						sb.Append(char.ToUpper(character));
						previousWasCapital = true;
					}
					else
					{
						sb.Append(character);
						previousWasCapital = false;
					}
				}

				// Uppercase character. Prepend a space, unless this followed another
				// capitalized character, in which case lowercase it. This is to support
				// formatting strings like "YES" to "Yes".
				else if (char.IsUpper(character))
				{
					if (!first && !previousWasCapital)
						sb.Append(' ');
					if (previousWasCapital)
						sb.Append(char.ToLower(character));
					else
						sb.Append(character);
					previousWasCapital = true;
				}

				// For any other character type, just record it as is.
				else
				{
					sb.Append(character);
					previousWasCapital = false;
				}

				first = false;
				capitalizeNext = false;
			}

			return sb.ToString();
		}

		public static void HelpMarker(string text)
		{
			PushEnabled();
			Text("(?)", HelpWidth, true);
			if (ImGui.IsItemHovered())
			{
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 80.0f);
				ImGui.TextUnformatted(text);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
			PopEnabled();
		}

		/// <summary>
		/// Draws an ImGUi Text element with a specified width.
		/// </summary>
		/// <param name="text">Text to display in the ImGUi Text element.</param>
		/// <param name="width">Width of the element.</param>
		/// <param name="disabled">Whether or not the element should be disabled.</param>
		public static void Text(string text, float width, bool disabled = false)
		{
			// Wrap the text in Table in order to control the size precisely.
			if (ImGui.BeginTable(text, 1, ImGuiTableFlags.None, new System.Numerics.Vector2(width, 0), width))
			{
				ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, 100.0f);
				ImGui.TableNextRow();
				ImGui.TableSetColumnIndex(0);
				if (disabled)
					ImGui.TextDisabled(text);
				else
					ImGui.Text(text);
				ImGui.EndTable();
			}
		}

		public static bool SliderUInt(string text, ref uint value, uint min, uint max, string format, ImGuiSliderFlags flags)
		{
			int iValue = (int)value;
			var ret = ImGui.SliderInt(text, ref iValue, (int)min, (int)max, format, flags);
			value = (uint)iValue;
			if (value < min)
				value = min;
			if (value > max)
				value = max;
			return ret;
		}

		public static bool InputInt(string label, ref int value, int min = int.MinValue, int max = int.MaxValue)
		{
			var ret = ImGui.InputInt(label, ref value);
			if (ret)
			{
				value = Math.Max(min, value);
				value = Math.Min(max, value);
			}
			return ret;
		}

		public static bool DragInt(
			ref int value,
			string label,
			float speed,
			string format,
			int min = int.MinValue,
			int max = int.MaxValue)
		{
			var ret = ImGui.DragInt(label, ref value, speed, min, max, format);

			if (ret)
			{
				value = Math.Max(min, value);
				value = Math.Min(max, value);
			}

			return ret;
		}

		public static unsafe bool DragDouble(
			ref double value,
			string label,
			float speed,
			string format,
			double min = double.MinValue,
			double max = double.MaxValue)
		{
			var ret = false;
			fixed (double* p = &value)
			{
				IntPtr pData = new IntPtr(p);
				IntPtr pMin = new IntPtr(&min);
				IntPtr pMax = new IntPtr(&max);
				ret = ImGui.DragScalar(label, ImGuiDataType.Double, pData, speed, pMin, pMax, format);
			}

			if (ret)
			{
				value = Math.Max(min, value);
				value = Math.Min(max, value);
			}

			return ret;
		}

		public static unsafe bool DragDouble(ref double value, string label)
		{
			var ret = false;
			fixed (double* p = &value)
			{
				IntPtr pData = new IntPtr(p);
				ret = ImGui.DragScalar(label, ImGuiDataType.Double, pData);
			}
			return ret;
		}

		public static void PushEnabled()
		{
			var wasEnabled = EnabledStack.Count <= 0 || EnabledStack[^1];
			EnabledStack.Add(true);
			if (!wasEnabled)
				ImGui.EndDisabled();
		}

		public static void PushDisabled()
		{
			var wasEnabled = EnabledStack.Count <= 0 || EnabledStack[^1];
			EnabledStack.Add(false);
			if (wasEnabled)
				ImGui.BeginDisabled();
		}

		public static void PopEnabled()
		{
			Debug.Assert(EnabledStack.Count >= 0 && EnabledStack[^1]);
			PopEnabledOrDisabled();
		}

		public static void PopDisabled()
		{
			Debug.Assert(EnabledStack.Count >= 0 && !EnabledStack[^1]);
			PopEnabledOrDisabled();
		}

		private static void PopEnabledOrDisabled()
		{
			var wasEnabled = EnabledStack.Count <= 0 || EnabledStack[^1];
			EnabledStack.RemoveAt(EnabledStack.Count - 1);
			var isEnabled = EnabledStack.Count <= 0 || EnabledStack[^1];
			if (isEnabled && !wasEnabled)
				ImGui.EndDisabled();
			if (!isEnabled && wasEnabled)
				ImGui.BeginDisabled();
		}

		public static void DrawImage(
			string id,
			IntPtr textureImGui,
			Texture2D textureMonogame,
			uint width,
			uint height,
			TextureLayoutMode mode)
		{
			DrawImageInternal(id, textureImGui, textureMonogame, width, height, mode, false);
		}

		public static bool DrawButton(
			string id,
			IntPtr textureImGui,
			Texture2D textureMonogame,
			uint width,
			uint height,
			TextureLayoutMode mode)
		{
			return DrawImageInternal(id, textureImGui, textureMonogame, width, height, mode, true);
		}

		private static bool DrawImageInternal(
			string id,
			IntPtr textureImGui,
			Texture2D textureMonogame,
			uint width,
			uint height,
			TextureLayoutMode mode,
			bool button)
		{
			var result = false;

			// Record original spacing and padding so we can edit it and restore it.
			var originalItemSpacingX = ImGui.GetStyle().ItemSpacing.X;
			var originalItemSpacingY = ImGui.GetStyle().ItemSpacing.Y;
			var originalFramePaddingX = ImGui.GetStyle().FramePadding.X;
			var originalFramePaddingY = ImGui.GetStyle().FramePadding.Y;

			// The total dimensions to draw including the frame padding.
			var totalWidth = width + originalFramePaddingX * 2.0f;
			var totalHeight = height + originalFramePaddingY * 2.0f;

			var (xOffset, yOffset, size, uv0, uv1) = GetTextureUVs(textureMonogame, width, height, mode);

			// Set the padding and spacing so we can draw dummy boxes to offset the image.
			ImGui.GetStyle().ItemSpacing.X = 0;
			ImGui.GetStyle().ItemSpacing.Y = 0;
			ImGui.GetStyle().FramePadding.X = 0;
			ImGui.GetStyle().FramePadding.Y = 0;

			// Begin a child so we can add dummy items to offset the image.
			if (ImGui.BeginChild(id, new System.Numerics.Vector2(totalWidth, totalHeight)))
			{
				// Offset in Y.
				if (yOffset > 0.0f)
					ImGui.Dummy(new System.Numerics.Vector2(width, yOffset));
				
				// Offset in X.
				if (xOffset > 0.0f)
				{
					ImGui.Dummy(new System.Numerics.Vector2(xOffset, size.Y));
					ImGui.SameLine();
				}

				// Reset the padding now so it draws correctly on the image.
				ImGui.GetStyle().FramePadding.X = originalFramePaddingX;
				ImGui.GetStyle().FramePadding.Y = originalFramePaddingY;
				
				// Draw the image.
				if (button)
					result = ImGui.ImageButton(textureImGui, size, uv0, uv1);
				else
					ImGui.Image(textureImGui, size, uv0, uv1);
			}
			ImGui.EndChild();

			// Restore the padding and spacing values.
			ImGui.GetStyle().FramePadding.X = originalFramePaddingX;
			ImGui.GetStyle().FramePadding.Y = originalFramePaddingY;
			ImGui.GetStyle().ItemSpacing.X = originalItemSpacingX;
			ImGui.GetStyle().ItemSpacing.Y = originalItemSpacingY;

			return result;
		}

		public static unsafe void PushAlpha(ImGuiCol col, float alpha)
		{
			var color = ImGui.GetStyleColorVec4(col);
			uint newColor =   ((uint)(byte)(alpha * color->W * byte.MaxValue) << 24)
			                | ((uint)(byte)(color->Z * byte.MaxValue) << 16)
			                | ((uint)(byte)(color->Y * byte.MaxValue) << 8)
			                | (byte)(color->X * byte.MaxValue);
			ImGui.PushStyleColor(col, newColor);
		}

		#endregion ImGui Helpers

		#region Texture Helpers

		private static (float xOffset, float yOffset, System.Numerics.Vector2 size, System.Numerics.Vector2 uv0, System.Numerics.Vector2 uv1) GetTextureUVs(
			Texture2D texture, uint width, uint height, TextureLayoutMode mode)
		{
			float xOffset = 0.0f;
			float yOffset = 0.0f;

			// The size of the image to draw.
			var size = new System.Numerics.Vector2(width, height);

			// The UV coordinates for drawing the texture on the image.
			var uv0 = new System.Numerics.Vector2(0.0f, 0.0f);
			var uv1 = new System.Numerics.Vector2(1.0f, 1.0f);

			switch (mode)
			{
				// Maintain the original size of the texture.
				// Crop and offset as needed.
				case TextureLayoutMode.OriginalSize:
				{
					// If the texture is wider than the destination area then adjust the UV X values
					// so that we crop the texture.
					if (texture.Width > width)
					{
						xOffset = 0.0f;
						size.X = width;
						uv0.X = (texture.Width - width) * 0.5f / texture.Width;
						uv1.X = 1.0f - uv0.X;
					}
					// If the destination area is wider than the texture, then set the X offset value
					// so that we center the texture in X within the destination area.
					else if (texture.Width < width)
					{
						xOffset = (width - texture.Width) * 0.5f;
						size.X = texture.Width;
						uv0.X = 0.0f;
						uv1.X = 1.0f;
					}

					// If the texture is taller than the destination area then adjust the UV Y values
					// so that we crop the texture.
					if (texture.Height > height)
					{
						yOffset = 0.0f;
						size.Y = height;
						uv0.Y = (texture.Height - height) * 0.5f / texture.Height;
						uv1.Y = 1.0f - uv0.Y;
					}
					// If the destination area is taller than the texture, then set the Y offset value
					// so that we center the texture in Y within the destination area.
					else if (texture.Height < height)
					{
						yOffset = (height - texture.Height) * 0.5f;
						size.Y = texture.Height;
						uv0.Y = 0.0f;
						uv1.Y = 1.0f;
					}

					break;
				}

				// Stretch the texture to exactly fill the destination area.
				// The parameters are already set for rendering in this mode.
				case TextureLayoutMode.Stretch:
				{
					break;
				}

				// Scale the texture uniformly such that it fills the entire destination area.
				// Crop the dimension which goes beyond the destination area as needed.
				case TextureLayoutMode.Fill:
				{
					var textureAspectRatio = (float)texture.Width / texture.Height;
					var destinationAspectRatio = (float)width / height;

					// If the texture is wider than the destination area, crop the left and right.
					if (textureAspectRatio > destinationAspectRatio)
					{
						// Crop left and right.
						var scaledTextureW = texture.Width * ((float)height / texture.Height);
						uv0.X = (scaledTextureW - height) * 0.5f / scaledTextureW;
						uv1.X = 1.0f - uv0.X;

						// Fill Y.
						uv0.Y = 0.0f;
						uv1.Y = 1.0f;
					}

					// If the texture is taller than the destination area, crop the top and bottom.
					else if (textureAspectRatio < destinationAspectRatio)
					{
						// Fill X.
						uv0.X = 0.0f;
						uv1.X = 1.0f;

						// Crop top and bottom.
						var scaledTextureH = texture.Height * ((float)width / texture.Width);
						uv0.Y = (scaledTextureH - width) * 0.5f / scaledTextureH;
						uv1.Y = 1.0f - uv0.Y;
					}

					break;
				}

				// Scale the texture uniformly such that it fills the destination area without going over
				// in either dimension.
				case TextureLayoutMode.Box:
				{
					var textureAspectRatio = (float)texture.Width / texture.Height;
					var destinationAspectRatio = (float)width / height;

					// If the texture is wider than the destination area, letterbox.
					if (textureAspectRatio > destinationAspectRatio)
					{
						var scale = (float)width / texture.Width;
						size.X = texture.Width * scale;
						size.Y = texture.Height * scale;
						yOffset = (height - texture.Height * scale) * 0.5f;
					}

					// If the texture is taller than the destination area, pillarbox.
					else if (textureAspectRatio < destinationAspectRatio)
					{
						var scale = (float)height / texture.Height;
						size.X = texture.Width * scale;
						size.Y = texture.Height * scale;
						xOffset = (width - texture.Width * scale) * 0.5f;
					}

					break;
				}
			}

			return (xOffset, yOffset, size, uv0, uv1);
		}

		public static void DrawTexture(
			SpriteBatch spriteBatch,
			Texture2D texture,
			int x,
			int y,
			uint width,
			uint height,
			TextureLayoutMode mode)
		{
			var (xOffset, yOffset, size, uv0, uv1) = GetTextureUVs(texture, width, height, mode);

			var destRect = new Rectangle((int)(x + xOffset), (int)(y + yOffset), (int)size.X, (int)size.Y);
			var sourceRect = new Rectangle((int)(uv0.X * texture.Width), (int)(uv0.Y * texture.Height), (int)(uv1.X * texture.Width), (int)(uv1.Y * texture.Height));

			spriteBatch.Draw(texture, destRect, sourceRect, Color.White);
		}

		#endregion Texture Helpers

		#region File Open Helpers

		public static string FileOpenFilterForImages(string name, bool includeAllFiles)
		{
			var extenstionTypes = new List<string[]> { ExpectedImageFormats };
			return FileOpenFilter(name, extenstionTypes, includeAllFiles);
		}

		public static string FileOpenFilterForImagesAndVideos(string name, bool includeAllFiles)
		{
			var extenstionTypes = new List<string[]> { ExpectedImageFormats, ExpectedVideoFormats };
			return FileOpenFilter(name, extenstionTypes, includeAllFiles);
		}

		public static string FileOpenFilterForAudio(string name, bool includeAllFiles)
		{
			var extenstionTypes = new List<string[]> { ExpectedAudioFormats };
			return FileOpenFilter(name, extenstionTypes, includeAllFiles);
		}

		public static string FileOpenFilterForVideo(string name, bool includeAllFiles)
		{
			var extenstionTypes = new List<string[]> { ExpectedVideoFormats };
			return FileOpenFilter(name, extenstionTypes, includeAllFiles);
		}

		public static string FileOpenFilterForLyrics(string name, bool includeAllFiles)
		{
			var extenstionTypes = new List<string[]> { ExpectedLyricsFormats };
			return FileOpenFilter(name, extenstionTypes, includeAllFiles);
		}

		private static string FileOpenFilter(string name, List<string[]> extensionTypes, bool includeAllFiles)
		{
			var sb = new StringBuilder();
			sb.Append(name);
			sb.Append(" Files (");
			var first = true;
			foreach (var extensions in extensionTypes)
			{
				foreach (var extension in extensions)
				{
					if (!first)
						sb.Append(",");
					sb.Append("*.");
					sb.Append(extension);
					first = false;
				}
			}

			sb.Append(")|");
			first = true;
			foreach (var extensions in extensionTypes)
			{
				foreach (var extension in extensions)
				{
					if (!first)
						sb.Append(";");
					sb.Append("*.");
					sb.Append(extension);
					first = false;
				}
			}

			if (includeAllFiles)
			{
				sb.Append("|All files (*.*)|*.*");
			}

			return sb.ToString();
		}

		public static string BrowseFile(string name, string initialDirectory, string currentFileRelativePath, string filter)
		{
			string relativePath = null;
			using var openFileDialog = new OpenFileDialog();
			var startInitialDirectory = initialDirectory;
			if (!string.IsNullOrEmpty(currentFileRelativePath))
			{
				initialDirectory = Path.Combine(initialDirectory, currentFileRelativePath);
				initialDirectory = System.IO.Path.GetDirectoryName(initialDirectory);
			}

			openFileDialog.InitialDirectory = initialDirectory;
			openFileDialog.Filter = filter;
			openFileDialog.FilterIndex = 1;
			openFileDialog.Title = $"Open {name} File";

			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				var fileName = openFileDialog.FileName;
				relativePath = Path.GetRelativePath(startInitialDirectory, fileName);
			}

			return relativePath;
		}

		#endregion File Open Helpers

		#region Application Focus

		public static bool IsApplicationFocused()
		{
			var activatedHandle = GetForegroundWindow();
			if (activatedHandle == IntPtr.Zero)
				return false;

			GetWindowThreadProcessId(activatedHandle, out var activeProcId);
			return activeProcId == Process.GetCurrentProcess().Id;
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

		#endregion Application Focus
	}

	public static class EditorExtensions
	{
		public static bool FloatEquals(this float f, float other)
		{
			return f - float.Epsilon <= other && f + float.Epsilon >= other;
		}

		public static bool DoubleEquals(this double d, double other)
		{
			return d - double.Epsilon <= other && d + double.Epsilon >= other;
		}
	}
}
