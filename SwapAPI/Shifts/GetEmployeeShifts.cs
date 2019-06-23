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
    public static class GetEmployeeShifts
    {
        [FunctionName("GetEmployeeShifts")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Employee, Role.Manager }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string UserID = data.UserID;

            log.LogInformation(UserID);

            try
            {
                User[] allShifts = null;
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {
                    string text = @"SELECT * FROM Shifts " +
                        "WHERE UserID = @UserID";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@UserID", UserID);
                    connection.Open();

                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        log.LogInformation("C# HTTP trigger function processed a request. 6");
                        var list = new List<User>();
                        while (oReader.Read())
                        {
                            ulong dbrow = BitConverter.ToUInt64((byte[])oReader["Ver"], 0);
                            log.LogInformation(dbrow.ToString());
                            string temp = BitConverter.ToString(BitConverter.GetBytes(dbrow).ToArray()).Replace("-", "");
                            log.LogInformation(string.Format("0x{0:X}", temp));
                            
                            list.Add(new User
                            {
                                ShiftID = oReader["ShiftID"].ToString(),
                                shiftDate = oReader["shiftDate"].ToString(),
                                startTime = oReader["startTime"].ToString(),
                                endTime = oReader["endTime"].ToString(),
                                Version = string.Format("0x{0:X}", temp)
                            });
                            allShifts = list.ToArray();
                        }

                        connection.Close();
                    }

                }
                return req.CreateResponse(HttpStatusCode.OK, allShifts);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class User
        {
            public string ShiftID { get; set; }
            public string shiftDate { get; set; }
            public string startTime { get; set; }
            public string endTime { get; set; }
            public string Version { get; set; }
        }
    }
}
