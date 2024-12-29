using OOTD_API.StatusCode;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using Microsoft.AspNetCore.Mvc;
using OOTD_API.Security;
using Microsoft.AspNetCore.Authorization;
using NSwag.Annotations;
using System.IdentityModel.Tokens.Jwt;
using OOTD_API.Models;

namespace OOTD_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly OOTDV1Entities db;
        private readonly JwtAuthUtil _JwtAuthUtil;

        public MessageController(OOTDV1Entities db, JwtAuthUtil JwtAuthUtil)
        {
            this.db = db;
            this._JwtAuthUtil = JwtAuthUtil;

        }

        /// <summary>
        /// 取得聯絡人
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("~/api/Message/GetContacts")]
        [ResponseType(typeof(List<ResponseContactDto>))]
        public IActionResult GetContacts()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            var contacts = db.Messages
                .Where(x => x.Receiver.Uid == uid || x.Sender.Uid == uid)
                .Select(x => x.Receiver.Uid == uid ?
                new ResponseContactDto { UID = x.Sender.Uid, Username = x.Sender.Username } :
                new ResponseContactDto { UID = x.Receiver.Uid, Username = x.Receiver.Username })
                .Distinct()
                .ToList();
            if (contacts.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(contacts);
        }

        /// <summary>
        /// 取得聊天訊息
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("~/api/Message/GetMessages")]
        [ResponseType(typeof(List<ResponseMessageDto>))]
        public IActionResult GetMessages(int contactUID)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var messages = db.Messages
                .Where(x => (x.Receiver.Uid == uid && x.Sender.Uid == contactUID) || (x.Receiver.Uid == contactUID && x.Sender.Uid == uid))
                .Select(x =>
                    new ResponseMessageDto
                    {
                        IsSender = x.Receiver.Uid == uid,
                        Message = x.Message1,
                        CreatedAt = x.CreatedAt
                    }
                ).ToList();
            if (messages.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(messages);
        }

        /// <summary>
        /// 發送訊息
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("~/api/Message/SendMessage")]

        public IActionResult SendMessage([FromBody] RequestSendMessageDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var message = new Message()
            {
                MessageId = db.Messages.Any() ? db.Messages.Max(x => x.MessageId) + 1 : 1,
                SenderId = uid,
                ReceiverId = dto.ReceiverID,
                Message1 = dto.Message,
                CreatedAt = DateTime.UtcNow
            };
            db.Messages.Add(message);
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        public class ResponseContactDto
        {
            public int UID { get; set; }
            public string Username { get; set; }
        }

        public class ResponseMessageDto
        {
            public bool IsSender { get; set; }
            public string Message { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
        }

        public class RequestSendMessageDto
        {
            public int ReceiverID { get; set; }
            public string Message { get; set; }
        }
    }
}