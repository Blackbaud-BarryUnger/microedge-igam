
namespace MicroEdge.Igam.Providers.Cache
{
	/// <summary>
	/// Interface that any cache provider used by the cache manager must implement.
	/// </summary>
	/// 
	/// <remarks>
	/// Author:  LDF
	/// Created: 9/25/2008
	/// </remarks>
	public interface ICacheProvider
	{
		T GetCacheObject<T>(string idKey);
		void SetCacheObject(object cacheObject, string idKey);
		void ClearCacheObject<T>(string idKey);
		void ClearAll();
	}
}
