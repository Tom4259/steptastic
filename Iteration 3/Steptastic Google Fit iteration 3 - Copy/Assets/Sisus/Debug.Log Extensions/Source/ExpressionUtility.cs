#define SAFE_MODE

//#define DEBUG_GET_OWNER

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Expression = System.Linq.Expressions.Expression;
using Object = UnityEngine.Object;

namespace Sisus.Debugging
{
	public static class ExpressionUtility
	{
		public static object GetValue(Expression<Func<object>> expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			if(expression.NodeType == ExpressionType.Lambda)
			{
				var lambdaExpression = (LambdaExpression)expression;

				var compiled = (Func<object>)lambdaExpression.Compile();

				#if DEV_MODE
				Debug.Assert(compiled != null);
				#endif

				// WARNING: Using compiled.DynamicInvoke(null) can cause Editor freezing! Always use compiled() instead.
				return compiled();
			}
			return null;
		}

		[CanBeNull]
		public static object GetValue([NotNull]Expression expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			switch(expression.NodeType)
			{
				case ExpressionType.Constant:
					var constant = (ConstantExpression)expression;
					return constant.Value;
				case ExpressionType.MemberAccess:
					var memberExpression = (MemberExpression)expression;
					var memberInfo = memberExpression.Member;
				
					var field = memberInfo as FieldInfo;
					if(field != null)
					{
						// memberExpression.Expression gets the containing object of the field
						return field.GetValue(field.IsStatic ? null : GetValue(memberExpression.Expression));
					}
				
					var property = memberInfo as PropertyInfo;
					if(property != null)
					{
						if(!property.CanRead)
						{
							UnityEngine.Debug.LogError("Log.GetValue expression contained property with no get accessor.");
							return null;
						}
						// memberExpression.Expression gets the containing object of the property
						return property.GetValue(property.GetGetMethod(true).IsStatic ? null : GetValue(memberExpression.Expression), null);
					}
				
					var method = memberInfo as MethodInfo;
					if(method != null)
					{
						if(method.ReturnType == typeof(void))
						{
							UnityEngine.Debug.LogError("Validate.Arguments given argument that was a method with no return type.");
							return typeof(void);
						}

						// TO DO: Support default arguments
						if(method.GetParameters().Length > 0)
						{
							UnityEngine.Debug.LogError("Validate.Arguments given argument that was a method with parameters.");
							return method;
						}

						// memberExpression.Expression gets the containing object of the method
						return method.Invoke(method.IsStatic ? null : GetValue(memberExpression.Expression), null);
					}

					UnityEngine.Debug.LogError("Validate.Arguments given argument was of type MemberAccess but was not a field, property or method.");
					return null;
				case ExpressionType.Lambda:
					var lambdaExpression = (LambdaExpression)expression;
					var compiled = lambdaExpression.Compile() as Func<object>;
					if(compiled == null)
					{
						UnityEngine.Debug.LogError("Lambda expression target could not be cast to Func<object>.");
						return null;
					}

					//return compiled.DynamicInvoke(null); // <---- NOTE: this can cause Editor freezing!
					return compiled();
				case ExpressionType.Call:
					lambdaExpression = expression as LambdaExpression;
					if(lambdaExpression != null)
					{
						compiled = lambdaExpression.Compile() as Func<object>;
						if(compiled == null)
						{
							UnityEngine.Debug.LogError("Call expression target could not be cast to Func<object>.");
							return null;
						}

						//return compiled.DynamicInvoke(null); // <---- this can cause Editor freezing!
						return compiled();
					}

					var methodCallExpression = (MethodCallExpression)expression;
					method = methodCallExpression.Method;

					if(method == null)
					{
						UnityEngine.Debug.LogError("Validate given argument was of type Call but MethodInfo was null.");
						return null;
					}

					if(method.GetParameters().Length > 0)
					{
						UnityEngine.Debug.LogError("Validate given argument was of type Call but MethodInfo had parameters.");
						return null;
					}

					return method.Invoke(method.IsStatic ? null : GetValue(methodCallExpression.Object), null);
				default:
					UnityEngine.Debug.LogError("Validate provided argument was not a constant, MemberAccess or Call: " + expression.NodeType);
					return null;
			}
		}

