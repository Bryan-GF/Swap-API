using System;
using System.Net;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using JWT;
using JWT.Serializers;

namespace SwapAPI
{
    public static class VerifyToken
    {
        [FunctionName("VerifyToken")]
        public static async Task<object> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string ReqToken = data.token;
            log.LogInformation("C# HTTP trigger function processed a request.");
            string token = ReqToken;
            string secret = Environment.GetEnvironmentVariable("Secret");

            try
            {
                IJsonSerializer serializer = new JsonNetSerializer();
                IDateTimeProvider provider = new UtcDateTimeProvider();
                IJwtValidator validator = new JwtValidator(serializer, provider);
                IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
                IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

                var json = decoder.Decode(token, secret, verify: true);
                Console.WriteLine(json);
                return req.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (TokenExpiredException)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Token has expired.");
            }
            catch (SignatureVerificationException)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid Token!");
            }
        }
    }
}