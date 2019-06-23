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
    public static class GetManagers
    {
        [FunctionName("GetManagers")]
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
            string CompanyID = data.CompanyID;

            try
            {
                Manager[] allManagers = null;

                var str = Environment.GetEnvironmentVariable("sqldb_connection");
                Dictionary<string, List<Manager>> ManagerDict = new Dictionary<string, List<Manager>>();

                using (SqlConnection connection = new SqlConnection(str))
                {
                    string text = @"SELECT U.UserID, U.Firstname, U.Lastname, U.email, U.branchID " +
                                    "FROM Users U " +
                                    "WHERE U.CompanyID = @CompanyID AND U.Role = 'Manager'";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@CompanyID", CompanyID);
                    connection.Open();

                    using (SqlDataReader oReader = command.ExecuteReader())
                    {
                        var list = new List<Manager>();
                        while (oReader.Read())
                        {
                            Manager temp = new Manager
                            {
                                UserID = oReader["UserID"].ToString(),
                                Firstname = oReader["Firstname"].ToString(),
                                Lastname = oReader["Firstname"].ToString(),
                                email = oReader["email"].ToString(),
                                branchID = oReader["branchID"].ToString()
                            };

                            if (!ManagerDict.ContainsKey(temp.branchID))
                            {
                                ManagerDict.Add(temp.branchID, new List<Manager>());
                                
                            }
                            ManagerDict[temp.branchID].Add(temp);



                            list.Add(temp);
                            allManagers = list.ToArray();
                        }
                        connection.Close();
                    }

                }
                return req.CreateResponse(HttpStatusCode.OK, ManagerDict);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class Manager
        {
            public string UserID { get; set; }
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string email { get; set; }
            public string branchID { get; set; }

        }
    }
}