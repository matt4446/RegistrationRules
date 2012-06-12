using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RegistrationRules.Services;
using Orchard.Users.Services;
using Orchard.Roles.Services;
using Orchard.Data;
using Orchard;
using Orchard.Roles.Models;
using Orchard.UI.Admin;
using Orchard.ContentManagement;
using Orchard.Users.Models;


namespace RegistrationRules.Controllers
{
    [Admin]
    public class UserRoleAdminController : Controller
    {
        private readonly IRepository<UserRolesPartRecord> userRolesRepository;

        private readonly IValidateRuleService validateRuleService;

        private readonly IUserService userService;

        private readonly IRoleService roleService;

        private readonly IOrchardServices services;

        public UserRoleAdminController(IValidateRuleService validateRuleService,
            IUserService userService,
            IRoleService roleService,
            IOrchardServices services,
            IRepository<UserRolesPartRecord> userRolesRepository) 
        {
            this.services = services;
            this.roleService = roleService;
            this.userService = userService;
            this.validateRuleService = validateRuleService;
            this.userRolesRepository = userRolesRepository;
        }

        public ActionResult Index() 
        {
            return View();
        }

        //var readRulesRoute = Url.Action("ListRules");
        //var readUserCollection = Url.Action("ListUserCollection");

        [HttpGet]
        public JsonResult ListRules() 
        {
            var userRoles = roleService.GetRoles();

            return Json(
                    userRoles.Select(e => new { 
                        e.Id,
                        e.Name
                    }), JsonRequestBehavior.AllowGet
                );
        }

        [HttpGet]
        public JsonResult ListUserCollection(int roleId) 
        {
            var userRoles = userRolesRepository.Fetch(e => e.Role.Id == roleId).ToList();
            var rolesUserId = userRoles.Select(e=> e.UserId).ToList();

            //var users = this.services.ContentManager.Query<UserPart, UserPartRecord>().Where(e => rolesUserId.Any(id => e.Id == id)).List();
            var users = rolesUserId.Select(e => this.services.ContentManager.Get(e).As<UserPart>()).Where(e=> e != null);

            return Json(users.Select(e=> new { e.Id, e.Email, e.UserName, RoleId = roleId }), JsonRequestBehavior.AllowGet); 
                
        }

        public JsonResult RemoveUser(int id, int roleId)
        {
            var userRoles = userRolesRepository.Fetch(e => e.UserId == id && e.Role.Id == roleId).ToList();

            foreach (var role in userRoles)
            {
                userRolesRepository.Delete(role);
            }

            return Json(null, JsonRequestBehavior.DenyGet);
        }

        public JsonResult RemoveUserAddRule(int id, int roleId) 
        {
            var userRoles = userRolesRepository.Fetch(e => e.UserId == id && e.Role.Id == roleId).ToList();

            foreach (var role in userRoles)
            {
                userRolesRepository.Delete(role);
            }

            return Json(null, JsonRequestBehavior.DenyGet);
        }

    }
}