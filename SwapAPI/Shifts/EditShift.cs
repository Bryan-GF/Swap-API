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
using System.Data;
using System.Text;

namespace SwapAPI
{
    public static class EditShift
    {
        [FunctionName("EditShift")]
        public static async Task<object> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route= null)]
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
            string shiftDate = data.shiftDate;
            string startTime = data.startTime;
            string endTime = data.endTime;
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
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Item has already been deleted!");
                        }
                        command.CommandText = @"UPDATE Shifts " +
                                "SET shiftDate = @shiftDate, startTime = @startTime, endTime = @endTime " +
                                "WHERE ShiftID = @ShiftID2 AND Ver = CONVERT(VARBINARY, @Version, 1);";
                        command.Parameters.AddWithValue("@ShiftID2", ShiftID);
                        command.Parameters.AddWithValue("@shiftDate", shiftDate);
                        command.Parameters.AddWithValue("@startTime", startTime);
                        command.Parameters.AddWithValue("@endTime", endTime);
                        command.Parameters.AddWithValue("@Version", Version);

 
                     int count = command.ExecuteNonQuery();
                        if (count <= 0)
                        {
                            return req.CreateResponse(HttpStatusCode.BadRequest, "Item was already updated!");
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
