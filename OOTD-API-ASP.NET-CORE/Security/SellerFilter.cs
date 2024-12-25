using OOTD_API.EntityFramework;
using OOTD_API.StatusCode;
using System.Linq;

namespace OOTD_API.Security
{
    public class SallerFilter : ActionFilterAttribute
    {
        OOTDV1Entities db = new OOTDV1Entities();
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            var userToken = JwtAuthFilter.GetToken(request.Headers.Authorization.Parameter);
            var uid = (int)userToken["UID"];

            var store = db.Store
                .FirstOrDefault(x => x.Enabled && x.OwnerID == uid);

            if (store == null)
                throw new HttpResponseException(CatStatusCode.ForbiddenResponse());
        }
    }
}