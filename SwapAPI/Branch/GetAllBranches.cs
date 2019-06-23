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
    public static class GetAllBranches
    {
        [FunctionName("GetAllBranches")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {

            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Owner }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }


            //We retrieve the userName field, which comes as a parameter to the function, by deserializing req.Content.
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            string CompanyID = data.CompanyID;

            try
            {
                Branch[] allBranches = null;

                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {
                    string text = @"SELECT * FROM BranchTable " +
                        "WHERE companyID = @CompanyID";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@CompanyID", CompanyID);
                    connection.Open();

                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        log.LogInformation("C# HTTP trigger function processed a request. 6");
                        var list = new List<Branch>();
                        while (oReader.Read())
                        {
                            list.Add(new Branch
                            {
                                roomId = oReader["roomId"].ToString(),
                                branchID = oReader["branchID"].ToString(),
                                Name = oReader["Name"].ToString(),
                            });
                            allBranches = list.ToArray();
                        }

                        connection.Close();
                    }

                }
                return req.CreateResponse(HttpStatusCode.OK, allBranches);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class Branch
        {
            public string branchID { get; set; }
            public string Name { get; set; }
            public string roomId { get; set; }

        }
    }
}
