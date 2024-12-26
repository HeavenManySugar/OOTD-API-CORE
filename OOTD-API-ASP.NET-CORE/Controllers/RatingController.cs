using OOTD_API.Security;
using OOTD_API.StatusCode;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using NSwag.Annotations;
using Microsoft.AspNetCore.Authorization;
using OOTD_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;


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
                .Include(r => r.UidNavigation)
                .Include(r => r.Product)
                .ThenInclude(p => p.ProductVersionControls)
                .Include(r => r.Product.ProductImages)
                .Where(x => x.ProductId == productId)
                .Select(x => new ResponseRatingDto()
                {
                    Username = x.UidNavigation.Username,
                    Rating = x.Rating1,
                    CreatedAt = x.CreatedAt,
                    ProductID = x.ProductId,
                    ProductName = x.Product.ProductVersionControls.OrderByDescending(y => y.Version).FirstOrDefault().Name,
                    ProductImageUrl = x.Product.ProductImages.FirstOrDefault().Url
                })
                .ToList();
            //if (result.Count == 0)
            //    return CatStatusCode.NotFound();
            return Ok(result);
        }

        /// <summary>
        /// 取得剩餘留言次數
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("api/Rating/GetRemainingRatingTimes")]
        [ResponseType(typeof(ResponseRemainingRatingDto))]
        public IActionResult GetRemainingRatingTimes(int productId)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            var orderCount = db.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Pvc)
                .Count(x => x.Order.Uid == uid && x.Pvc.ProductId == productId);
            var ratingCount = db.Ratings.Count(x => x.Uid == uid && x.ProductId == productId);
            var remainingRatingTimes = Math.Max(0, orderCount - ratingCount);
            return Ok(
                new ResponseRemainingRatingDto()
                {
                    RemainingRatingTimes = remainingRatingTimes
                });
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

        public class ResponseRemainingRatingDto
        {
            public int RemainingRatingTimes { get; set; }
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
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public string ProductImageUrl { get; set; }

        }
    }
}