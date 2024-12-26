using OOTDV1Entities = OOTD_API.Models.Ootdv1Context;
using OOTD_API.StatusCode;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace OOTD_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KeywordController : ControllerBase
    {
        private readonly OOTDV1Entities db;

        public KeywordController(OOTDV1Entities db)
        {
            this.db = db;
        }

        /// <summary>
        /// 取得熱門關鍵字
        /// </summary>
        [HttpGet]
        [Route("~/api/Keyword/GetTopKeyword")]
        [ResponseType(typeof(List<string>))]
        public IActionResult GetTopKeyword(int count = 5)
        {
            var result = db.ProductKeywords
                .Select(x => new
                {
                    Keyword = x.Keyword,
                    Count = x.Product.ProductVersionControls.Sum(y => y.OrderDetails.Count)
                })
                .GroupBy(x => x.Keyword)
                .Select(x => new
                {
                    Keyword = x.Key,
                    Count = x.Sum(y => y.Count)
                })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .Select(x => x.Keyword)
                .ToList();
            if (result.Count == 0)
                return CatStatusCode.NotFound();
            return Ok(result);
        }
    }
}