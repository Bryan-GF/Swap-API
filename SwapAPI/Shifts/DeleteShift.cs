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
    public static class DeleteShift
    {
        [FunctionName("DeleteShift")]
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
            string ShiftID = data.ShiftID;

            try
            {
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {

                    string text = @"DELETE FROM Shifts WHERE ShiftID=@ShiftID";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@ShiftID", ShiftID);

                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();

                }

                return req.CreateResponse(HttpStatusCode.OK, "Success");
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
    }
}
