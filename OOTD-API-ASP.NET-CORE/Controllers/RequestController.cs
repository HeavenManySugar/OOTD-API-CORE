using OOTD_API.Security;
using OOTD_API.StatusCode;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using NSwag.Annotations;
using Microsoft.AspNetCore.Authorization;
using OOTD_API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Microsoft.EntityFrameworkCore;


namespace OOTD_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly OOTDV1Entities db;
        private readonly JwtAuthUtil _JwtAuthUtil;

        public RequestController(OOTDV1Entities db, JwtAuthUtil JwtAuthUtil)
        {
            this.db = db;
            this._JwtAuthUtil = JwtAuthUtil;
        }

        /// <summary>
        /// 管理員取得所有請求
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Request/GetRequests")]
        [ResponseType(typeof(List<ResponseRequestDto>))]
        public async Task<IActionResult> GetRequests()
        {
            var result = await db.Requests
                .AsNoTracking().Select(
                x => new ResponseRequestDto
                {
                    ID = x.RequestId,
                    Username = x.UidNavigation.Username,
                    CreatedAt = x.CreatedAt,
                    Message = x.Message,
                    Status = x.Status.Status1
                }).ToListAsync();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result);
        }

        /// <summary>
        /// 用戶取的自己的請求
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("~/api/Request/GetOwnRequests")]
        [ResponseType(typeof(List<ResponseOwnRequestDto>))]
        public async Task<IActionResult> GetOwnRequests()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            var user = await db.Users.Include(u => u.Requests).ThenInclude(r => r.Status).AsNoTracking().FirstAsync(u => u.Uid == uid);
            if (!user.Requests.Any())
                return CatStatusCode.NotFound();
            var result = user.Requests.Select(x => new ResponseOwnRequestDto()
            {
                CreatedAt = x.CreatedAt,
                Message = x.Message,
                Status = x.Status.Status1
            });
            return Ok(result);
        }

        /// <summary>
        /// 發送請求 
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("~/api/Request/SendRequest")]
        public async Task<IActionResult> SendRequest(string message)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            Request request = new Request
            {
                RequestId = await db.Requests.AsNoTracking().MaxAsync(x => x.RequestId) + 1,
                Uid = uid,
                CreatedAt = DateTime.UtcNow,
                Message = message,
                StatusId = 1
            };

            db.Requests.Add(request);
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 管理員修改 Request 狀態 
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Request/ModifyRequestStatus")]
        public async Task<IActionResult> ModifyRequestStatus(int requestID, Status status)
        {
            if (!await db.Requests.AnyAsync(x => x.RequestId == requestID))
                return CatStatusCode.BadRequest();
            var request = await db.Requests.FindAsync(requestID);
            request.StatusId = (int)status;
            if (!await db.Statuses.AsNoTracking().AnyAsync(s => s.StatusId == (int)status))
                return CatStatusCode.BadRequest();
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        [JsonConverter(typeof(StringEnumConverter))] // 若需要將enum以字串顯示
        public enum Status
        {
            NotExamined = 1,
            NotPass = 2,
            Pass = 3
        }

        public class ResponseOwnRequestDto
        {
            public DateTimeOffset CreatedAt { get; set; }
            public string Message { get; set; }
            public string Status { get; set; }
        }

        public class ResponseRequestDto
        {
            public int ID { get; set; }
            public string Username { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public string Message { get; set; }
            public string Status { get; set; }
        }
    }
}