		public static Object GetContext([NotNull]Expression<Func<object>> expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			var value = GetValue(expression) as Object;
			if(value != null)
			{
				return value;
			}
			return GetOwner(expression);
		}

		public static Object GetOwner([NotNull]Expression<Func<object>> expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			if(expression.NodeType == ExpressionType.Lambda)
			{
				var lambdaExpression = (LambdaExpression)expression;
				var unaryExpression = lambdaExpression.Body as UnaryExpression;
				if(unaryExpression != null)
				{
					var operand = unaryExpression.Operand as MemberExpression;
					if(operand != null)
					{
						return GetOwner(operand);
					}
					#if DEV_MODE
					else { UnityEngine.Debug.Log("LambdaExpression unaryExpression.Operand " + (unaryExpression.Operand == null ? "null" : unaryExpression.Operand.GetType().Name) + " not MemberExpression"); }
					#endif
				}
				else
				{
					var memberExpression = lambdaExpression.Body as MemberExpression;
					if(memberExpression != null)
					{
						return GetOwner(memberExpression);
					}
					#if DEV_MODE
					else { UnityEngine.Debug.Log("LambdaExpression body "+ lambdaExpression.Body.GetType().Name+ " not UnaryExpression nor MemberExpression"); }
					#endif
				}
			}
			#if DEV_MODE
			else { UnityEngine.Debug.Log("null with expression="+expression.GetType().Name+ ", NodeType="+ expression.NodeType); }
			#endif

			return null;
		}

		[CanBeNull]
		public static Object GetOwner([NotNull]Expression expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			switch(expression.NodeType)
			{
				case ExpressionType.Lambda:
					var lambdaExpression = (LambdaExpression)expression;
					var unaryExpression = lambdaExpression.Body as UnaryExpression;
					if(unaryExpression != null)
					{
						var operand = unaryExpression.Operand as MemberExpression;
						if(operand != null)
						{
							return GetOwner(operand);
						}
						#if DEV_MODE && DEBUG_GET_OWNER
						UnityEngine.Debug.Log("LambdaExpression unaryExpression.Operand " + (unaryExpression.Operand == null ? "null" : unaryExpression.Operand.GetType().Name) + " not MemberExpression");
						#endif
					}
					#if DEV_MODE && DEBUG_GET_OWNER
					else { UnityEngine.Debug.Log("LambdaExpression body "+ lambdaExpression.Body.GetType().Name+ " no UnaryExpression"); }
					#endif
					return null;
				case ExpressionType.MemberAccess:
					var memberExpression = (MemberExpression)expression;
					#if DEV_MODE && DEBUG_GET_OWNER
					UnityEngine.Debug.Log("GetValue("+memberExpression.Expression.GetType()+"): "+(GetValue(memberExpression.Expression) == null ? "null" : GetValue(memberExpression.Expression).GetType().Name) +" as Object");
					#endif
					return GetValue(memberExpression.Expression) as Object;
				case ExpressionType.Call:
					var methodCallExpression = (MethodCallExpression)expression;
					if(methodCallExpression != null)
					{
						return GetValue(methodCallExpression.Object) as Object;
					}
					return null;
				default:
					return null;
			}
		}

		[CanBeNull]
		public static string GetMemberName([NotNull]Expression<Func<object>> expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			var memberInfo = GetMemberInfo(expression);
			if(memberInfo == null)
			{
				return null;
			}
			return memberInfo.Name;
		}

		[CanBeNull]
		public static string GetMemberName([NotNull]Expression expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			var memberInfo = GetMemberInfo(expression);
			if(memberInfo == null)
			{
				return null;
			}
			return memberInfo.Name;
		}

