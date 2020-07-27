namespace NorthwoodLib.Pools
{
	public interface IPool<T>
	{
		T Rent();
		void Return(T obj);
	}
}
