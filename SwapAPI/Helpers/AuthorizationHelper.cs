using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using JWT.Builder;
using JWT.Serializers;
namespace MyProject.Helpers
{
    public sealed class AuthorizationHelper
    {
        
        public static bool Authorized(string token, string[] target)
        {

            try
            {
                string secret = Environment.GetEnvironmentVariable("Secret");
                var payload = new JwtBuilder()
                .WithSecret(secret)
                .MustVerifySignature()
                .Decode<IDictionary<string, object>>(token);
                for (int i = 0; i < target.Length; i++)
                {
                    if (payload["roles"].ToString() == target[i])
                    {
                        return true;
                    }
                }
                return false;

            }
            catch
            {
                return false;
            }
        }
    }
}