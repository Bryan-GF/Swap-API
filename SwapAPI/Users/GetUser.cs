using System;
using System.Net;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using WebApi.Entities;
using MyProject.Helpers;

namespace SwapAPI
{
    public static class GetUser
    {
        [FunctionName("GetUser")]
        public static async Task<object> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Manager }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string UserID = data.UserID;

            User newUser = new User();

            try
            {
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))

                {
                    string text = @"SELECT email, Firstname, Lastname, Position FROM Users WHERE UserID = @UserID";
                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@UserID", UserID);
                    connection.Open();
                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            newUser.email = oReader["email"].ToString();
                            newUser.Firstname = oReader["Firstname"].ToString();
                            newUser.Lastname = oReader["Lastname"].ToString();
                            newUser.Position = oReader["Position"].ToString();
                        }

                        connection.Close();
                    }

                }
                newUser.UserID = UserID;
                return req.CreateResponse(HttpStatusCode.OK, newUser);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class User
        {
            public string email { get; set; }
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Position { get; set; }
            public string UserID { get; set; }
        }
    }
}