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
    public static class GetUserRequests
    {
        [FunctionName("GetUserRequests")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Employee }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            log.LogInformation("C# HTTP trigger function processed a request.");
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string UserID = data.UserID;

            try
            {
                Dictionary<string, RequestStatus> UserRequestList = new Dictionary<string, RequestStatus>();
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {
                    string text = @"SELECT R.ShiftID " +
                                    "FROM Requests R " +
                                    "WHERE R.UserID = @UserID";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@UserID", UserID);
                    connection.Open();

                    using (SqlDataReader oReader = command.ExecuteReader())
                    {

                        while (oReader.Read())
                        {
                            string ShiftID = oReader["ShiftID"].ToString();

                            UserRequestList.Add(ShiftID, new RequestStatus
                            {
                                status = true
                            });

                        }

                        connection.Close();
                    }

                }
                return req.CreateResponse(HttpStatusCode.OK, UserRequestList);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class RequestStatus
        {
            public Boolean status { get; set; }

        }
    }
}
