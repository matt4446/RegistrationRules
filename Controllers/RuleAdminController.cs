using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard;
using Orchard.Environment.Extensions;
using Orchard.Projections.Models;
using Orchard.UI.Admin;
using Orchard.Users.Services;
using RegistrationRules.Services;
using Orchard.Users.Models;
using Orchard.ContentManagement;
using Orchard.Roles.Services;
using Orchard.Roles.Models;
using Orchard.Data;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Title.Models;
using RegistrationRules.ViewModels;
using Orchard.Security;

namespace RegistrationRules.Controllers
{
    [Admin]
    public class RuleAdminController : Controller
    {
        private readonly IValidateRuleService validateRuleService;

        private readonly IUserService userService;

        private readonly IOrchardServices services;

        private readonly IRoleService roleService;

        private readonly IRepository<UserRolesPartRecord> userRolesRepository;

        public RuleAdminController(
            IValidateRuleService validateRuleService,
            IUserService userService,
            IRoleService roleService,
            IOrchardServices services,
            IRepository<UserRolesPartRecord> userRolesRepository) 
        {
            this.userRolesRepository = userRolesRepository;
            this.roleService = roleService;
            this.services = services;
            this.userService = userService;
            this.validateRuleService = validateRuleService;
        }

        public ActionResult Index() 
        {
            var collection = validateRuleService.GetRoleCollection();

            return View(collection);
        }

