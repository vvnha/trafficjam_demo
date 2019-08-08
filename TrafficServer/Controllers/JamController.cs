using Alturos.Yolo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace TrafficServer.Controllers
{
    [RoutePrefix("api/jam")]
    public class JamController : ApiController
    {
        SqlCommand cmd;
        SqlConnection con;
        [Route("upimg")]
        [HttpPost]
        public HttpResponseMessage Upload()
        {
            try
            {
                var request = HttpContext.Current.Request;
                var description = request.Form["description"];
                var longtitude = request.Form["longtitude"];
                var latitude = request.Form["latitude"];
                var streetName = request.Form["streetName"];
                var cityName = request.Form["cityName"];
                var photo = request.Files["photo"];
                var imgPath = HttpContext.Current.Server.MapPath("~/Content/Uploads/"+ photo.FileName);
                photo.SaveAs(imgPath);
                AddImage(photo.FileName,Detect(imgPath),longtitude,latitude,streetName,cityName);
                notification(streetName,cityName);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }
        [HttpGet]
        public List<TraficJam> GetFoodLists()
        {
            DBTrafficDataContext db = new DBTrafficDataContext();
            return db.TraficJams.ToList();
            
        }
        [HttpGet]
        public TraficJam GetFood(int id)
        {
            DBTrafficDataContext db = new DBTrafficDataContext();
            return db.TraficJams.FirstOrDefault(x => x.Id == id);
        }
        public void AddImage(String name,int num, String longti , String lati, String stName, String ctName)
        {
            SqlConnection myConnection = new SqlConnection();
            myConnection.ConnectionString = @"Server=desktop-8n0o4eo\mssqlserver1;Database=dbTrafficJam;User ID=sa;Password=nhavovvv;";

            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.CommandType = CommandType.Text;
            sqlCmd.CommandText = "INSERT INTO TraficJam(ImgName,vehicleNum,Longtitude,Latitude,streetName,city,Date) Values (@Name,@num,@Long,@Lat,@stName,@ctName,@date)";
            sqlCmd.Connection = myConnection;

            sqlCmd.Parameters.AddWithValue("@Name", name);
            sqlCmd.Parameters.AddWithValue("@num", num);
            sqlCmd.Parameters.AddWithValue("@Long", longti);
            sqlCmd.Parameters.AddWithValue("@Lat", lati);
            sqlCmd.Parameters.AddWithValue("@stName", stName);
            sqlCmd.Parameters.AddWithValue("@ctName", ctName);
            sqlCmd.Parameters.AddWithValue("@date", DateTime.Now);

            myConnection.Open();
            int rowInserted = sqlCmd.ExecuteNonQuery();
            myConnection.Close();
        }
        public int Detect(string imgPath)
        {
            string target1 = @"D:\Visual Project\TrafficServer\TrafficServer\bin\yolov2-tiny-voc.cfg";
            string target2 = @"D:\Visual Project\TrafficServer\TrafficServer\bin\yolov2-tiny-voc.weights";
            string target3 = @"D:\Visual Project\TrafficServer\TrafficServer\bin\voc.names";
            if (File.Exists(target1))
            {
                var configurationDetector = new ConfigurationDetector();
                using (var yoloWrapper = new YoloWrapper(target1, target2, target3))
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    var items = yoloWrapper.Detect(imgPath);
                    //txtKetQua.Text = "Number of Object Detected: " + items.Count().ToString() + "\tin: " + sw.Elapsed.TotalSeconds + "s\n";
                    int y = 0;
                    foreach (Alturos.Yolo.Model.YoloItem s in items)
                    {
                       
                        if (s.Type.ToString().Equals("car"))
                        {
                            y++;
                        }

                    }
                    sw.Stop();
                    return items.Count();
                }
                //txtKetQua.Text = "Yes";
            }
            else
            {
                return 0;
            }
        }
        public int setCount(String stName, String ctName)
        {
            DateTime aTime = DateTime.Now;
            TimeSpan bTime = new System.TimeSpan(0, 5, 0, 0);
            DateTime newTime = aTime.Subtract(bTime);


            string strConnection = System.Configuration.ConfigurationManager.AppSettings["strConnection"];
            SqlConnection conn = new SqlConnection(strConnection);
            conn.Open();
            string sql = "select count(*) from TraficJam where convert(varchar(20),Date) > @nTime AND city = @ctName AND streetName = @stName";

            SqlCommand command = new SqlCommand(sql, conn);
            command.Parameters.AddWithValue("@nTime", newTime);
            command.Parameters.AddWithValue("@ctName", ctName);
            command.Parameters.AddWithValue("@stName", stName);
            Int32 kq = (Int32)command.ExecuteScalar();//tiến hành insert
            conn.Close();
            return kq;
        }
        public void notification( String stName, String city)
        {
            if (setCount(stName,city) == 5 || setCount(stName,city)== 10)
            {
                string td = "Traffic jam";
                string nd = "Kẹt xe: "+stName+ ", " + city;

                FCMController fcmController = new FCMController();
                List<FCM> dsFcm = fcmController.getFCMS();
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
                
            }

        }
    }
}
