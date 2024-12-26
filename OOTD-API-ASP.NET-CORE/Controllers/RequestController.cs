using OOTD_API.Security;
using OOTD_API.StatusCode;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using NSwag.Annotations;
using Microsoft.AspNetCore.Authorization;
using OOTD_API.Models;


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
        /// 取得所有請求
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Request/GetRequest")]
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