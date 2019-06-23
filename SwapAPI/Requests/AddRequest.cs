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
    public static class AddRequest
    {
        [FunctionName("AddRequest")]
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

            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string ShiftID = data.ShiftID;
            string UserID = data.UserID;
            string Comment = data.Comment;
            Boolean Urgent = data.Urgent;
            string Version = data.Version;

            try
            {
                var str = Environment.GetEnvironmentVariable("sqldb_connection");

                using (SqlConnection connection = new SqlConnection(str))
                {
                    connection.Open();
                    SqlTransaction sqlTran = connection.BeginTransaction();
                    SqlCommand command = connection.CreateCommand();
                    command.Transaction = sqlTran;           
                    
                    try
                    {
                        command.CommandText = "SELECT COUNT(*) FROM Shifts WHERE ShiftID = @ShiftID;";
                        command.Parameters.AddWithValue("@ShiftID", ShiftID);

                        Int32 numRows = (Int32)command.ExecuteScalar();

                        if (numRows <= 0)
                        {
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Shift no longer exists!");
                        }

                        command.CommandText = "SELECT COUNT(*) FROM Shifts WHERE ShiftID = @ShiftID2 AND  Ver = CONVERT(VARBINARY, @Version, 1);";
                        command.Parameters.AddWithValue("@ShiftID2", ShiftID);
                        command.Parameters.AddWithValue("@Version", Version);

                        Int32 numRowsVer = (Int32)command.ExecuteScalar();

                        if (numRowsVer <= 0)
                        {
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Shift was updated before adding.");
                        }

                        // Inserts the request.
                        command.CommandText = @"INSERT INTO Requests (ShiftID, UserID, Comment, Urgent) " +
                        "VALUES (@ShiftID3, @UserID, @Comment, @Urgent)";
              
                        command.Parameters.AddWithValue("@ShiftID3", ShiftID);
                        command.Parameters.AddWithValue("@UserID", UserID);
                        command.Parameters.AddWithValue("@Comment", Comment);
                        command.Parameters.AddWithValue("@Urgent", Urgent);

                        int countUpdated = command.ExecuteNonQuery();
                        if (countUpdated <= 0)
                        {
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Shift was already updated!");
                        }

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
