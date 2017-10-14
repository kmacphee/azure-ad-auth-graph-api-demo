using HtmlAgilityPack;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ToDoGraphDemo.Models
{
    /// <summary>
    /// A service class that wraps the usage of Microsoft Graph API to synchronize a todo list with
    /// a OneNote page owned by the user.
    /// </summary>
    public class ToDoService
    {
        private string todoPageTitle = "ToDoGraphDemo: My To Dos";
        private string todoSectionTitle = "ToDoGraphDemo";
        private string todoNotebookTitle = "ToDoGraphDemo";

        /// <summary>
        /// Reads the synchronized page in OneNote and translates the content into a list of todo objects.
        /// </summary>
        /// <returns>The list of todos synchronized with OneNote.</returns>
        public async Task<List<ToDo>> GetToDoList()
        {
            // First get the synchronized page. If it doesn't exist, create it.
            var client = GraphServiceHelper.GetAuthenticatedClient();
            var pages = await OneNoteService.GetMyPages(client);
            var todoPage = pages.Where(page => page.Title == todoPageTitle).SingleOrDefault();

            if (todoPage == null)
                todoPage = await CreateToDoPage(client);

            // Read the HTML content of the synchronized page and translate that into ToDo objects.
            List<ToDo> result = new List<ToDo>();
            Stream contentStream = await OneNoteService.GetPageContent(client, todoPage);
            using (StreamReader reader = new StreamReader(contentStream))
            {
                string pageContent = reader.ReadToEnd();
                var document = new HtmlDocument();
                document.LoadHtml(pageContent);

                // A OneNote todo list is a sequence of <p> tags with the data-tag attribute set
                // to "to-do" or "to-do:completed".
                foreach (var node in document.DocumentNode.Descendants("p").Where(
                    p => p.GetAttributeValue("data-tag", "not-to-do") != "not-to-do"))
                {
                    string dataTag = node.Attributes["data-tag"].Value;
                    if (dataTag != "to-do" && dataTag != "to-do:completed")
                        continue; // node is not a OneNote todo

                    result.Add(new ToDo
                    {
                        Task = node.InnerText,
                        Done = dataTag == "to-do:completed"
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Updates the content of the synchronized OneNote page to reflect the given list of ToDo
        /// objects.
        /// </summary>
        /// <param name="todos">The list of todos to synchronize with OneNote.</param>
        /// <returns></returns>
        public async Task UpdateToDoList(List<ToDo> todos)
        {
            // First get the page that we are syncing with from OneNote.
            var client = GraphServiceHelper.GetAuthenticatedClient();
            var pages = await OneNoteService.GetMyPages(client);
            var todoPage = pages.Where(page => page.Title == todoPageTitle).SingleOrDefault();

            if (todoPage == null)
                throw new Exception("Cannot update todo list. Failed to get OneNote page to update.");

            // Load the HTML content of the page with OneNote IDs.
            Stream contentStream = await OneNoteService.GetPageContent(client, todoPage, includeIds: true);
            var pageContent = new HtmlDocument();
            using (StreamReader reader = new StreamReader(contentStream))
            {
                string stringContent = reader.ReadToEnd();
                pageContent.LoadHtml(stringContent);
            }

            // The container of our todos will be the first <div> tag on the page.
            var targetDiv = pageContent.DocumentNode.Descendants("div").FirstOrDefault();
            if (targetDiv == null)
                throw new Exception("Cannot update todo list. Text content on a OneNote page must be contained within a div tag.");

            // The div's ID will be used to identify the target on the page that we want to update.
            var targetDivId = targetDiv.ChildAttributes("id").FirstOrDefault();
            if(targetDivId == null)
                throw new Exception("Cannot update todo list. Target div tag does not have an id.");

            // Regenerate HTML content for current todos.
            targetDiv.RemoveAllChildren();
            foreach (ToDo todo in todos)
            {
                string dataTag = todo.Done ? "to-do:completed" : "to-do";
                targetDiv.AppendChild(HtmlNode.CreateNode($"<p data-tag=\"{dataTag}\">{todo.Task}</p>"));
            }

            // Update the OneNote page.
            await OneNoteService.UpdatePage(client, todoPage, targetDivId.Value, targetDiv.InnerHtml);
        }

        private async Task<OnenotePage> CreateToDoPage(GraphServiceClient client)
        {
            var sections = await OneNoteService.GetMySections(client);
            var todoSection = sections.Where(section => section.DisplayName == todoSectionTitle).SingleOrDefault();

            if (todoSection == null)
            {
                todoSection = await CreateToDoSection(client);
            }

            await OneNoteService.CreatePage(client, todoPageTitle, todoSection);

            // There can be a small delay between successful creation of a new page and that page appearing in GET requests.
            // We won't return from this method until the new page can be retrieved with a GET or we timeout.
            var stopwatch = new Stopwatch();
            while (true)
            {
                try
                {
                    var pages = await OneNoteService.GetMyPages(client);
                    var todoPage = pages.Where(page => page.Title == todoPageTitle).SingleOrDefault();

                    if (todoPage != null)
                        return todoPage;

                    if(stopwatch.ElapsedMilliseconds > 10000)
                    {
                        throw new Exception("Failed to create synchronized OneNote page.");
                    }
                }
                catch
                {
                    // Catch any errors in the GET and retry until we timeout.
                    Thread.Sleep(500);
                }
            }
        }

        private async Task<OnenoteSection> CreateToDoSection(GraphServiceClient client)
        {
            var notebooks = await OneNoteService.GetMyNotebooks(client);
            var todoNotebook = notebooks.Where(notebook => notebook.DisplayName == todoNotebookTitle).SingleOrDefault();

            if (todoNotebook == null)
            {
                todoNotebook = await CreateToDoNotebook(client);
            }

            return await OneNoteService.CreateSection(client, todoSectionTitle, todoNotebook);
        }

        private async Task<Notebook> CreateToDoNotebook(GraphServiceClient client)
        {
            return await OneNoteService.CreateNotebook(client, todoNotebookTitle);
        }
    }
}