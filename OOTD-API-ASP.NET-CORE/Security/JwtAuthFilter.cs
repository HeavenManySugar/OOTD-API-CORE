using Jose;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Web.Configuration;

namespace OOTD_API.Security
{
    public class JwtAuthFilter : ActionFilterAttribute
    {
        // 加解密的 key，如果不一樣會無法成功解密
        private static readonly string secretKey = WebConfigurationManager.AppSettings["TokenKey"];

        /// <summary>
        /// 過濾有用標籤 [JwtAuthFilter] 請求的 API 的 JwtToken 狀態及內容
        /// </summary>
        /// <param name="actionContext"></param>
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // 取出請求內容並排除不需要驗證的 API
            var request = actionContext.Request;
            if (!WithoutVerifyToken(request.RequestUri.ToString()))
            {
                // 有取到 JwtToken 後，判斷授權格式不存在且不正確時
                if (request.Headers.Authorization == null || request.Headers.Authorization.Scheme != "Bearer")
                {
                    // 可考慮配合前端專案開發期限，不修改 StatusCode 預設 200，將請求失敗搭配 Status: false 供前端判斷
                    string messageJson = JsonConvert.SerializeObject(new { Status = false, Message = "請重新登入" }); // JwtToken 遺失，需導引重新登入
                    var errorMessage = new HttpResponseMessage()
                    {
                        // StatusCode = System.Net.HttpStatusCode.Unauthorized, // 401
                        ReasonPhrase = "JwtToken Lost",
                        Content = new StringContent(messageJson,
                                    Encoding.UTF8,
                                    "application/json"),
                        StatusCode = System.Net.HttpStatusCode.Unauthorized
                    };
                    throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
                }
                else
                {
                    try
                    {
                        // 有 JwtToken 且授權格式正確時執行，用 try 包住，因為如果有篡改可能解密失敗
                        // 解密後會回傳 Json 格式的物件 (即加密前的資料)
                        var jwtObject = GetToken(request.Headers.Authorization.Parameter);

                        // 檢查有效期限是否過期，如 JwtToken 過期，需導引重新登入
                        if (IsTokenExpired(jwtObject["Exp"].ToString()))
                        {
                            string messageJson = JsonConvert.SerializeObject(new { Status = false, Message = "請重新登入" }); // JwtToken 過期，需導引重新登入
                            var errorMessage = new HttpResponseMessage()
                            {
                                // StatusCode = System.Net.HttpStatusCode.Unauthorized, // 401
                                ReasonPhrase = "JwtToken Expired",
                                Content = new StringContent(messageJson,
                                    Encoding.UTF8,
                                    "application/json"),
                                StatusCode = System.Net.HttpStatusCode.Unauthorized
                            };
                            throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
                        }
                    }
                    catch (Exception)
                    {
                        // 解密失敗
                        string messageJson = JsonConvert.SerializeObject(new { Status = false, Message = "請重新登入" }); // JwtToken 不符，需導引重新登入
                        var errorMessage = new HttpResponseMessage()
                        {
                            // StatusCode = System.Net.HttpStatusCode.Unauthorized, // 401
                            ReasonPhrase = "JwtToken NotMatch",
                            Content = new StringContent(messageJson,
                                    Encoding.UTF8,
                                    "application/json"),
                            StatusCode = System.Net.HttpStatusCode.Unauthorized
                        };
                        throw new HttpResponseException(errorMessage); // Debug 模式會停在此行，點繼續執行即可
                    }
                }
            }
            base.OnActionExecuting(actionContext);
        }

        /// <summary>
        /// 將 Token 解密取得夾帶的資料
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Dictionary<string, object> GetToken(string token)
        {
            return JWT.Decode<Dictionary<string, object>>(token, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
        }

        /// <summary>
        /// 有在 Global 設定一律檢查 JwtToken 時才需設定排除，例如 Login 不需要驗證因為還沒有 token
        /// </summary>
        /// <param name="requestUri"></param>
        /// <returns></returns>
        public bool WithoutVerifyToken(string requestUri)
        {
            //if (requestUri.EndsWith("/login")) return true;
            return false;
        }

        /// <summary>
        /// 驗證 token 時效
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public bool IsTokenExpired(string dateTime)
        {
            return Convert.ToDateTime(dateTime) < DateTime.UtcNow;
        }
    }
}