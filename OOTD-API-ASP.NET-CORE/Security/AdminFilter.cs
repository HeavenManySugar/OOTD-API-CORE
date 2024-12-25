using OOTD_API.EntityFramework;
using OOTD_API.StatusCode;

namespace OOTD_API.Security
{
    public class AdminFilter : ActionFilterAttribute
    {
        OOTDV1Entities db = new OOTDV1Entities();
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            var userToken = JwtAuthFilter.GetToken(request.Headers.Authorization.Parameter);
            var user = db.User.Find(userToken["UID"]);

            if (!user.IsAdministrator)
                throw new HttpResponseException(CatStatusCode.ForbiddenResponse());
            base.OnActionExecuting(actionContext);
        }
    }
}