		[CanBeNull]
		public static Type GetMemberType([NotNull]Expression<Func<object>> expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			var memberInfo = GetMemberInfo(expression);
			if(memberInfo == null)
			{
				return null;
			}

			var field = memberInfo as FieldInfo;
			if(field != null)
			{
				return field.FieldType;
			}
			var property = memberInfo as PropertyInfo;
			if(property != null)
			{
				return property.PropertyType;
			}
			var method = memberInfo as MethodInfo;
			if(method != null)
			{
				return method.ReturnType;
			}
			return null;
		}

		[CanBeNull]
		public static MemberInfo GetMemberInfo([NotNull]Expression<Func<object>> expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			if(expression.NodeType == ExpressionType.Lambda)
			{
				var lambdaExpression = (LambdaExpression)expression;
				var body = lambdaExpression.Body;

				var unaryExpression = body as UnaryExpression;
				if(unaryExpression != null)
				{
					var operand = unaryExpression.Operand as MemberExpression;
					if(operand != null)
					{
						return operand.Member;
					}

					return null;
				}

				var memberExpression = body as MemberExpression;
				if(memberExpression != null)
				{
					return memberExpression.Member;
				}
			}
			return null;
		}

		[CanBeNull]
		public static MemberInfo GetMemberInfo([NotNull]Expression expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return null;
			}

			switch(expression.NodeType)
			{
				case ExpressionType.Lambda:
					var lambdaExpression = (LambdaExpression)expression;
					var body = lambdaExpression.Body;
					var unaryExpression = body as UnaryExpression;
					if(unaryExpression != null)
					{
						var operand = unaryExpression.Operand as MemberExpression;
						if(operand != null)
						{
							return operand.Member;
						}
					}

					var methodCallExpression = body as MethodCallExpression;
					if(methodCallExpression != null)
					{
						return methodCallExpression.Method;
					}
					return null;
				case ExpressionType.MemberAccess:
					var memberExpression = (MemberExpression)expression;
					return memberExpression.Member;
				case ExpressionType.Call:
					methodCallExpression = (MethodCallExpression)expression;
					if(methodCallExpression != null)
					{
						return methodCallExpression.Method;
					}
					return null;
				default:
					return null;
			}
		}

		public static string TargetToString(Expression expression)
		{
			if(expression == null)
			{
				UnityEngine.Debug.LogError("Expression was null.");
				return "null";
			}

			switch(expression.NodeType)
			{
				case ExpressionType.Lambda:
					var lambdaExpression = (LambdaExpression)expression;
					var body = lambdaExpression.Body;
					var unaryExpression = body as UnaryExpression;
					if(unaryExpression != null)
					{
						var operand = unaryExpression.Operand as MemberExpression;
						if(operand != null)
						{
							var memberInfo = operand.Member;
							if(memberInfo != null)
							{
								return ToString(memberInfo);
							}
						}
					}

					var methodCallExpression = body as MethodCallExpression;
					if(methodCallExpression != null)
					{
						var methodInfo = methodCallExpression.Method;
						if(methodInfo != null)
						{
							var parameters = methodInfo.GetParameters();
							int parameterCount = parameters.Length;
							if(parameterCount == 0)
							{
								return ToString(methodInfo);
							}

							#if !SAFE_MODE
							var arguments = methodCallExpression.Arguments;
							if(parameterCount > arguments.Count)
							{
								parameterCount = arguments.Count;
							}
							#endif
							
							var sb = new StringBuilder(32);
							sb.Append(methodInfo.DeclaringType.Name);
							sb.Append('.');
							sb.Append(methodInfo.Name);
							sb.Append('(');

							
							sb.Append(ToString(parameters[0].ParameterType));
							sb.Append(' ');
							sb.Append(parameters[0].Name);

							#if !SAFE_MODE
							object argumentValue;
							if(TryGetArgumentValue(arguments[0], out argumentValue))
							{
								sb.Append(" = ");
								sb.Append(argumentValue);
							}
							#endif

							for(int n = 1; n < parameterCount; n++)
							{
								sb.Append(", ");
								
								sb.Append(ToString(parameters[n].ParameterType));
								sb.Append(' ');
								sb.Append(parameters[n].Name);

								#if !SAFE_MODE
								if(TryGetArgumentValue(arguments[n], out argumentValue))
								{
									sb.Append(" = ");
									sb.Append(argumentValue);
								}
								#endif
							}

							sb.Append(')');
							return sb.ToString();
						}
					}
					return "null";
				case ExpressionType.MemberAccess:
					var memberExpression = (MemberExpression)expression;
					return ToString(memberExpression.Member);
				case ExpressionType.Call:
					methodCallExpression = (MethodCallExpression)expression;
					if(methodCallExpression != null)
					{
						return ToString(methodCallExpression.Method);
					}
					return "null";
				default:
					return "null";
			}
		}

