using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RoleBaseAuth.Controllers
{
    [Authorize(Policy = "UserPolicy")]
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
