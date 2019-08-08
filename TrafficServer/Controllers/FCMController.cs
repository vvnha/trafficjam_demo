using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace TrafficServer.Controllers
{
    public class FCMController : ApiController
    {
        ///<summary>
        /// trả về FCM theo id
        /// </summary>

        /// <param name="id">id trong CSDL</param>
        /// <returns></returns>
        //[HttpGet]
        public FCM getFCM(int id)
        {
            try
            {
                string strConnection =
                System.Configuration.ConfigurationManager.AppSettings["strConnection"];

                SqlConnection conn = new SqlConnection(strConnection);
                conn.Open();
                string sql = "select * from FCM where id=@id";
                SqlCommand command = new SqlCommand(sql, conn);
                command.Parameters.Add("@id", SqlDbType.NVarChar).Value = id;
                SqlDataReader reader = command.ExecuteReader();
                FCM fcm = null;
                while (reader.Read())//trong khi còn dữ liệu để đọc
                {
                    fcm = new FCM();
                    fcm.Id = reader.GetInt32(0);
                    fcm.Token = reader.GetString(1);
                }
                conn.Close();
                return fcm;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        ///<summary>
        /// Hàm trả về toàn bộ FCM để ta gửi Push Message cho toàn bộ device
        /// </summary>

        /// <returns></returns>
        [HttpGet]
        public List<FCM> getFCMS()
        {
            //notification();
            try
            {
                List<FCM> dsFcm = new List<FCM>();
                string strConnection =
                System.Configuration.ConfigurationManager.AppSettings["strConnection"];

                SqlConnection conn = new SqlConnection(strConnection);
                conn.Open();
                string sql = "select * from FCM";
                SqlCommand command = new SqlCommand(sql, conn);
                SqlDataReader reader = command.ExecuteReader();
                FCM fcm = null;
                while (reader.Read())//trong khi còn dữ liệu để đọc
                {
                    fcm = new FCM();
                    fcm.Id = reader.GetInt32(0);
                    fcm.Token = reader.GetString(1);
                    dsFcm.Add(fcm);
                }
                conn.Close();
                return notification(dsFcm);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        ///<summary>
        /// trả về FCM dựa vào 1 token
        /// </summary>

        /// <param name="token">token từ Firebase</param>
        /// <returns>Nếu có trả về FCM, không có trả về null</returns>

        public FCM getFCM(string token)
        {
            try
            {
                string strConnection =
                System.Configuration.ConfigurationManager.AppSettings["strConnection"];

                SqlConnection conn = new SqlConnection(strConnection);
                conn.Open();
                string sql = "select * from FCM where token=@token";
                SqlCommand command = new SqlCommand(sql, conn);
                command.Parameters.Add("@token", SqlDbType.NVarChar).Value = token;
                SqlDataReader reader = command.ExecuteReader();
                FCM fcm = null;
                while (reader.Read())//trong khi còn dữ liệu để đọc
                {
                    fcm = new FCM();
                    fcm.Id = reader.GetInt32(0);
                    fcm.Token = reader.GetString(1);
                }
                conn.Close();
                return fcm;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        ///<summary>
        /// Dịch vụ này dùng để lưu token của Device vào cơ sở dữ liệu
        /// </summary>

        /// <param name="token">token do Firebase truyền về</param>
        /// <returns>lưu thành công trả về true, thất bại flase</returns>
        [HttpPost]
        public bool saveToken(string token)
        {
            try
            {
                if (getFCM(token) != null) return false;

                SqlConnection myConnection = new SqlConnection();
                myConnection.ConnectionString = @"Server=desktop-8n0o4eo\mssqlserver1;Database=dbTrafficJam;User ID=sa;Password=nhavovvv;";
                //SqlCommand sqlCmd = new SqlCommand("INSERT INTO tblEmployee (EmployeeId,Name,ManagerId) Values (@EmployeeId,@Name,@ManagerId)", myConnection);  
                SqlCommand sqlCmd = new SqlCommand();
                sqlCmd.CommandType = CommandType.Text;
                sqlCmd.CommandText = "insert into FCM values(@token)";
                sqlCmd.Connection = myConnection;

                sqlCmd.Parameters.Add("@token", SqlDbType.NVarChar).Value = token;

                myConnection.Open();
                int kq = sqlCmd.ExecuteNonQuery();
                myConnection.Close();
                return kq > 0;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public int Count()
        {
            try
            {
                string strConnection =
               System.Configuration.ConfigurationManager.AppSettings["strConnection"];

                SqlConnection conn = new SqlConnection(strConnection);
                conn.Open();
                string sql = "select count(*) from FCM "/*where token=@token*/;
                SqlCommand command = new SqlCommand(sql, conn);
                Int32 kq = (Int32)command.ExecuteScalar();//tiến hành insert
                conn.Close();
                return kq;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<FCM> notification(List<FCM> dsFcm)
        {
            int i = 6;
            if (i >= 5)
            {
                string td = "Traffic jam";
                string nd = "Kẹt xe tại xyz";

                //FCMController fcmController = new FCMController();
                //List<FCM> dsFcm = fcmController.getFCMS();
                WebRequest tRequest;
                //thiết lập FCM send
                tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
                tRequest.Method = "POST";
                tRequest.UseDefaultCredentials = true;

                tRequest.PreAuthenticate = true;

                tRequest.Credentials = CredentialCache.DefaultNetworkCredentials;

                //định dạng JSON
                tRequest.ContentType = "application/json";
                tRequest.Headers.Add(string.Format("Authorization: key={0}", "AAAA4lX6JOQ:APA91bG1jWTVMmLQ9IXYWRRJ7deisQeqEFwK9BhgdBLAFc0zMxIt3jzPHR3-BCQX4RB0sNg6rt_z75Kc1NTn_5PNQOQwb5DGRWP1qtiOMJlLxkeBUSxAMFew_NynWHucynNenXJxYy5d"));
                tRequest.Headers.Add(string.Format("Sender: id={0}", "972105065700"));

                string[] arrRegid = dsFcm.Select(x => x.Token).ToArray();
                string RegArr = string.Empty;
                RegArr = string.Join("\",\"", arrRegid);

                string postData = "{ \"registration_ids\": [ \"" + RegArr + "\" ],\"data\": {\"message\": \"" + nd + "\",\"body\": \"" + nd + "\",\"title\": \"" + td + "\",\"collapse_key\":\"" + nd + "\"}}";

                Byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                tRequest.ContentLength = byteArray.Length;

                Stream dataStream = tRequest.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse tResponse = tRequest.GetResponse();

                dataStream = tResponse.GetResponseStream();

                StreamReader tReader = new StreamReader(dataStream);

                String sResponseFromServer = tReader.ReadToEnd();

                //txtKetQua.Text = sResponseFromServer; //Lấy thông báo kết quả từ FCM server.
                tReader.Close();
                dataStream.Close();
                tResponse.Close();
                return dsFcm;
            }
            else
            {
                return null;
            }

        }
}
}