		public static bool TryGetArgumentValue(Expression argument, out object value)
		{
			#if SAFE_MODE
			value = null;
			return false;
			#else
			if(argument == null)
			{
				value = null;
				return false;
			}

			LambdaExpression lambdaExpression;
			if(argument.NodeType == ExpressionType.Lambda)
			{
				lambdaExpression = (LambdaExpression)argument;
			}
			else
			{
				lambdaExpression = Expression.Lambda(argument);
			}

			var compiled = lambdaExpression.Compile();

			#if DEV_MODE
			Debug.Assert(compiled != null);
			#endif

			var func = compiled as Func<object>;
			if(func != null)
			{
				value = func();
				return true;
			}
			
			// This might cause Editor freezing if called during Update.
			value = compiled.DynamicInvoke(null);
			return true;
			#endif
		}


		public static string ToString([CanBeNull]MemberInfo memberInfo)
		{
			if(memberInfo == null)
			{
				return "null";
			}

			var methodInfo = memberInfo as MethodInfo;
			if(methodInfo != null)
			{
				return ToString(methodInfo);
			}

			var sb = new StringBuilder(32);
			sb.Append(ToString(memberInfo.DeclaringType));
			sb.Append('.');
			sb.Append(memberInfo.Name);
			return sb.ToString();
		}

		public static string ToString([CanBeNull]MethodInfo methodInfo)
		{
			if(methodInfo == null)
			{
				return "null";
			}

			var sb = new StringBuilder(32);
			sb.Append(methodInfo.DeclaringType.Name);
			sb.Append('.');
			sb.Append(methodInfo.Name);
			sb.Append('(');
			var parameters = methodInfo.GetParameters();
			int parameterCount = parameters.Length;
			if(parameterCount > 0)
			{
				var parameter = parameters[0];
				sb.Append(ToString(parameter.ParameterType));
				sb.Append(' ');
				sb.Append(parameter.Name);

				for(int n = 1; n < parameterCount; n++)
				{
					sb.Append(", ");
					parameter = parameters[n];
					sb.Append(ToString(parameter.ParameterType));
					sb.Append(' ');
					sb.Append(parameter.Name);
				}
			}
			sb.Append(')');
			return sb.ToString();
		}

		public static string ToString([CanBeNull]Type type)
		{
			if(type == null)
			{
				return "null";
			}

			// important to check for IsEnum before GetTypeCode, because GetTypeCode returns the underlying type for enums!
			if(type.IsEnum)
			{
				return type.FullName;
			}

			switch(Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
					return "bool";
				case TypeCode.Char:
					return "char";
				case TypeCode.SByte:
					return "sbyte";
				case TypeCode.Byte:
					return "byte";
				case TypeCode.Int16:
					return "short";
				case TypeCode.UInt16:
					return "ushort";
				case TypeCode.Int32:
					return "int";
				case TypeCode.UInt32:
					return "uint";
				case TypeCode.Int64:
					return "long";
				case TypeCode.UInt64:
					return "ulong";
				case TypeCode.Single:
					return "float";
				case TypeCode.Double:
					return "double";
				case TypeCode.Decimal:
					return "decimal";
				case TypeCode.String:
					return "string";
			}

			if(type.IsGenericType)
			{
				var sb = new StringBuilder();
				sb.Append(type.Name);
				sb.Append('<');
				var genericTypes = type.GetGenericArguments();
				sb.Append(genericTypes[0]);
				int genericTypeCount = genericTypes.Length;
				for(int n = 1; n < genericTypeCount; n++)
				{
					sb.Append(", ");
					sb.Append(genericTypes[n].Name);
				}
				sb.Append('>');
				return sb.ToString();
			}

			return type.Name;
		}

