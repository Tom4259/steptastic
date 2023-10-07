using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace Sisus.Debugging
{
	public static class MemberInfoExtensions
	{
		[NotNull]
		public static Func<object> GetValueDelegate(this FieldInfo fieldInfo, object memberOwner)
		{
			var ownerExpression = Expression.Constant(memberOwner);
			var fieldExpression = Expression.Field(ownerExpression, fieldInfo.Name);
			var lambda = Expression.Lambda<Func<object>>(fieldExpression);
			return lambda.Compile();
		}

		[NotNull]
		public static Func<object> GetValueDelegate(this PropertyInfo propertyInfo, object memberOwner)
		{
			var ownerExpression = Expression.Constant(memberOwner);
			var fieldExpression = Expression.Property(ownerExpression, propertyInfo.Name);
			var lambda = Expression.Lambda<Func<object>>(fieldExpression);
			return lambda.Compile();
		}

		[NotNull]
		public static Func<object> GetValueDelegate(this MemberInfo memberInfo, object memberOwner)
		{
			var ownerExpression = Expression.Constant(memberOwner);
			var memberExpression = Expression.PropertyOrField(ownerExpression, memberInfo.Name);
			var convertedToObjectType = Expression.Convert(memberExpression, typeof(object));
			var lambda = Expression.Lambda<Func<object>>(convertedToObjectType);
			return lambda.Compile();
		}

		[CanBeNull]
		public static Type Type(this MemberInfo memberInfo)
		{
			var fieldInfo = memberInfo as FieldInfo;
			if(fieldInfo != null)
			{
				return fieldInfo.FieldType;
			}
			var propertyInfo = memberInfo as PropertyInfo;
			if(propertyInfo != null)
			{
				return propertyInfo.PropertyType;
			}
			var methodInfo = memberInfo as MethodInfo;
			if(methodInfo != null)
			{
				return methodInfo.ReturnType;
			}
			return null;
		}
	}
}