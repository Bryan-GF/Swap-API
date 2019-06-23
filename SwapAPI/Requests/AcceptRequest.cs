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
    public static class AcceptRequest
    {
        [FunctionName("AcceptRequest")]
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
            string AddUserID = data.AddUserID;
            string DelUserID = data.DelUserID;
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

                        // UPDATE UserID for the Shift, to the person who accepted the request
                        command.CommandText = @"UPDATE Shifts " +
                            "SET UserID = @AddUserID " +
                            "WHERE ShiftID = @ShiftID2 AND Ver = CONVERT(VARBINARY, @Version, 1);"; 
                        command.Parameters.AddWithValue("@ShiftID2", ShiftID);
                        command.Parameters.AddWithValue("@AddUserID", AddUserID);
                        command.Parameters.AddWithValue("@Version", Version);

                        int countUpdated = command.ExecuteNonQuery();
                        if (countUpdated <= 0)
                        {
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Shift was already updated!");
                        }

                        
                        // REMOVE REQUEST FROM TABLE
                        command.CommandText = @"DELETE FROM Requests WHERE ShiftID = @ShiftID3 AND UserID = @DelUserID;";
                        command.Parameters.AddWithValue("@ShiftID3", ShiftID);                        
                        command.Parameters.AddWithValue("@DelUserID", DelUserID);
                   
                        int countDeleted = command.ExecuteNonQuery();
                        if (countDeleted <= 0)
                        {
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Request no longer exists!");
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
