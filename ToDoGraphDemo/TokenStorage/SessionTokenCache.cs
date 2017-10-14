using System.Threading;
using System.Web;
using Microsoft.Identity.Client;

namespace ToDoGraphDemo.TokenStorage
{
    /// <summary>
    /// Thread-safe wrapper around an MSAL token cache and HTTP context for a specific user.
    /// </summary>
    public class SessionTokenCache
    {
        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        string UserId = string.Empty;
        private string CacheId = string.Empty;
        // Contains the session object for the user.
        HttpContextBase httpContext = null;
        // The MSAL token cache.
        TokenCache cache = new TokenCache();

        public SessionTokenCache(string userId, HttpContextBase httpContext)
        {
            UserId = userId;
            CacheId = UserId + "_TokenCache";
            this.httpContext = httpContext;
            Load();
        }

        /// <summary>
        /// Get the underlying MSAL token cache for the user session.
        /// </summary>
        /// <returns>The token cache of the user.</returns>
        public TokenCache GetMsalCacheInstance()
        {
            cache.SetBeforeAccess(BeforeAccessNotification);
            cache.SetAfterAccess(AfterAccessNotification);
            Load();
            return cache;
        }

        public void SaveUserStateValue(string state)
        {
            SessionLock.EnterWriteLock();
            httpContext.Session[CacheId + "_state"] = state;
            SessionLock.ExitWriteLock();
        }

        public string ReadUserStateValue()
        {
            string state = string.Empty;
            SessionLock.EnterReadLock();
            state = (string)httpContext.Session[CacheId + "_state"];
            SessionLock.ExitReadLock();

            return state;
        }

        // Loads any access or refresh tokens associated with the session into the 
        // persistant cache.
        public void Load()
        {
            SessionLock.EnterReadLock();
            cache.Deserialize((byte[])httpContext.Session[CacheId]);
            SessionLock.ExitReadLock();
        }

        // Synchronizes user session with contents of cache.
        public void Persist()
        {
            SessionLock.EnterWriteLock();

            // Optimistically set HasStateChanged to false. We need to do it early to avoid
            // losing changes made by a concurrent thread.
            cache.HasStateChanged = false;

            // Reflect changes in the persistent store.
            httpContext.Session[CacheId] = cache.Serialize();
            SessionLock.ExitWriteLock();
        }

        // Triggered just before MSAL needs to access the cache. Reload the cache from the
        // persistent store in case it has changed since last access.
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Load();
        }

        // Triggered just after MSAL accessed the cache.
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // If the access operation resulting in a cache update.
            if (cache.HasStateChanged)
            {
                Persist();
            }
        }
    }
}