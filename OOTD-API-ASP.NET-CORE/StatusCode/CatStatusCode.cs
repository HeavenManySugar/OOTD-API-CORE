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

            var response = new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(statusMessage), Encoding.UTF8, "application/json"),
                ReasonPhrase = "Not Found"
            };

            return new ObjectResult(response.Content.ReadAsStringAsync().Result)
            {
                StatusCode = (int)response.StatusCode
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

            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(statusMessage), Encoding.UTF8, "application/json"),
                ReasonPhrase = "Bad Request"
            };

            return new ObjectResult(response.Content.ReadAsStringAsync().Result)
            {
                StatusCode = (int)response.StatusCode
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

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(statusMessage), Encoding.UTF8, "application/json"),
                ReasonPhrase = "Ok"
            };

            return new ObjectResult(response.Content.ReadAsStringAsync().Result)
            {
                StatusCode = (int)response.StatusCode
            };
        }

        public static HttpResponseMessage ForbiddenResponse()
        {
            var statusMessage = new StatusMessage
            {
                Message = "Forbidden meow meow.",
                StatusCode = 403,
                CatUrl = "https://http.cat/403"
            };

            var response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(statusMessage), Encoding.UTF8, "application/json"),
                ReasonPhrase = "Forbidden"
            };
            return response;
        }

        public static IActionResult Forbidden()
        {
            var response = ForbiddenResponse();
            return new ObjectResult(response.Content.ReadAsStringAsync().Result)
            {
                StatusCode = (int)response.StatusCode
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

            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(statusMessage), Encoding.UTF8, "application/json"),
                ReasonPhrase = "Unauthorized"
            };

            return new ObjectResult(response.Content.ReadAsStringAsync().Result)
            {
                StatusCode = (int)response.StatusCode
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

            var response = new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(statusMessage), Encoding.UTF8, "application/json"),
                ReasonPhrase = "Conflict"
            };

            return new ObjectResult(response.Content.ReadAsStringAsync().Result)
            {
                StatusCode = (int)response.StatusCode
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