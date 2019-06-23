using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Data.SqlClient;
using System.Net;
using System.Collections.Generic;
using MyProject.Helpers;
using WebApi.Entities;

namespace SwapAPI.Branch
{
    public static class GetBranchEmployees
    {
        [FunctionName("GetBranchEmployees")]
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


            //We retrieve the userName field, which comes as a parameter to the function, by deserializing req.Content.
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string branchID = data.branchID;

            //If there is no username, we return the error message.
            try
            {
                User[] allUsers = null;
                //We get the Connection String in the Function App Settings section we defined.
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {
                    string text = @"SELECT UserID, email, Firstname, Lastname, Position FROM Users " +
                        "WHERE branchID = @branchID AND Role NOT IN ('Owner', 'Manager')";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@branchID", branchID);
                    connection.Open();

                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        log.LogInformation("C# HTTP trigger function processed a request. 6");
                        var list = new List<User>();
                        while (oReader.Read())
                        {
                            list.Add(new User
                            {
                                UserID = oReader["UserID"].ToString(),
                                email = oReader["email"].ToString(),
                                Firstname = oReader["Firstname"].ToString(),
                                Lastname = oReader["Lastname"].ToString(),
                                Position = oReader["Position"].ToString()
                            });
                            allUsers = list.ToArray();
                        }

                        connection.Close();
                    }

                }
                return req.CreateResponse(HttpStatusCode.OK, allUsers);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class User
        {
            public string UserID { get; set; }
            public string email { get; set; }
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Position { get; set; }
        }
    }
}
