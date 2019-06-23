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
    public static class DeleteRequest
    {
        [FunctionName("DeleteRequest")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Employee }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            //We retrieve the userName field, which comes as a parameter to the function, by deserializing req.Content.
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            string UserID = data.UserID;
            string ShiftID = data.ShiftID;
            //If there is no username, we return the error message.
            try
            {
                //We get the Connection String in the Function App Settings section we defined.
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {

                    string text = @"DELETE FROM Requests WHERE UserID = @UserID AND ShiftID = @ShiftID";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@UserID", UserID);
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
