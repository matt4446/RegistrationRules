using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Environment.Extensions;
using System.ComponentModel.DataAnnotations;
using RegistrationRules.Models;

namespace RegistrationRules.Models
{
    public class RegistrationRuleRecord
    {
        public virtual int Id { get; set; }

        public virtual string Operator { get; set; }

        public virtual string RuleName { get; set; }

        public virtual string RuleAction { get; set; }

        public virtual bool InculusionRule { get; set;  }

        public virtual bool CheckUserOnRegistration { get; set; }

        public virtual RoleTargetRecord RoleTargetRecord { get; set; }
    }
}