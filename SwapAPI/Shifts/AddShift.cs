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
using WebApi.Entities;

namespace SwapAPI
{
    public static class AddShift
    {
        [FunctionName("AddShift")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Manager}))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string shiftDate = data.shiftDate;
            string startTime = data.startTime;
            string endTime = data.endTime;
            string UserID = data.UserID;

            try
            {
                var str = Environment.GetEnvironmentVariable("sqldb_connection");
                int modified;
                using (SqlConnection connection = new SqlConnection(str))
                {

                    string text = @"INSERT INTO Shifts (shiftDate, startTime, endTime, UserID) " +
                        "OUTPUT INSERTED.ShiftID " +
                        "VALUES (@shiftDate, @startTime, @endTime, @UserID);";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@shiftDate", shiftDate);
                    command.Parameters.AddWithValue("@startTime", startTime);
                    command.Parameters.AddWithValue("@endTime", endTime);
                    command.Parameters.AddWithValue("@UserID", UserID);

                    connection.Open();
                    modified = (int)command.ExecuteScalar();
                    connection.Close();

                }

                return req.CreateResponse(HttpStatusCode.OK, modified);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
    }
}
