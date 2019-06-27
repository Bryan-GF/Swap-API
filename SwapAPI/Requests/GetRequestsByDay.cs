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
using System.Linq;

namespace SwapAPI.Branch
{
    public static class GetRequestsByDay
    {
        [FunctionName("GetRequestsByDay")]
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
            string shiftDate = data.shiftDate;
            string branchID = data.branchID;

            try
            {
                Request[] allRequests = null;
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {
                    string text = @"SELECT R.UserID, R.ShiftID, R.Comment, R.Urgent, U.Firstname, U.email, U.Position, S.startTime, S.endTime, S.Ver " +
                                    "FROM Requests R " +
                                    "INNER JOIN Users U ON R.UserID = U.UserID " +
                                    "INNER JOIN Shifts S ON R.ShiftID = S.ShiftID " +                            
                                    "WHERE S.shiftDate = @shiftDate AND U.branchID = @branchID";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@shiftDate", shiftDate);
                    command.Parameters.AddWithValue("@branchID", branchID);
                    connection.Open();

                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        log.LogInformation("C# HTTP trigger function processed a request. 6");
                        var list = new List<Request>();
                        while (oReader.Read())
                        {
                            ulong dbrow = BitConverter.ToUInt64((byte[])oReader["Ver"], 0);
                            log.LogInformation(dbrow.ToString());
                            string temp = BitConverter.ToString(BitConverter.GetBytes(dbrow).ToArray()).Replace("-", "");
                            log.LogInformation(string.Format("0x{0:X}", temp));
                            list.Add(new Request
                            {
                                UserID = oReader["UserID"].ToString(),
                                ShiftID = oReader["ShiftID"].ToString(),
                                Comment = oReader["Comment"].ToString(),
                                Urgent = (bool)oReader["Urgent"],
                                email = oReader["email"].ToString(),
                                Firstname = oReader["Firstname"].ToString(),
                                Position = oReader["Position"].ToString(),
                                startTime = oReader["startTime"].ToString(),
                                endTime = oReader["endTime"].ToString(),
                                Version = string.Format("0x{0:X}", temp)
                            });
                            allRequests = list.ToArray();
                        }

                        connection.Close();
                    }

                }
                return req.CreateResponse(HttpStatusCode.OK, allRequests);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class Request
        {
            public string UserID { get; set; }
            public string ShiftID { get; set; }
            public string email { get; set; }
            public string Comment { get; set; }
            public Boolean Urgent { get; set; }
            public string Firstname { get; set; }
            public string Position { get; set; }
            public string startTime { get; set; }
            public string endTime { get; set; }
            public string Version { get; set; }
        }
    }
}
