using LoginRegistrationApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using OtpNet;
using QRCoder;



namespace LoginRegistrationApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RegistrationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GenerateQrCodeUrl(string appName, string username, string secretKey)
        {
            string qrCodeUrl = $"https://api.qrserver.com/v1/create-qr-code/?data=otpauth://totp/{appName}:{username}%3Fsecret={secretKey}&size=200x200";
            return qrCodeUrl;
        }

        [HttpPost]
        [Route("registration")]
            public string Registration(Registration registration, [FromQuery] string secretKey)
            {
                Response.Headers.Add("Access-Control-Allow-Methods", "POST");
                Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

            //string secretKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey());
            //string secretKey = registration.secretKey;
            Console.WriteLine("Hello, world!");
            Console.WriteLine("secretKey"+ secretKey);

            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ToysCon"));
                SqlCommand cmd = new SqlCommand("INSERT INTO Registration (username, password, Secretekey, FaStatus) VALUES (@username, @password, @secretekey, @fastatus)", con);
                cmd.Parameters.AddWithValue("@username", registration.username);
                cmd.Parameters.AddWithValue("@password", registration.password);
                cmd.Parameters.AddWithValue("@secretekey", secretKey);
                cmd.Parameters.AddWithValue("@fastatus", registration.fastatus);

            con.Open();
            int i = cmd.ExecuteNonQuery();
            con.Close();

            if (i > 0)
            {
                string qrCodeUrl = GenerateQrCodeUrl("MyApp", registration.username, secretKey);
                return qrCodeUrl;
                return "Data inserted";
            }
            else
            {
                return "Error";
            }
        }




        [HttpPost]
        [Route("login")]
        public string Login(Registration registration)
        {
            Response.Headers.Add("Access-Control-Allow-Methods", "POST");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ToysCon"));
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Registration WHERE UserName = @username AND Password = @password AND FaStatus = 1", con);
            da.SelectCommand.Parameters.AddWithValue("@username", registration.username);
            da.SelectCommand.Parameters.AddWithValue("@password", registration.password);

            DataTable dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count > 0)
            {
                return "User found";
            }
            else
            {
                return "Invalid User";
            }
        }
        [HttpGet]
        [Route("auth/setup")]
        public ActionResult<object> SetupTwoFactorAuthentication(string username)
        {
            // Retrieve the secret key from the user's account or your database
            //string secretKey = GetSecretKeyFromDatabase(username);
            string secretKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey());
            string qrCodeUrl = GenerateQrCodeUrl("MyApp", username, secretKey);

            return new
            {
                SecretKey = secretKey,
                QrCodeUrl = qrCodeUrl
            };
        }
        private string GetSecretKeyFromDatabase(string username,string password)
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ToysCon"));
            SqlCommand cmd = new SqlCommand("SELECT Secretekey FROM Registration WHERE UserName = @username AND Password = @password", con);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);


            con.Open();
            string secretKey = cmd.ExecuteScalar()?.ToString();
            con.Close();

            return secretKey;
        }
        [HttpGet]
        [Route("auth/generate")]
        public ActionResult<object> SetupTwoFactor(string username, string otp, string password)
            
        {
            // Retrieve the secret key from the user's account or your database
            //string secretKey = GetSecretKeyFromDatabase(username);
            Console.WriteLine("in login check129" );
            if(otp == null) {
                Console.WriteLine("otp not req");
                return true;
            }
            string secretKey = GetSecretKeyFromDatabase(username,password);
            Console.WriteLine("secretKey"+secretKey);
            Console.WriteLine("username"+ username);
            Console.WriteLine("pass" + password);
            Console.WriteLine("otp" + otp);


            //string secretKey = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey());
            //string qrCodeUrl = GenerateQrCodeUrl("MyApp", username, secretKey);
            if (string.IsNullOrEmpty(secretKey))
            {
                Console.WriteLine("getting false");
                return false; // Secret key not provided
            }

            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            Console.WriteLine("totp"+totp);
            Console.WriteLine("otp" + otp);


            string systemOTP = totp.ComputeTotp();
            Console.WriteLine("systemOTP"+ systemOTP);

            bool isOTPValid = systemOTP == otp;
            Console.WriteLine("getting true");
            Console.WriteLine(isOTPValid);
            return isOTPValid;
        }



        [HttpGet]
        [Route("auth/generates")]
        public ActionResult<bool> SetupTwoFactor1(string username, string password)

        {
            Console.WriteLine("check for no otp");
            bool isMatch = false;
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ToysCon"));
            SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Registration WHERE UserName = @username AND Password = @password ", con);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", password);


            con.Open();
            int count = (int)cmd.ExecuteScalar();
            isMatch = count > 0;

            con.Close();
            return isMatch;
        }

        /*[HttpGet]
        [Route("totp/generate")]
        public string GenerateTOTP(string username)
        {
            string secretKey = GetSecretKeyFromDatabase(username);

            if (string.IsNullOrEmpty(secretKey))
            {
                return "Secret key not found";
            }

            string otp = GenerateTOTP(secretKey);

            return otp;
        }*/
        /*
                [HttpPost]
                [Route("totp/generate")]
                public string GenerateTOTP([FromBody] string username)
                {
                    string secretKey = GetSecretKeyFromDatabase(username);

                    if (string.IsNullOrEmpty(secretKey))
                    {
                        return "Secret key not found";
                    }

                    string otp = GenerateTOTP(secretKey);

                    return otp;
                }
                */
        [HttpPost]
        [Route("totp/generate")]
        public bool GenerateAndVerifyTOTP([FromQuery] string username, [FromQuery] string secretKey, [FromQuery] string otp)
        {
            if (secretKey == ""){
                return false;
            }
            if (string.IsNullOrEmpty(secretKey))
            {
                return false; // Secret key not provided
            }

            var totp = new Totp(Base32Encoding.ToBytes(secretKey));
            string systemOTP = totp.ComputeTotp();
            bool isOTPValid = systemOTP == otp;
            Console.WriteLine("secretekey" + secretKey);

            Console.WriteLine("totp" + totp);
            Console.WriteLine("otp" + otp);
            Console.WriteLine("systemOTP" + systemOTP);

            return isOTPValid;
        }


        [HttpGet]
        [Route("totp/check")]
        public ActionResult<bool> Check(string username)
        {

            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ToysCon"));
            SqlCommand cmd = new SqlCommand("SELECT Fastatus FROM Registration WHERE UserName = @username ", con);
            cmd.Parameters.AddWithValue("@username", username);

            con.Open();
            object result = cmd.ExecuteScalar();
            bool status = (result != null && Convert.ToInt32(result) == 1);
            con.Close();
            Console.WriteLine("status"+status);
            
            return status;

        }


        [HttpPost]
        [Route("auth/update")]
        public ActionResult<object> UpdateRow(string username, string secretKey,string fastatus)
        {
            Console.WriteLine("in update");
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ToysCon"));
            SqlCommand cmd = new SqlCommand("UPDATE Registration SET Secretekey = @secretekey, FaStatus = @fastatus WHERE UserName = @username", con);
            cmd.Parameters.AddWithValue("@secretekey", secretKey);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@fastatus", fastatus);


            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            con.Close();

            if (rowsAffected > 0)
            {
                return Ok("Row updated successfully.");
            }
            else
            {
                return NotFound("No matching row found for the provided username.");
            }
        }

        [HttpPost]
        [Route("auth/updates")]
        public ActionResult<object> UpdateRows(string username, string secretKey, string fastatus)
        {
          
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("ToysCon"));
            SqlCommand cmd = new SqlCommand("UPDATE Registration SET Secretekey = @secretekey, FaStatus = @fastatus WHERE UserName = @username", con);
            cmd.Parameters.AddWithValue("@secretekey", secretKey);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@fastatus", fastatus);


            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            con.Close();

            if (rowsAffected > 0)
            {
                return Ok(new { message = "Row updated successfully." });
            }
            else
            {
                return NotFound(new { error = "No matching row found for the provided username." });
            }
        }
    }
}
