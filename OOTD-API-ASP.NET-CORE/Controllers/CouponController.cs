using OOTD_API.Security;
using OOTD_API.StatusCode;
using Microsoft.AspNetCore.Mvc;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using System.IdentityModel.Tokens.Jwt;
using NSwag.Annotations;
using Microsoft.AspNetCore.Authorization;
using OOTD_API.Models;
using Microsoft.EntityFrameworkCore;

namespace OOTD_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponController : ControllerBase
    {
        private readonly OOTDV1Entities db;
        private readonly JwtAuthUtil _JwtAuthUtil;

        public CouponController(OOTDV1Entities db, JwtAuthUtil JwtAuthUtil)
        {
            this.db = db;
            this._JwtAuthUtil = JwtAuthUtil;

        }

        /// <summary>
        /// 買家取得用戶可用優惠券
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("~/api/Coupon/GetUserCoupons")]
        [ResponseType(typeof(List<ResponseUserCouponDto>))]
        public async Task<IActionResult> GetUserCoupons()
        {
            var Uid = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            var result = await db.UserCoupons
                .Where(x => x.Coupon.Enabled && DateTime.UtcNow >= x.Coupon.StartDate && DateTime.UtcNow <= x.Coupon.ExpireDate)
                .Where(x => x.Uid == int.Parse(Uid ?? "0") && x.Quantity > 0)
                .Select(x => new ResponseUserCouponDto
                {
                    CouponID = x.CouponId,
                    Name = x.Coupon.Name,
                    Description = x.Coupon.Description,
                    Discount = x.Coupon.Discount,
                    StartDate = x.Coupon.StartDate,
                    ExpireDate = x.Coupon.ExpireDate,
                    Quantity = x.Quantity
                }).ToListAsync();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result);
        }

        /// <summary>
        /// 管理員取得所有優惠券
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Coupon/GetAllCoupons")]
        [ResponseType(typeof(List<ResponseCouponDto>))]
        public async Task<IActionResult> GetAllCoupons()
        {
            var result = await db.Coupons
                .Select(x =>
                    new ResponseCouponDto
                    {
                        CouponID = x.CouponId,
                        Name = x.Name,
                        Description = x.Description,
                        Discount = x.Discount,
                        StartDate = x.StartDate,
                        ExpireDate = x.ExpireDate,
                        Enabled = x.Enabled
                    }
                ).ToListAsync();

            if (result.Count == 0)
                return CatStatusCode.NotFound();

            return Ok(result);
        }

        /// <summary>
        /// 管理員新增優惠券
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Coupon/AddCoupon")]
        public async Task<IActionResult> AddCoupon(RequestAddCouponDto dto)
        {
            var coupon = new Coupon()
            {
                CouponId = await db.Coupons.AnyAsync() ? await db.Coupons.MaxAsync(x => x.CouponId) + 1 : 1,
                Name = dto.Name,
                Description = dto.Description,
                Discount = dto.Discount,
                StartDate = dto.StartDate.UtcDateTime,
                ExpireDate = dto.ExpireDate.UtcDateTime,
                Enabled = dto.Enabled,
            };

            await db.Coupons.AddAsync(coupon);
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 管理員發放優惠券給所有人
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Coupon/GiveCouponToAllUser")]
        public async Task<IActionResult> GiveCouponToAllUser(int couponId, int count)
        {
            var couponFlag = await db.Coupons.AnyAsync(x => x.CouponId == couponId);
            if (!couponFlag)
                return CatStatusCode.BadRequest();

            var userCoupons = await db.UserCoupons.Where(x => x.CouponId == couponId).ToListAsync();
            userCoupons.ForEach(x => x.Quantity += count);

            int lastId = await db.UserCoupons.AnyAsync() ? await db.UserCoupons.MaxAsync(x => x.UserCouponId) + 1 : 1;
            var users = await db.Users.Where(x => !x.UserCoupons.Any(y => y.CouponId == couponId)).ToListAsync();
            users.ForEach(x =>
                db.UserCoupons.Add(new UserCoupon()
                {
                    UserCouponId = lastId++,
                    Uid = x.Uid,
                    CouponId = couponId,
                    Quantity = count
                }));

            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 管理員發放優惠券給指定的人
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Coupon/GiveCouponToSpecificlUser")]
        public async Task<IActionResult> GiveCouponToSpecificlUser(RequsetGiveCouponToSpecificlUserDto dto)
        {
            var couponFlag = await db.Coupons.AnyAsync(x => x.CouponId == dto.CouponID);
            var userFlag = await db.Users.AnyAsync(x => x.Uid == dto.UID);
            if (!(couponFlag && userFlag))
                return CatStatusCode.BadRequest();

            var userCoupon = await db.UserCoupons.FirstOrDefaultAsync(x => x.CouponId == dto.CouponID && x.Uid == dto.UID);
            if (userCoupon != null)
            {
                userCoupon.Quantity += dto.Count;
            }
            else
            {
                await db.UserCoupons.AddAsync(new UserCoupon()
                {
                    UserCouponId = await db.UserCoupons.AnyAsync() ? await db.UserCoupons.MaxAsync(x => x.UserCouponId) + 1 : 1,
                    Uid = dto.UID,
                    CouponId = dto.CouponID,
                    Quantity = dto.Count
                });
            }
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 管理員修改優惠券資料
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Coupon/ModifyCoupon")]
        public async Task<IActionResult> ModifyCoupon(ResponseCouponDto dto)
        {
            var coupon = await db.Coupons.FirstOrDefaultAsync(x => x.CouponId == dto.CouponID);
            if (coupon == null)
                return CatStatusCode.NotFound();

            coupon.Name = dto.Name;
            coupon.Discount = dto.Discount;
            coupon.Description = dto.Description;
            coupon.StartDate = dto.StartDate.UtcDateTime;
            coupon.ExpireDate = dto.ExpireDate.UtcDateTime;
            coupon.Enabled = dto.Enabled;
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        public class RequsetGiveCouponToSpecificlUserDto
        {
            public int UID { get; set; }
            public int CouponID { get; set; }
            public int Count { get; set; }
        }

        public class ResponseCouponDto
        {
            public int CouponID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public double Discount { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public DateTimeOffset ExpireDate { get; set; }
            public bool Enabled { get; set; }
        }

        public class RequestAddCouponDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public double Discount { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public DateTimeOffset ExpireDate { get; set; }
            public bool Enabled { get; set; }
        }

        public class ResponseUserCouponDto
        {
            public int CouponID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public double Discount { get; set; }
            public DateTimeOffset StartDate { get; set; }
            public DateTimeOffset ExpireDate { get; set; }
            public int Quantity { get; set; }
        }
    }
}