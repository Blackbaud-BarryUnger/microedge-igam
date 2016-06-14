using System.Collections.Generic;

namespace MicroEdge.Igam.Providers.Cache
{    

	/// <summary>
	/// CacheManager manages caching functionality by leveraging a CacheProvider 
	/// as specified in the configuration file. 
	/// </summary>
	/// 
	/// <remarks>
	/// Author: Chris Hlusak
	/// Created: 6/24/2007
	/// </remarks>
	public static class CacheManager
	{
		#region Fields

		private static ICacheProvider _provider;

		#endregion Fields

		#region Constructor

		/// <summary>
		/// Default constructor.
		/// </summary>
		static CacheManager()
		{
			_provider = null;
		}

		#endregion Constructor

		#region Properties

		/// <summary>
		/// Get the current cache provider.
		/// </summary>
		private static ICacheProvider Provider
		{
			get
			{
				if (_provider == null)
                    _provider = new NoCacheProvider();
			    
				return _provider;
			}
		}

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initializes to use the provided log provider
        /// </summary>
        public static void Initialize(ICacheProvider provider)
        {
            _provider = provider;
        }

	    /// <summary>
	    /// Get a cached object given the type of the object and the key.
	    /// </summary>
	    /// <param name="key">
	    /// The key of the object to retrieve from the cache.
	    /// </param>
	    /// <returns>
	    /// The indicated cached object.
	    /// </returns>
	    public static T GetCacheObject<T>(string key)
		{
			return Provider.GetCacheObject<T>(key);
		}

		/// <summary>
		/// Add an object to the cache.
		/// </summary>
		/// <param name="cacheObject">
		/// The object to add to the cache.
		/// </param>
		/// <param name="key">
		/// The key to use when adding the object to the cache.
		/// </param>
		public static void SetCacheObject(object cacheObject, string key)
		{
			Provider.SetCacheObject(cacheObject, key);
		}

		/// <summary>
		/// Remove an object from the cache.
		/// </summary>
		/// <param name="key">
		/// The key of the object to remove.
		/// </param>
		public static void ClearCacheObject<T>(string key)
		{
			Provider.ClearCacheObject<T>(key);
		}

		/// <summary>
		/// Clears all objects from the cache.
		/// </summary>
		public static void ClearAll()
		{
			Provider.ClearAll();
		}

		#endregion Methods
	}

    /// <summary>
    /// This is the class that is used for the CacheProvider when there is no cache provider setup in the 
    /// config file. This will suffice for a windows app but not for a web app.
    /// </summary>
    public class NoCacheProvider : ICacheProvider
    {
        #region Fields

        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        #endregion Fields

        #region Methods

        /// <summary>
        /// Get the object from the cache for the indicated type and key.
        /// </summary>
        /// <param name="key">
        /// The second part of the key of the object to return.
        /// </param>
        /// <returns>
        /// The object from the cache for the indicated type and key.
        /// </returns>
        public T GetCacheObject<T>(string key)
        {
            if (_cache.ContainsKey(key))
                return (T)_cache[key];

            return default(T);
        }

        /// <summary>
        /// Put the object in the cache. The key to the object will be the type name of the object and the indicated key in the format [TypeName]_[Key].
        /// </summary>
        /// <param name="cacheObject">
        /// The object to place in the cache.
        /// </param>
        /// <param name="key">
        /// The key suffix for the cached object.
        /// </param>
        public void SetCacheObject(object cacheObject, string key)
        {
            _cache[key] = cacheObject;
        }

        /// <summary>
        /// Remove the item with the indicated type and key from the cache.
        /// </summary>
        /// <param name="key">
        /// The second part of the key of the object to remove.
        /// </param>
        public void ClearCacheObject<T>(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// Clears cache and releases all resources.
        /// </summary>
        public void ClearAll()
        {
            _cache.Clear();
        }

        #endregion Methods
    }
}
