using Microsoft.Identity.Client;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using ToDoGraphDemo.TokenStorage;

namespace ToDoGraphDemo.Auth
{
    /// <summary>
    /// GraphAuthProvider is a helper class responsible for retriving access tokens for the user
    /// from an MSAL token cache.
    /// </summary>
    public sealed class GraphAuthProvider : IAuthProvider
    {
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        private string clientId = ConfigurationManager.AppSettings["ClientId"];
        // A secret shared between the registration portal and this application.
        private string clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
        // RedirectUri is the URL where the user will be redirected to after they sign in.
        private string redirectUri = ConfigurationManager.AppSettings["RedirectUri"];
        // Scopes are the specific permissions we are requesting for the application.
        private string scopes = ConfigurationManager.AppSettings["Scopes"];

        // The cache storing user access tokens.
        private SessionTokenCache tokenCache { get; set; }

        // Singleton instance
        private static readonly GraphAuthProvider instance = new GraphAuthProvider();
        private GraphAuthProvider() { }
        public static GraphAuthProvider Instance
        {
            get
            {
                return instance;
            }
        }

        /// <summary>
        /// Gets the current user's access token from the MSAL token cache.
        /// </summary>
        /// <returns>An access token</returns>
        public async Task<string> GetUserAccessTokenAsync()
        {
            string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            HttpContextWrapper httpContext = new HttpContextWrapper(HttpContext.Current);
            TokenCache userTokenCache = new SessionTokenCache(userId, httpContext).GetMsalCacheInstance();

            ConfidentialClientApplication cca = new ConfidentialClientApplication(
                clientId, redirectUri, new ClientCredential(clientSecret), userTokenCache, null);

            // Attempt to retrieve access token from the cache. Could also make a network call for a new
            // access token if the cached one is expired or close to expiration and a refresh token is
            // available.
            try
            {
                AuthenticationResult result = await cca.AcquireTokenSilentAsync(
                    scopes.Split(new char[] { ' ' }), cca.Users.First());

                return result.AccessToken;
            }
            // Unable to retrieve the access token silently.
            catch (Exception)
            {
                HttpContext.Current.Request.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties() { RedirectUri = "/" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);

                throw new Microsoft.Graph.ServiceException(
                    new Microsoft.Graph.Error
                    {
                        Code = Microsoft.Graph.GraphErrorCode.AuthenticationFailure.ToString(),
                        Message = "Authentication is required."
                    });
            }
        }
    }
}