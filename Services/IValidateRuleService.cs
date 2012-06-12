using System;
using System.Linq;
using Orchard;
using RegistrationRules.Models;
using Orchard.Security;
using System.Collections.Generic;

namespace RegistrationRules.Services
{
    public interface IValidateRuleService : IDependency
    {
        RoleCollectionRecord GetRoleCollection();
        RoleCollectionRecord CreateDefaultCollection(bool defaultRoles, IList<string> roles);


        RoleTargetRecord GetRoleRuleCollection(int id);
        RoleTargetRecord AddRoleRuleCollection(string ruleName, RoleCollectionRecord collection);

        RegistrationRuleRecord GetRule(int id);
        IEnumerable<RegistrationRuleRecord> GetRules();
        RegistrationRuleRecord AddRule(bool checkUserOnRegistration, string rule, string ruleOperator, string ruleAction, bool inculusionRule, RoleTargetRecord roleTarget);

        //bool UserHasRole(RoleTargetRecord roleTarget);

        void UpdateRule(RegistrationRuleRecord model);
   
        void RemoveRole(RoleTargetRecord roleTarget);

        void RemoveRule(RegistrationRuleRecord rule);


        void SatisfyRule(IEnumerable<Orchard.Security.IUser> users, Action<IUser, RoleTargetRecord, SatisfyAction> approveAction);
        void SatisfyRule(Orchard.Security.IUser user, Action<IUser, RoleTargetRecord, SatisfyAction> approveAction);
        bool SatisfyRule(IUser user, RegistrationRuleRecord ruleRecord);
    }
}