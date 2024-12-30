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
        public IActionResult SearchStores(string keyword, int page = 1, int pageLimitNumber = 3)
        {
            var allFilterStores = db.Stores
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
                }).ToList();

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
        public IActionResult GetStoreById(int storeID)
        {
            var store = db.Stores
                .Include(s => s.Owner)
                .FirstOrDefault(x => x.Enabled && x.StoreId == storeID);
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
        public IActionResult GetStore()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var store = db.Stores
                .Include(s => s.Owner)
                .FirstOrDefault(x => x.Enabled && x.OwnerId == uid);

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
        [ResponseType(typeof(List<ResponseOrderDto>))]
        public IActionResult GetStoreOrders()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var store = db.Stores
                .First(x => x.Enabled && x.OwnerId == uid);

            var orderDetails = db.OrderDetails
                .Include(od => od.Pvc)
                .ThenInclude(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(od => od.Order)
                .ThenInclude(o => o.Status)
                .Include(od => od.Order.Coupon)
                .Where(x => x.Pvc.Product.StoreId == store.StoreId)
                .GroupBy(x => x.Order)
                .Select(x =>
                new ResponseOrderDto()
                {
                    OrderID = x.Key.OrderId,
                    CreateAt = x.Key.CreatedAt,
                    Status = x.Key.Status.Status1,
                    Amount = x.Sum(y => y.Quantity * y.Pvc.Price),
                    Discount = x.Key.Coupon == null ? 1 : x.Key.Coupon.Discount,
                    Details = x.Select(y => new ResponseOrderDetailDto()
                    {
                        PVCID = y.Pvcid,
                        Name = y.Pvc.Name,
                        Price = y.Pvc.Price,
                        Quantity = y.Quantity,
                        Images = y.Pvc.Product.ProductImages.Select(img => img.Url).ToList()
                    }).ToList()
                }).ToList();

            if (orderDetails.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(orderDetails);
        }

        /// <summary>
        /// 取得商店商品和銷量
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Store/GetStoreProductAndSale")]
        [ResponseType(typeof(List<ResponseProductWithSaleDto>))]
        public IActionResult GetStoreProductAndSale()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);


            var store = db.Stores
                .Include(s => s.Owner)
                .FirstOrDefault(x => x.Enabled && x.OwnerId == uid);

            var productGroups = db.ProductVersionControls
            .Include(s => s.Product)
            .Where(x => x.Product.StoreId == store.StoreId && x.Product.Enabled)
            .GroupBy(x => x.ProductId)
            .ToList(); // Execute the query up to this point

            var products = productGroups
                .Select(x => new ProdcutWithSale()
                {
                    Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
                })
                .Select(x => new ResponseProductWithSaleDto
                {
                    ID = x.LastestPVC.ProductId,
                    Name = x.LastestPVC.Name,
                    Description = x.LastestPVC.Description,
                    Price = x.LastestPVC.Price,
                    Quantity = x.LastestPVC.Product.Quantity,
                    StoreID = x.LastestPVC.Product.StoreId,
                    Sale = x.Sale,
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
        public IActionResult GetStoreRatings()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var store = db.Stores
                .First(x => x.Enabled && x.OwnerId == uid);

            var result = db.Ratings
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
                }).ToList();
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
        public IActionResult GetStores(int page = 1, int pageLimitNumber = 50, bool isASC = true)
        {
            var temp = isASC ? db.Stores.OrderBy(x => x.StoreId) : db.Stores.OrderByDescending(x => x.StoreId);
            var result = temp
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
               }).ToList();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            int count = db.Stores.Count();
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
        public IActionResult CreateStore(RequestCreateStoreDto dto)
        {
            // 沒有這個用戶
            if (!db.Users.Any(x => x.Uid == dto.OwnerID))
                return CatStatusCode.BadRequest();
            // 這個用戶已經有商店
            if (db.Stores.Any(x => x.OwnerId == dto.OwnerID))
                return CatStatusCode.BadRequest();
            var store = new Store()
            {
                StoreId = db.Stores.Any() ? db.Stores.Max(x => x.StoreId) + 1 : 1,
                OwnerId = dto.OwnerID,
                Name = dto.Name,
                Description = dto.Description,
                Enabled = true
            };
            db.Stores.Add(store);
            db.SaveChanges();
            return CatStatusCode.Ok();
        }
        /// <summary>
        /// 賣家修改商店資訊
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Store/ModifyStore")]
        public IActionResult ModifyStore(RequestModifyStoreDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            var store = db.Stores
                .First(x => x.OwnerId == uid);
            store.Name = dto.Name;
            store.Description = dto.Description;
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 管理員修改商店 enabled 狀態
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Admin")]
        [Route("~/api/Store/ModifyStoreEnabled")]
        public IActionResult ModifyStoreEnabled(RequestModifyStoreEnabledDto dto)
        {
            if (!db.Stores.Any(x => x.StoreId == dto.StoreID))
                return CatStatusCode.BadRequest();
            var store = db.Stores.Find(dto.StoreID);
            store.Enabled = dto.Enabled;
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        public class RequestModifyStoreDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }
        public class RequestCreateStoreDto
        {
            public int OwnerID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
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