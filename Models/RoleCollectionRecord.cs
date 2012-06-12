using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace RegistrationRules.Models
{
    public class RoleCollectionRecord 
    {
        public RoleCollectionRecord() 
        {
            this.RoleTargetRecords = new List<RoleTargetRecord>();
        }

        public virtual int Id { get; set; }

        [Required]
        public virtual string Name { get; set; }

        public virtual IList<RoleTargetRecord> RoleTargetRecords { get; set; }
    }
}