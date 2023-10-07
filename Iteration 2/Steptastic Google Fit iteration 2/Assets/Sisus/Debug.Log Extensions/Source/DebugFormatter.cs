using System;
using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using JetBrains.Annotations;
using Sisus.Debugging.Extensions;
using Object = UnityEngine.Object;

namespace Sisus.Debugging
{
	public class DebugFormatter
	{
		private const MethodImplOptions AggressiveInlining = (MethodImplOptions)256; // MethodImplOptions.AggressiveInlining only exists in .NET 4.5. and later

		private const int numbersAsStringCached = 1000;

		private static readonly string[] numbersAsString;

		public readonly Formatting defaultFormatting = Formatting.Clean;
		public readonly int maxLengthBeforeLineSplitting = 175;
		public readonly string NameValueSeparator = "=";
		public readonly string MultipleEntrySeparator = ", ";

		public readonly string colorTrue = "green";
		public readonly string colorFalse = "red";
		public readonly string colorString = "orange";
		public readonly string colorNumeric = "cyan";

		public readonly char BeginChannel = '[';
		public readonly char EndChannel = ']';

		public readonly char BeginCollection = '[';
		public readonly char EndCollection = ']';
		public readonly string EmptyCollection = "[]";

		public readonly string NameValueSeparatorUnformatted = "=";
		public readonly string MultipleEntrySeparatorUnformatted = ", ";
		public readonly string NullUncolorized = "null";
		public readonly string NaNUncolorized = "NaN";
		public readonly string EmptyStringUncolorized = "\"\"";
		public readonly string TrueUncolorized = "True";
		public readonly string FalseUncolorized = "False";

		public readonly string Null = "<color=red>null</color>";
		public readonly string NaN = "<color=red>NaN</color>";
		public readonly string EmptyString = "<color=orange>\"\"</color>";
		public readonly string True = "<color=green>True</color>";
		public readonly string False = "<color=red>False</color>";
		public readonly string BeginNull = "<color=red>";
		public readonly string BeginString = "<color=orange>";
		public readonly string BeginStringWithQuotationMark = "<color=orange>\"";
		public readonly string BeginChar = "<color=orange>'";
		public readonly string NullChar = "<color=orange>'\\0'</color>";
		public readonly string BeginNumeric = "<color=cyan>";
		public readonly string BeginTrue = "<color=green>";
		public readonly string BeginFalse = "<color=red>";
		public readonly string BeginNegative = "<color=red>";
		public readonly string BeginLargeSize = "<size=23>";
		public readonly string BeginNameValueSeparator = "<color=grey>";

		#if UNITY_EDITOR
		public readonly bool colorize = true;
		#else
		public readonly bool colorize = false;
		#endif

		static DebugFormatter()
		{
			numbersAsString = new string[numbersAsStringCached + 1];
			for(int n = 0; n <= numbersAsStringCached; n++)
			{
				numbersAsString[n] = n.ToString();
			}
		}

		public DebugFormatter()
		{
			if(!Application.isEditor)
			{
				BeginLargeSize = "";
			}
			else
			{
				string unityVersion = Application.unityVersion;
				if(unityVersion.Length <= 6)
				{
					BeginLargeSize = "<size=23>";
				}
				else
				{
					switch(Application.unityVersion.Substring(0, 2))
					{
						case "5.":
						case "4.":
						case "3.":
						case "2.":
						case "1.":
							BeginLargeSize = "<size=22>";
							break;
						case "20":
							switch(Application.unityVersion.Substring(2, 2))
							{
								case "19": // 2019.x
									switch(Application.unityVersion[5])
									{
										case '1': // 2019.1
										case '2': // 2019.2
											BeginLargeSize = "<size=22>";
											break;
										default: // 2019.3 and later
											BeginLargeSize = "<size=23>";
											break;
									}
									break;
								case "18": // 2018.x
								case "17": // 2017.x
									BeginLargeSize = "<size=22>";
									break;
								default:  // 2020.x and later
									BeginLargeSize = "<size=23>";
									break;
							}
							break;
						default: //2100.1 and later :P
							BeginLargeSize = "<size=23>";
							break;

					}
				}
			}
		}

		public DebugFormatter(Formatting setDefaultFormatting, int setMaxLengthBeforeLineSplitting = 175, bool setColorize = true, string setNameValueSeparator = "=", string setMultipleEntrySeparator = ", ", char beginCollection = '[', char endCollection = ']', bool setAllChannelsEnabledByDefault = true, string setColorTrue = "green", string setColorFalse = "red", string setColorString = "orange", string setColorNumeric = "cyan", string setColorNameValueSeparator = "grey")
		{
			var settings = DebugLogExtensionsProjectSettings.Get();
			if(settings != null)
			{
				settings.Apply();
			}
			#if DEV_MODE
			else { Debug.LogError("Failed to load DebugFormatter settings. Default channel colors might be missing."); }
			#endif

			defaultFormatting = setDefaultFormatting;
			maxLengthBeforeLineSplitting = setMaxLengthBeforeLineSplitting;
			#if UNITY_EDITOR
			colorize = setColorize;
			#endif
			Debug.channels.AllChannelsEnabledByDefault = setAllChannelsEnabledByDefault;

			colorTrue = setColorTrue;
			colorFalse = setColorFalse;
			colorString = setColorString;
			colorNumeric = setColorNumeric;

			MultipleEntrySeparator = setMultipleEntrySeparator;
			MultipleEntrySeparatorUnformatted = setMultipleEntrySeparator;
			NameValueSeparatorUnformatted = setNameValueSeparator;

			if(colorize)
			{
				NaN = "<color=" + setColorFalse + ">NaN</color>";
				Null = "<color=" + setColorFalse + ">null</color>";
				BeginNull = "<color=" + setColorFalse + ">";

				True = "<color="+ setColorTrue +">True</color>";
				False = "<color="+ setColorFalse +">False</color>";
				BeginTrue = "<color=" + setColorTrue + ">";
				BeginFalse = "<color=" + setColorFalse + ">";

				BeginString = "<color=" + setColorString + ">";
				BeginStringWithQuotationMark = "<color=" + setColorString + ">\"";
				BeginChar = "<color=" + setColorString + ">'";
				NullChar = "<color=" + setColorString + ">'\\0'</color>";
				EmptyString = "<color=" + setColorString + ">\"\"</color>";

				BeginNumeric = "<color=" + setColorNumeric + ">";
				BeginNegative = "<color=" + setColorFalse + ">";
				BeginNameValueSeparator = "<color=" + setColorNameValueSeparator + ">";
				NameValueSeparator = BeginNameValueSeparator + setNameValueSeparator + "</color>";

				BeginCollection = beginCollection;
				EndCollection = endCollection;
				EmptyCollection = new string(new char[] { beginCollection, endCollection });
			}
			else
			{
				Null = "null";
				BeginNull = "";

				True = "True";
				False = "False";
				BeginTrue = "";
				BeginFalse = "";

				BeginString = "";
				BeginStringWithQuotationMark = "\"";
				BeginChar = "'";
				NullChar = "'\\0'";

				EmptyString = "\"\"";

				BeginNumeric = "";
				BeginNegative = "";
				BeginNameValueSeparator = "";
				NameValueSeparator = setNameValueSeparator;

				BeginCollection = beginCollection;
				EndCollection = endCollection;
				EmptyCollection = new string(new char[] { beginCollection, endCollection });
			}

			if(!Application.isEditor)
			{
				BeginLargeSize = "";
			}
			else
			{
				string unityVersion = Application.unityVersion;
				if(unityVersion.Length <= 6)
				{
					BeginLargeSize = "<size=23>";
				}
				else
				{
					switch(Application.unityVersion.Substring(0, 2))
					{
						case "5.":
						case "4.":
						case "3.":
						case "2.":
						case "1.":
							BeginLargeSize = "<size=22>";
							break;
						case "20":
							switch(Application.unityVersion.Substring(2, 2))
							{
								case "19": // 2019.x
									switch(Application.unityVersion[5])
									{
										case '1': // 2019.1
										case '2': // 2019.2
											BeginLargeSize = "<size=22>";
											break;
										default: // 2019.3 and later
											BeginLargeSize = "<size=23>";
											break;
									}
									break;
								case "18": // 2018.x
								case "17": // 2017.x
									BeginLargeSize = "<size=22>";
									break;
								default:  // 2020.x and later
									BeginLargeSize = "<size=23>";
									break;
							}
							break;
						default: //2100.1 and later :P
							BeginLargeSize = "<size=23>";
							break;

					}
				}
			}
		}

		public void ToStringUncolorized(object fieldOwner, [NotNull] FieldInfo field, StringBuilder sb)
		{
			sb.Append(field.Name);
			sb.Append(NameValueSeparator);
			var value = field.GetValue(fieldOwner);
			ToStringUncolorized(value, sb, true);
		}

		public void ToStringUncolorized(object fieldOwner, [NotNull]PropertyInfo property, StringBuilder sb)
		{
			sb.Append(property.Name);
			sb.Append(NameValueSeparator);
			var value = property.GetValue(fieldOwner, null);
			ToStringUncolorized(value, sb, true);
		}

		public void ToStringColorized(object fieldOwner, [NotNull]FieldInfo field, StringBuilder sb)
		{
			sb.Append(field.Name);
			sb.Append(NameValueSeparator);
			var value = field.GetValue(fieldOwner);
			ToStringColorized(value, sb, true);
		}

		public void ToStringColorized(object fieldOwner, [NotNull]PropertyInfo property, StringBuilder sb)
		{
			sb.Append(property.Name);
			sb.Append(NameValueSeparator);
			var value = property.GetValue(fieldOwner, null);
			ToStringColorized(value, sb, true);
		}

		public string ToStringUncolorized<T>([NotNull]Expression<Func<T>> classMember)
		{
			var sb = new StringBuilder();
			ToStringUncolorized(classMember, sb);
			return sb.ToString();
		}

