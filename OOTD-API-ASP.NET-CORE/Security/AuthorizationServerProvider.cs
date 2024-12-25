using System.Threading.Tasks;

namespace OOTD_API.Security
{
    /// <summary>
    /// OAuth 配置並繼承 OAuthAuthorizationServerProvider
    /// </summary>
    public class AuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        /// <summary>
        /// 在驗證客戶端身分前調用，並依客戶端請求來源配置 CORS 允許類型設定
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task MatchEndpoint(OAuthMatchEndpointContext context)
        {
            // 依請求來源配置 CORS 允許類型設定
            SetCORSPolicy(context.OwinContext);

            // 如果請求為預檢請求則設為完成直接回傳
            if (context.Request.Method == "OPTIONS")
            {
                context.RequestCompleted();
                return Task.FromResult(0);
            }

            return base.MatchEndpoint(context);
        }

        /// <summary>
        /// 允許全部跨域
        /// </summary>
        /// <param name="context"></param>
        private void SetCORSPolicy(IOwinContext context)
        {
            // 全部跨域都允許

            string origin = context.Request.Headers.Get("Origin");
            if (origin != null)
                context.Response.Headers.Add("Access-Control-Allow-Origin",
                                                     new string[] { origin });
            // 配置允許請求的 Headers 內容
            context.Response.Headers.Add("Access-Control-Allow-Headers",
                                   new string[] { "Authorization", "Content-Type" });
            // 配置允許請求的 Headers 方法
            context.Response.Headers.Add("Access-Control-Allow-Methods",
                                   new string[] { "OPTIONS", "GET", "POST", "PUT", "DELETE" });
        }
    }
}