        public JsonResult ListUsers() 
        {
            var users = services.ContentManager.Query<UserPart>().List();
       
            return Json(users.Select(e=> new {
                e.Id, e.Email
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult ListUsersInRole(string roleName) 
        {
            var role = roleService.GetRoleByName(roleName);
            var usersList = services.ContentManager.Query<UserPart>().List().ToList();
            //var users = services.ContentManager.Query<UserRolesPart, UserRolesRecord>()

            var rightUsers = new List<UserPart>();
            
            var userIds = userRolesRepository.Fetch(x=> x.Role.Id == role.Id).Select(e=> e.UserId).Distinct();
            var users = userIds.Select(RoleUserId => usersList.Where(userPart => userPart.Id == RoleUserId).FirstOrDefault());

            return Json(users.Select(user => new { 
                user.Id,
                user.UserName
            }), JsonRequestBehavior.AllowGet);
        }

        public JsonResult ListCollection()
        {

            var collection = this.validateRuleService.GetRoleCollection();
            //roles within the collection
            var items = collection.RoleTargetRecords.ToList();
             
            var model = items.Select(e => new
                                          {
                                              e.Id,
                                              e.RoleName
                                          });

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ListRules(int roleId) 
        {
            var rules = this.validateRuleService.GetRoleRuleCollection(roleId);

            var model = rules.Rules.Select(e => new
                                                {
                                                    e.Id,
                                                    e.RuleName,
                                                    e.Operator,
                                                    e.RuleAction,
                                                    e.CheckUserOnRegistration,
                                                    e.InculusionRule,
                                                    RoleId = e.RoleTargetRecord.Id
                                                });

            return Json(model, JsonRequestBehavior.AllowGet);
        }

        public JsonResult CreateRule(int roleId, RegistrationRuleRecordViewModel model) 
        {
            var roleCollection = this.validateRuleService.GetRoleRuleCollection(roleId);

            var createdModel = this.validateRuleService.AddRule(model.CheckUserOnRegistration, model.RuleName, model.Operator, model.RuleAction, model.InculusionRule, roleCollection);

            return Json(createdModel.Id, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public JsonResult UpdateRule(int roleId, RegistrationRuleRecordViewModel model)
        {
            var roleCollection = this.validateRuleService.GetRoleRuleCollection(roleId);
            var updateModel = roleCollection.Rules.FirstOrDefault(e => e.Id == model.Id);

            updateModel.Operator = model.Operator;
            updateModel.RuleAction = model.RuleAction;
            updateModel.RuleName = model.RuleName;
            updateModel.CheckUserOnRegistration = model.CheckUserOnRegistration;
            updateModel.InculusionRule = model.InculusionRule;

            this.validateRuleService.UpdateRule(updateModel);

            return Json(true, JsonRequestBehavior.DenyGet);
        }

        [HttpPost]
        public JsonResult DestroyRule(int roleId, RegistrationRuleRecordViewModel model)
        {
            var roleCollection = this.validateRuleService.GetRoleRuleCollection(roleId);
            var updateModel = roleCollection.Rules.FirstOrDefault(e => e.Id == model.Id);

            if (updateModel == null)
                return Json(false);

            this.validateRuleService.RemoveRule(updateModel);

            return Json(true);
        }

        [HttpPost]
        public JsonResult Process(int id) 
        {
            var rule = this.validateRuleService.GetRule(id);

            if (rule == null)
                return Json(false);

            var role = roleService.GetRoleByName(rule.RoleTargetRecord.RoleName);

            var userRolesUserIds = userRolesRepository.Fetch(x=> x.Role.Id == role.Id)
                .Select(e=> new {
                    UserId = e.UserId,
                    RoleId = e.Role.Id
                }).ToList();
            
            var users = this.services.ContentManager.Query<UserPart>().List().Where(e=> !string.IsNullOrWhiteSpace(e.Email));
            
            var satisfyRule = users
                .Where(e=>  this.validateRuleService.SatisfyRule(e, rule))
                .ToList();

            var result = new {
                WithinRole = satisfyRule
                    .Where(e => userRolesUserIds.Any(userRole => userRole.UserId == e.Id))
                    .Select(user => new { user.Id, user.Email, user.UserName }),
                NeededRole = satisfyRule
                    .Where(e => userRolesUserIds.Count == 0 || !userRolesUserIds.Any(userRole => userRole.UserId == e.Id))
                    .Select(user => new { user.Id, user.Email, user.UserName }),
            };

            foreach (var user in result.NeededRole) 
            {
                userRolesRepository.Create(new UserRolesPartRecord() { 
                    Role = role,
                    UserId = user.Id
                }); 
   
            }

            return Json(
                result
            );
        }

        public JsonResult ProcessAll(int id) 
        {
            var users = this.services.ContentManager.Query<UserPart>().List<IUser>();
            var roles = roleService.GetRoles().ToList();

            var addedRoles = new Dictionary<IUser, IList<RoleRecord>>();
            var removedRoles = new Dictionary<IUser, IList<RoleRecord>>();  
            var hasroles = new Dictionary<IUser, IList<RoleRecord>>();

            Action<IUser, Models.RoleTargetRecord> addUserRole = (a, b) => {
                var role = roles.FirstOrDefault(e=> e.Name.Equals(b.RoleName, StringComparison.CurrentCultureIgnoreCase));
                userRolesRepository.Create(new UserRolesPartRecord() { 
                    Role = role,
                    UserId = a.Id
                });

                if (addedRoles.ContainsKey(a))
                    addedRoles[a].Add(role);
                else
                    addedRoles.Add(a, new List<RoleRecord>() { role });
            };

            Action<IUser, Models.RoleTargetRecord> removeUserRole = (a, b) => { 
                var role = roles.FirstOrDefault(e=> e.Name.Equals(b.RoleName, StringComparison.CurrentCultureIgnoreCase));
                var userRole = userRolesRepository.Fetch(e=> e.UserId == a.Id && e.Role.Id == role.Id).FirstOrDefault();
                
                userRolesRepository.Delete(userRole);

                if (removedRoles.ContainsKey(a))
                    removedRoles[a].Add(role);
                else
                    removedRoles.Add(a, new List<RoleRecord>() { role });
            };

            Action<IUser, Models.RoleTargetRecord> hasRole = (a, b) => {
                var role = roles.FirstOrDefault(e => e.Name.Equals(b.RoleName, StringComparison.CurrentCultureIgnoreCase));

                if (hasroles.ContainsKey(a))
                    hasroles[a].Add(role);
                else
                    hasroles.Add(a, new List<RoleRecord>() { role });
            };
            
            this.validateRuleService.SatisfyRule(users, (user, roleFacade, action) => {
                switch (action)
                {
                    case SatisfyAction.Approve: { addUserRole(user, roleFacade); break; }
                    case SatisfyAction.Remove: { removeUserRole(user, roleFacade); break; }
                    case SatisfyAction.HasRole: { hasRole(user, roleFacade); break; }
                    default: { break; }
                }
            });

            return Json(new { 
                AddedRoles = addedRoles,
                RemovedRoles = removedRoles,
                HasRoles = hasroles
            });
        }
    }
}