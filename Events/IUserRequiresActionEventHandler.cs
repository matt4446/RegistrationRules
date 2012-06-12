using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Security;
using Orchard.Events;

namespace RegistrationRules.Events
{
    public interface IUserRequiresActionEventHandler : IEventHandler
    {
        void RoleAuthorization(IUser user);
        void RoleAuthorization(IUser user, string roleName);
    }
}