		public static bool Equals(Expression x, Expression y)
		{
			if(x == y)
			{
				return true;
			}

			if(x == null || y == null || x.NodeType != y.NodeType || x.Type != y.Type)
			{
				return false;
			}

			switch(x.NodeType)
			{
				case ExpressionType.Constant:
					var xConstant = (ConstantExpression)x;
					var yConstant = (ConstantExpression)y;
					return xConstant.Value == yConstant.Value;
				case ExpressionType.MemberAccess:
					var xMemberExpression = (MemberExpression)x;
					var xMemberInfo = xMemberExpression.Member;

					var yMemberExpression = (MemberExpression)y;
					var yMemberInfo = yMemberExpression.Member;
					return xMemberInfo == yMemberInfo;
				case ExpressionType.Lambda:
					var xLambdaExpression = (LambdaExpression)x;
					var yLambdaExpression = (LambdaExpression)y;

					var xUnaryExpression = xLambdaExpression.Body as UnaryExpression;
					if(xUnaryExpression != null)
					{
						var yUnaryExpression = yLambdaExpression.Body as UnaryExpression;
						if(yUnaryExpression == null)
						{
							return false;
						}

						var xOperand = xUnaryExpression.Operand as MemberExpression;
						if(xOperand != null)
						{
							var yOperand = yUnaryExpression.Operand as MemberExpression;
							if(yOperand == null)
							{
								return false;
							}

							return xOperand.Member == yOperand.Member;
						}
					}
					else
					{
						xMemberExpression = xLambdaExpression.Body as MemberExpression;
						if(xMemberExpression != null)
						{
							yMemberExpression = yLambdaExpression.Body as MemberExpression;
							if(yMemberExpression == null)
							{
								return false;
							}

							return xMemberExpression.Member == yMemberExpression.Member;
						}
					}

					var xCompiled = xLambdaExpression.Compile() as Func<object>;
					var yCompiled = yLambdaExpression.Compile() as Func<object>;

					if(xCompiled == null)
					{
						return yCompiled == null;
					}

					if(yCompiled == null)
					{
						return false;
					}

					var xValue = xCompiled();
					var yValue = yCompiled();
					if(xValue == null)
					{
						return yValue == null;
					}
					if(yValue == null)
					{
						return false;
					}
					return xValue.Equals(yValue);
				case ExpressionType.Call:
					xLambdaExpression = (LambdaExpression)x;
					if(xLambdaExpression != null)
					{
						yLambdaExpression = (LambdaExpression)y;
						if(yLambdaExpression == null)
						{
							return false;
						}

						xCompiled = xLambdaExpression.Compile() as Func<object>;
						yCompiled = yLambdaExpression.Compile() as Func<object>;
						return xCompiled == null ? yCompiled == null : xCompiled() == yCompiled();
					}

					var xMethodCallExpression = (MethodCallExpression)x;
					var yMethodCallExpression = (MethodCallExpression)y;
					var xMethod = xMethodCallExpression.Method;
					var yMethod = yMethodCallExpression.Method;
					return xMethod == yMethod;
				default:
					UnityEngine.Debug.LogError("Validate provided argument was not a constant, MemberAccess or Call: " + x.NodeType);
					return true;
			}
		}
	}
}