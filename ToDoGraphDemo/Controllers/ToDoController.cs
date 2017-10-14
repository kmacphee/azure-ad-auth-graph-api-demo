using System.Threading.Tasks;
using System.Web.Mvc;
using ToDoGraphDemo.Models;

namespace ToDoGraphDemo.Controllers
{
    public class ToDoController : Controller
    {
        private ToDoService service = new ToDoService();

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var todos = await service.GetToDoList();

            return View("Index", todos);
        }

        [HttpGet]
        public ActionResult New()
        {
            return View("New");
        }

        [HttpPost]
        public async Task<ActionResult> New(ToDo newTodo)
        {
            // Get all todos, add new one and submit.
            var todos = await service.GetToDoList();
            todos.Add(newTodo);
            await service.UpdateToDoList(todos);

            ModelState.Clear();
            return View("Index", todos);
        }

        [HttpPost]
        public async Task<ActionResult> Update(ToDo value)
        {
            // Get all todos, update with the given todo.
            var todos = await service.GetToDoList();
            int index = todos.FindIndex(todo => todo.Task == value.Task);
            todos[index].Done = value.Done;
            await service.UpdateToDoList(todos);

            ModelState.Clear();
            return View("Index", todos);
        }

        [HttpPost]
        public async Task<ActionResult> Delete(ToDo value)
        {
            // Get all todos, remove the given todo and update.
            var todos = await service.GetToDoList();
            todos.Remove(todos.Find(todo => todo.Task == value.Task && todo.Done == value.Done));
            await service.UpdateToDoList(todos);

            ModelState.Clear();
            return View("Index", todos);
        }
    }
}