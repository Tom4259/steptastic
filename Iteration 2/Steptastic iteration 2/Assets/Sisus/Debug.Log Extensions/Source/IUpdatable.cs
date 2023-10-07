namespace Sisus.Debugging
{
	public interface IUpdatable
	{
		bool TargetEquals(object other);

		void Update();
	}
}