using OOTD_API.StatusCode;
using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using Microsoft.AspNetCore.Mvc;
using OOTD_API.Security;
using Microsoft.AspNetCore.Authorization;
using NSwag.Annotations;
using System.IdentityModel.Tokens.Jwt;
using OOTD_API.Models;
using Microsoft.Extensions.Primitives;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.CodeAnalysis;

namespace OOTD_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly OOTDV1Entities db;
        private readonly JwtAuthUtil _JwtAuthUtil;

        public ProductController(OOTDV1Entities db, JwtAuthUtil JwtAuthUtil)
        {
            this.db = db;
            this._JwtAuthUtil = JwtAuthUtil;

        }

        private Dictionary<ProductOrderField, Func<ProdcutWithSale, object>> keySelector = new Dictionary<ProductOrderField, Func<ProdcutWithSale, object>>
        {
            { ProductOrderField.Price, x => x.LastestPVC.Price },
            { ProductOrderField.Quantity, x => x.LastestPVC.Product.Quantity },
            { ProductOrderField.Sale , x=>x.Sale },
            { ProductOrderField.Default , x=>x.LastestPVC.ProductId }
        };


        private ResponseProductsDto GenerateProductPage(List<ProdcutWithSale> products, int page, int pageLimitNumber, ProductOrderField orderField = ProductOrderField.Default, bool isASC = true)
        {
            var count = products.Count;
            var pageCount = count / pageLimitNumber + (count % pageLimitNumber == 0 ? 0 : 1);

            var result = (isASC ? products.OrderBy(keySelector[orderField]) : products.OrderByDescending(keySelector[orderField]))
                .Skip((page - 1) * pageLimitNumber)
                .Take(pageLimitNumber)
                .Select(x => new ResponseProductWithSaleDto // 選擇輸出欄位
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
            return new ResponseProductsDto()
            {
                PageCount = pageCount,
                Products = result
            };
        }

        /// <summary>
        /// 取得全站所有產品
        /// </summary>
        /// <returns></returns>

        [HttpGet]
        [Route("~/api/Product/GetAllProducts")]
        [ResponseType(typeof(ResponseProductsDto))]
        public IActionResult GetAllProducts(int page = 1, int pageLimitNumber = 50, ProductOrderField orderField = ProductOrderField.Default, bool isASC = true)
        {
            var product = db.ProductVersionControls
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(pvc => pvc.OrderDetails)
                .Where(x => x.Product.Enabled)
                .GroupBy(x => x.ProductId)
                .Select(x =>
                new ProdcutWithSale()
                {
                    Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
                })
                .ToList();

            var result = GenerateProductPage(product, page, pageLimitNumber, orderField, isASC);

            if (result.Products.Count == 0)
                return CatStatusCode.NotFound();

            return Ok(result);
        }

        /// <summary>
        /// 取得賣場內的產品
        /// </summary>
        [HttpGet]
        [Route("~/api/Product/GetStoreProducts")]
        [ResponseType(typeof(ResponseProductsDto))]
        public IActionResult GetStoreProducts(int storeId, int page = 1, int pageLimitNumber = 50, ProductOrderField orderField = ProductOrderField.Default, bool isASC = true)
        {
            var product = db.ProductVersionControls
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(pvc => pvc.OrderDetails)
                .Where(x => x.Product.Enabled && x.Product.StoreId == storeId)
                .GroupBy(x => x.ProductId)
                .Select(x =>
                new ProdcutWithSale()
                {
                    Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
                })
                .ToList();

            var result = GenerateProductPage(product, page, pageLimitNumber, orderField, isASC);

            if (result.Products.Count == 0)
                return CatStatusCode.NotFound();

            return Ok(result);
        }

        /// <summary>
        /// 取得用戶購物車內的產品
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("~/api/Product/GetCartProducts")]
        [ResponseType(typeof(List<ResponseCartProductDto>))]
        public IActionResult GetCartProducts()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var cart = db.CartProducts.Where(x => x.Uid == uid).ToList();
            if (cart.Count == 0)
                return CatStatusCode.NotFound();

            var productVersionControls = db.ProductVersionControls
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Where(x => x.Product.Enabled)
                .GroupBy(x => x.ProductId)
                .Select(g => g.OrderByDescending(x => x.Version).FirstOrDefault())
                .ToList();

            var result = cart.Select(c =>
            {
                var productVersionControl = productVersionControls.FirstOrDefault(pvc => pvc.ProductId == c.ProductId);
                return new ResponseCartProductDto
                {
                    ID = c.ProductId,
                    Name = productVersionControl?.Name,
                    Description = productVersionControl?.Description,
                    Price = productVersionControl.Price,
                    Quantity = c.Quantity,
                    Storage = productVersionControl.Product.Quantity,
                    StoreID = c.Product.StoreId,
                    Images = c.Product.ProductImages.Select(img => img.Url).ToList()
                };
            }).ToList();
            return Ok(result);
        }

        /// <summary>
        /// 搜尋產品
        /// </summary>
        [HttpGet]
        [Route("~/api/Product/SearchProducts")]
        [ResponseType(typeof(ResponseProductsDto))]
        public IActionResult SearchProducts(string keyword, int page = 1, int pageLimitNumber = 50, ProductOrderField orderField = ProductOrderField.Default, bool isASC = true)
        {
            keyword = keyword.ToLower();

            var productVersionControls = db.ProductVersionControls
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(pvc => pvc.OrderDetails)
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductKeywords)
                .Where(x => x.Product.Enabled)
                .ToList(); // Switch to client-side evaluation


            var product = productVersionControls
                .GroupBy(x => x.ProductId)
                .Select(x =>
                new ProdcutWithSale()
                {
                    Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
                })
                .Where(x => x.LastestPVC.Name.ToLower().Contains(keyword) || x.LastestPVC.Description.ToLower().Contains(keyword) || x.LastestPVC.Product.ProductKeywords.Any(y => y.Keyword.ToLower().Contains(keyword)))
                .ToList();

            var result = GenerateProductPage(product, page, pageLimitNumber, orderField, isASC);

            if (result.Products.Count == 0)
                return CatStatusCode.NotFound();

            return Ok(result);
        }

        /// <summary>
        /// 取得熱門產品
        /// </summary>
        [HttpGet]
        [Route("~/api/Product/GetTopProducts")]
        [ResponseType(typeof(ResponseProductsDto))]
        public IActionResult GetTopProducts(int count = 5)
        {
            var product = db.ProductVersionControls
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(pvc => pvc.OrderDetails)
                .GroupBy(x => x.ProductId)
                .Select(x => new ProdcutWithSale()
                {
                    Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
                })
            .ToList();

            var result = GenerateProductPage(product, 1, count, ProductOrderField.Sale, false);
            return Ok(result.Products);
        }

        /// <summary>
        /// 使用 Product id 取得單一產品
        /// </summary>
        [HttpGet]
        [Route("~/api/Product/GetProduct")]
        [ResponseType(typeof(ResponseProductWithSaleDto))]
        public IActionResult GetProduct(int id)
        {
            // 減掉用戶放進購物車的
            int minusCount = 0;
            if (!StringValues.IsNullOrEmpty(Request.Headers.Authorization))
            {
                var userToken = _JwtAuthUtil.GetToken(Request.Headers.Authorization.ToString());
                var uid = int.Parse(userToken[JwtRegisteredClaimNames.Sub].ToString());
                var cartProduct = db.CartProducts
                    .FirstOrDefault(x => x.Uid == uid && x.ProductId == id);
                if (cartProduct != null)
                    minusCount = cartProduct.Quantity;
            }

            var PVC = db.ProductVersionControls
                    .Where(x => x.Product.Enabled && x.ProductId == id);

            if (PVC.Count() == 0)
                return CatStatusCode.NotFound();

            var lastestPCV = PVC
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            var sale = PVC.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0);

            var response = new ResponseProductWithSaleDto
            {
                ID = lastestPCV.ProductId,
                Name = lastestPCV?.Name,
                Description = lastestPCV?.Description,
                Price = lastestPCV.Price,
                Quantity = lastestPCV.Product.Quantity - minusCount,
                Sale = sale,
                StoreID = lastestPCV.Product.StoreId,
                Images = lastestPCV.Product.ProductImages.Select(img => img.Url).ToList()
            };
            return Ok(response);
        }


        /// <summary>
        /// 使用 Product version control id 取得某一版本產品
        /// </summary>
        [HttpGet]
        [Route("~/api/Product/GetProdcutByPVCID")]
        [ResponseType(typeof(ResponseProductDto))]
        public IActionResult GetProdcutByPVCID(int PVCID)
        {
            var productVersionControl = db.ProductVersionControls
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Where(x => x.Product.Enabled)
                .Where(x => x.Pvcid == PVCID)
                .FirstOrDefault();

            if (productVersionControl == null)
                return CatStatusCode.NotFound();

            var result = new ResponseProductDto
            {
                ID = productVersionControl.ProductId,
                Name = productVersionControl.Name,
                Description = productVersionControl.Description,
                Price = productVersionControl.Price,
                Quantity = productVersionControl.Product.Quantity,
                StoreID = productVersionControl.Product.StoreId,
                Images = productVersionControl.Product.ProductImages.Select(img => img.Url).ToList()
            };

            return Ok(result);
        }

        /// <summary>
        /// 賣家上傳商品圖片 (請使用 form-data)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Seller")]
        [Route("api/Product/UploadProductImages")]
        public async Task<IActionResult> UploadProductImages(int productID, [FromForm] UploadProductImageDto dto)
        {
            try
            {
                // Read the multipart data and save the files
                int firstID = db.ProductImages.Any() ? db.ProductImages.Max(x => x.Id) + 1 : 1;
                foreach (var file in dto.files)
                {
                    var link = await UploadImgeToImgur(file);
                    db.ProductImages.Add(new ProductImage()
                    {
                        Id = firstID++,
                        ProductId = productID,
                        Url = link
                    });
                }
                db.SaveChanges();
                return CatStatusCode.Ok();
            }
            catch (Exception ex)
            {
                return CatStatusCode.BadRequest();
            }
        }

        /// <summary>
        /// 賣家建立商品
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Seller")]
        [Route("api/Product/CreateProduct")]
        [ResponseType(typeof(ResponseCreateProductDto))]
        public IActionResult CreateProduct(RequestCreateProductDto dto)
        {
            if (dto.Price < 0 || dto.Quantity < 0)
                return CatStatusCode.BadRequest();
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            int storeID = db.Stores.FirstOrDefault(x => x.OwnerId == uid).StoreId;
            var product = new Product()
            {
                ProductId = db.Products.Any() ? db.Products.Max(x => x.ProductId) + 1 : 1,
                StoreId = storeID,
                CreatedAt = DateTime.UtcNow,
                Enabled = true,
                Quantity = dto.Quantity
            };
            // product
            db.Products.Add(product);
            // PVC
            var pvc = new ProductVersionControl()
            {
                Pvcid = db.ProductVersionControls.Any() ? db.ProductVersionControls.Max(x => x.Pvcid) + 1 : 1,
                ProductId = product.ProductId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Version = 1
            };
            db.ProductVersionControls.Add(pvc);

            int keywordIndex = db.ProductKeywords.Any() ? db.ProductKeywords.Max(y => y.ProduckKeywordId) + 1 : 1;
            // keywords
            dto.Keywords.ForEach(x =>
            {
                db.ProductKeywords.Add(new ProductKeyword()
                {
                    ProduckKeywordId = keywordIndex++,
                    ProductId = product.ProductId,
                    Keyword = x
                });
            });
            db.SaveChanges();
            return Ok(new ResponseCreateProductDto()
            {
                ProductID = product.ProductId
            });
        }

        private async Task<string> UploadImgeToImgur(IFormFile file)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.imgur.com/3/image");
            request.Headers.Add("Authorization", "Bearer 9c028fd5d56810d5d476b712734db37ef3e520fd");
            var content = new MultipartFormDataContent();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                var byteContent = new ByteArrayContent(stream.ToArray());
                byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                content.Add(byteContent, "image", file.FileName);
            }
            content.Add(new StringContent("PoNUlEH"), "album");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var imgurResponse = JsonConvert.DeserializeObject<ImgurResponse>(responseString);
            return imgurResponse?.Data?.Link ?? throw new InvalidOperationException("Imgur upload failed");
        }

        /// <summary>
        /// 新增商品到購物車
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("~/api/Product/AddToCart")]
        public IActionResult AddToCart([FromBody] RequestAddToCartDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            // 已經存在的產品
            var cart = db.CartProducts
                .Include(c => c.Product)
                .Where(x => x.Uid == uid && x.ProductId == dto.ProductID)
                .FirstOrDefault();
            if (cart != null)
            {
                cart.Quantity += dto.Quantity;
                if (cart.Quantity > cart.Product.Quantity)
                    return CatStatusCode.BadRequest();
            }
            else
            {
                var product = db.Products.Where(x => x.ProductId == dto.ProductID).FirstOrDefault();
                if (dto.Quantity > product.Quantity)
                    return CatStatusCode.BadRequest();

                // 新加入的產品
                var cartProduct = new CartProduct()
                {
                    CartId = db.Messages.Any() ? db.CartProducts.Max(x => x.CartId) + 1 : 1,
                    Uid = uid,
                    ProductId = dto.ProductID,
                    Quantity = dto.Quantity
                };
                db.CartProducts.Add(cartProduct);
            }

            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 修改購物車內的商品數量
        /// </summary>
        [HttpPut]
        [Authorize]
        [Route("~/api/Product/ModifyProductQuantityInCart")]
        public IActionResult ModifyProductQuantityInCart([FromBody] RequestModifyCartProductQuantityDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var cartProduct = db.CartProducts
                .Include(cp => cp.Product)
                .FirstOrDefault(x => x.Uid == uid && x.ProductId == dto.ProductID);
            if (cartProduct == null)
                return CatStatusCode.BadRequest();

            if (dto.Quantity == 0)
                db.CartProducts.Remove(cartProduct);
            else
                cartProduct.Quantity = dto.Quantity;
            if (cartProduct.Quantity > cartProduct.Product.Quantity)
                return CatStatusCode.BadRequest();

            db.SaveChanges();
            return CatStatusCode.Ok();
        }

                /// <summary>
        /// 賣家修改產品資訊
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Seller")]
        [Route("api/Product/ModifyProduct")]
        public IActionResult ModifyProduct(RequestModifyProductDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            // 沒有這個商品
            if (!db.Products.Any(x => x.ProductId == dto.ProductID))
                return CatStatusCode.BadRequest();
            var product = db.Products.Include(p => p.Store).FirstOrDefault(p => p.ProductId == dto.ProductID);
            // 商品不屬於這個賣家
            if (product.Store.OwnerId != uid)
                return CatStatusCode.BadRequest();
            product.Quantity = dto.Quantity;
            product.Enabled = dto.Enabled;
            var latestPVC = product.ProductVersionControls?.OrderByDescending(x => x.Version).FirstOrDefault();
            if (latestPVC == null || (latestPVC.Name != dto.Name || latestPVC.Description != dto.Description || latestPVC.Price != dto.Price))
            {
                var pvc = new ProductVersionControl()
                {
                    Pvcid = db.ProductVersionControls.Any() ? db.ProductVersionControls.Max(x => x.Pvcid) + 1 : 1,
                    ProductId = product.ProductId,
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    Version = latestPVC == null ? 1 : latestPVC.Version + 1
                };
                db.ProductVersionControls.Add(pvc);
            }
            db.SaveChanges();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 從購物車移除商品
        /// </summary>
        [HttpDelete]
        [Authorize]
        [Route("~/api/Product/RemoveProductFromCart")]
        public IActionResult RemoveProductFromCart([FromBody] RequestRemoveProductFromCart dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);


            // 檢查是否有這些商品
            foreach (int id in dto.IDs)
            {
                var cartProduct = db.CartProducts.FirstOrDefault(x => x.Uid == uid && x.ProductId == id);
                if (cartProduct == null)
                    return CatStatusCode.BadRequest();
            }

            // 移除購物商品
            foreach (int id in dto.IDs)
            {
                var cartProduct = db.CartProducts.FirstOrDefault(x => x.Uid == uid && x.ProductId == id);
                db.CartProducts.Remove(cartProduct);
            }

            db.SaveChanges();

            return CatStatusCode.Ok();
        }

        public enum ProductOrderField
        {
            Default,
            Price,
            Sale,
            Quantity
        }
        public class UploadProductImageDto
        {
            public List<IFormFile> files { get; set; }
        }

        public class RequestModifyProductDto
        {
            public int ProductID { get; set; }
            public bool Enabled { get; set; }
            public int Quantity { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
        }


        public class ResponseCreateProductDto
        {
            public int ProductID { get; set; }
        }
        public class RequestCreateProductDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public List<string> Keywords { get; set; }
        }

        public class ProdcutWithSale
        {
            public int Sale { get; set; }
            public ProductVersionControl LastestPVC { get; set; }
        }

        public class RequestRemoveProductFromCart
        {
            public List<int> IDs { get; set; }
        }
        public class ResponseProductsDto
        {
            public int PageCount { get; set; }
            public List<ResponseProductWithSaleDto> Products { get; set; }
        }

        public class RequestModifyCartProductQuantityDto
        {
            public int ProductID { get; set; }
            public int Quantity { get; set; }
        }

        public class ResponseProductDto
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public int StoreID { get; set; }
            public List<string> Images { get; set; }
        }

        public class ResponseProductWithSaleDto : ResponseProductDto
        {
            public int Sale { get; set; }
        }

        public class ResponseCartProductDto : ResponseProductDto
        {
            public int Storage { get; set; }
        }

        public class RequestAddToCartDto
        {
            public int ProductID { get; set; }
            public int Quantity { get; set; }
        }

        // Model for Imgur response
        public class ImgurResponse
        {
            public ImgurData Data { get; set; }
        }
        public class ImgurData
        {
            public string Link { get; set; }
        }

    }
}