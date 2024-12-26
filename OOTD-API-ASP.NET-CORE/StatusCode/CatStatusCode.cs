using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace OOTD_API.StatusCode
{
    public class CatStatusCode
    {
        public static IActionResult NotFound()
        {
            var statusMessage = new StatusMessage
            {
                Message = "Not found meow meow.",
                StatusCode = 404,
                CatUrl = "https://http.cat/404"
            };

            return new ObjectResult(statusMessage)
            {
                StatusCode = statusMessage.StatusCode
            };
        }

        public static IActionResult BadRequest()
        {
            var statusMessage = new StatusMessage
            {
                Message = "Bad request meow meow.",
                StatusCode = 400,
                CatUrl = "https://http.cat/400"
            };

            return new ObjectResult(statusMessage)
            {
                StatusCode = statusMessage.StatusCode
            };
        }

        public static IActionResult Ok()
        {
            var statusMessage = new StatusMessage
            {
                Message = "Ok meow meow.",
                StatusCode = 200,
                CatUrl = "https://http.cat/200"
            };

            return new ObjectResult(statusMessage)
            {
                StatusCode = statusMessage.StatusCode
            };
        }

        public static IActionResult Forbidden()
        {
            var statusMessage = new StatusMessage
            {
                Message = "Forbidden meow meow.",
                StatusCode = 403,
                CatUrl = "https://http.cat/403"
            };

            return new ObjectResult(statusMessage)
            {
                StatusCode = statusMessage.StatusCode
            };
        }

        public static IActionResult Unauthorized()
        {
            var statusMessage = new StatusMessage
            {
                Message = "Unauthorized meow meow.",
                StatusCode = 401,
                CatUrl = "https://http.cat/401"
            };

            return new ObjectResult(statusMessage)
            {
                StatusCode = statusMessage.StatusCode
            };
        }

        public static IActionResult Conflict()
        {
            var statusMessage = new StatusMessage
            {
                Message = "Conflict meow meow.",
                StatusCode = 409,
                CatUrl = "https://http.cat/409"
            };

            return new ObjectResult(statusMessage)
            {
                StatusCode = statusMessage.StatusCode
            };
        }

        private class StatusMessage
        {
            public required string Message { get; set; }
            public int StatusCode { get; set; }
            public required string CatUrl { get; set; }
        }
    }
}