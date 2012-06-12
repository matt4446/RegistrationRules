using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Users.Events;
using Orchard.Roles.Services;
using RegistrationRules.Services;
using Orchard.Data;
using Orchard.Roles.Models;
using Orchard.ContentManagement;

namespace RegistrationRules.Handlers
{
    public class RegisteredRulesHandler : IUserEventHandler 
    {
        private readonly IRoleService roleService;

        private readonly IValidateRuleService validationService;

        private readonly IRepository<UserRolesPartRecord> userRolesRepository;

        public RegisteredRulesHandler(IRoleService roleService, IValidateRuleService validationService, IRepository<UserRolesPartRecord> userRolesRepository) 
        {
            this.userRolesRepository = userRolesRepository;
            this.validationService = validationService;
            this.roleService = roleService;
        }

        #region IUserEventHandler Members

        public void Creating(UserContext context)
        {
            
        }

        public void Created(UserContext context)
        {
            //run through each role to see if the new user complies to a role to let them in a certain area.
            var rules = validationService.GetRules().Where(e => e.CheckUserOnRegistration).ToList();
            var rulesGroupByRole = rules.GroupBy(e => e.RoleTargetRecord).ToDictionary(e => e.Key, e => e.ToList());

            foreach (var group in rulesGroupByRole.Keys)
            {
                var actualRole = roleService.GetRoleByName(group.RoleName);
                var collecion = rulesGroupByRole[group];
                foreach (var rule in collecion)
                {
                    if (!validationService.SatisfyRule(context.User, rule))
                        continue;

                    var userRolePart = context.User.As<UserRolesPart>();
                    userRolePart.Roles.Add(actualRole.Name);

                    userRolesRepository.Create(new UserRolesPartRecord()
                    {
                        UserId = context.User.Id,
                        Role = actualRole
                    });

                    break;
                }
            }
        }

        public void LoggedIn(Orchard.Security.IUser user)
        {
        }

        public void LoggedOut(Orchard.Security.IUser user)
        {
        }

        public void AccessDenied(Orchard.Security.IUser user)
        {
        }

        public void ChangedPassword(Orchard.Security.IUser user)
        {
        }

        public void SentChallengeEmail(Orchard.Security.IUser user)
        {
        }

        public void ConfirmedEmail(Orchard.Security.IUser user)
        {
        }

        public void Approved(Orchard.Security.IUser user)
        {
        }
        #endregion
    }
}