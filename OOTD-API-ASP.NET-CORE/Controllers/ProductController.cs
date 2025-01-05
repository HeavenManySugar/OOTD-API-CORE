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

        readonly private Dictionary<ProductOrderField, Func<ProdcutWithSale, object>> keySelector = new Dictionary<ProductOrderField, Func<ProdcutWithSale, object>>
        {
            { ProductOrderField.Price, x => x.LastestPVC.Price },
            { ProductOrderField.Quantity, x => x.LastestPVC.Product.Quantity },
            { ProductOrderField.Sale , x=>x.Sale },
            { ProductOrderField.Default , x=>x.LastestPVC.ProductId }
        };


        private async Task<ResponseProductsDto> GenerateProductPageAsync(List<ProdcutWithSale> products, int page, int pageLimitNumber, ProductOrderField orderField = ProductOrderField.Default, bool isASC = true)
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

            return await Task.FromResult(new ResponseProductsDto()
            {
                PageCount = pageCount,
                Products = result
            });
        }

        [HttpGet]
        [Route("~/api/Product/GetAllProducts")]
        [ResponseType(typeof(ResponseProductsDto))]
        public async Task<IActionResult> GetAllProductsAsync(int page = 1, int pageLimitNumber = 50, ProductOrderField orderField = ProductOrderField.Default, bool isASC = true)
        {
            //var product = await db.ProductVersionControls
            //    .Include(pvc => pvc.Product)
            //    .ThenInclude(p => p.ProductImages)
            //    .Include(pvc => pvc.OrderDetails)
            //    .Include(pvc => pvc.Product.Store)
            //    .Where(x => x.Product.Enabled && x.Product.Store.Enabled)
            //    .GroupBy(x => x.ProductId)
            //    .Select(x =>
            //    new ProdcutWithSale()
            //    {
            //        Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
            //        LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
            //    })
            //    .ToListAsync();

            var product = await db.Products
                .Include(p => p.ProductVersionControls)
                .ThenInclude(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(p => p.ProductVersionControls)
                .ThenInclude(pvc => pvc.OrderDetails)
                .Include(p => p.Store)
                .AsNoTracking()
                .Where(x => x.Enabled && x.Store.Enabled && x.ProductVersionControls.Any())
                .Select(x =>
                new ProdcutWithSale()
                {
                    Sale = x.ProductVersionControls.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.ProductVersionControls.OrderByDescending(y => y.Version).FirstOrDefault()
                }).ToListAsync();

            var result = await GenerateProductPageAsync(product, page, pageLimitNumber, orderField, isASC);

            if (result.Products.Count == 0)
                return CatStatusCode.NotFound();

            return Ok(result);
        }

        [HttpGet]
        [Route("~/api/Product/GetStoreProducts")]
        [ResponseType(typeof(ResponseProductsDto))]
        public async Task<IActionResult> GetStoreProductsAsync(int storeId, int page = 1, int pageLimitNumber = 50, ProductOrderField orderField = ProductOrderField.Default, bool isASC = true)
        {
            //var product = await db.ProductVersionControls
            //    .Include(pvc => pvc.Product)
            //    .ThenInclude(p => p.ProductImages)
            //    .Include(pvc => pvc.OrderDetails)
            //    .Include(pvc => pvc.Product.Store)
            //    .Where(x => x.Product.Enabled && x.Product.Store.Enabled && x.Product.StoreId == storeId)
            //    .GroupBy(x => x.ProductId)
            //    .Select(x =>
            //    new ProdcutWithSale()
            //    {
            //        Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
            //        LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
            //    })
            //    .ToListAsync();

            var product = await db.Products
                .Include(p => p.ProductVersionControls)
                .ThenInclude(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(p => p.ProductVersionControls)
                .ThenInclude(pvc => pvc.OrderDetails)
                .Include(p => p.Store)
                .AsNoTracking()
                .Where(x => x.Enabled && x.Store.Enabled && x.StoreId == storeId && x.ProductVersionControls.Any())
                .Select(x =>
                new ProdcutWithSale()
                {
                    Sale = x.ProductVersionControls.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.ProductVersionControls.OrderByDescending(y => y.Version).FirstOrDefault()
                }).ToListAsync();

            var result = await GenerateProductPageAsync(product, page, pageLimitNumber, orderField, isASC);

            if (result.Products.Count == 0)
                return CatStatusCode.NotFound();

            return Ok(result);
        }

        [HttpGet]
        [Authorize]
        [Route("~/api/Product/GetCartProducts")]
        [ResponseType(typeof(List<ResponseCartProductDto>))]
        public async Task<IActionResult> GetCartProductsAsync()
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            var cartProduct = await db.CartProducts.Include(cp => cp.Product).ThenInclude(p => p.Store).Where(x => x.Uid == uid).ToListAsync();
            foreach (var item in cartProduct)
            {
                if (!item.Product.Enabled || !item.Product.Store.Enabled)
                    cartProduct.Remove(item);
            }
            await db.SaveChangesAsync();
            cartProduct = await db.CartProducts.Where(x => x.Uid == uid).ToListAsync();
            if (cartProduct.Count == 0)
                return CatStatusCode.NotFound();

            var productVersionControls = await db.ProductVersionControls
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(pvc => pvc.Product.Store)
                .AsNoTracking()
                .Where(x => x.Product.Enabled && x.Product.Store.Enabled)
                .GroupBy(x => x.ProductId)
                .Select(g => g.OrderByDescending(x => x.Version).FirstOrDefault())
                .ToListAsync();

            var result = cartProduct.Select(c =>
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
        public async Task<IActionResult> SearchProductsAsync(string keyword, int page = 1, int pageLimitNumber = 50, ProductOrderField orderField = ProductOrderField.Default, bool isASC = true)
        {
            keyword = keyword.ToLower();

            //var productVersionControls = await db.ProductVersionControls
            //    .Include(pvc => pvc.Product)
            //    .ThenInclude(p => p.ProductImages)
            //    .Include(pvc => pvc.OrderDetails)
            //    .Include(pvc => pvc.Product)
            //    .ThenInclude(p => p.ProductKeywords)
            //    .Include(pvc => pvc.Product.Store)
            //    .Where(x => x.Product.Enabled && x.Product.Store.Enabled)
            //    .ToListAsync(); // Switch to client-side evaluation


            //var product = productVersionControls
            //    .GroupBy(x => x.ProductId)
            //    .Select(x =>
            //    new ProdcutWithSale()
            //    {
            //        Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
            //        LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
            //    })
            //    .Where(x => x.LastestPVC.Name.ToLower().Contains(keyword) || x.LastestPVC.Description.ToLower().Contains(keyword) || x.LastestPVC.Product.ProductKeywords.Any(y => y.Keyword.ToLower().Contains(keyword)))
            //    .ToList();

            var product = await db.Products
              .Include(p => p.ProductVersionControls)
              .ThenInclude(pvc => pvc.Product)
              .ThenInclude(p => p.ProductImages)
              .Include(p => p.ProductVersionControls)
              .ThenInclude(pvc => pvc.OrderDetails)
              .Include(p => p.Store)
              .Include(p => p.ProductVersionControls)
              .ThenInclude(pvc => pvc.Product.ProductKeywords)
              .Where(x => x.Enabled && x.Store.Enabled && x.ProductVersionControls.Any())
              .AsNoTracking()
              .Select(x =>
                new ProdcutWithSale()
                {
                    Sale = x.ProductVersionControls.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.ProductVersionControls.OrderByDescending(y => y.Version).FirstOrDefault()
                })
                .Where(x => x.LastestPVC.Name.ToLower().Contains(keyword) || x.LastestPVC.Description.ToLower().Contains(keyword) || x.LastestPVC.Product.ProductKeywords.Any(y => y.Keyword.ToLower().Contains(keyword)))
                .ToListAsync();

            var result = await GenerateProductPageAsync(product, page, pageLimitNumber, orderField, isASC);

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
        public async Task<IActionResult> GetTopProductsAsync(int count = 5)
        {
            //var product = await db.ProductVersionControls
            //    .Include(pvc => pvc.Product)
            //    .ThenInclude(p => p.ProductImages)
            //    .Include(pvc => pvc.OrderDetails)
            //    .GroupBy(x => x.ProductId)
            //    .Select(x => new ProdcutWithSale()
            //    {
            //        Sale = x.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
            //        LastestPVC = x.OrderByDescending(y => y.Version).FirstOrDefault()
            //    })
            //    .ToListAsync();

            var product = await db.Products
              .Include(p => p.ProductVersionControls)
              .ThenInclude(pvc => pvc.Product)
              .ThenInclude(p => p.ProductImages)
              .Include(p => p.ProductVersionControls)
              .ThenInclude(pvc => pvc.OrderDetails)
              .Include(p => p.Store)
              .Where(x => x.Enabled && x.Store.Enabled && x.ProductVersionControls.Any())
              .AsNoTracking()
              .Select(x =>
                new ProdcutWithSale()
                {
                    Sale = x.ProductVersionControls.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0),
                    LastestPVC = x.ProductVersionControls.OrderByDescending(y => y.Version).FirstOrDefault()
                }).ToListAsync();

        var result = await GenerateProductPageAsync(product, 1, count, ProductOrderField.Sale, false);
            if (result.Products.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result.Products);
        }
        /// <summary>
        /// 使用 Product id 取得單一產品
        /// </summary>
        [HttpGet]
        [Route("~/api/Product/GetProduct")]
        [ResponseType(typeof(ResponseProductWithSaleDto))]
        public async Task<IActionResult> GetProductAsync(int id)
        {
            // 減掉用戶放進購物車的
            int quantityInCart = 0;
            if (!StringValues.IsNullOrEmpty(Request.Headers.Authorization))
            {
                var userToken = _JwtAuthUtil.GetToken(Request.Headers.Authorization.ToString());
                var uid = int.Parse(userToken[JwtRegisteredClaimNames.Sub].ToString());
                var cartProduct = await db.CartProducts
                    .FirstOrDefaultAsync(x => x.Uid == uid && x.ProductId == id);
                if (cartProduct != null)
                    quantityInCart = cartProduct.Quantity;
            }

            //var PVC = db.ProductVersionControls
            //        .Include(pvc => pvc.Product)
            //        .ThenInclude(p => p.ProductImages)
            //        .Where(x => x.Product.Enabled && x.Product.Enabled && x.ProductId == id);

            var product = await db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVersionControls)
                .ThenInclude(pvc => pvc.OrderDetails)
                .Include(p => p.Store)
                .Where(x => x.Enabled && x.Store.Enabled && x.ProductVersionControls.Any())
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == id);

            if (product == null)
                return CatStatusCode.NotFound();

            var lastestPCV = product.ProductVersionControls
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            var sale = product.ProductVersionControls.Sum(y => y.OrderDetails.Any() ? y.OrderDetails.Sum(z => z.Quantity) : 0);

            var response = new ResponseProductWithSaleDto
            {
                ID = lastestPCV.ProductId,
                Name = lastestPCV?.Name,
                Description = lastestPCV?.Description,
                Price = lastestPCV.Price,
                Quantity = lastestPCV.Product.Quantity - quantityInCart,
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
        public async Task<IActionResult> GetProdcutByPVCIDAsync(int PVCID)
        {
            var productVersionControl = await db.ProductVersionControls
                .Include(pvc => pvc.Product)
                .ThenInclude(p => p.ProductImages)
                .Include(pvc => pvc.Product.Store)
                .AsNoTracking()
                .Where(x => x.Product.Enabled && x.Product.Store.Enabled)
                .Where(x => x.Pvcid == PVCID)
                .FirstOrDefaultAsync();

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
        [Route("~/api/Product/UploadProductImages")]
        public async Task<IActionResult> UploadProductImages(int productID, [FromForm] UploadProductImageDto dto)
        {
            // 產品不是該賣家的
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            if (!await db.Products.AsNoTracking().AnyAsync(x => x.ProductId == productID && x.Store.OwnerId == uid))
                return CatStatusCode.BadRequest();

            try
            {
                // Read the multipart data and save the files
                int firstID = await db.ProductImages.AsNoTracking().AnyAsync() ? await db.ProductImages.AsNoTracking().MaxAsync(x => x.Id) + 1 : 1;
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
        [Route("~/api/Product/CreateProduct")]
        [ResponseType(typeof(ResponseCreateProductDto))]
        public async Task<IActionResult> CreateProductAsync(RequestCreateProductDto dto)
        {
            if (dto.Price < 0 || dto.Quantity < 0)
                return CatStatusCode.BadRequest();
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            int storeID = await db.Stores.Where(x => x.OwnerId == uid).Select(x => x.StoreId).FirstOrDefaultAsync();
            var product = new Product()
            {
                ProductId = await db.Products.AsNoTracking().AnyAsync() ? await db.Products.AsNoTracking().MaxAsync(x => x.ProductId) + 1 : 1,
                StoreId = storeID,  
                CreatedAt = DateTime.UtcNow,
                Enabled = true,
                Quantity = dto.Quantity
            };
            // product
            await db.Products.AddAsync(product);
            // PVC
            var pvc = new ProductVersionControl()
            {
                Pvcid = await db.ProductVersionControls.AsNoTracking().AnyAsync() ? await db.ProductVersionControls.AsNoTracking().MaxAsync(x => x.Pvcid) + 1 : 1,
                ProductId = product.ProductId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Version = 1
            };
            await db.ProductVersionControls.AddAsync(pvc);

            int keywordIndex = await db.ProductKeywords.AsNoTracking().AnyAsync() ? await db.ProductKeywords.AsNoTracking().MaxAsync(y => y.ProduckKeywordId) + 1 : 1;
            // keywords
            dto.Keywords.ForEach(async x =>
            {
                await db.ProductKeywords.AddAsync(new ProductKeyword()
                {
                    ProduckKeywordId = keywordIndex++,
                    ProductId = product.ProductId,
                    Keyword = x
                });
            });
            await db.SaveChangesAsync();
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
        public async Task<IActionResult> AddToCart([FromBody] RequestAddToCartDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            // 沒有這個商品
            if (!await db.Products.Include(p => p.Store).Where(x => x.Enabled && x.Store.Enabled).AnyAsync(x => x.ProductId == dto.ProductID))
                return CatStatusCode.BadRequest();

            // 已經存在的產品
            var cart = await db.CartProducts
                .Include(c => c.Product)
                .Where(x => x.Uid == uid && x.ProductId == dto.ProductID)
                .FirstOrDefaultAsync();
            if (cart != null)
            {
                cart.Quantity += dto.Quantity;
                if (cart.Quantity > cart.Product.Quantity)
                    return CatStatusCode.BadRequest();
            }
            else
            {
                var product = await db.Products.Where(x => x.ProductId == dto.ProductID).FirstOrDefaultAsync();
                if (dto.Quantity > product.Quantity)
                    return CatStatusCode.BadRequest();

                // 新加入的產品
                var cartProduct = new CartProduct()
                {
                    CartId = await db.Messages.AnyAsync() ? await db.CartProducts.MaxAsync(x => x.CartId) + 1 : 1,
                    Uid = uid,
                    ProductId = dto.ProductID,
                    Quantity = dto.Quantity
                };
                await db.CartProducts.AddAsync(cartProduct);
            }

            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 修改購物車內的商品數量
        /// </summary>
        [HttpPut]
        [Authorize]
        [Route("~/api/Product/ModifyProductQuantityInCart")]
        public async Task<IActionResult> ModifyProductQuantityInCart([FromBody] RequestModifyCartProductQuantityDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            // 沒有這個商品
            if (!await db.Products.AnyAsync(x => x.ProductId == dto.ProductID))
                return CatStatusCode.BadRequest();

            var cartProduct = await db.CartProducts
                .Include(cp => cp.Product)
                .FirstOrDefaultAsync(x => x.Uid == uid && x.ProductId == dto.ProductID);
            if (cartProduct == null)
                return CatStatusCode.BadRequest();

            if (dto.Quantity == 0)
                db.CartProducts.Remove(cartProduct);
            else
                cartProduct.Quantity = dto.Quantity;
            if (cartProduct.Quantity > cartProduct.Product.Quantity)
                return CatStatusCode.BadRequest();

            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 賣家修改產品資訊
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Product/ModifyProduct")]
        public async Task<IActionResult> ModifyProduct(RequestModifyProductDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            // 沒有這個商品
            if (!await db.Products.AnyAsync(x => x.ProductId == dto.ProductID))
                return CatStatusCode.BadRequest();
            var product = await db.Products
                            .Include(p => p.Store)
                            .Include(p => p.ProductVersionControls)
                            .FirstOrDefaultAsync(p => p.ProductId == dto.ProductID);
            // 商品不屬於這個賣家
            if (product.Store.OwnerId != uid)
                return CatStatusCode.BadRequest();
            product.Quantity = dto.Quantity;
            product.Enabled = dto.Enabled;
            var latestPVC = product.ProductVersionControls.OrderByDescending(x => x.Version).FirstOrDefault();
            if (latestPVC == null || (latestPVC.Name != dto.Name || latestPVC.Description != dto.Description || latestPVC.Price != dto.Price))
            {
                var pvc = new ProductVersionControl()
                {
                    Pvcid = await db.ProductVersionControls.AsNoTracking().AnyAsync() ? await db.ProductVersionControls.AsNoTracking().MaxAsync(x => x.Pvcid) + 1 : 1,
                    ProductId = product.ProductId,
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    Version = latestPVC == null ? 1 : latestPVC.Version + 1
                };
                await db.ProductVersionControls.AddAsync(pvc);
            }
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        /// <summary>
        /// 從購物車移除商品
        /// </summary>
        [HttpDelete]
        [Authorize]
        [Route("~/api/Product/RemoveProductFromCart")]
        public async Task<IActionResult> RemoveProductFromCart([FromBody] RequestRemoveProductFromCart dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

            // 檢查是否有這些商品
            foreach (int id in dto.IDs)
            {
                var cartProduct = await db.CartProducts.FirstOrDefaultAsync(x => x.Uid == uid && x.ProductId == id);
                if (cartProduct == null)
                    return CatStatusCode.BadRequest();
            }

            // 移除購物商品
            foreach (int id in dto.IDs)
            {
                var cartProduct = await db.CartProducts.FirstOrDefaultAsync(x => x.Uid == uid && x.ProductId == id);
                db.CartProducts.Remove(cartProduct);
            }

            await db.SaveChangesAsync();

            return CatStatusCode.Ok();
        }
        /// <summary>
        /// 賣家移除商品照片
        /// </summary>
        [HttpDelete]
        [Authorize(Roles = "Seller")]
        [Route("~/api/Product/RemoveProductImages")]
        public async Task<IActionResult> RemoveProductImagesAsync(RequestRemoveProductImagesDto dto)
        {
            var uid = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            // 產品不是該賣家的
            if (!await db.Products.AnyAsync(x => x.ProductId == dto.ProductID && x.Store.OwnerId == uid))
                return CatStatusCode.BadRequest();
            foreach (string url in dto.Urls)
            {
                var image = await db.ProductImages.FirstOrDefaultAsync(x => x.ProductId == dto.ProductID && x.Url == url);
                if (image == null)
                    return CatStatusCode.BadRequest();
                db.ProductImages.Remove(image);
            }
            await db.SaveChangesAsync();
            return CatStatusCode.Ok();
        }

        public enum ProductOrderField
        {
            Default,
            Price,
            Sale,
            Quantity
        }

        public class RequestRemoveProductImagesDto
        {
            public int ProductID { get; set; }
            public List<string> Urls { get; set; }
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