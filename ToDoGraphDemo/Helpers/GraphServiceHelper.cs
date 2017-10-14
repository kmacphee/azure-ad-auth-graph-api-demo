using Microsoft.Graph;
using System.Net.Http.Headers;
using ToDoGraphDemo.Auth;

namespace ToDoGraphDemo
{
    /// <summary>
    /// A helper class for acquiring an authenticated GraphServiceClient for interacting with Graph API.
    /// </summary>
    public class GraphServiceHelper
    {
        // GraphServiceClient singleton.
        private static GraphServiceClient client = null;

        // Returns a GraphServiceClient with the user's access token set to the HTTP auth header.
        public static GraphServiceClient GetAuthenticatedClient()
        {
            if (client == null)
            {
                client = new GraphServiceClient(
                    new DelegateAuthenticationProvider(
                        async (requestMessage) =>
                        {
                            // Get access token.
                            string accessToken = await GraphAuthProvider.Instance.GetUserAccessTokenAsync();

                            // Set access token to HTTP auth header.
                            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                        }));
            }

            return client;
        }

        public static void SignOut()
        {
            client = null;
        }
    }
}