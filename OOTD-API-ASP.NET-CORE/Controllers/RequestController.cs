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
        public IActionResult GetRequests()
        {
            var result = db.Requests.Select(
                x => new ResponseRequestDto
                {
                    ID = x.RequestId,
                    Username = x.UidNavigation.Username,
                    CreatedAt = x.CreatedAt,
                    Message = x.Message,
                    Status = x.Status.Status1
                }).ToList();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result);
        }

        /// <summary>
        /// 用戶取的自己的請求
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("api/Request/GetOwnRequests")]
        [ResponseType(typeof(List<ResponseOwnRequestDto>))]
        public IActionResult GetOwnRequests()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            var user = db.Users.Include(u => u.Requests).ThenInclude(r => r.Status).First(u => u.Uid == uid);
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
        public IActionResult SendRequest(string message)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            Request request = new Request
            {
                RequestId = db.Requests.Max(x => x.RequestId) + 1,
                Uid = uid,
                CreatedAt = DateTime.UtcNow,
                Message = message,
                StatusId = 1
            };

            db.Requests.Add(request);
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 管理員修改 Request 狀態 
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Admin")]
        [Route("api/Request/ModifyRequestStatus")]
        public IActionResult ModifyRequestStatus(int requestID, Status status)
        {
            if (!db.Requests.Any(x => x.RequestId == requestID))
                return CatStatusCode.BadRequest();
            var request = db.Requests.Find(requestID);
            request.StatusId = (int)status;
            if (!db.Statuses.Any(s => s.StatusId == (int)status))
                return CatStatusCode.BadRequest();
            db.SaveChanges();
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