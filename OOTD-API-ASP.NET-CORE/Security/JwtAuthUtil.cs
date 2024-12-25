using Jose;
using OOTD_API.EntityFramework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Configuration;

namespace OOTD_API.Security
{
    public class JwtAuthUtil
    {
        private readonly OOTDV1Entities db = new OOTDV1Entities();

        /// <summary>
        /// 生成 JwtToken
        /// </summary>
        public string GenerateToken(int id)
        {
            string secretKey = WebConfigurationManager.AppSettings["TokenKey"];
            var user = db.User.Find(id);

            var payload = new Dictionary<string, object>
            {
                { "UID", user.UID },
                { "Username", user.Username },
                { "Exp", DateTimeOffset.UtcNow.AddMinutes(30).ToString()}
            };

            var token = JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
            return token;
        }

        /// <summary>
        /// 生成只刷新效期的 JwtToken
        /// </summary>
        public string ExpRefreshToken(Dictionary<string, object> tokenData)
        {
            string secretKey = WebConfigurationManager.AppSettings["TokenKey"];

            var payload = tokenData;
            payload["Exp"] = DateTimeOffset.UtcNow.AddMinutes(30).ToString();

            var token = JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
            return token;
        }

        /// <summary>
        /// 生成無效 JwtToken
        /// </summary>
        public string RevokeToken()
        {
            string secretKey = "RevokeToken";

            var payload = new Dictionary<string, object>
            {
                { "UID", 0 },
                { "Username", "Revoke" },
                { "Exp", DateTimeOffset.UtcNow.AddMinutes(-15).ToString()}
            };

            var token = JWT.Encode(payload, Encoding.UTF8.GetBytes(secretKey), JwsAlgorithm.HS512);
            return token;
        }
    }
}