		public void ToString<T>([NotNull]Expression<Func<T>> classMember, out string uncolorized, out string colorized)
		{
			var sb = new StringBuilder();

			#if UNITY_EDITOR
			MemberExpression memberExpression;
			var body = classMember.Body;
			var unaryExpression = body as UnaryExpression;
			if(unaryExpression != null)
			{
				var operand = unaryExpression.Operand;
				memberExpression = operand as MemberExpression;
				if(memberExpression == null)
				{
					var constantExpression = (ConstantExpression)operand;
					var value = constantExpression.Value;

					sb.Append("const");
					sb.Append(NameValueSeparatorUnformatted);
					sb.Append(ToStringUncolorized(value, true));
					uncolorized = sb.ToString();
					sb.Length = 0;

					sb.Append("const");
					sb.Append(NameValueSeparator);
					sb.Append(ToStringColorized(value, true));
					colorized = sb.ToString();
					return;
				}
			}
			else
			{
				memberExpression = body as MemberExpression;
				if(memberExpression == null)
				{
					var methodCallExpression = body as MethodCallExpression;
					if(methodCallExpression != null)
					{
						var method = methodCallExpression.Method;

						if(method == null)
						{
							UnityEngine.Debug.LogError("MethodCallExpression MethodInfo was null.");

							uncolorized = NullUncolorized;
							colorized = Null;
							return;
						}

						// If target method is string.Format then skip adding method name and name value separator
						// and just display result of string.Format with syntax formatting applied to the result.
						// This makes it possible to use string interpolation with Debug.DisplayOnScreen etc.
						if(method.DeclaringType == typeof(string) && string.Equals(method.Name, "Format"))
						{
							var compiled = classMember.Compile();

							uncolorized = compiled() as string;
							colorized = ColorizePlainText(uncolorized);
							return;
						}

						string methodName = method.Name;
						sb.Append(methodName);

						if(method.GetParameters().Length > 0)
						{
							// We can't invoke the Method but we can compile the original expression into Func<object> and get the result that way.
							var compiled = classMember.Compile();
							var result = compiled();
							
							sb.Append(NameValueSeparatorUnformatted);
							sb.Append(ToStringUncolorized(result, true));
							uncolorized = sb.ToString();
							sb.Length = 0;

							sb.Append(methodName);
							sb.Append(NameValueSeparator);
							sb.Append(ToStringColorized(result, true));
							colorized = sb.ToString();
						}
						else
						{
							var result = method.Invoke(method.IsStatic ? null : ExpressionUtility.GetValue(methodCallExpression.Object), null);

							sb.Append(NameValueSeparatorUnformatted);
							sb.Append(ToStringUncolorized(result, true));
							uncolorized = sb.ToString();
							sb.Length = 0;

							sb.Append(methodName);
							sb.Append(NameValueSeparator);
							sb.Append(ToStringColorized(result, true));
							colorized = sb.ToString();
						}
						return;
					}

					var constantExpression = body as ConstantExpression;
					if(constantExpression != null)
					{
						var value = constantExpression.Value;

						sb.Append("const");
						sb.Append(NameValueSeparatorUnformatted);
						sb.Append(ToStringUncolorized(value, true));
						uncolorized = sb.ToString();
						sb.Length = 0;

						sb.Append("const");
						sb.Append(NameValueSeparator);
						sb.Append(ToStringColorized(value, true));
						colorized = sb.ToString();
						return;
					}

					#if DEV_MODE
					Debug.Log(body == null ? "null" : body.GetType().ToString());
					#endif

					var compiled2 = classMember.Compile();
					var text = compiled2() as string;

					#if DEV_MODE
					Debug.Log(text);
					#endif

					uncolorized = text == null ? NullUncolorized : text;
					colorized = ColorizePlainText(text);
					return;
				}
			}

			var memberName = memberExpression.Member.Name;
			var memberValue = ExpressionUtility.GetValue(memberExpression);

			sb.Append(memberName);
			sb.Append(NameValueSeparatorUnformatted);
			ToStringUncolorized(memberValue, sb, true);
			uncolorized = sb.ToString();
			sb.Length = 0;

			sb.Append(memberName);
			sb.Append(NameValueSeparator);
			ToStringColorized(memberValue, sb, true);
			colorized = sb.ToString();
			#else
			ToStringUncolorized(classMember, sb);
			uncolorized = sb.ToString();
			colorized = uncolorized;
			#endif
		}

		public void ToStringUncolorized<T>([NotNull]Expression<Func<T>> classMember, StringBuilder sb)
		{
			MemberExpression memberExpression;
			var body = classMember.Body;
			var unaryExpression = body as UnaryExpression;
			if(unaryExpression != null)
			{
				var operand = unaryExpression.Operand;
				memberExpression = operand as MemberExpression;
				if(memberExpression == null)
				{
					var constantExpression = (ConstantExpression)operand;
					sb.Append("const");
					sb.Append(NameValueSeparator);
					sb.Append(constantExpression.Value);
					return;
				}
			}
			else
			{
				memberExpression = body as MemberExpression;
				if(memberExpression == null)
				{
					var methodCallExpression = body as MethodCallExpression;
					if(methodCallExpression != null)
					{
						var method = methodCallExpression.Method;

						if(method == null)
						{
							UnityEngine.Debug.LogError("MethodCallExpression MethodInfo was null.");
							return;
						}

						sb.Append(method.Name);

						if(method.GetParameters().Length > 0)
						{
							UnityEngine.Debug.LogError("MethodCallExpression MethodInfo had parameters.");

							return;
						}

						sb.Append(NameValueSeparator);
						sb.Append(method.Invoke(method.IsStatic ? null : ExpressionUtility.GetValue(methodCallExpression.Object), null));
						return;
					}

					var constantExpression = (ConstantExpression)body;
					sb.Append("const");
					sb.Append(NameValueSeparator);
					sb.Append(constantExpression.Value);
					return;
				}
			}

			sb.Append(memberExpression.Member.Name);
			sb.Append(NameValueSeparator);
			ToStringColorized(ExpressionUtility.GetValue(memberExpression), sb, false);
		}

		public string ToStringColorized<T>([NotNull]Expression<Func<T>> classMember)
		{
			var sb = new StringBuilder();
			ToStringColorized(classMember, sb);
			return sb.ToString();
		}

		public void ToStringColorized<T>([NotNull]Expression<Func<T>> classMember, StringBuilder sb)
		{
			#if UNITY_EDITOR
			MemberExpression memberExpression;
			var body = classMember.Body;
			var unaryExpression = body as UnaryExpression;
			if(unaryExpression != null)
			{
				var operand = unaryExpression.Operand;
				memberExpression = operand as MemberExpression;
				if(memberExpression == null)
				{
					var constantExpression = (ConstantExpression)operand;
					sb.Append("const");
					sb.Append(NameValueSeparator);
					sb.Append(ToStringColorized(constantExpression.Value, true));
					return;
				}
			}
			else
			{
				memberExpression = body as MemberExpression;
				if(memberExpression == null)
				{
					var methodCallExpression = body as MethodCallExpression;
					if(methodCallExpression != null)
					{
						var method = methodCallExpression.Method;

						if(method == null)
						{
							UnityEngine.Debug.LogError("MethodCallExpression MethodInfo was null.");
							return;
						}

						// If target method is string.Format then skip adding method name and name value separator
						// and just display result of string.Format with syntax formatting applied to the result.
						// This makes it possible to use string interpolation with Debug.DisplayOnScreen etc.
						if(method.DeclaringType == typeof(string) && string.Equals(method.Name, "Format"))
						{
							var compiled = classMember.Compile();
							var result = compiled() as string;
							sb.Append(ColorizePlainText(result));
							return;
						}

						sb.Append(method.Name);

						if(method.GetParameters().Length > 0)
						{
							// We can't invoke the Method but we can compile the original expression into Func<object> and get the result that way.
							var compiled = classMember.Compile();
							sb.Append(NameValueSeparator);
							sb.Append(ToStringColorized(compiled(), true));
							return;
						}

						sb.Append(NameValueSeparator);
						sb.Append(ToStringColorized(method.Invoke(method.IsStatic ? null : ExpressionUtility.GetValue(methodCallExpression.Object), null), true));
						return;
					}

					var constantExpression = body as ConstantExpression;
					if(constantExpression != null)
					{
						sb.Append("const");
						sb.Append(NameValueSeparator);
						sb.Append(ToStringColorized(constantExpression.Value, true));
						return;
					}

					#if DEV_MODE
					Debug.Log(body == null ? "null" : body.GetType().ToString());
					#endif

					var compiled2 = classMember.Compile();
					var result2 = compiled2() as string;

					#if DEV_MODE
					Debug.Log(result2);
					#endif

					sb.Append(ColorizePlainText(result2));
					return;
				}
			}

			sb.Append(memberExpression.Member.Name);
			sb.Append(NameValueSeparator);
			ToStringColorized(ExpressionUtility.GetValue(memberExpression), sb, true);
			#else
			ToStringUncolorized(classMember, sb);
			#endif
		}

