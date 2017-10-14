using System.Threading.Tasks;

namespace ToDoGraphDemo.Auth
{
    public interface IAuthProvider
    {
        Task<string> GetUserAccessTokenAsync();
    }
}