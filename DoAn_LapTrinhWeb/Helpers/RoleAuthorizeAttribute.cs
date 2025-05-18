using System.Linq;
using System.Web.Security;
using System.Web;
using System.Web.Mvc;

public class RoleAuthorizeAttribute : AuthorizeAttribute
{
    private readonly string[] allowedRoles;

    public RoleAuthorizeAttribute(params string[] roles)
    {
        this.allowedRoles = roles;
    }

    protected override bool AuthorizeCore(HttpContextBase httpContext)
    {
        var authCookie = httpContext.Request.Cookies[FormsAuthentication.FormsCookieName];
        if (authCookie == null)
            return false;

        var ticket = FormsAuthentication.Decrypt(authCookie.Value);
        string userRole = ticket.UserData;

        return allowedRoles.Contains(userRole);
    }
}