		public void ToStringUncolorized([CanBeNull]object value, StringBuilder sb, bool formatStrings)
		{
			if(value == null)
			{
				sb.Append(NullUncolorized);
				return;
			}

			var text = value as string;
			if(text != null)
			{
				if(formatStrings)
				{
					if(text.Length == 0)
					{
						sb.Append(EmptyStringUncolorized);
						return;
					}

					switch(text[0])
					{
						case '"':
						case '\'':
							sb.Append(text);
							return;
						default:
							sb.Append("\"");
							sb.Append(text);
							sb.Append("\"");
							return;
					}
				}

				sb.Append(text);
				return;
			}

			if(value is int)
			{
				var number = (int)value;
				ToStringUncolorized(number, sb);
				return;
			}

			if(value is float)
			{
				var number = (float)value;
				ToStringUncolorized(number, sb);
				return;
			}

			if(value is double)
			{
				var number = (double)value;
				sb.Append(number.ToString("0.###"));
				return;
			}

			if(value is long)
			{
				var number = (long)value;
				if(number < -numbersAsStringCached || number > numbersAsStringCached)
				{
					sb.Append(number.ToString());
					return;
				}
				else if(number < 0)
				{
					sb.Append('-');
					sb.Append(numbersAsString[Mathf.Abs((int)number)]);
					return;
				}
				sb.Append(numbersAsString[number]);
				return;
			}

			if(value is short)
			{
				var number = (short)value;
				if(number < -numbersAsStringCached || number > numbersAsStringCached)
				{
					sb.Append(number.ToString());
					return;
				}
				else if(number < 0)
				{
					sb.Append('-');
					sb.Append(numbersAsString[Mathf.Abs(number)]);
					return;
				}
				sb.Append(numbersAsString[number]);
				return;
			}

			if(value is uint)
			{
				var number = (uint)value;
				if(number > numbersAsStringCached)
				{
					sb.Append(number.ToString());
					return;
				}
				sb.Append(numbersAsString[number]);
				return;
			}

			var expression = value as Expression<Func<object>>;
			if(expression != null)
			{
				sb.Append(ToStringUncolorized(expression));
				return;
			}

			var collection = value as ICollection;
			if(collection != null)
			{
				sb.Append(ToStringUncolorized(collection, formatStrings));
				return;
			}

			if(value is Object)
			{
				var unityObject = value as Object;
				if(unityObject == null)
				{
					sb.Append("null");
					return;
				}

				var gameObject = unityObject as GameObject;
				if(gameObject != null)
				{
					sb.Append("\"");
					sb.Append(gameObject.name);
					sb.Append("\"");
					return;
				}
				var transform = unityObject as Transform;
				if(transform != null)
				{
					sb.Append("\"");
					sb.Append(transform.name);
					sb.Append("\"(Transform)");
					return;
				}
				sb.Append(unityObject.GetType().Name);
				return;
			}

			sb.Append(value);
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(Vector2 v)
		{
			var sb = new StringBuilder();
			ToStringUncolorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(Vector2Int v)
		{
			var sb = new StringBuilder();
			ToStringUncolorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(Vector3 v)
		{
			var sb = new StringBuilder();
			ToStringUncolorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(Vector3Int v)
		{
			var sb = new StringBuilder();
			ToStringUncolorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(Vector4 v)
		{
			var sb = new StringBuilder();
			ToStringUncolorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(Quaternion q)
		{
			var sb = new StringBuilder();
			ToStringUncolorized(q, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(Vector2 v, StringBuilder sb)
		{
			sb.Append("(");
			ToStringUncolorized(v[0], sb);
			sb.Append(", ");
			ToStringUncolorized(v[1], sb);
			sb.Append(")");
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(Vector2Int v, StringBuilder sb)
		{
			sb.Append("(");
			ToStringUncolorized(v[0], sb);
			sb.Append(", ");
			ToStringUncolorized(v[1], sb);
			sb.Append(")");
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(Vector3 v, StringBuilder sb)
		{
			sb.Append("(");
			ToStringUncolorized(v[0], sb);
			sb.Append(", ");
			ToStringUncolorized(v[1], sb);
			sb.Append(", ");
			ToStringUncolorized(v[2], sb);
			sb.Append(")");
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(Vector3Int v, StringBuilder sb)
		{
			sb.Append("(");
			ToStringUncolorized(v[0], sb);
			sb.Append(", ");
			ToStringUncolorized(v[1], sb);
			sb.Append(", ");
			ToStringUncolorized(v[2], sb);
			sb.Append(")");
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(Vector4 v, StringBuilder sb)
		{
			sb.Append("(");
			ToStringUncolorized(v[0], sb);
			sb.Append(", ");
			ToStringUncolorized(v[1], sb);
			sb.Append(", ");
			ToStringUncolorized(v[2], sb);
			sb.Append(", ");
			ToStringUncolorized(v[3], sb);
			sb.Append(")");
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(Quaternion q, StringBuilder sb)
		{
			sb.Append("(");
			ToStringUncolorized(q[0], sb);
			sb.Append(", ");
			ToStringUncolorized(q[1], sb);
			sb.Append(", ");
			ToStringUncolorized(q[2], sb);
			sb.Append(", ");
			ToStringUncolorized(q[3], sb);
			sb.Append(")");
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(float number, StringBuilder sb)
		{
			if(float.IsNaN(number))
			{
				sb.Append("NaN");
				return;
			}

			int asInt = Mathf.RoundToInt(number);
			if(asInt == number)
			{
				if(asInt < 0)
				{
					if(number < -numbersAsStringCached)
					{
						sb.Append(asInt.ToString());
						return;
					}
					sb.Append('-');
					asInt = Mathf.Abs(asInt);
				}
				sb.Append(asInt <= numbersAsStringCached ? numbersAsString[asInt] : asInt.ToString());
				return;
			}

			sb.Append(number.ToString("0.###", CultureInfo.InvariantCulture));
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(char character)
		{
			if(character == '\0')
			{
				return "'\\0'";
			}
			return "'" + character.ToString() + "'";
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(char character, StringBuilder sb)
		{
			if(character == '\0')
			{
				sb.Append("'\\0'");
				return;
			}
			sb.Append('\'');
			sb.Append(character);
			sb.Append('\'');
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(int number)
		{
			if(number >= 0 && number <= numbersAsStringCached)
			{
				return numbersAsString[number];
			}
			return number.ToString(CultureInfo.InvariantCulture);
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized(float number)
		{
			if(float.IsNaN(number))
			{
				return "NaN";
			}

			int asInt = Mathf.RoundToInt(number);
			if(asInt == number)
			{
				if(asInt >= 0 && asInt <= numbersAsStringCached)
				{
					return numbersAsString[asInt];
				}

				return asInt.ToString(CultureInfo.InvariantCulture);
			}

			return number.ToString("0.###", CultureInfo.InvariantCulture);
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(int number, StringBuilder sb)
		{
			if(number < 0)
			{
				if(number < -numbersAsStringCached)
				{
					sb.Append(number.ToString(CultureInfo.InvariantCulture));
					return;
				}
				sb.Append('-');
				number = Mathf.Abs(number);
			}
			sb.Append(number <= numbersAsStringCached ? numbersAsString[number] : number.ToString(CultureInfo.InvariantCulture));
			return;
		}

		/// <summary>
		/// Appends value to StringBuilder with syntax formatting if DebugFormatter.colorize is true.
		/// </summary>
		/// <param name="value"> Value to append to StringBuilder. </param>
		/// <param name="formatStrings"> If value is string type, should it be placed inside quotation marks and colorized, or returned as it was? </param>
		public void ToStringColorized([CanBeNull]object value, StringBuilder sb, bool formatStrings)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(value, sb, formatStrings);
				return;
			}

			if(value == null)
			{
				sb.Append(Null);
				return;
			}

			var text = value as string;
			if(text != null)
			{
				if(!formatStrings)
				{
					sb.Append(text);
					return;
				}

				switch(text.Length)
				{
					case 0:
						sb.Append(EmptyString);
						return;
					case 1:
						sb.Append(BeginStringWithQuotationMark);
						sb.Append(text);
						sb.Append("\"</color>");
						return;
					default:
						if(text[0] == '"' || text[0] == '\'')
						{
							sb.Append(BeginString);
							sb.Append(text);
							sb.Append("</color>");
							return;
						}
						if(text[0] == '<')
						{
							sb.Append(text);
							return;
						}
						sb.Append(BeginStringWithQuotationMark);
						sb.Append(text);
						sb.Append("\"</color>");
						return;
				}
			}

			if(value is char)
			{
				ToStringColorized((char)value, sb);
				return;
			}

			if(value is bool)
			{
				sb.Append((bool)value ? True : False);
				return;
			}

			if(value is int)
			{
				var number = (int)value;
				ToStringColorized(number, sb);
				return;
			}

			if(value is float)
			{
				var number = (float)value;
				ToStringColorized(number, sb);
				return;
			}

			if(value is double)
			{
				var number = (double)value;
				sb.Append(number >= 0d ? BeginNumeric : BeginNegative);
				sb.Append(number.ToString("0.###", CultureInfo.InvariantCulture));
				sb.Append("</color>");
				return;
			}

			if(value is long)
			{
				var number = (long)value;
				sb.Append(number >= 0L ? BeginNumeric : BeginNegative);
				if(number < -numbersAsStringCached || number > numbersAsStringCached)
				{
					sb.Append(number.ToString(CultureInfo.InvariantCulture));
					sb.Append("</color>");
					return;
				}
				else if(number < 0)
				{
					sb.Append('-');
					sb.Append(numbersAsString[Mathf.Abs((int)number)]);
					sb.Append("</color>");
					return;
				}
				sb.Append(numbersAsString[number]);
				sb.Append("</color>");
				return;
			}

			if(value is short)
			{
				var number = (short)value;
				sb.Append(number >= 0 ? BeginNumeric : BeginNegative);

				if(number < -numbersAsStringCached || number > numbersAsStringCached)
				{
					sb.Append(number.ToString(CultureInfo.InvariantCulture));
					sb.Append("</color>");
					return;
				}
				else if(number < 0)
				{
					sb.Append('-');
					sb.Append(numbersAsString[Mathf.Abs(number)]);
					sb.Append("</color>");
					return;
				}
				sb.Append(numbersAsString[number]);
				sb.Append("</color>");
				return;
			}

			if(value is uint)
			{
				var number = (uint)value;
				sb.Append(BeginNumeric);
				sb.Append(number <= numbersAsStringCached ? numbersAsString[number] : number.ToString(CultureInfo.InvariantCulture));
				sb.Append("</color>");
				return;
			}

			if(value is Vector3)
			{
				ToStringColorized((Vector3)value, sb);
				return;
			}

			if(value is Vector2)
			{
				ToStringColorized((Vector2)value, sb);
				return;
			}

			if(value is Vector4)
			{
				ToStringColorized((Vector4)value, sb);
				return;
			}

			if(value is Vector2Int)
			{
				ToStringColorized((Vector2Int)value, sb);
				return;
			}

			if(value is Vector3Int)
			{
				ToStringColorized((Vector3Int)value, sb);
				return;
			}

			if(value is Color)
			{
				ToStringColorized((Color)value, sb);
				return;
			}

			if(value is Color32)
			{
				ToStringColorized((Color32)value, sb);
				return;
			}

			if(value is Quaternion)
			{
				ToStringColorized((Quaternion)value, sb);
				return;
			}

			var expression = value as Expression<Func<object>>;
			if(expression != null)
			{
				sb.Append(ToStringColorized(expression));
				return;
			}

			var collection = value as ICollection;
			if(collection != null)
			{
				sb.Append(ToStringColorized(collection, formatStrings));
				return;
			}

			if(value is Object)
			{
				var unityObject = value as Object;
				if(unityObject == null)
				{
					sb.Append(Null);
					return;
				}

				var gameObject = unityObject as GameObject;
				if(gameObject != null)
				{
					sb.Append(BeginStringWithQuotationMark);
					sb.Append(gameObject.name);
					sb.Append("\"</color>");
					return;
				}
				var transform = unityObject as Transform;
				if(transform != null)
				{
					sb.Append(BeginStringWithQuotationMark);
					sb.Append(transform.name);
					sb.Append("\"</color>(Transform)");
					return;
				}
				sb.Append(unityObject.GetType().Name);
				return;
			}

			sb.Append(value.ToString());
			#else
			ToStringUncolorized(value, sb, formatStrings);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(float number)
		{
			var sb = new StringBuilder();
			ToStringColorized(number, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(float number, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(number, sb);
				return;
			}

			if(float.IsNaN(number))
			{
				sb.Append(BeginNegative);
				sb.Append("NaN</color>");
				return;
			}

			int asInt = Mathf.RoundToInt(number);
			if(asInt == number)
			{
				if(asInt < 0)
				{
					sb.Append(BeginNegative);

					if(number < -numbersAsStringCached)
					{
						sb.Append(number.ToString(CultureInfo.InvariantCulture));
						sb.Append("</color>");
						return;
					}

					sb.Append('-');
					asInt = Mathf.Abs(asInt);
				}
				else
				{
					sb.Append(BeginNumeric);
				}
				sb.Append(asInt <= numbersAsStringCached ? numbersAsString[asInt] : asInt.ToString(CultureInfo.InvariantCulture));
				sb.Append("</color>");
				return;
			}

			sb.Append(number >= 0f ? BeginNumeric : BeginNegative);
			sb.Append(number.ToString("0.###", CultureInfo.InvariantCulture));
			sb.Append("</color>");
			#else
			ToStringUncolorized(number, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(Vector2 v)
		{
			var sb = new StringBuilder();
			ToStringColorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(Vector2Int v)
		{
			var sb = new StringBuilder();
			ToStringColorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(Vector3 v)
		{
			var sb = new StringBuilder();
			ToStringColorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(Vector3Int v)
		{
			var sb = new StringBuilder();
			ToStringColorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(Vector4 v)
		{
			var sb = new StringBuilder();
			ToStringColorized(v, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(Quaternion q)
		{
			var sb = new StringBuilder();
			ToStringColorized(q, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized([CanBeNull] ICollection collection, bool formatStrings)
		{
			if(collection == null)
			{
				return Null;
			}

			int count = collection.Count;
			if(count == 0)
			{
				return EmptyCollection;
			}

			var sb = new StringBuilder(6 * collection.Count);
			ToStringColorized(collection, sb, formatStrings);
			return sb.ToString();
		}

		private void ToStringColorized([NotNull] ICollection collection, StringBuilder sb, bool formatStrings)
		{
			sb.Append(BeginCollection);

			var dictionary = collection as IDictionary;
			if(dictionary != null)
			{
				int count = dictionary.Count;
				if(count == 0)
				{
					sb.Append(EndCollection);
					return;
				}

				var keys = dictionary.Keys.GetEnumerator();
				var values = dictionary.Values.GetEnumerator();
				keys.MoveNext();
				values.MoveNext();

				int charCountWas = sb.Length;

				sb.Append('[');
				ToStringColorized(keys.Current, sb, formatStrings);
				sb.Append(MultipleEntrySeparator);
				ToStringColorized(values.Current, sb, formatStrings);
				sb.Append(']');

				int charsAdded = sb.Length - charCountWas;

				string separator = charsAdded > maxLengthBeforeLineSplitting ? separator = "," + Environment.NewLine : MultipleEntrySeparator;

				for(int n = 1; n < count; n++)
				{
					sb.Append(separator);

					keys.MoveNext();
					values.MoveNext();

					sb.Append('[');
					ToStringColorized(keys.Current, sb, formatStrings);
					sb.Append(MultipleEntrySeparator);
					ToStringColorized(values.Current, sb, formatStrings);
					sb.Append(']');
				}
			}
			else
			{
				var enumerator = collection.GetEnumerator();
				if(!enumerator.MoveNext())
				{
					sb.Append(EmptyCollection);
					return;
				}

				int charCountWas = sb.Length;
				ToStringColorized(enumerator.Current, sb, formatStrings);
				int charsAdded = sb.Length - charCountWas;

				string separator = charsAdded > maxLengthBeforeLineSplitting ? separator = "," + Environment.NewLine : MultipleEntrySeparator;

				while(enumerator.MoveNext())
				{
					sb.Append(separator);
					ToStringColorized(enumerator.Current, sb, formatStrings);
				}
			}

			sb.Append(EndCollection);
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringUncolorized([CanBeNull] ICollection collection, bool formatStrings)
		{
			if(collection == null)
			{
				return NullUncolorized;
			}

			int count = collection.Count;
			if(count == 0)
			{
				return EmptyCollection;
			}

			var sb = new StringBuilder(6 * collection.Count);
			ToStringUncolorized(collection, sb, formatStrings);
			return sb.ToString();
		}

		private void ToStringUncolorized([NotNull] ICollection collection, StringBuilder sb, bool formatStrings)
		{
			sb.Append(BeginCollection);

			var dictionary = collection as IDictionary;
			if(dictionary != null)
			{
				int count = dictionary.Count;
				if(count == 0)
				{
					sb.Append(EndCollection);
					return;
				}

				var keys = dictionary.Keys.GetEnumerator();
				var values = dictionary.Values.GetEnumerator();
				keys.MoveNext();
				values.MoveNext();

				int charCountWas = sb.Length;

				sb.Append('[');
				ToStringUncolorized(keys.Current, sb, formatStrings);
				sb.Append(MultipleEntrySeparator);
				ToStringUncolorized(values.Current, sb, formatStrings);
				sb.Append(']');

				int charsAdded = sb.Length - charCountWas;

				string separator = charsAdded > maxLengthBeforeLineSplitting ? separator = "," + Environment.NewLine : MultipleEntrySeparator;

				for(int n = 1; n < count; n++)
				{
					sb.Append(separator);

					keys.MoveNext();
					values.MoveNext();

					sb.Append('[');
					ToStringUncolorized(keys.Current, sb, formatStrings);
					sb.Append(MultipleEntrySeparator);
					ToStringUncolorized(values.Current, sb, formatStrings);
					sb.Append(']');
				}
			}
			else
			{
				var enumerator = collection.GetEnumerator();
				if(!enumerator.MoveNext())
				{
					sb.Append(EmptyCollection);
					return;
				}

				int charCountWas = sb.Length;
				ToStringUncolorized(enumerator.Current, sb, formatStrings);
				int charsAdded = sb.Length - charCountWas;

				string separator = charsAdded > maxLengthBeforeLineSplitting ? separator = "," + Environment.NewLine : MultipleEntrySeparator;

				while(enumerator.MoveNext())
				{
					sb.Append(separator);
					ToStringUncolorized(enumerator.Current, sb, formatStrings);
				}
			}

			sb.Append(EndCollection);
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(Vector2 v, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(v, sb);
				return;
			}
			sb.Append(BeginNumeric);
			ToStringUncolorized(v, sb);
			sb.Append("</color>");
			#else
			ToStringUncolorized(v, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(Vector2Int v, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(v, sb);
				return;
			}
			sb.Append(BeginNumeric);
			ToStringUncolorized(v, sb);
			sb.Append("</color>");
			#else
			ToStringUncolorized(v, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(Vector3 v, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(v, sb);
				return;
			}
			sb.Append(BeginNumeric);
			ToStringUncolorized(v, sb);
			sb.Append("</color>");
			#else
			ToStringUncolorized(v, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(Vector3Int v, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(v, sb);
				return;
			}
			sb.Append(BeginNumeric);
			ToStringUncolorized(v, sb);
			sb.Append("</color>");
			#else
			ToStringUncolorized(v, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(Vector4 v, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(v, sb);
				return;
			}
			sb.Append(BeginNumeric);
			ToStringUncolorized(v, sb);
			sb.Append("</color>");
			#else
			ToStringUncolorized(v, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(Quaternion q, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(q, sb);
				return;
			}
			sb.Append(BeginNumeric);
			ToStringUncolorized(q, sb);
			sb.Append("</color>");
			#else
			ToStringUncolorized(q, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(Color color, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(color, sb);
				return;
			}
			sb.Append("<color=#");
			sb.Append(ColorUtility.ToHtmlStringRGB(color));
			sb.Append(">");
			ToStringUncolorized(color, sb);
			sb.Append("</color>");
			#else
			ToStringUncolorized(color, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(Color color, StringBuilder sb)
		{
			sb.Append("(");
			ToStringUncolorized(color[0], sb);
			sb.Append(", ");
			ToStringUncolorized(color[1], sb);
			sb.Append(", ");
			ToStringUncolorized(color[2], sb);
			if(color[3] < 1f)
			{
				sb.Append(", ");
				ToStringUncolorized(color[3], sb);
			}
			sb.Append(")");
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(Color32 color, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(color, sb);
				return;
			}
			sb.Append("<color=#");
			sb.Append(ColorUtility.ToHtmlStringRGB(color));
			sb.Append(">");
			ToStringUncolorized(color, sb);
			sb.Append("</color>");
			#else
			ToStringUncolorized(color, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringUncolorized(Color32 color, StringBuilder sb)
		{
			sb.Append("(");
			ToStringUncolorized(color.r, sb);
			sb.Append(", ");
			ToStringUncolorized(color.g, sb);
			sb.Append(", ");
			ToStringUncolorized(color.b, sb);
			if(color.a < 255)
			{
				sb.Append(", ");
				ToStringUncolorized(color.a, sb);
			}
			sb.Append(")");
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(char character)
		{
			var sb = new StringBuilder();
			ToStringColorized(character, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(char character, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(character, sb);
				return;
			}

			if(character == '\0')
			{
				sb.Append(NullChar);
				return;
			}

			sb.Append(BeginChar);
			sb.Append(character);
			sb.Append("\'</color>");
			#else
			ToStringUncolorized(character, sb);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public string ToStringColorized(int number)
		{
			var sb = new StringBuilder();
			ToStringColorized(number, sb);
			return sb.ToString();
		}

		[MethodImpl(AggressiveInlining)]
		public void ToStringColorized(int number, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				ToStringUncolorized(number, sb);
				return;
			}

			if(number < 0)
			{
				sb.Append(BeginNegative);

				if(number < -numbersAsStringCached)
				{
					sb.Append(number.ToString(CultureInfo.InvariantCulture));
					sb.Append("</color>");
					return;
				}

				sb.Append('-');
				number = Mathf.Abs(number);
			}
			else
			{
				sb.Append(BeginNumeric);
			}
			sb.Append(number <= numbersAsStringCached ? numbersAsString[number] : number.ToString(CultureInfo.InvariantCulture));
			sb.Append("</color>");
			#else
			ToStringUncolorized(number, sb);
			#endif
		}

		/// <summary>
		/// Converts value to string and applies syntax formatting to it if DebugFormatter.colorize is true.
		/// </summary>
		/// <param name="value"> Value to convert to string. </param>
		/// <param name="formatStrings"> If value is string type, should it be placed inside quotation marks and colorized, or returned as it was? </param>
		/// <returns> Value as string and syntax formatting. </returns>
		public string ToStringColorized([CanBeNull]object value, bool formatStrings)
		{
			#if UNITY_EDITOR
			if(!colorize)
			{
				if(formatStrings)
				{
					var txt = value as string;
					if(txt != null)
					{
						if(txt.Length > 1)
						{
							switch(txt[0])
							{
								case '"':
								case '\'':
									return txt;
							}
						}
						return "\"" + txt + "\"";
					}
				}
				return ToStringUncolorized(value, formatStrings);
			}

			if(value == null)
			{
				return Null;
			}

			var text = value as string;
			if(text != null)
			{
				if(formatStrings)
				{
					switch(text.Length)
					{
						case 0:
							return EmptyString;
						case 1:
							return BeginStringWithQuotationMark + text + "\"</color>";
						default:
							if(text[0] == '"' || text[0] == '\'')
							{
								return BeginString + text + "</color>";
							}
							// use IndexOf to check whole string instead?
							if(text[0] == '<')
							{
								return "\"" + text + "\"";
							}
							return BeginStringWithQuotationMark + text + "\"</color>";
					}
				}
				return text;
			}

			if(value is char)
			{
				return BeginChar + (char)value + "\'</color>";
			}

			if(value is bool)
			{
				return ((bool)value) ? True : False;
			}

			if(value is int)
			{
				int number = (int)value;
				return ToStringColorized(number);
			}

			if(value is float)
			{
				float number = (float)value;
				return ToStringColorized(number);
			}

			if(value is Vector3)
			{
				var v = (Vector3)value;
				return ToStringColorized(v);
			}

			if(value is Vector2)
			{
				var v = (Vector2)value;
				return ToStringColorized(v);
			}

			if(value is Vector4)
			{
				var v = (Vector4)value;
				return ToStringColorized(v);
			}

			if(value is Vector2Int)
			{
				var v = (Vector2Int)value;
				return ToStringColorized(v);
			}

			if(value is Vector3Int)
			{
				var v = (Vector3Int)value;
				return ToStringColorized(v);
			}

			if(value is Quaternion)
			{
				var q = (Quaternion)value;
				return ToStringColorized(q);
			}

			var expression = value as Expression<Func<object>>;
			if(expression != null)
			{
				return ToStringColorized(expression);
			}

			var collection = value as ICollection;
			if(collection != null)
			{
				return ToStringColorized(collection, formatStrings);
			}

			if(value is Object)
			{
				var unityObject = value as Object;
				if(unityObject == null)
				{
					return "null";
				}

				var gameObject = unityObject as GameObject;
				if(gameObject != null)
				{
					return "\"" + gameObject.name + "\"";
				}

				var transform = unityObject as Transform;
				if(transform != null)
				{
					return "\"" + transform.name + "\"(Transform)";
				}

				return unityObject.GetType().Name;
			}

			return value.ToString();
			#else
			return ToStringUncolorized(value, formatStrings);
			#endif
		}

		public string WithUncolorizedPrefix(int channel, string text)
		{
			var sb = new StringBuilder();
			WithUncolorizedPrefix(channel, text, sb);
			return sb.ToString();
		}

		public void WithUncolorizedPrefix(int channel, string text, StringBuilder sb)
		{
			if(channel != 0)
			{
				sb.Append('[');
				sb.Append(Channels.Get(channel));
				sb.Append("] ");
			}
			sb.Append(text);
		}

		public string WithUncolorizedPrefixes(int channel1, int channel2, string text)
        {
			var sb = new StringBuilder();
			WithUncolorizedPrefixes(channel1, channel2, text, sb);
			return sb.ToString();
		}

		public void WithUncolorizedPrefixes(int channel1, int channel2, string text, StringBuilder sb)
        {
			if(channel1 != 0)
			{
				sb.Append('[');
				sb.Append(Channels.Get(channel1));

				if(channel2 != 0)
				{
					sb.Append("][");
					sb.Append(Channels.Get(channel2));
				}

				sb.Append("] ");
			}
			sb.Append(text);
		}

		public string ToStringUncolorized(int channel, [CanBeNull] object value, bool formatStrings)
		{
			return WithUncolorizedPrefix(channel, ToStringUncolorized(value, formatStrings));
		}

		public string ToStringUncolorized(int channel1, int channel2, [CanBeNull] object value, bool formatStrings)
		{
			return WithUncolorizedPrefixes(channel1, channel2, ToStringUncolorized(value, formatStrings));
		}

		public string ToStringUncolorized([CanBeNull]object value, bool formatStrings)
		{
			if(value == null)
			{
				return NullUncolorized;
			}
			
			var text = value as string;
			if(text != null)
			{
				if(formatStrings)
				{
					var sb = new StringBuilder(text.Length + 2);
					sb.Append('\"');
					sb.Append(text);
					sb.Append('\"');
					return sb.ToString();
				}

				return text;
			}

			if(value is char)
			{
				return ToStringUncolorized((char)value);
			}

			if(value is int)
			{
				var number = (int)value;
				return ToStringUncolorized(number);
			}

			if(value is float)
			{
				float number = (float)value;
				return ToStringUncolorized(number);
			}

			if(value is Vector3)
			{
				var v = (Vector3)value;
				return ToStringUncolorized(v);
			}

			if(value is Vector2)
			{
				var v = (Vector2)value;
				return ToStringUncolorized(v);
			}

			if(value is Vector4)
			{
				var v = (Vector4)value;
				return ToStringUncolorized(v);
			}

			if(value is Vector2Int)
			{
				var v = (Vector2Int)value;
				return ToStringUncolorized(v);
			}

			if(value is Vector3Int)
			{
				var v = (Vector3Int)value;
				return ToStringUncolorized(v);
			}

			if(value is Quaternion)
			{
				var q = (Quaternion)value;
				return ToStringUncolorized(q);
			}

			var expression = value as Expression<Func<object>>;
			if(expression != null)
			{
				return ToStringUncolorized(expression);
			}

			var collection = value as ICollection;
			if(collection != null)
			{
				return ToStringUncolorized(collection, formatStrings);
			}

			if(value is Object)
			{
				var unityObject = value as Object;
				if(unityObject == null)
				{
					return "null";
				}

				var gameObject = unityObject as GameObject;
				if(gameObject != null)
				{
					return "\"" + gameObject.name + "\"";
				}
				var transform = unityObject as Transform;
				if(transform != null)
				{
					return "\"" + transform.name + "\"(Transform)";
				}
				return unityObject.GetType().Name;
			}

			return value.ToString();
		}

		public string JoinColorized([NotNull]string messagePrefix, [NotNull]params Expression<Func<object>>[] classMembers)
		{
			if(classMembers == null)
			{
				return messagePrefix;
			}

			int count = classMembers.Length;
			if(count == 0)
			{
				return messagePrefix;
			}

			var sb = new StringBuilder();

			ToStringColorized(classMembers[0], sb);
			for(int n = 1; n < count; n++)
			{
				sb.Append(Environment.NewLine);
				ToStringColorized(classMembers[n], sb);
			}

			int prefixLength = messagePrefix.Length;
			if(prefixLength > 0)
			{
				if(sb.Length <= maxLengthBeforeLineSplitting - prefixLength)
				{
					sb.Replace(Environment.NewLine, MultipleEntrySeparator);

					if(messagePrefix[prefixLength - 1] != ' ')
					{
						sb.Insert(0, messagePrefix + " ");
					}
					else
					{
						sb.Insert(0, messagePrefix);
					}
				}
				else
				{
					sb.Insert(0, messagePrefix + Environment.NewLine);
				}
			}
			else
			{
				if(sb.Length <= maxLengthBeforeLineSplitting)
				{
					sb.Replace(Environment.NewLine, MultipleEntrySeparator);
				}
			}

			return sb.ToString();
		}

		public string JoinUncolorized([NotNull]string messagePrefix, [NotNull]params Expression<Func<object>>[] classMembers)
		{
			if(classMembers == null)
			{
				return messagePrefix;
			}

			int count = classMembers.Length;
			if(count == 0)
			{
				return messagePrefix;
			}

			var sb = new StringBuilder();

			ToStringUncolorized(classMembers[0], sb);
			for(int n = 1; n < count; n++)
			{
				sb.Append(Environment.NewLine);
				ToStringUncolorized(classMembers[n], sb);
			}

			int prefixLength = messagePrefix.Length;
			if(prefixLength > 0)
			{
				if(sb.Length <= maxLengthBeforeLineSplitting - prefixLength)
				{
					sb.Replace(Environment.NewLine, MultipleEntrySeparatorUnformatted);

					if(messagePrefix[prefixLength - 1] != ' ')
					{
						sb.Insert(0, messagePrefix + " ");
					}
					else
					{
						sb.Insert(0, messagePrefix);
					}
				}
				else
				{
					sb.Insert(0, messagePrefix + Environment.NewLine);
				}
			}
			else
			{
				if(sb.Length <= maxLengthBeforeLineSplitting)
				{
					sb.Replace(Environment.NewLine, MultipleEntrySeparatorUnformatted);
				}
			}

			return sb.ToString();
		}

		public string ColorizePlainText(int channel, string text)
		{
			var sb = new StringBuilder();
			if(channel != 0)
			{
				AppendPrefixColorized(channel, sb);
				sb.Append(' ');
			}
			ColorizePlainText(text, sb);
			return sb.ToString();
		}

		public string ColorizePlainText(int channel1, int channel2, string text)
		{
			var sb = new StringBuilder();
			if(channel1 != 0)
			{
				AppendPrefixColorized(channel1, sb);
				if(channel2 != 0)
				{
					AppendPrefixColorized(channel2, sb);
				}
				sb.Append(' ');
			}
			ColorizePlainText(text, sb);
			return sb.ToString();
		}

		public void WithColorizedPrefix(int channel, string text, StringBuilder sb)
		{
			if(channel != 0)
			{
				AppendPrefixColorized(channel, sb);
				sb.Append(' ');
			}
			sb.Append(text);
		}

		public void WithColorizedPrefixes(int channel1, int channel2, string text, StringBuilder sb)
		{
			if(channel1 != 0)
			{
				AppendPrefixColorized(channel1, sb);
				if(channel2 != 0)
				{
					AppendPrefixColorized(channel2, sb);
				}
				sb.Append(' ');
			}
			sb.Append(text);
		}

		public string WithColorizedPrefixes(int channel1, int channel2, string text)
		{
			var sb = new StringBuilder();
			WithColorizedPrefixes(channel1, channel2, text, sb);
			return sb.ToString();
		}

		public void ColorizePlainTextWithPrefix(int channel, string text, StringBuilder sb)
        {
			if(channel != 0)
			{
				AppendPrefixColorized(channel, sb);
				sb.Append(' ');
			}
			ColorizePlainText(text, sb);
        }

		public void ColorizePlainTextWithPrefixes(int channel1, int channel2, string text, StringBuilder sb)
		{
			if(channel1 != 0)
			{
				AppendPrefixColorized(channel1, sb);
				if(channel2 != 0)
				{
					AppendPrefixColorized(channel2, sb);
				}
				sb.Append(' ');
			}			
			ColorizePlainText(text, sb);
		}

		public void ColorizePlainText(string text, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(text == null)
			{
				sb.Append(Null);
			}
			else if(colorize)
			{
				// Handle channel prefixes
				int channelTagsEnd = -1;
				if(StartsWithChannelPrefix(text))
				{
					int length = text.Length;
					for(int from = 0, to = text.IndexOf(EndChannel, 1); to != -1; to = text.IndexOf(EndChannel, from + 1))
					{
						string channel = text.Substring(from + 1, to - from - 1);
						sb.Append(GetColorizedChannelPrefix(channel));
						from = to + 1;
						channelTagsEnd = from;

						if(length <= from)
						{
							break;
						}

						if(text[from] != BeginChannel)
						{
							break;
						}
					}

					if(channelTagsEnd != -1)
					{
						if(length > channelTagsEnd)
						{
							sb.Append(ColorizePlainText(text.Substring(channelTagsEnd)));
						}
					}
				}

				if(channelTagsEnd == -1)
				{
					sb.Append(ColorizePlainText(text));
				}
			}
			else
            {
				sb.Append(text);
            }
			#else
			sb.Append(text == null ? NullUncolorized : text);
			#endif
		}

		[MethodImpl(AggressiveInlining)]
		public bool StartsWithChannelPrefix([NotNull] string text)
		{
			int length = text.Length;
			if(length <= 4 || text[0] != BeginChannel)
			{
				return false;
			}

			// Avoid interpreting json ouput starting with the [" characters as a channel prefix.
			if(!char.IsLetterOrDigit(text[1]))
			{
				return false;
			}

			int channelEnd = text.IndexOf(']', 1);
			if(channelEnd == -1)
			{
				return false;
			}

			// Avoid interpreting texts ending with the ] character as having channel prefixes.
			// It's more likely an array or json output or something else other than a channel prefix.
			if(channelEnd == length - 1)
			{
				return false;
			}

			return !Debug.channels.IgnoreUnlistedChannels || Debug.channels.Exists(text.Substring(1, channelEnd - 1));
		}

		[Pure]
		public string ColorizePlainText(string text)
		{
			#if UNITY_EDITOR
			if(text == null)
			{
				return colorize ? Null : NullUncolorized;
			}

			if(!colorize)
			{
				return text;
			}

			int length = text.Length;
			if(length == 0)
			{
				return text;
			}

			// for performance reasons don't colorize really long texts
			if(length >= 1920)
			{
				return text;
			}

			var sb = new StringBuilder();

			if(StartsWithChannelPrefix(text))
			{
				int channelTagsEnd = -1;
				for(int from = 0, to = text.IndexOf(EndChannel, 1); to != -1; to = text.IndexOf(EndChannel, from + 1))
				{
					string channel = text.Substring(from + 1, to - from - 1);
					sb.Append(GetColorizedChannelPrefix(channel));
					from = to + 1;
					channelTagsEnd = from;

					if(length <= from)
					{
						break;
					}

					if(text[from] != EndChannel)
					{
						break;
					}
				}
				if(channelTagsEnd != -1)
				{
					if(length > channelTagsEnd)
					{
						sb.Append(ColorizePlainText(text.Substring(channelTagsEnd)));
					}

					return sb.ToString();
				}
			}

			bool lessThanSignFound = false;

			var currentType = BlockFormatting.Unformatted;
			

			for(int index = 0; index < length; index++)
			{
				var currentChar = text[index];

				switch(currentChar)
				{
					case '\0':
						if(currentType != BlockFormatting.Unformatted)
						{
							if(currentType != BlockFormatting.Number)
							{
								sb.Append("null");
								break;
							}
							sb.Append("</color>");
							currentType = BlockFormatting.Unformatted;
						}
						sb.Append(Null);
						break;
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
					case '0':
						if(currentType != BlockFormatting.Unformatted)
						{
							sb.Append(currentChar);
							break;
						}

						// Only highlight numbers if not preceded by a letter.
						// E.g. skip colorizing numbers inside "Vector2" or "Color32".
						if(index > 0)
						{
							bool skipHighlightNumber = false;

							for(int prev = index - 1; prev >= 0; prev--)
							{
								if(char.IsLetter(text[prev]))
								{
									skipHighlightNumber = true;
									break;
								}
								if(!char.IsDigit(text[prev]))
								{
									break;
								}
							}

							if(skipHighlightNumber)
							{
								sb.Append(currentChar);
								break;
							}
						}

						currentType = BlockFormatting.Number;
						sb.Append(BeginNumeric);
						sb.Append(currentChar);
						break;
					case '\'':
						if(currentType != BlockFormatting.Unformatted)
						{
							if(currentType == BlockFormatting.Char)
							{
								sb.Append(currentChar);
								sb.Append("</color>");
								currentType = BlockFormatting.Unformatted;
								break;
							}
							else if(currentType == BlockFormatting.Number)
							{
								sb.Append("</color>");
								currentType = BlockFormatting.Unformatted;
							}
						}

						if(index + 2 < length && text[index + 2] == '\'')
						{
							currentType = BlockFormatting.Char;
							sb.Append(BeginString);
						}

						sb.Append('\'');
						break;
					case '\"':
						if(currentType != BlockFormatting.Unformatted)
						{
							if(currentType == BlockFormatting.String)
							{
								sb.Append('\"');
								sb.Append("</color>");
								currentType = BlockFormatting.Unformatted;
								break;
							}
							sb.Append("</color>");
						}
						currentType = BlockFormatting.String;
						sb.Append(BeginString);
						sb.Append(currentChar);
						break;
					case '\n':
					case '\r':
						if(currentType == BlockFormatting.Number)
						{
							sb.Append("</color>");
							currentType = BlockFormatting.Unformatted;
						}
						sb.Append(currentChar);
						break;
					case ':':
					case '=':
						if(currentType != BlockFormatting.Unformatted)
						{
							if(currentType != BlockFormatting.Number)
							{
								sb.Append(currentChar);
								break;
							}
							sb.Append("</color>");
							currentType = BlockFormatting.Unformatted;
						}
						sb.Append(BeginNameValueSeparator);
						sb.Append(currentChar);
						sb.Append("</color>");
						break;
					case '<':
						lessThanSignFound = true;
						if(currentType == BlockFormatting.Number)
						{
							sb.Append("</color>");
							currentType = BlockFormatting.Unformatted;
						}
						sb.Append('<');
						break;
					case '>':
						// if text might already contain rich text tags don't do any formatting
						if(lessThanSignFound)
						{
							return text;
						}
						if(currentType == BlockFormatting.Number)
						{
							sb.Append("</color>");
							currentType = BlockFormatting.Unformatted;
						}
						sb.Append('>');
						break;
					default:
						if(currentType != BlockFormatting.Unformatted)
						{
							if(currentType == BlockFormatting.Number)
							{
								sb.Append("</color>");
								currentType = BlockFormatting.Unformatted;
							}
						}
						else
						{
							bool skipAddChar = false;
							switch(currentChar)
							{
								// True
								case 'T':
								case 't':
									if(index + 3 < length)
									{
										if(char.ToUpperInvariant(text[index + 1]) == 'R' && char.ToUpperInvariant(text[index + 2]) == 'U' && char.ToUpperInvariant(text[index + 3]) == 'E')
										{
											if(length == index + 4 || !char.IsLetterOrDigit(text[index + 4]))
											{
												if(index == 0 || !char.IsLetterOrDigit(text[index - 1]))
												{
													skipAddChar = true;
													sb.Append(BeginTrue);
													sb.Append(text, index, 4);
													sb.Append("</color>");
													index += 3;
												}
											}
										}
									}
									break;
								// True
								case 'F':
								case 'f':
									if(index + 4 < length)
									{
										if(char.ToUpperInvariant(text[index + 1]) == 'A' && char.ToUpperInvariant(text[index + 2]) == 'L' && char.ToUpperInvariant(text[index + 3]) == 'S' && char.ToUpperInvariant(text[index + 4]) == 'E')
										{
											if(length == index + 5 || !char.IsLetterOrDigit(text[index + 5]))
											{
												if(index == 0 || !char.IsLetterOrDigit(text[index - 1]))
												{
													skipAddChar = true;
													sb.Append(BeginFalse);
													sb.Append(text, index, 5);
													sb.Append("</color>");
													index += 4;
												}
											}
										}
									}
									break;
								// Null
								case 'N':
								case 'n':
									if(index + 3 < length)
									{
										if(char.ToUpperInvariant(text[index + 1]) == 'U' && char.ToUpperInvariant(text[index + 2]) == 'L' && char.ToUpperInvariant(text[index + 3]) == 'L')
										{
											if(length == index + 4 || !char.IsLetterOrDigit(text[index + 4]))
											{
												if(index == 0 || !char.IsLetterOrDigit(text[index - 1]))
												{
													skipAddChar = true;
													sb.Append(BeginNull);
													sb.Append(text, index, 4);
													sb.Append("</color>");
													index += 3;
												}
											}
										}
									}
									break;
								// HTML Color tag. E.g. #FF0000 or #FF0000FF)
								case '#':
									if(index + 9 <= length)
									{
										Color color;
										string colorTag = text.Substring(index, 9);
										if(ColorUtility.TryParseHtmlString(colorTag, out color))
										{
											skipAddChar = true;
											sb.Append("<color=");
											sb.Append(colorTag);
											sb.Append(">");
											sb.Append(colorTag);
											sb.Append("</color>");
											index += 8;
											break;
										}
									}

									if(index + 7 <= length)
									{
										Color color;
										string colorTag = text.Substring(index, 7);
										if(ColorUtility.TryParseHtmlString(colorTag, out color))
										{
											skipAddChar = true;
											sb.Append("<color=");
											sb.Append(colorTag);
											sb.Append(">");
											sb.Append(colorTag);
											sb.Append("</color>");
											index += 6;
										}
									}
									break;
								// Color.ToString and Color32.ToString results.
								// E.g. RGBA(1.000, 0.000, 0.000, 1.000) or RGBA(255, 0, 0, 255)
								case 'R':
									if(text[index + 1] != 'G' || text[index + 2] != 'B' || text[index + 3] != 'A' || text[index + 4] != '(')
									{
										break;
									}

									// Color.ToString
									// RGBA(1.000, 1.000, 1.000, 1.000)
									if(index + 32 <= length)
									{
										if(text[index + 31] == ')')
										{
											float r, g, b, a;
											if(float.TryParse(text.Substring(index + 5, 5), out r) && float.TryParse(text.Substring(index + 12, 5), out g) && float.TryParse(text.Substring(index + 19, 5), out b) && float.TryParse(text.Substring(index + 26, 5), out a))
											{
												skipAddChar = true;
												sb.Append("<color=#");
												sb.Append(ColorUtility.ToHtmlStringRGBA(new Color(r, g, b, a)));
												sb.Append(">");
												sb.Append(text.Substring(index, 32));
												sb.Append("</color>");
												index += 31;
												break;
											}
										}
									}

									// Color32.ToString
									// RGBA(255, 0, 0, 255)
									if(index + 16 <= length)
									{
										int end = text.IndexOf(')', index + 15, Mathf.Min(9, length - index - 15));
										if(end != -1)
										{
											int start = index + 5;
											var rgba = text.Substring(start, end - start).Split(new string[]{", "}, StringSplitOptions.None);
											if(rgba.Length == 4)
											{
												byte r, g, b, a;
												if(byte.TryParse(rgba[0], out r) && byte.TryParse(rgba[1], out g) && byte.TryParse(rgba[2], out b) && byte.TryParse(rgba[3], out a))
												{
													skipAddChar = true;
													sb.Append("<color=#");
													sb.Append(ColorUtility.ToHtmlStringRGBA(new Color32(r, g, b, a)));
													sb.Append(">");
													sb.Append(text.Substring(index, end - index + 1));
													sb.Append("</color>");
													index = end;
													break;
												}
											}
										}
									}

									
									break;
							}
							if(skipAddChar)
							{
								break;
							}
						}

						sb.Append(currentChar);
						break;
				}
			}
			if(currentType != BlockFormatting.Unformatted)
			{
				sb.Append("</color>");
			}
			return sb.ToString();
			#else
			return text;
			#endif
		}

		public string Format([CanBeNull]string text)
		{
			#if UNITY_EDITOR
			int length;
			if(text == null)
			{
				text = "";
			}
			else
			{
				if(colorize && StartsWithChannelPrefix(text))
				{
					length = text.Length;
					var sb = new StringBuilder();
					for(int from = 0, to = text.IndexOf(EndChannel, 1); to != -1; to = text.IndexOf(EndChannel, from + 1))
					{
						string channel = text.Substring(from + 1, to - from - 1);
						AddPrefixColorized(channel, sb);
						from = to + 1;
						if(length <= from)
						{
							break;
						}

						if(text[from] != BeginChannel)
						{
							sb.Append(text.Substring(from));
							break;
						}
					}
					if(sb.Length != 0)
					{
						text = sb.ToString();
					}
				}
			}

			switch(defaultFormatting)
			{
				case Formatting.Clean:
					return text + "\n";
				case Formatting.LargeFont:
					return FormatLarge(text);
				case Formatting.Auto:
					return text.IndexOf('\n') == -1 ? FormatLarge(text) : text;
				default:
					return text;
			}
			#else
			return text;
			#endif
		}

		public void Format(StringBuilder sb)
        {
			switch(defaultFormatting)
			{
				case Formatting.Clean:
					sb.Append("\n");
					return;
				case Formatting.LargeFont:
					FormatLarge(sb);
					return;
				case Formatting.Auto:
					if(!sb.Contains('\n'))
					{
						FormatLarge(sb);
					}
					return;
			}
        }

		public string FormatUncolorized(string format, object[] args)
		{
			if(args == null)
			{
				return format;
			}
			int count = args.Length;
			var argsToString = new string[count];
			for(int n = count - 1; n >= 0; n--)
			{
				argsToString[n] = ToStringUncolorized(args[n], false);
			}
			return string.Format(format, argsToString);
		}

		public string FormatColorized(string format, object[] args)
		{
			#if UNITY_EDITOR
			if(args == null)
			{
				return format;
			}
			int count = args.Length;
			var argsColorized = new string[count];
			for(int n = count - 1; n >= 0; n--)
			{
				argsColorized[n] = ToStringColorized(args[n], false);
			}
			return string.Format(format, argsColorized);
			#else
			return FormatUncolorized(format, args);
			#endif
		}

		public void JoinUncolorizedWithSeparatorCharacter([CanBeNull]Expression<Func<object>>[] classMembers, StringBuilder sb)
		{
			if(classMembers == null)
			{
				sb.Append(NullUncolorized);
				return;
			}

			int count = classMembers.Length;
			if(count == 0)
			{
				return;
			}

			ToStringUncolorized(classMembers[0], sb);
			for(int n = 1; n < count; n++)
			{
				sb.Append(Environment.NewLine);
				ToStringUncolorized(classMembers[n], sb);
			}

			bool singleLine = sb.Length <= maxLengthBeforeLineSplitting;
			if(singleLine)
			{
				sb.Replace(Environment.NewLine, MultipleEntrySeparator);
			}
		}

		public void JoinColorizedWithSeparatorCharacter([CanBeNull]Expression<Func<object>>[] classMembers, StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(classMembers == null)
			{
				sb.Append(Null);
				return;
			}

			int count = classMembers.Length;
			if(count == 0)
			{
				return;
			}

			ToStringColorized(classMembers[0], sb);
			for(int n = 1; n < count; n++)
			{
				sb.Append(Environment.NewLine);
				ToStringColorized(classMembers[n], sb);
			}

			bool singleLine = sb.Length <= maxLengthBeforeLineSplitting;
			if(singleLine)
			{
				sb.Replace(Environment.NewLine, MultipleEntrySeparator);
			}
			#else
			JoinUncolorizedWithSeparatorCharacter(classMembers, sb);
			#endif
		}

		public string GetColorizedChannelPrefix(string channel)
		{
			#if UNITY_EDITOR
			return Debug.channels.GetRichTextPrefix(channel);
			#else
			return "[" + channel + "]";
			#endif
		}

		public void AddPrefixColorized(string channel, StringBuilder sb)
		{
			#if UNITY_EDITOR
			Debug.channels.GetRichTextPrefix(channel, sb);
			#else
			sb.Append('[');
			sb.Append(channel);
			sb.Append(']');
			#endif
		}

		public string GetColorizedChannelPrefix(int channel)
		{
			#if UNITY_EDITOR
			return channel == 0 ? "" : Debug.channels.GetRichTextPrefix(channel);
			#else
			return "[" + Channels.Get(channel) + "]";
			#endif
		}

		public void AppendPrefixColorized(int channel, StringBuilder sb)
		{
			#if DEV_MODE
			Debug.Assert(channel != 0);
			#endif

			#if UNITY_EDITOR
			Debug.channels.GetRichTextPrefix(channel, sb);
			#else
			sb.Append('[');
			sb.Append(Channels.Get(channel));
			sb.Append(']');
			#endif
		}

		public void AppendPrefixUncolorized(int channel, StringBuilder sb)
		{
			#if DEV_MODE
			Debug.Assert(channel != 0);
			#endif

			sb.Append('[');
			sb.Append(Channels.Get(channel));
			sb.Append(']');
		}

		public void AddPrefixesUncolorized(int channel1, int channel2, StringBuilder sb)
		{
			#if DEV_MODE
			Debug.Assert(channel1 != 0);
			Debug.Assert(channel2 != 0);
			#endif

			sb.Append('[');
			sb.Append(Channels.Get(channel1));
			sb.Append("][");
			sb.Append(Channels.Get(channel2));
			sb.Append(']');
		}

		public void AddPrefixesColorized(int channel1, int channel2, StringBuilder sb)
		{
			#if UNITY_EDITOR
			Debug.channels.GetRichTextPrefix(channel1, sb);
			Debug.channels.GetRichTextPrefix(channel2, sb);
			#else
			sb.Append('[');
			sb.Append(Channels.Get(channel1));
			sb.Append("][");
			sb.Append(Channels.Get(channel2));
			sb.Append(']');
			#endif
		}

		public string JoinUncolorized(string[] parts)
		{
			if(parts == null)
			{
				return NullUncolorized;
			}
			var sb = new StringBuilder();
			for(int n = 0, count = parts.Length; n < count; n++)
			{
				sb.Append(parts[n]);
			}
			return sb.ToString();
		}

		public string JoinUncolorized(string part1, string[] parts)
		{
			var sb = new StringBuilder();
			sb.Append(part1);
			for(int n = 0, count = parts.Length; n < count; n++)
			{
				sb.Append(parts[n]);
			}
			return sb.ToString();
		}

		public string JoinUncolorized(string part1, string part2, string[] parts)
		{
			var sb = new StringBuilder();
			sb.Append(part1);
			sb.Append(part2);
			for(int n = 0, count = parts.Length; n < count; n++)
			{
				sb.Append(parts[n]);
			}
			return sb.ToString();
		}

		public string JoinColorized(string[] parts)
		{
			#if UNITY_EDITOR
			if(parts == null)
			{
				return Format(Null);
			}

			int count = parts.Length;

			switch(parts.Length)
			{
				case 0:
					return EmptyCollection;
				case 1:
					return ColorizePlainText(parts[0]);
				default:
					var sb = new StringBuilder();
					for(int n = 0; n < count; n++)
					{
						sb.Append(ColorizePlainText(parts[n]));
					}
					return sb.ToString();
			}
			#else
			return JoinUncolorized(parts);
			#endif
		}

		public string JoinColorized(string messagePrefix, string arg, params string[] args)
		{
			var sb = new StringBuilder();
			sb.Append(ColorizePlainText(messagePrefix));
			sb.Append(ColorizePlainText(arg));
			for(int n = 0, count = args == null ? 0 : args.Length; n < count; n++)
			{
				sb.Append(ColorizePlainText(args[n]));
			}
			return sb.ToString();
		}

		public string FormatLarge([CanBeNull]string text)
		{
			#if UNITY_EDITOR
			if(string.IsNullOrEmpty(text))
			{
				return text;
			}

			int lineEnd = text.IndexOf('\n');
			if(lineEnd != -1)
			{
				return BeginLargeSize + text.Substring(0, lineEnd) + "</size>" + text.Substring(lineEnd);
			}
			
			if(text.Length >= 16 && text.IndexOf("</size>", 9, StringComparison.OrdinalIgnoreCase) != -1)
			{
				return text;
			}

			return BeginLargeSize + text + "</size>";
			#else
			return text;
			#endif
		}

		public void FormatLarge(StringBuilder sb)
		{
			#if UNITY_EDITOR
			if(sb.Length == 0)
			{
				return;
			}

			string text = sb.ToString();

			int lineEnd = text.IndexOf('\n');
			if(lineEnd != -1)
			{
				sb.Length = 0;
				sb.Append(BeginLargeSize);
				sb.Append(text.Substring(0, lineEnd));
				sb.Append("</size>");
				sb.Append(text.Substring(lineEnd));
				return;
			}
			
			if(text.Length >= 16 && text.IndexOf("</size>", 9, StringComparison.OrdinalIgnoreCase) != -1)
			{
				return;
			}

			sb.Length = 0;
			sb.Append(BeginLargeSize);
			sb.Append(text);
			sb.Append("</size>");
			#endif
		}

		/// <summary>
		/// Cleans up the unnecessary clutter from stack trace by removing first row from it.
		/// </summary>
		public string CleanUpStackTrace(string stackTrace)
		{
			int i = stackTrace.IndexOf('\n');
			if(i != -1)
			{
				return stackTrace.Substring(i);
			}
			return stackTrace;
		}

		#if DEV_MODE //TEST - this can be used to log with cleaner stack trace. however it breaks double-click to go to line feature so it's not used currently.
		public string FormatWithStackTrace(string text, string stackTrace)
		{
			int length;
			if(text == null)
			{
				text = "";
			}
			else
			{
				length = text.Length;

				if(length >= 3 && colorize && text[0] == BeginCollection)
				{
					int channelEnd = text.IndexOf(EndCollection, 1);
					if(channelEnd != -1)
					{
						string channel = text.Substring(1, channelEnd - 1);
						text = GetColorizedChannelPrefix(channel) + text.Substring(channelEnd + 1);
					}
				}
			}

			switch(defaultFormatting)
			{
				case Formatting.Clean:
					return text + "\n" + CleanUpStackTrace(stackTrace);
				case Formatting.LargeFont:
					return FormatLarge(text) + "\n" + CleanUpStackTrace(stackTrace);
				case Formatting.Auto:
					return (text.IndexOf('\n') == -1 ? FormatLarge(text) : text) + "\n" + CleanUpStackTrace(stackTrace);
				default:
					return text + CleanUpStackTrace(stackTrace);
			}
		}
		#endif

		/// <summary>
		/// Gets the full hierarchy path of the given <paramref name="transform"/>.
		/// </summary>
		/// <param name="transform"> The <see cref="Transform"/> component of a <see cref="GameObject"/> whose hierarchy path to return. </param>
		/// <returns> <see cref="string"/> containing the <see cref="Transform.name">name</see> of the transform and all its <see cref="Transform.parent">parents</see> separated by a '/' character. </returns>
		public string GetFullHierarchyPath([CanBeNull]Transform transform)
		{
			if(transform == null)
			{
				return colorize ? Null : NullUncolorized;
			}

			var sb = new StringBuilder();
			
			sb.Append(transform.name);

			if(colorize)
			{
				sb.Append("\"</color>");
			}
			else
			{
				sb.Append("\"");
			}

			while(transform.parent != null)
			{
				transform = transform.parent;
				sb.Insert(0, "/");
				sb.Insert(0, transform.name);
			}

			if(colorize)
			{
				sb.Insert(0, BeginStringWithQuotationMark);
			}
			else
			{
				sb.Insert(0, "\"");
			}

			return sb.ToString();
		}

		public string GetHierarchyPathToSelectionBase([NotNull]Transform transform)
		{
			var sb = new StringBuilder();

			sb.Append(transform.name);

			if(colorize)
			{
				sb.Append("\"</color>");
			}
			else
			{
				sb.Append("\"");
			}

			while(transform.parent != null)
			{
				transform = transform.parent;
				sb.Insert(0, "/");
				sb.Insert(0, transform.name);

				var components = transform.GetComponents<Component>();
				for(int n = components.Length - 1; n >= 1; n--) //skip checking Transform
				{
					var component = components[n];
					if(component != null)
					{
						if(component.GetType().GetCustomAttribute<SelectionBaseAttribute>() != null)
						{
							if(colorize)
							{
								sb.Insert(0, BeginStringWithQuotationMark);
							}
							else
							{
								sb.Insert(0, "\"");
							}
							return sb.ToString();
						}
					}
				}
			}

			if(colorize)
			{
				sb.Insert(0, BeginStringWithQuotationMark);
			}
			else
			{
				sb.Insert(0, "\"");
			}

			return sb.ToString();
		}
	}
}