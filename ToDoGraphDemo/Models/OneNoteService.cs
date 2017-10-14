using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace ToDoGraphDemo.Models
{
    /// <summary>
    /// A service class that wraps the use of the GraphServiceClient OData calls for interacting with OneNote.
    /// </summary>
    public static class OneNoteService
    {
        /// <summary>
        /// Gets the OneNote notebooks associated with the given GraphServiceClient.
        /// </summary>
        /// <param name="client">An authenticated GraphServiceClient containing the user's access token.</param>
        /// <returns>The list of notebooks owned by the user.</returns>
        public static async Task<List<Notebook>> GetMyNotebooks(GraphServiceClient client)
        {
            IOnenoteNotebooksCollectionPage results = await client.Me.Onenote.Notebooks.Request().GetAsync();
            return results.ToList();
        }

        /// <summary>
        /// Creates a OneNote notebook with the given display name. Requires an authenticated GraphServiceClient.
        /// </summary>
        /// <param name="client">An authenticated GraphServiceClient containing the user's access token.</param>
        /// <param name="displayName">The name of the notebook to create.</param>
        /// <returns>The created notebook.</returns>
        public static async Task<Notebook> CreateNotebook(GraphServiceClient client, string displayName)
        {

            Notebook newNotebook = new Notebook { DisplayName = displayName };
            return await client.Me.Onenote.Notebooks.Request().AddAsync(newNotebook);
        }

        /// <summary>
        /// Gets the OneNote sections of the user authenticated with GraphServiceClient.
        /// </summary>
        /// <param name="client">An authenticated GraphServiceClient containing the user's access token.</param>
        /// <returns>The list of sections owned by the user.</returns>
        public static async Task<List<OnenoteSection>> GetMySections(GraphServiceClient client)
        {
            IOnenoteSectionsCollectionPage results = await client.Me.Onenote.Sections.Request().GetAsync();
            return results.ToList();
        }

        /// <summary>
        /// Creates a OneNote section with the given display name in the given notebook. Requires an authenticated GraphServiceClient.
        /// </summary>
        /// <param name="client">An authenticated GraphServiceClient containing the user's access token.</param>
        /// <param name="displayName">The name of the section to create.</param>
        /// <param name="parentNotebook">The notebook that should contain the new section.</param>
        /// <returns>The created section.</returns>
        public static async Task<OnenoteSection> CreateSection(GraphServiceClient client, string displayName, Notebook parentNotebook)
        {
            OnenoteSection newSection = new OnenoteSection { DisplayName = displayName };
            return await client.Me.Onenote.Notebooks[parentNotebook.Id].Sections.Request().AddAsync(newSection);
        }

        /// <summary>
        /// Gets the OneNote pages of the user authenticated with GraphServiceClient.
        /// </summary>
        /// <param name="client">An authenticated GraphServiceClient containing the user's access token.</param>
        /// <returns>The list of pages owned by the user.</returns>
        public static async Task<List<OnenotePage>> GetMyPages(GraphServiceClient client)
        {
            IOnenotePagesCollectionPage results = await client.Me.Onenote.Pages.Request().GetAsync();
            return results.ToList();
        }

        /// <summary>
        /// Creates an empty OneNote page with the given display name in the given section. Requires an authenticated
        /// GraphServiceClient.
        /// </summary>
        /// <param name="client">An authenticated GraphServiceClient containing the user's access token.</param>
        /// <param name="displayName">The name of the page to be created.</param>
        /// <param name="parentSection">The section that should contain the given page.</param>
        /// <returns>The created page.</returns>
        public static async Task<OnenotePage> CreatePage(GraphServiceClient client, string displayName, OnenoteSection parentSection)
        {
            string emptyPageHtml = $"<html><head><title>{displayName}</title></head><body><div><p>placeholder</p></div></body></html>";
            return await client.Me.Onenote.Sections[parentSection.Id].Pages.Request().AddAsync(emptyPageHtml, "text/html");
        }

        /// <summary>
        /// Returns a stream to the HTML content of the given OneNote page. Requires an authenticated GraphServiceClient.
        /// </summary>
        /// <param name="client">An authenticated GraphServiceClient containing the user's access token.</param>
        /// <param name="page">The page whose contents to return a stream to.</param>
        /// <param name="includeIds">If true, includes the OneNote element IDs in the content. False by default.</param>
        /// <returns>A stream to the HTML content of the page.</returns>
        public static async Task<System.IO.Stream> GetPageContent(GraphServiceClient client, OnenotePage page, bool includeIds = false)
        {
            List<QueryOption> queryOptions = new List<QueryOption>();
            if (includeIds)
                queryOptions.Add(new QueryOption("includeIDs", "true"));

            return await client.Me.Onenote.Pages[page.Id].Content.Request(queryOptions).GetAsync();
        }

        /// <summary>
        /// Updates the content of the given OneNote page with the given HTML. Requires an authenticated GraphServiceClient.
        /// </summary>
        /// <param name="client">An authenticated GraphServiceClient containing the user's access token.</param>
        /// <param name="page">The page whose contents to update.</param>
        /// <param name="targetId">The ID of an element on the OneNote page that will be a container for the new content.</param>
        /// <param name="htmlContent">The new HTML content.</param>
        /// <returns></returns>
        public static async Task UpdatePage(GraphServiceClient client, OnenotePage page, string targetId, string htmlContent)
        {
            // The auto-generated OData client does not currently support updating a OnenotePage. Graph API expects a PATCH request
            // with a particular JSON body that does not correspond directly to the OnenotePage object. Here we need to get our hands
            // a little dirtier.
            string requestUri = client.Me.Onenote.Pages[page.Id].Content.Request().RequestUrl;

            // The expected body of the request is a JSON array of objects. This list will serialize to an array of one.
            List<OnenotePatchContentCommand> patchCommands = new List<OnenotePatchContentCommand>();
            patchCommands.Add(new OnenotePatchContentCommand() {
                Action = OnenotePatchActionType.Replace,
                Target = targetId,
                Content = htmlContent
            });

            // Create PATCH request
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri);

            // Serialize our list of one OnenotePatchContentCommand to JSON and set content type.
            request.Content = new StringContent(client.HttpProvider.Serializer.SerializeObject(patchCommands));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            // Adds the user's access token from the GraphServiceClient to the request.
            await client.AuthenticationProvider.AuthenticateRequestAsync(request);

            HttpResponseMessage response = await client.HttpProvider.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new Microsoft.Graph.ServiceException(
                    new Error
                    {
                        Code = response.StatusCode.ToString(),
                        Message = await response.Content.ReadAsStringAsync()
                    });
            }
        }
    }
}