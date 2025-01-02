using OOTD_API.Security;
using OOTD_API.StatusCode;
using static OOTD_API.Controllers.OrderController;
using static OOTD_API.Controllers.ProductController;
using static OOTD_API.Controllers.RatingController;
using Microsoft.AspNetCore.Mvc;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using System.IdentityModel.Tokens.Jwt;
using NSwag.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OOTD_API.Models;


namespace OOTD_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly OOTDV1Entities db;
        private readonly JwtAuthUtil _JwtAuthUtil;

        public StoreController(OOTDV1Entities db, JwtAuthUtil JwtAuthUtil)
        {
            this.db = db;
            this._JwtAuthUtil = JwtAuthUtil;

        }

        /// <summary>
        /// 搜尋商店
        /// </summary>
        [HttpGet]
        [Route("~/api/Store/SearchStores")]
        [ResponseType(typeof(List<ResponseStoresDto>))]
        public async Task<IActionResult> SearchStores(string keyword, int page = 1, int pageLimitNumber = 3)
        {
            var allFilterStores = await db.Stores
                .Include(s => s.Owner)
                .Where(x => x.Enabled)
                .Where(x => x.Name.Contains(keyword) || x.Description.Contains(keyword))
                .Select(x => new ResponseStoreDto
                {
                    StoreID = x.StoreId,
                    OwnerID = x.OwnerId,
                    OwnerUsername = x.Owner.Username,
                    Name = x.Name,
                    Description = x.Description
                }).ToListAsync();

            var count = allFilterStores.Count();
            var pageCount = count / pageLimitNumber + (count % pageLimitNumber == 0 ? 0 : 1);

            var stores = allFilterStores
                .Skip((page - 1) * pageLimitNumber)
                .Take(pageLimitNumber)
                .ToList();

            if (stores.Count == 0)
                return CatStatusCode.NotFound();

            return Ok(new ResponseStoresDto()
            {
                PageCount = pageCount,
                Stores = stores
            });
        }


        /// <summary>
        /// 使用 store id 取得商店
        /// </summary>
        [HttpGet]
        [Route("~/api/Store/GetStoreById")]
        [ResponseType(typeof(ResponseStoreDto))]
        public async Task<IActionResult> GetStoreById(int storeID)
        {
            var store = await db.Stores
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(x => x.Enabled && x.StoreId == storeID);
            if (store == null)
                return CatStatusCode.NotFound();

            var result = new ResponseStoreDto
            {
                StoreID = store.StoreId,
                OwnerID = store.OwnerId,
                OwnerUsername = store.Owner.Username,
                Name = store.Name,
                Description = store.Description
            };
            return Ok(result);
        }

        /// <summary>
        /// 賣家取得商店資訊
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Store/GetStore")]
        [ResponseType(typeof(ResponseStoreDto))]
        public async Task<IActionResult> GetStore()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var store = await db.Stores
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(x => x.Enabled && x.OwnerId == uid);

            if (store == null)
            {
                return NotFound("Store not found.");
            }

            if (store.Owner == null)
            {
                return BadRequest("Store owner information is missing.");
            }

            var result = new ResponseStoreDto
            {
                StoreID = store.StoreId,
                OwnerID = store.OwnerId,
                OwnerUsername = store.Owner.Username,
                Name = store.Name,
                Description = store.Description
            };
            return Ok(result);
        }

        /// <summary>
        /// 賣家取得商店訂單
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Store/GetStoreOrders")]
        [ResponseType(typeof(List<ResponseStoreOrderDto>))]
        public async Task<IActionResult> GetStoreOrders()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var store = await db.Stores
                .FirstAsync(x => x.OwnerId == uid);

            var orderDetails = await db.OrderDetails
                .Include(od => od.Pvc)
                .ThenInclude(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(od => od.Order)
                .ThenInclude(o => o.Status)
                .Include(od => od.Order.Coupon)
                .Include(od => od.Order.UidNavigation)
                .Where(x => x.Pvc.Product.StoreId == store.StoreId)
                .ToListAsync(); // 先執行查詢

            var groupedOrderDetails = orderDetails
                .GroupBy(x => new { x.Order.OrderId, x.Order.CreatedAt, x.Order.Status.Status1, CouponDiscount = x.Order.Coupon == null ? 1 : x.Order.Coupon.Discount, x.Order.UidNavigation })
                .Select(g => new ResponseStoreOrderDto()
                {
                    OrderID = g.Key.OrderId,
                    CreateAt = g.Key.CreatedAt,
                    Status = g.Key.Status1,
                    Amount = g.Sum(y => y.Quantity * y.Pvc.Price),
                    Discount = g.Key.CouponDiscount,
                    Address = g.Key.UidNavigation.Address,
                    Username = g.Key.UidNavigation.Username,
                    Details = g.Select(y => new ResponseOrderDetailDto()
                    {
                        PVCID = y.Pvcid,
                        Name = y.Pvc.Name,
                        Price = y.Pvc.Price,
                        Quantity = y.Quantity,
                        Images = y.Pvc.Product.ProductImages.Select(img => img.Url).ToList()
                    }).ToList()
                }).ToList();

            if (groupedOrderDetails.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(groupedOrderDetails);
        }

        /// <summary>
        /// 取得商店商品和銷量
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Store/GetStoreProductAndSale")]
        [ResponseType(typeof(List<ResponseStoreProductWithSaleDto>))]
        public async Task<IActionResult> GetStoreProductAndSale()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var store = await db.Stores
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(x => x.Enabled && x.OwnerId == uid);

            var productGroups = await db.ProductVersionControls
                .Include(s => s.Product)
                .Where(x => x.Product.StoreId == store.StoreId && x.Product.Enabled)
                .GroupBy(x => x.ProductId)
                .ToListAsync(); // Execute the query up to this point

            var products = productGroups
                .Select(x => new ProdcutWithSale()
                {
                    Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
                })
                .Select(x => new ResponseStoreProductWithSaleDto
                {
                    ID = x.LastestPVC.ProductId,
                    Name = x.LastestPVC.Name,
                    Description = x.LastestPVC.Description,
                    Price = x.LastestPVC.Price,
                    Quantity = x.LastestPVC.Product.Quantity,
                    StoreID = x.LastestPVC.Product.StoreId,
                    Sale = x.Sale,
                    Enabled = x.LastestPVC.Product.Enabled,
                    Images = x.LastestPVC.Product.ProductImages.Select(img => img.Url).ToList()
                }).ToList();

            return Ok(products);
        }

        /// <summary>
        /// 取得買家的評價
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Store/GetStoreRatings")]
        [ResponseType(typeof(List<ResponseRatingDto>))]
        public async Task<IActionResult> GetStoreRatings()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var store = await db.Stores
                .FirstAsync(x => x.Enabled && x.OwnerId == uid);

            var result = await db.Ratings
                .Include(r => r.Product)
                .ThenInclude(p => p.ProductVersionControls)
                .Include(r => r.Product.ProductImages)
                .Where(x => x.Product.StoreId == store.StoreId)
                .Select(x => new ResponseRatingDto
                {
                    Username = x.UidNavigation.Username,
                    Rating = x.Rating1,
                    CreatedAt = x.CreatedAt,
                    ProductID = x.ProductId,
                    ProductName = x.Product.ProductVersionControls.OrderByDescending(y => y.Version).FirstOrDefault().Name,
                    ProductImageUrl = x.Product.ProductImages.FirstOrDefault().Url
                }).ToListAsync();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result);
        }

        /// <summary>
        /// 管理員取得所有商店資訊
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Store/GetStores")]
        [ResponseType(typeof(ResponseStoresForAdminDto))]
        public async Task<IActionResult> GetStores(int page = 1, int pageLimitNumber = 50, bool isASC = true)
        {
            var temp = isASC ? db.Stores.OrderBy(x => x.StoreId) : db.Stores.OrderByDescending(x => x.StoreId);
            var result = await temp
               .Include(s => s.Owner)
               .Skip((page - 1) * pageLimitNumber)
               .Take(pageLimitNumber)
               .Select(x => new ResponseStoreForAdminDto
               {
                   StoreID = x.StoreId,
                   OwnerID = x.OwnerId,
                   OwnerUsername = x.Owner.Username,
                   Name = x.Name,
                   Description = x.Description,
                   Enabled = x.Enabled
               }).ToListAsync();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            int count = await db.Stores.CountAsync();
            if (count % pageLimitNumber == 0)
                count = count / pageLimitNumber;
            else
                count = count / pageLimitNumber + 1;
            return Ok(new ResponseStoresForAdminDto()
            {
                PageCount = count,
                Stores = result.ToList()
            });
        }

        /// <summary>
        /// 管理員幫賣家建立商店
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Store/CreateStore")]
        public async Task<IActionResult> CreateStore(RequestCreateStoreDto dto)
        {
            // 沒有這個用戶
            if (!await db.Users.AnyAsync(x => x.Uid == dto.OwnerID))
                return CatStatusCode.BadRequest();
            // 這個用戶已經有商店
            if (await db.Stores.AnyAsync(x => x.OwnerId == dto.OwnerID))
                return CatStatusCode.BadRequest();
            var store = new Store()
            {
                StoreId = await db.Stores.AnyAsync() ? await db.Stores.MaxAsync(x => x.StoreId) + 1 : 1,
                OwnerId = dto.OwnerID,
                Name = dto.Name,
                Description = dto.Description,
                Enabled = true
            };
            db.Stores.Add(store);
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }
        /// <summary>
        /// 賣家修改商店資訊
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Store/ModifyStore")]
        public async Task<IActionResult> ModifyStore(RequestModifyStoreDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            var store = await db.Stores
                .FirstAsync(x => x.OwnerId == uid);
            store.Name = dto.Name;
            store.Description = dto.Description;
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 管理員修改商店 enabled 狀態
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Store/ModifyStoreEnabled")]
        public async Task<IActionResult> ModifyStoreEnabled(RequestModifyStoreEnabledDto dto)
        {
            if (!await db.Stores.AnyAsync(x => x.StoreId == dto.StoreID))
                return CatStatusCode.BadRequest();
            var store = await db.Stores.FindAsync(dto.StoreID);
            store.Enabled = dto.Enabled;
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        public class ResponseStoreProductWithSaleDto : ResponseProductWithSaleDto
        {
            public bool Enabled { get; set; }
        }
        public class ResponseStoreOrderDto : ResponseOrderDto
        {
            public string Address { get; set; }
            public string Username { get; set; }
        }

        public class RequestModifyStoreDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }
        public class RequestCreateStoreDto
        {
            public int OwnerID { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }


        public class RequestModifyStoreEnabledDto
        {
            public int StoreID { get; set; }
            public bool Enabled { get; set; }
        }
        public class ResponseStoresForAdminDto
        {
            public int PageCount { get; set; }
            public List<ResponseStoreForAdminDto> Stores { get; set; }
        }
        public class ResponseStoreForAdminDto : ResponseStoreDto
        {
            public bool Enabled { get; set; }
        }

        public class ResponseStoreDto
        {
            public int StoreID { get; set; }
            public int OwnerID { get; set; }
            public string OwnerUsername { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        public class ResponseStoresDto
        {
            public int PageCount { get; set; }
            public List<ResponseStoreDto> Stores { get; set; }
        }
    }
}