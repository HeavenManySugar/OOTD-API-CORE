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
    public class RatingController : ControllerBase
    {
        private readonly OOTDV1Entities db;
        private readonly JwtAuthUtil _JwtAuthUtil;

        public RatingController(OOTDV1Entities db, JwtAuthUtil JwtAuthUtil)
        {
            this.db = db;
            this._JwtAuthUtil = JwtAuthUtil;

        }

        /// <summary>
        /// 取得產品評論
        /// </summary>
        [HttpGet]
        [Route("~/api/Rating/GetProductRating")]
        [ResponseType(typeof(List<ResponseRatingDto>))]
        public IActionResult GetProductRating(int productId)
        {
            var result = db.Ratings
                .Where(x => x.ProductId == productId)
                .Select(x => new ResponseRatingDto()
                {
                    Username = x.UidNavigation.Username,
                    Rating = x.Rating1,
                    CreatedAt = x.CreatedAt
                })
                .ToList();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result);
        }

        /// <summary>
        /// 留下評分
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("~/api/Rating/LeaveRating")]
        public IActionResult LeaveRating([FromBody] RequestRatingDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var orderCount = db.OrderDetails.Count(x => x.Order.Uid == uid && x.Pvc.ProductId == dto.ProductID);
            var ratingCount = db.Ratings.Count(x => x.Uid == uid && x.ProductId == dto.ProductID);

            if (ratingCount + 1 > orderCount)
                return CatStatusCode.BadRequest();

            var rating = new Rating()
            {
                RatingId = db.Ratings.Any() ? db.Ratings.Max(x => x.RatingId) + 1 : 1,
                ProductId = dto.ProductID,
                Uid = uid,
                Rating1 = dto.Rating,
                CreatedAt = DateTime.UtcNow
            };

            db.Ratings.Add(rating);
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        public class RequestRatingDto
        {
            public int ProductID { get; set; }
            public double Rating { get; set; }
        }

        public class ResponseRatingDto
        {
            public string Username { get; set; }
            public double Rating { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
        }
    }
}