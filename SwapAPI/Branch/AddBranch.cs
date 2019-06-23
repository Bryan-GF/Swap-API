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
    public static class AddBranch
    {
        [FunctionName("AddBranch")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Owner }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string Name = data.Name;
            string CompanyID = data.CompanyID;
            string roomId = data.roomId;

            try
            {
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                int modified;
                using (SqlConnection connection = new SqlConnection(str))
                {

                    string text = @"INSERT INTO BranchTable (companyID, Name, roomId) " +
                        "OUTPUT INSERTED.branchID " +
                        "VALUES (@CompanyID, @Name, @roomId);";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@CompanyID", CompanyID);
                    command.Parameters.AddWithValue("@Name", Name);
                    command.Parameters.AddWithValue("@roomId", roomId);
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
