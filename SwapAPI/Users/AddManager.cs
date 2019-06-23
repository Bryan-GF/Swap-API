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
using Microsoft.AspNetCore.Authorization;
//SHOULD BE UPDATED FOR NEW AUTH
/// <summary>
/// INSTEAD OF STORED PROCEDURE JUST USE SQL QUERY TO GET MATCHING QUERY TO EMPLOYEE ID.
/// ALSO INSTEAD OF USING SQL HASHING, USE .NET hashing and push that in as the password when creating a user and just use .NET hash compare.
/// </summary>
namespace SwapAPI
{
    [Authorize(Roles = Role.Manager)]
    public static class AddManager
    {
        [FunctionName("AddManager")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Owner }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            //We retrieve the userName field, which comes as a parameter to the function, by deserializing req.Content.
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string email = data.email;
            string Firstname = data.Firstname;
            string Lastname = data.Lastname;
            string Password = data.Password;
            string branchID = data.branchID;
            string CompanyID = data.CompanyID;

            Password = SecurePasswordHasherHelper.Hash(Password);
            //If there is no username, we return the error message.
            try
            {
                //We get the Connection String in the Function App Settings section we defined.
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                int modified;
                using (SqlConnection connection = new SqlConnection(str))
                {

                    string text = @"INSERT INTO Users (email, Firstname, Lastname, Position, branchID, PasswordHash, Role, CompanyID) " +
                        "OUTPUT INSERTED.UserID " +
                        "VALUES (@email, @Firstname, @Lastname, @Position, @branchID, @PasswordHash, @Role, @CompanyID);";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@Firstname", Firstname);
                    command.Parameters.AddWithValue("@Lastname", Lastname);
                    command.Parameters.AddWithValue("@Position", "Manager");
                    command.Parameters.AddWithValue("@branchID", branchID);
                    command.Parameters.AddWithValue("@PasswordHash", Password);
                    command.Parameters.AddWithValue("@Role", Role.Manager);
                    command.Parameters.AddWithValue("@CompanyID", CompanyID);
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

