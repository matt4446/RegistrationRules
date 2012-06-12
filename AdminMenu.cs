using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.UI.Navigation;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Environment.Extensions;

namespace RegistrationRules
{
    public class AdminMenu : INavigationProvider
    {
        public AdminMenu()
        {
        }

        public Localizer T { get; set; }
        public string MenuName
        {
            get
            {
                return "admin";
            }
        }

        public void GetNavigation(NavigationBuilder builder)
        {
            builder.Add(T("Users"), menu => menu
                .Add(T("User Signup Rules"), "4",
                    item => item
                            .Action("Index", "RuleAdmin", new { Area = "RegistrationRules" })
                            .LocalNav()
                            .Permission(StandardPermissions.SiteOwner))
                .Add(T("User Roles"), "5", 
                    item => item
                            .Action("Index", "UserRoleAdmin", new { Area = "RegistrationRules" })
                            .LocalNav()
                            .Permission(StandardPermissions.SiteOwner))
            );

        }
    }
}