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
using WebApi.Entities;
using MyProject.Helpers;

namespace SwapAPI.Branch
{
    public static class GetRequestCounts
    {
        [FunctionName("GetRequestCounts")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Employee, Role.Manager, Role.Owner }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string branchID = data.branchID;


            try
            {
                Dictionary<string, RequestCounts> RequestCountList = new Dictionary<string, RequestCounts>();
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {
                    string text = @"SELECT S.shiftDate, COUNT(*) AS NumRequests " +
                                    "FROM Shifts S " +
                                    "JOIN Requests R ON R.ShiftID = S.ShiftID " +
                                    "JOIN Users U ON U.UserID = R.UserID " +
                                    "WHERE U.branchID = @branchID " +
                                    "GROUP BY S.shiftDate";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@branchID", branchID);
                    connection.Open();

                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        log.LogInformation("C# HTTP trigger function processed a request. 6");
                        while (oReader.Read())
                        {
                            string date = oReader["shiftDate"].ToString().Substring(0, 10);
                            if(date[9] != ' ')
                            {
                                date = date.Substring(0, 8);
                            }
                            date = date.Trim();
                            RequestCountList.Add(date, new RequestCounts
                            {
                                Count = (int)oReader["NumRequests"],                              
                            });
                            
                        }

                        connection.Close();
                    }

                }
                return req.CreateResponse(HttpStatusCode.OK, RequestCountList);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class RequestCounts
        {
            public int Count { get; set; }

        }
    }
}
