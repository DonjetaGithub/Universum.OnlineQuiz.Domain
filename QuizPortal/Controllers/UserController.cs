using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuizPortal.Helper;
using QuizPortal.Models;
using QuizPortal.Models.Dtos;
using QuizPortal.Repositories;

namespace QuizPortal.Controllers
{
    public class UserController : Controller
    {
        private readonly IRepositoryFactory _repositoryFactory;
        private readonly IMapper _mapper;

        public UserController(IRepositoryFactory repositoryFactory, IMapper mapper)
        {
            _repositoryFactory = repositoryFactory;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString(Constants.SessionUserId) != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(UserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Vërtetimi i regjistrit të përdoruesit dështoi" });
            }

            IUserRepository userRepository = _repositoryFactory.GetUserRepository();

            if (await userRepository.UserExistsAsync(userDto.Username))
            {
                return Json(new { success = false, message = $"Perdoruesi tashme egziston: {userDto.Username}" });
            }

            User user = _mapper.Map<User>(userDto);

            await userRepository.CreateUserAsync(user);
            await _repositoryFactory.SaveAsync();

            return Json(new { success = true, message = "Registrimi ishte me sukses", url = Url.Action("Login", "User") });
        }

        [HttpPost]
        public async Task<IActionResult> Login(UserDto userDto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Vërtetimi i hyrjes së përdoruesit dështoi" });
            }

            IUserRepository userRepository = _repositoryFactory.GetUserRepository();

            if (!await userRepository.UserExistsAsync(userDto.Username, userDto.Password))
            {
                return Json(new { success = false, message = "Emri i përdoruesit ose fjalëkalimi është i pasaktë" });
            }

            var userFromDb = await userRepository.GetUserAsync(userDto.Username);

            HttpContext.Session.SetString(Constants.SessionUserId, userFromDb.Id.ToString());

            return Json(new { success = true, message = "Identifikimi i suksesshëm", url = Url.Action("Index", "Home") });
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString(Constants.SessionUserId) != null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove(Constants.SessionUserId);

            return RedirectToAction("Login", "User");
        }
    }
}
