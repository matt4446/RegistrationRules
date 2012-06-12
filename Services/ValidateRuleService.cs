using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Roles.Services;
using Orchard.Data;
using Orchard.Security;
using RegistrationRules.Models;
using Orchard.Roles.Models;

namespace RegistrationRules.Services
{
    public enum SatisfyAction { Approve, HasRole, Remove, Decline }; 
    public class ValidateRuleService : IValidateRuleService
    {
        private readonly IRepository<RegistrationRuleRecord> ruleRepository;

        private readonly IRepository<RoleCollectionRecord> roleCollectionRecordRepository;

        private readonly IRepository<RoleTargetRecord> roleTargetRecordRepository;

        private readonly IRoleService roleService;


        private readonly IRepository<UserRolesPartRecord> userRolesRepository;

        public ValidateRuleService(
            IRoleService roleService,
            IRepository<UserRolesPartRecord> userRolesRepository,
            IRepository<RegistrationRuleRecord> emailRegistrationMaskRecordRepository,
            IRepository<RoleCollectionRecord> roleCollectionRecordRepository,
            IRepository<RoleTargetRecord> roleTargetRecordRepository) 
        {
            this.userRolesRepository = userRolesRepository;
            this.roleService = roleService;
            this.roleTargetRecordRepository = roleTargetRecordRepository;
            this.roleCollectionRecordRepository = roleCollectionRecordRepository;
            this.ruleRepository = emailRegistrationMaskRecordRepository;
        }

        public RoleCollectionRecord GetRoleCollection()
        {
            RoleCollectionRecord item = roleCollectionRecordRepository.Table.FirstOrDefault();

            if (item == null)
            {
                this.CreateDefaultCollection(true, new List<string>());
            }
            this.UpdateDefaultCollection(item);

            return item;
        }

        private void UpdateDefaultCollection(RoleCollectionRecord collection)
        {
            foreach (var item in this.roleService.GetRoles())
            {
                if (collection.RoleTargetRecords.Any(e => e.RoleName.Equals(item.Name)))
                    continue;

                this.AddRoleRuleCollection(item.Name, collection);
                //var roleGroup = this.AddRoleRuleCollection(item.Name, collection);
            }    
        }

        public RoleTargetRecord GetRoleRuleCollection(int id)
        {
            return roleTargetRecordRepository.Get(id);
        }

        public RegistrationRuleRecord GetRule(int id)
        {
            return ruleRepository.Get(id);
        }

        public RegistrationRuleRecord AddRule(bool checkUserOnRegistration, string rule, string ruleOperator, string ruleAction, bool inculusionRule, RoleTargetRecord roleTarget)
        {
            var model = new RegistrationRuleRecord()
                            {
                                RuleName = rule,
                                Operator = ruleOperator,
                                RuleAction = ruleAction,
                                InculusionRule = inculusionRule,
                                RoleTargetRecord = roleTarget
                            };

            this.ruleRepository.Create(model);

            return model; 
        }

        public RoleTargetRecord AddRoleRuleCollection(string roleName, RoleCollectionRecord collection)
        {
            var model = new RoleTargetRecord()
                            {
                                RoleCollectionRecord = collection,
                                RoleName = roleName,
                            };

            collection.RoleTargetRecords.Add(model);

            this.roleTargetRecordRepository.Create(model);

            return model;
        }

