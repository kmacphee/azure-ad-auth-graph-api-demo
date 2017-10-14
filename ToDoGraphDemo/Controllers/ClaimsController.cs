using System.Web.Mvc;

namespace ToDoGraphDemo.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        // GET: Claims
        public ActionResult Index()
        {
            var claimsPrincipalCurrent = System.Security.Claims.ClaimsPrincipal.Current;
            // Get the user's first and last name.
            ViewBag.Name = claimsPrincipalCurrent.FindFirst("name").Value;

            // The 'preferred_username' claim can be used for showing the username.
            ViewBag.Username = claimsPrincipalCurrent.FindFirst("preferred_username").Value;

            // The subject claim can be used to uniquely identify the user across the web.
            ViewBag.Subject = claimsPrincipalCurrent.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;

            // TenantId is the unique Tenant Id, which represents an organization in Azure AD.
            ViewBag.TenantId = claimsPrincipalCurrent.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

            return View();
        }
    }
}