using UnityEngine;

namespace Sisus.Debugging
{
	public interface IDisplayable
	{
		bool WantsRepaint { get; set; }

		bool TargetEquals(object other);

		void Draw(GUIStyle guiStyle);
	}
}