        public RoleCollectionRecord CreateDefaultCollection(bool defaultRoles, IList<string> roles)
        {
            var collection = new RoleCollectionRecord();
            collection.Name = "Default";

            this.roleCollectionRecordRepository.Create(collection);

            if (defaultRoles)
            {
                foreach (var item in this.roleService.GetRoles())
                {
                    var roleGroup = this.AddRoleRuleCollection(item.Name, collection);
                }
            }

            if (roles == null || roles.Count == 0)
                return collection;

            foreach (var role in roles) 
            {
                if (collection.RoleTargetRecords.Any(e => e.RoleName.Equals(role, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                this.AddRoleRuleCollection(role, collection);
            }

            return collection;
        }

        public void RemoveRole(RoleTargetRecord roleTarget)
        {
            this.roleTargetRecordRepository.Delete(roleTarget);

        }

        private IList<UserRolesPartRecord> roles; 
        private IList<RegistrationRuleRecord> rolesToCheck;
        private Dictionary<string, List<RegistrationRuleRecord>> roleTargetRecords; 
        public void SatisfyRule(Orchard.Security.IUser user, Action<IUser, RoleTargetRecord, SatisfyAction> approveAction)
        {
            var currentRoles = roles ?? (roles = this.userRolesRepository.Table.ToList());
            var roleRules = rolesToCheck ?? (rolesToCheck = this.ruleRepository.Table.ToList());
            var roleGroup = 
                roleTargetRecords ?? (roleTargetRecords = roleRules.GroupBy(e=> e.RoleTargetRecord.RoleName).ToDictionary(e=> e.Key, e=> e.ToList()));

            foreach (var item in roleGroup.Keys)
            {
                var approve = roleGroup[item].Where(e => e.InculusionRule);
                var notApproved = roleGroup[item].Where(e => !e.InculusionRule);
                var hasCurrentRole = currentRoles.Any(e => e.UserId == user.Id);

                var firstNotApproved = notApproved.FirstOrDefault(e => SatisfyRule(user, e));
                if (firstNotApproved != null)
                {
                    
                    if (hasCurrentRole)
                        approveAction(user, firstNotApproved.RoleTargetRecord, SatisfyAction.Remove);
                    else
                        approveAction(user, firstNotApproved.RoleTargetRecord, SatisfyAction.Decline);
                    continue;
                }

                var firstApproved = approve.FirstOrDefault(e => SatisfyRule(user, e));
                if (firstApproved != null) 
                {
                    if (hasCurrentRole)
                        approveAction(user, firstNotApproved.RoleTargetRecord, SatisfyAction.HasRole);
                    else
                        approveAction(user, firstApproved.RoleTargetRecord, SatisfyAction.Approve);
                    continue;
                }
            }


            //var 

            //foreach (var roleGroup in roleGroups) 
            //{
            //    if (roleGroup.Rules.Count == 0)
            //        continue;

            //    foreach (var rule in roleGroup.Rules)
            //    {
            //        if(!CheckRole(user, rule))
            //            continue;

            //        yield return roleGroup;
            //        break;
            //    }

            //}

            //yield break;
        }



        public bool SatisfyRule(IUser user, RegistrationRuleRecord ruleRecord)
        {
            var checkedValue = this.CheckRole(user, ruleRecord);

            return checkedValue;
        }

        public void SatisfyRule(IEnumerable<IUser> users, Action<IUser, RoleTargetRecord, SatisfyAction> approveAction)
        {
            foreach (var user in users)
            {
                SatisfyRule(user, approveAction);
            }
        }

        private bool CheckRole(IUser user, RegistrationRuleRecord ruleRecord) 
        {
            if (string.IsNullOrWhiteSpace(user.Email))
                return false;

            var split = new char[]{ ',', '|' };
            var availableRules = ruleRecord.RuleAction
                .Split(split, StringSplitOptions.RemoveEmptyEntries)
                .Select(e=> e.Trim())
                .Where(e=> e.Length > 0)
                .ToArray();

            switch (ruleRecord.Operator)
            {
                    //{ text: "Equals" },
                    //    { text: "Begins with" },
                    //    { text: "Ends with" },
                    //    { text: "Contains" }
                case "Equals": { return availableRules.Any(e => e.Equals(user.Email, StringComparison.CurrentCultureIgnoreCase)); }
                case "Begins with": { return availableRules.Any(e => user.Email.IndexOf(e, StringComparison.CurrentCultureIgnoreCase) == 0); }
                case "Ends with": { return availableRules.Any(e => user.Email.EndsWith(e, StringComparison.CurrentCultureIgnoreCase)); }
                case "Contains": { return availableRules.Any(e => user.Email.IndexOf(e, StringComparison.CurrentCultureIgnoreCase) >= 0 ); }
            }

            return false;

        }

        public void UpdateRule(RegistrationRuleRecord model)
        {
            this.ruleRepository.Update(model);
        }

        public void RemoveRule(RegistrationRuleRecord rule)
        {
            this.ruleRepository.Delete(rule);
        }

        public IEnumerable<RegistrationRuleRecord> GetRules()
        {
            return this.ruleRepository.Table;
        }

    }
}