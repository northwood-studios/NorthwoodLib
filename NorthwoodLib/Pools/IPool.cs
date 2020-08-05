namespace NorthwoodLib.Pools
{
	/// <summary>
	/// Provides pooled instances of requested type
	/// </summary>
	/// <typeparam name="T">Pooled type</typeparam>
	public interface IPool<T> where T : class
	{
		/// <summary>
		/// Returns a pooled instance of <typeparamref name="T"/>
		/// </summary>
		/// <returns><typeparamref name="T"/> from the pool</returns>
		T Rent();
		/// <summary>
		/// Returns a <typeparamref name="T"/> to the pool
		/// </summary>
		/// <param name="obj">Pooled object</param>
		void Return(T obj);
	}
}
