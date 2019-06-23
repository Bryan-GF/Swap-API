using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Net.Http;
using System.Net;
using WebApi.Entities;
using MyProject.Helpers;

namespace SwapAPI
{
    public static class ResetPassword
    {
        [FunctionName("ResetPassword")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route= null)]
            HttpRequestMessage req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string token = req.Headers.Authorization.ToString();
            if (!AuthorizationHelper.Authorized(token, new string[] { Role.Employee, Role.Manager, Role.Owner }))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Unauthorized!");
            }

            //We retrieve the userName field, which comes as a parameter to the function, by deserializing req.Content.
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            string email = data.email;
            string oldPass = data.oldPassword;
            string newPass = data.newPassword;

            newPass = SecurePasswordHasherHelper.Hash(newPass);

            try
            {
                //We get the Connection String in the Function App Settings section we defined.
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {
                    connection.Open();
                    SqlTransaction sqlTran = connection.BeginTransaction();
                    SqlCommand command = connection.CreateCommand();
                    command.Transaction = sqlTran;
                    try
                    {
                        command.CommandText = @"SELECT PasswordHash FROM Users WHERE email = @email";
                        command.Parameters.AddWithValue("@email", email);

                        string verifyPass = "";
                        using (SqlDataReader oReader = command.ExecuteReader())
                        {
                            while (oReader.Read())
                            {
                                verifyPass = oReader["PasswordHash"].ToString();
                            }
                        }

                        if(!SecurePasswordHasherHelper.Verify(oldPass, verifyPass))
                        {
                            return req.CreateResponse(HttpStatusCode.NotAcceptable, "Incorrect Password!");
                        }

                        command.CommandText = @"UPDATE Users " +
                                "SET PasswordHash = @NewPassword " +
                                "WHERE email = @email";
                        command.Parameters.AddWithValue("@email", email);
                        command.Parameters.AddWithValue("@NewPassword", newPass);
                        command.ExecuteNonQuery();

                        sqlTran.Commit();
                        connection.Close();
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
