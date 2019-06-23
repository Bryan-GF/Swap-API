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
using JWT.Builder;
using JWT.Algorithms;
//NEED TO MAKE BRANCH ID NULLABLE

namespace SwapAPI
{
    public static class AddCompany
    {
        [FunctionName("AddCompany")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //We retrieve the userName field, which comes as a parameter to the function, by deserializing req.Content.
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string email = data.email;
            string Firstname = data.Firstname;
            string Lastname = data.Lastname;
            string Password = data.Password;
            string CompanyName = data.CompanyName;
            string ContactNumber = data.CompanyNumber;

            Password = SecurePasswordHasherHelper.Hash(Password);
            //If there is no username, we return the error message.
            try
            {
               UserInfo newUser = new UserInfo();
                //We get the Connection String in the Function App Settings section we defined.
                var str = Environment.GetEnvironmentVariable("sqldb_connection");
                using (SqlConnection connection = new SqlConnection(str))
                {
                    connection.Open();
                    SqlTransaction sqlTran = connection.BeginTransaction();
                    string text =
                        @"declare @myVar smallint " +
                        "INSERT INTO CompanyTable (companyName, Contact_Number) " +
                        "VALUES (@CompanyName, @ContactNumber); " +

                        "SELECT @myVar = SCOPE_IDENTITY();  " +

                        "INSERT INTO Users (email, Firstname, Lastname, Position, branchID, PasswordHash, Role, CompanyID) " +
                        "VALUES(@email, @Firstname, @Lastname, 'Company Owner', @branchID, @PasswordHash, @Role, @myVar);  " +

                        "SELECT TOP 1 * FROM Users ORDER BY UserID DESC;";

                    SqlCommand command = new SqlCommand(text, connection);
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@Firstname", Firstname);
                    command.Parameters.AddWithValue("@Lastname", Lastname);
                    command.Parameters.AddWithValue("@branchID", System.Data.SqlTypes.SqlString.Null);
                    command.Parameters.AddWithValue("@PasswordHash", Password);
                    command.Parameters.AddWithValue("@Role", Role.Owner);
                    command.Parameters.AddWithValue("@CompanyName", CompanyName);
                    command.Parameters.AddWithValue("@ContactNumber", ContactNumber);
                    command.Transaction = sqlTran;
                    try {
                        using (SqlDataReader oReader = command.ExecuteReader())
                        {
                            while (oReader.Read())
                            {
                                newUser.UserID = oReader["UserID"].ToString();
                                newUser.CompanyID = oReader["CompanyID"].ToString();
                            }
                        }
                        sqlTran.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        try
                        {
                            // Attempt to roll back the transaction.
                            sqlTran.Rollback();
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Rollback!");
                        }
                        catch (Exception exRollback)
                        {
                            // Throws an InvalidOperationException if the connection 
                            // is closed or the transaction has already been rolled 
                            // back on the server.
                            Console.WriteLine(exRollback.Message);
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Rollback exception!");
                        }

                    }

                    connection.Close();
                    var token = new JwtBuilder()
                      .WithAlgorithm(new HMACSHA256Algorithm())
                      .WithSecret(Environment.GetEnvironmentVariable("Secret"))
                      .AddClaim("exp", DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds())
                      .AddClaim("UserID", newUser.UserID)
                      .AddClaim("email", email)
                      .AddClaim("Firstname", Firstname)
                      .AddClaim("Lastname", Lastname)
                      .AddClaim("Position", "Company Owner")
                      .AddClaim("branchID", null)
                      .AddClaim("CompanyID", newUser.CompanyID)
                      .AddClaim("roles", Role.Owner)
                      .Build();
                    newUser.Token = token;
                    }

                return req.CreateResponse(HttpStatusCode.OK, newUser);
            }
            catch
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Something went wrong!");
            }


        }
        public class UserInfo
        {
            public string UserID { get; set; }
            public string CompanyID { get; set; }
            public string Token { get; set; }

        }
    }
}

