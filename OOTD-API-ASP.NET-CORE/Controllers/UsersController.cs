using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using OOTD_API.Models;
using OOTD_API.Security;
using OOTD_API.StatusCode;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using System.IdentityModel.Tokens.Jwt;

namespace OOTD_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly OOTDV1Entities db;
        private readonly JwtAuthUtil _JwtAuthUtil;

        public UsersController(OOTDV1Entities db, JwtAuthUtil JwtAuthUtil)
        {
            this.db = db;
            this._JwtAuthUtil = JwtAuthUtil;

        }

        [HttpGet("claims")]
        [Authorize]
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return Ok(claims);
        }


        /// <summary>
        /// 使用 JWT 獲取用戶資訊
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/User/GetUser")]
        [Authorize]
        [ResponseType(typeof(ResponseUserDto))]
        public IActionResult GetUser()
        {
            var Uid = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(Uid))
            {
                return BadRequest("User ID is missing in the token.");
            }

            var user = db.Users.Find(int.Parse(Uid));
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var response = new ResponseUserDto
            {
                Username = user.Username,
                Email = user.Email,
                Address = user.Address,
                IsAdministrator = user.IsAdministrator,
                HaveStore = user.Stores.Any()
            };
            return Ok(response);
        }

        /// <summary>
        /// 取得新的 JWT Token
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("api/User/GetRefreshedJWT")]
        [ResponseType(typeof(TokenDto))]
        public IActionResult GetRefreshedJWT()
        {
            string token = _JwtAuthUtil.ExpRefreshToken(User.Claims.ToArray());
            return Ok(new TokenDto()
            {
                Token = token,
                ExpireDate = DateTimeOffset.Parse(_JwtAuthUtil.GetTokenExpireDate(token).ToString())
            });
        }

        // <summary>
        // 使用者登入
        // </summary>
        [HttpPost]
        [Route("api/User/Login")]
        [ResponseType(typeof(TokenDto))]
        public IActionResult Login([FromBody] RequestLoginDto dto)
        {
            User user = db.Users.FirstOrDefault(x => x.Email == dto.Email && x.Password == dto.Password);
            if (user == null)
                return CatStatusCode.Unauthorized(); // 登入失敗
            if (!user.Enabled)
                return CatStatusCode.Forbidden();// user 被停用
            string token = _JwtAuthUtil.GenerateToken(user.Uid);
            return Ok(new TokenDto()
            {
                Token = token,
                ExpireDate = DateTimeOffset.Parse(_JwtAuthUtil.GetTokenExpireDate(token).ToString())
            });
        }

        /// <summary>
        /// 使用者註冊
        /// </summary>
        [HttpPost]
        [Route("api/User/Register")]
        public IActionResult Register([FromBody] RequestRegisterDto dto)
        {
            // 檢查是否有相同 email
            if (db.Users.Any(x => x.Email == dto.Email))
                return CatStatusCode.Conflict();

            User user = new User()
            {
                Uid = db.Users.Any() ? db.Users.Max(x => x.Uid) + 1 : 1,
                Username = dto.Username,
                Password = dto.Password,
                Email = dto.Email,
                Address = dto.Address,
                IsAdministrator = false,
                CreatedAt = DateTime.UtcNow,
                Enabled = true
            };
            db.Users.Add(user);
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 變更密碼
        /// </summary>
        [HttpPut]
        [Authorize]
        [Route("api/User/ModifyPassword")]
        public IActionResult ModifyPassword([FromBody] RequesetModifyPasswordDto dto)
        {
            var Uid = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var user = db.Users.Find(int.Parse(Uid));
            if (user.Password != dto.OldPassword)
                return CatStatusCode.Unauthorized();
            user.Password = dto.NewPassword;
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 修改用戶資訊
        /// </summary>
        [HttpPut]
        [Authorize]
        [Route("api/User/ModifyUserInformation")]
        [ResponseType(typeof(string))]
        public IActionResult ModifyUserInformation([FromBody] RequestModifyUserInformationDto dto)
        {
            var Uid = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var user = db.Users.Find(int.Parse(Uid));
            user.Username = dto.Username;
            user.Address = dto.Address;
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        public class TokenDto
        {
            public string Token { get; set; }
            public DateTimeOffset ExpireDate { get; set; }
        }

        public class RequesetModifyPasswordDto
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
        }

        public class ResponseDto
        {
            public bool Status { get; set; }
            public string Message { get; set; }
        }

        public class ResponseUserDto
        {
            public string Username { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public bool IsAdministrator { get; set; }
            public bool HaveStore { get; set; }
        }

        public class RequestModifyUserInformationDto
        {
            public string Username { get; set; }
            public string Address { get; set; }
        }

        public class RequestLoginDto
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        public class RequestRegisterDto
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
        }
    }
}