using OOTD_API.StatusCode;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using Microsoft.AspNetCore.Mvc;
using OOTD_API.Security;
using Microsoft.AspNetCore.Authorization;
using NSwag.Annotations;
using System.IdentityModel.Tokens.Jwt;
using OOTD_API.Models;
using Microsoft.EntityFrameworkCore;

namespace OOTD_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OOTDV1Entities db;
        private readonly JwtAuthUtil _JwtAuthUtil;

        public OrderController(OOTDV1Entities db, JwtAuthUtil JwtAuthUtil)
        {
            this.db = db;
            this._JwtAuthUtil = JwtAuthUtil;

        }

        /// <summary>
        /// 取得用戶所有訂單
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("~/api/Order/GetUserOrders")]
        [ResponseType(typeof(List<ResponseOrderDto>))]
        public async Task<IActionResult> GetUserOrders()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var result = await db.Orders
                .Where(x => x.Uid == uid)
                .Select(x =>
                new ResponseOrderDto
                {
                    OrderID = x.OrderId,
                    CreateAt = x.CreatedAt,
                    Status = x.Status.Status1,
                    Amount = x.OrderDetails.Sum(y => y.Quantity * y.Pvc.Price),
                    Discount = x.Coupon == null ? 1 : x.Coupon.Discount,
                    Details = x.OrderDetails.Select(y =>
                    new ResponseOrderDetailDto
                    {
                        PVCID = y.Pvc.Pvcid,
                        Name = y.Pvc.Name,
                        Price = y.Pvc.Price,
                        Quantity = y.Quantity,
                        Images = y.Pvc.Product.ProductImages.Select(img => img.Url).ToList()
                    }).ToList()
                })
                .OrderByDescending(x => x.CreateAt)
                .ToListAsync();

            if (result.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result);
        }

        /// <summary>
        /// 取得單一訂單細節
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("~/api/Order/GetUserOrderDetail")]
        [ResponseType(typeof(ResponseOrderDto))]
        public async Task<IActionResult> GetUserOrderDetail(int orderId)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var order = await db.Orders
                .Include(o => o.Status)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Pvc)
                .ThenInclude(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(x => x.Uid == uid && x.OrderId == orderId);

            if (order == null)
                return CatStatusCode.NotFound();

            var result = new ResponseOrderDto
            {
                OrderID = order.OrderId,
                CreateAt = order.CreatedAt,
                Status = order.Status.Status1,
                Amount = order.OrderDetails.Sum(x => x.Quantity * x.Pvc.Price),
                Discount = order.Coupon == null ? 1 : order.Coupon.Discount,
                Details = order.OrderDetails.Select(x =>
                new ResponseOrderDetailDto
                {
                    PVCID = x.Pvc.Pvcid,
                    Name = x.Pvc.Name,
                    Price = x.Pvc.Price,
                    Quantity = x.Quantity,
                    Images = x.Pvc.Product.ProductImages.Select(img => img.Url).ToList()
                }).ToList()
            };
            return Ok(result);
        }

        /// <summary>
        /// 下訂單
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("~/api/Order/MakeOrder")]
        public async Task<IActionResult> MakeOrder([FromBody] RequestOrderDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);            // 檢查庫存

            foreach (var detail in dto.Details)
            {
                var product = await db.Products.FirstOrDefaultAsync(p => p.ProductId == detail.ProductID);
                if (product == null)
                    return CatStatusCode.BadRequest();

                var availableQuantity = product.Quantity - detail.Quantity;
                if (availableQuantity < 0)
                    return CatStatusCode.BadRequest();
            }

            // 優惠券使用
            if (dto.CouponID != null)
            {
                var userCoupon = await db.UserCoupons.Where(x => x.Uid == uid && x.CouponId == dto.CouponID).FirstAsync();
                userCoupon.Quantity -= 1;
            }

            // 建立訂單
            var order = new Order()
            {
                OrderId = db.Orders.Any() ? db.Orders.Max(x => x.OrderId) + 1 : 1,
                Uid = uid,
                StatusId = 4,
                CreatedAt = DateTime.UtcNow
            };
            if (dto.CouponID != null)
                order.CouponId = dto.CouponID;

            // 建立訂單細節並扣除庫存
            var orderDetails = new List<OrderDetail>();
            var orderDetailIDAcc = db.OrderDetails.Any() ? db.OrderDetails.Max(x => x.OrderDetailId) + 1 : 1;
            foreach (var detail in dto.Details)
            {
                var orderDetail = new OrderDetail()
                {
                    OrderDetailId = orderDetailIDAcc++,
                    OrderId = order.OrderId,
                    Pvcid = db.Products
                    .Include(p => p.ProductVersionControls)
                    .FirstOrDefault(x => x.ProductId == detail.ProductID).ProductVersionControls.OrderByDescending(x => x.Version)
                    .FirstOrDefault().Pvcid,
                    Quantity = detail.Quantity
                };
                var product = await db.Products.FirstOrDefaultAsync(p => p.ProductId == detail.ProductID);
                product.Quantity -= detail.Quantity;
                orderDetails.Add(orderDetail);
            }
            await db.Orders.AddAsync(order);
            await db.OrderDetails.AddRangeAsync(orderDetails);

            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        public class RequestOrderDto
        {
            public int? CouponID { get; set; }
            public List<RequestOrderDetailDto> Details { get; set; }
        }

        public class RequestOrderDetailDto
        {
            public int ProductID { get; set; }
            public int Quantity { get; set; }
        }
        public class ResponseOrderDetailDto
        {
            public int PVCID { get; set; }
            public string Name { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public List<string> Images { get; set; }
        }

        public class ResponseOrderDto
        {
            public int OrderID { get; set; } = 0;
            public DateTimeOffset CreateAt { get; set; }
            public string Status { get; set; }
            public decimal Amount { get; set; }
            public double Discount { get; set; }
            public List<ResponseOrderDetailDto> Details { get; set; }
        }
    }
}