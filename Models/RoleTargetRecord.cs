using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace RegistrationRules.Models
{
    public class RoleTargetRecord 
    {
        public RoleTargetRecord() 
        {
            this.Rules = new List<RegistrationRuleRecord>();
        }

        public virtual int Id { get; set; }

        [Required]
        public virtual string RoleName { get; set; }

        public virtual IList<RegistrationRuleRecord> Rules { get; set; }

        public virtual RoleCollectionRecord RoleCollectionRecord { get; set; }
    }
}