using System;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Sisus.Debugging
{
	public class ValueDisplayer : IDisplayable, IUpdatable
	{
		public readonly Expression<Func<object>> visualizedMember;
		public readonly Func<object> valueGetter;
		private readonly DebugFormatter formatter;

		private object lastValue;
		private string text;

		public bool WantsRepaint { get; set; }

		public ValueDisplayer(Expression<Func<object>> visualizeMember, DebugFormatter setFormatter)
		{
			visualizedMember = visualizeMember;
			var lambdaExpression = (LambdaExpression)visualizeMember;
			valueGetter = (Func<object>)lambdaExpression.Compile();
			formatter = setFormatter;

			lastValue = ExpressionUtility.GetValue(visualizedMember);
			text = formatter.ToStringColorized(visualizedMember);
		}

		public bool TargetEquals(object other)
		{
			var otherExpression = other as Expression<Func<object>>;
			return otherExpression != null && ExpressionUtility.Equals(visualizedMember, otherExpression);
		}

		public bool TargetEquals(Expression<Func<object>> otherExpression)
		{
			return otherExpression != null && ExpressionUtility.Equals(visualizedMember, otherExpression);
		}

		public void Draw(GUIStyle guiStyle)
		{
			GUILayout.Label(text, guiStyle);
		}

		public void Update()
		{
			var newValue = valueGetter();

			bool valueChanged;
			if(ReferenceEquals(newValue, null))
			{
				valueChanged = ReferenceEquals(lastValue, null);
			}
			else
			{
				valueChanged = !newValue.Equals(lastValue);
			}

			if(valueChanged)
			{
				#if UNITY_EDITOR
				if(newValue is Vector3 && lastValue is Vector3)
				{
					UnityEngine.Debug.DrawLine((Vector3)lastValue, (Vector3)newValue, Color.white, 5f);
				}
				#endif

				lastValue = newValue;
				text = formatter.ToStringColorized(visualizedMember);
				WantsRepaint = true;
			}
		}

		public override bool Equals(object obj)
		{
			var displayer = obj as ValueDisplayer;
			return displayer != null && ExpressionUtility.Equals(visualizedMember, displayer.visualizedMember);
		}

		public override int GetHashCode() => visualizedMember.GetHashCode();
	}
}