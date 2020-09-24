using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using VideoChat.Models;
using VideoChat.Hubs;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace VideoChat.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ClassRoom classrm)
        {
            if(ModelState.IsValid)
            {
                VideoChatDBContext vcdb = new VideoChatDBContext();
                ClassRoom clr = vcdb.ClassRoom.Where(c => c.ClassID == classrm.ClassID).SingleOrDefault();
                if (clr == null)
                {
                    vcdb.ClassRoom.Add(classrm);
                    vcdb.SaveChanges();
                    return RedirectToAction("Present", "Home", classrm);
                }
                else
                {
                    ModelState.AddModelError("", "Room has been exists!");
                }
            }
            return View();
        }
        public IActionResult Present(ClassRoom clsr)
        {
            return View(clsr);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Join(ClassRoom classrm)
        {
            if(ModelState.IsValid)
            {
                VideoChatDBContext vcdb = new VideoChatDBContext();
                ClassRoom rm = vcdb.ClassRoom.Where(cl => cl.ClassID == classrm.ClassID).FirstOrDefault();
                if( rm != null)
                {
                    return RedirectToAction("Present", "Home",classrm);
                }
                else
                {
                    ModelState.AddModelError("", "Room is not exists!");
                }
            }
            return View();
        }

        public IActionResult Login()
        {
            return View("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(IdentityUser usr)
        {
            if (ModelState.IsValid)
            {
                var dao = new Dao();
                var result = dao.Login(usr.UserName, usr.Passwd);
                if (result)
                {
                    var UserSession = dao.getByName(usr.UserName);
                    HttpContext.Session.SetString("_Name", UserSession.UserName);
                    HttpContext.Session.SetString("_ID", UserSession.ID);
                    return RedirectToAction("Create", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Login not success!");
                }
            }
            return View("Index");
        }
       
        

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
