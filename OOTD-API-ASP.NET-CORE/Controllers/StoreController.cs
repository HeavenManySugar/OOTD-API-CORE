﻿using OOTD_API.Security;
using OOTD_API.StatusCode;
using static OOTD_API.Controllers.OrderController;
using static OOTD_API.Controllers.ProductController;
using static OOTD_API.Controllers.RatingController;
using Microsoft.AspNetCore.Mvc;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using System.IdentityModel.Tokens.Jwt;
using NSwag.Annotations;
using Microsoft.AspNetCore.Authorization;


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
                .Where(x => x.Enabled)
                .Where(x => x.Name.Contains(keyword) || x.Description.Contains(keyword))
                .Select(x => new ResponseStoreDto
                {
                    StoreID = x.StoreId,
                    OwnerUsername = x.Owner.Username,
                    Name = x.Name,
                    Description = x.Description
                }).ToList();

            var count = allFilterStores.Count();
            var pageCount = count / pageLimitNumber + (count % pageLimitNumber == 0 ? 0 : 1);

            var stores = allFilterStores
                .Skip((page - 1) * pageLimitNumber)
                .Take(pageLimitNumber)
                .Select(x => new ResponseStoreDto
                {
                    StoreID = x.StoreID,
                    OwnerUsername = x.OwnerUsername,
                    Name = x.Name,
                    Description = x.Description
                }).ToList();

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
                .FirstOrDefault(x => x.Enabled && x.StoreId == storeID);
            if (store == null)
                return CatStatusCode.NotFound();

            var result = new ResponseStoreDto
            {
                StoreID = store.StoreId,
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
                .First(x => x.Enabled && x.OwnerId == uid);

            var result = new ResponseStoreDto
            {
                StoreID = store.StoreId,
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
        [ResponseType(typeof(List<RequestOrderDto>))]
        public IActionResult GetStoreOrders()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var store = db.Stores
                .First(x => x.Enabled && x.OwnerId == uid);

            var orderDetails = db.OrderDetails
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
        [ResponseType(typeof(List<ResponseProductDto>))]
        public IActionResult GetStoreProductAndSale()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);


            var store = db.Stores
                .First(x => x.Enabled && x.OwnerId == uid);

            var products = db.ProductVersionControls
                .Where(x => x.Product.StoreId == store.StoreId && x.Product.Enabled)
                .GroupBy(x => x.ProductId)
                .Select(x =>
                new ProdcutWithSale()
                {
                    Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
                })
                .Select(x => new ResponseProductDto // 選擇輸出欄位
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
                .Where(x => x.Product.StoreId == store.StoreId)
                .Select(x => new ResponseRatingDto
                {
                    Username = x.UidNavigation.Username,
                    Rating = x.Rating1,
                    CreatedAt = x.CreatedAt
                }).ToList();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result);
        }

        public class ResponseStoreDto
        {
            public int StoreID { get; set; }
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