using OOTD_API.StatusCode;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace OOTD_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        /// <summary>
        /// API 狀態
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("~/api/Status")]
        [ResponseType(typeof(string))]
        public IActionResult Get()
        {
            return CatStatusCode.Ok();
        }
    }
}
