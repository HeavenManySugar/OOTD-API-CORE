using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;


namespace OOTD_API.Security
{
    public class JwtAuthUtil 
    {
        private readonly OOTDV1Entities db;
        private readonly IConfiguration _configuration;

        public JwtAuthUtil(OOTDV1Entities db, IConfiguration configuration)
        {
            this.db = db;
            _configuration = configuration;
        }
        /// <summary>
        /// 生成 JwtToken
        /// </summary>
        public string GenerateToken(int id)
        {
            var user = db.Users.Find(id);
            var storeExists = db.Stores.Any(x => x.Enabled && x.OwnerId == id);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (user.IsAdministrator)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            if (storeExists)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Seller"));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// 生成只刷新效期的 JwtToken
        /// </summary>
        public string ExpRefreshToken(Claim[] claims)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));

            var creds = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwtSettings["TokenExpiryMinutes"])),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        /// <summary>
        /// 將 Token 解密取得夾帶的資料
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Dictionary<string, object> GetToken(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            if (secretKey == null)
            {
                throw new ArgumentNullException(nameof(secretKey), "Secret key cannot be null.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = key
            };

            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(token.Replace("Bearer ", string.Empty), validationParameters, out validatedToken);

            // 移除重複的 key
            var claims = principal.Claims
                .GroupBy(c => c.Type)
                .Select(g => g.First())
                .ToDictionary(c => c.Type, c => (object)c.Value);

            return claims;
        }

        /// <summary>
        /// 取得 Token 過期時間expires
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public DateTime GetTokenExpireDate(string token)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            if (secretKey == null)
            {
                throw new ArgumentNullException(nameof(secretKey), "Secret key cannot be null.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = key
            };

            SecurityToken validatedToken;
            var principal = tokenHandler.ValidateToken(token, validationParameters, out validatedToken);
            var expireDate = validatedToken.ValidTo;

            return expireDate;
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