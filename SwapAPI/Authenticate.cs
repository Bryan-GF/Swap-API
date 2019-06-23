using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net.Http;
using System.Net;
using MyProject.Helpers;
using JWT.Builder;
using JWT.Algorithms;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// INSTEAD OF STORED PROCEDURE JUST USE SQL QUERY TO GET MATCHING QUERY TO EMPLOYEE ID.
/// ALSO INSTEAD OF USING SQL HASHING, USE .NET hashing and push that in as the password when creating a user and just use .NET hash compare.
/// </summary>
namespace SwapAPI
{
    public static class Authenticate
    {
        [FunctionName("Authenticate")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            
            log.LogInformation("C# HTTP trigger function processed a request.");
            User userInfo = new User();
            //We retrieve the userName field, which comes as a parameter to the function, by deserializing req.Content.
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string email = data.email;
            string Password = data.Password;

            //If there is no username, we return the error message
            try
            {
                //We get the Connection String in the Function App Settings section we defined.
                var str = Environment.GetEnvironmentVariable("sqldb_connection");
                string holdPass = null;
                using (SqlConnection connection = new SqlConnection(str))
                {
                    string text = @"SELECT U.*, B.roomId FROM Users U " +
                                    "INNER JOIN BranchTable B ON U.branchID = B.branchID " +
                                    "WHERE U.email = @email";
                    
                    SqlCommand command = new SqlCommand(text, connection);
                  
                    command.Parameters.AddWithValue("@email", email);
                    connection.Open();
 
                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            userInfo.UserID = oReader["UserID"].ToString();
                            userInfo.roles = oReader["Role"].ToString();
                            userInfo.email = oReader["email"].ToString();
                            userInfo.Firstname = oReader["Firstname"].ToString();
                            userInfo.Lastname = oReader["Lastname"].ToString();
                            userInfo.Position = oReader["Position"].ToString();
                            userInfo.branchID = oReader["branchID"].ToString();
                            userInfo.CompanyID = oReader["CompanyID"].ToString();
                            userInfo.roomId = oReader["roomId"].ToString();
                            holdPass = oReader["PasswordHash"].ToString();

                        }            
                        connection.Close();
                    }
                }

                if (holdPass != null && SecurePasswordHasherHelper.Verify(Password, holdPass)) {
                    log.LogInformation("MATCH");
                    var token = new JwtBuilder()
                      .WithAlgorithm(new HMACSHA256Algorithm())
                      .WithSecret(Environment.GetEnvironmentVariable("Secret"))
                      .AddClaim("exp", DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds())
                      .AddClaim("UserID", userInfo.UserID)
                      .AddClaim("email", userInfo.email)
                      .AddClaim("Firstname", userInfo.Firstname)
                      .AddClaim("Lastname", userInfo.Lastname)
                      .AddClaim("Position", userInfo.Position)
                      .AddClaim("branchID", userInfo.branchID)
                      .AddClaim("CompanyID", userInfo.CompanyID)
                      .AddClaim("roomId", userInfo.roomId)
                      .AddClaim("roles", userInfo.roles)
                      .Build();
                    userInfo.Token = token;
                    return req.CreateResponse(HttpStatusCode.OK, userInfo);
                } else
                {
                    log.LogInformation("NO MATCH");
                    return req.CreateResponse(HttpStatusCode.NotAcceptable, "Invalid Credentials");
                }
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class User
        {
            public string UserID { get; set; }
            public string roles { get; set; }
            public string email { get; set; }
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Position { get; set; }
            public string branchID { get; set; }
            public string CompanyID { get; set; }
            public string roomId { get; set; }
            public string Token { get; set; }

        }
    }
}
