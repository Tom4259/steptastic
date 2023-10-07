using System;
using System.Linq.Expressions;
using UnityEngine;
using JetBrains.Annotations;

namespace Sisus.Debugging
{
	public class ButtonDisplayer : IDisplayable
	{
		public readonly GUIContent label;
		public readonly Expression<Action> onClickedExpression;
		public readonly Action onClicked;

		public bool WantsRepaint
		{
			get;
			set;
		}

		public ButtonDisplayer([NotNull]string buttonText, [NotNull]Action onButtonClicked)
		{
			label = new GUIContent(buttonText);
			onClicked = onButtonClicked;
			onClickedExpression = null;
		}

		public ButtonDisplayer(GUIContent buttonLabel, [NotNull]Action onButtonClicked)
		{
			label = buttonLabel;
			onClicked = onButtonClicked;
			onClickedExpression = null;
		}

		public ButtonDisplayer(Expression<Action> onButtonClicked)
		{
			label = new GUIContent("Button");
			onClickedExpression = onButtonClicked;
			var methodCallExpression = onButtonClicked.Body as MethodCallExpression;
			if(methodCallExpression != null)
			{
				var method = methodCallExpression.Method;
				if(method != null)
				{
					label.text = method.Name;
				}
			}
			onClicked = onButtonClicked.Compile();
		}

		public bool TargetEquals(Expression<Action> otherExpression)
		{
			return otherExpression != null && ExpressionUtility.Equals(onClickedExpression, otherExpression);
		}

		public bool TargetEquals(string otherText)
		{
			return string.Equals(label.text, otherText);
		}

		public bool TargetEquals(GUIContent otherLabel)
		{
			return otherLabel != null && string.Equals(label.text, otherLabel.text);
		}

		public bool TargetEquals(object other)
		{
			var otherExpression = other as Expression<Action>;
			if(otherExpression != null)
			{
				return ExpressionUtility.Equals(onClickedExpression, otherExpression);
			}

			var otherText = other as string;
			if(otherText != null)
			{
				return string.Equals(label.text, otherText);
			}
			
			var otherLabel = other as GUIContent;
			if(otherLabel != null)
			{
				return string.Equals(label.text, otherLabel.text);
			}

			return false;
		}

		public void Draw(GUIStyle guiStyle)
		{
			if(GUILayout.Button(label))
			{
				onClicked();
			}
		}
	}
}