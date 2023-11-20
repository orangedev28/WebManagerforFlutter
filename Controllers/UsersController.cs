using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using WebQuanLyAppOnTap.Models;
using BCrypt.Net;
using System.Drawing.Printing;
using System.Net.Mail;
using System.Net;

namespace WebQuanLyAppOnTap.Controllers
{
    public class UsersController : Controller
    {
        public ActionResult Index()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT u.id, u.username, u.fullname, u.email, t.nametype " +
                   "FROM users u " +
                   "INNER JOIN user_types t ON u.usertype_id = t.id";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                List<UserData> userList = new List<UserData>();

                try
                {
                    conn.Open();
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            UserData user = new UserData();
                            user.Id = Convert.ToInt32(dataReader["id"]);
                            user.Username = dataReader["username"].ToString();
                            user.FullName = dataReader["fullname"].ToString();
                            user.Email = dataReader["email"].ToString();
                            user.NameType = dataReader["nametype"].ToString();
                            userList.Add(user);
                        }
                    }

                    return View(userList);
                }
                catch (Exception ex)
                {
                    // Handle exceptions
                    return View("Error");
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public ActionResult EditUser(int userId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT id, username, fullname, email, usertype_id " +
                               "FROM users " +
                               "WHERE id = @userId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@userId", userId);

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        UserData user = new UserData();
                        user.Id = Convert.ToInt32(reader["id"]);
                        user.Username = reader["username"].ToString();
                        user.FullName = reader["fullname"].ToString();
                        user.Email = reader["email"].ToString();
                        user.UserType_ID = Convert.ToInt32(reader["usertype_id"]);

                        conn.Close();
                        return View(user);
                    }
                    else
                    {
                        conn.Close();
                        return View("Error");
                    }
                }
                catch (Exception ex)
                {
                    return View("Error");
                }
                finally
                {
                    conn.Close();
                }
            }

        }

        // Kiểm tra Username hợp lệ ( chưa tồn tại trong db)
        private bool IsUsernameValid(string username, int userId)
        {
            ConnectionMySQL connection = new ConnectionMySQL();

            string query = "SELECT COUNT(*) FROM users WHERE username = @username AND id != @userId";

            MySqlConnection conn = connection.ConnectionSQL();
            MySqlCommand cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@userId", userId);

            conn.Open();
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            conn.Close();

            return count == 0; // Nếu count = 0, username hợp lệ
        }

        // Kiểm tra Email hợp lệ (định dạng)
        private bool IsEmailValid(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUser(UserData user)
        {
            if (Session["usertype_id"] != null && (int)Session["usertype_id"] == 1)
            {
                if (ModelState.IsValid)
                {
                    if (IsUsernameValid(user.Username, user.Id) && IsEmailValid(user.Email))
                    {
                        ConnectionMySQL connection = new ConnectionMySQL();

                        string query = "UPDATE users " +
                                       "SET username = @username, fullname = @fullname, email = @email, usertype_id = @usertype_id " +
                                       "WHERE id = @userId";

                        MySqlConnection conn = connection.ConnectionSQL();
                        MySqlCommand cmd = new MySqlCommand(query, conn);

                        cmd.Parameters.AddWithValue("@username", user.Username);
                        cmd.Parameters.AddWithValue("@fullname", user.FullName);
                        cmd.Parameters.AddWithValue("@email", user.Email);
                        cmd.Parameters.AddWithValue("@usertype_id", user.UserType_ID);
                        cmd.Parameters.AddWithValue("@userId", user.Id);

                        try
                        {
                            conn.Open();
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                return RedirectToAction("Index");
                            }
                            else
                            {
                                ViewData["Error"] = "Không thể cập nhật thông tin người dùng.";
                                return View(user);
                            }
                        }
                        catch (Exception ex)
                        {
                            ViewData["Error"] = "Đã xảy ra lỗi trong quá trình cập nhật thông tin người dùng.";
                            return View(user);
                        }
                        finally
                        {
                            conn.Close();
                        }
                    }
                    else
                    {
                        ViewData["Error"] = "Tên người dùng đã tồn tại hoặc Email không hợp lệ.";
                        return View(user);
                    }
                }
                else
                {
                    ViewData["Error"] = "Dữ liệu không hợp lệ.";
                    return View(user);
                }
            }
            else
            {
                // Nếu usertype_id không phải 1, không cho phép chỉnh sửa thông tin
                ViewData["Error"] = "Không có quyền chỉnh sửa thông tin người dùng.";
                return View("Error");
            }
        }

        public ActionResult DeleteUser(int userId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                bool userHasScore = CheckIfUserHasScore(userId);

                if (userHasScore)
                {
                    ViewBag.HasScore = true;
                }
                else
                {
                    ViewBag.HasScore = false;
                }

                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "SELECT u.id, u.username, u.fullname, u.email, t.nametype " +
                               "FROM users u " +
                               "INNER JOIN user_types t ON u.usertype_id = t.id " + // Đã thêm khoảng trắng ở đây
                               "WHERE u.id = @userId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@userId", userId);

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        UserData user = new UserData();
                        user.Id = Convert.ToInt32(reader["id"]);
                        user.Username = reader["username"].ToString();
                        user.FullName = reader["fullname"].ToString();
                        user.Email = reader["email"].ToString();
                        user.NameType = reader["nametype"].ToString();

                        conn.Close();
                        return View(user);
                    }
                    else
                    {
                        conn.Close();
                        return View("Error");
                    }
                }
                catch (Exception ex)
                {
                    return View("Error");
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        // Kiểm tra nếu người dùng đã được lưu điểm trong db thì không được phép xóa
        private bool CheckIfUserHasScore(int userId)
        {
            ConnectionMySQL connection = new ConnectionMySQL();
            string query = "SELECT COUNT(*) FROM score WHERE user_id = @userId";

            MySqlConnection conn = connection.ConnectionSQL();
            MySqlCommand cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@userId", userId);

            conn.Open();
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            conn.Close();

            return count > 0;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int userId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                if (Session["usertype_id"] != null && (int)Session["usertype_id"] == 1)
                {
                    ConnectionMySQL connection = new ConnectionMySQL();
                    string query = "DELETE FROM users WHERE id = @userId";
                    MySqlConnection conn = connection.ConnectionSQL();
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@userId", userId);

                    try
                    {
                        conn.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return RedirectToAction("Index", "Users");
                        }
                        else
                        {
                            return View("Error");
                        }
                    }
                    catch (Exception ex)
                    {
                        return View("Error");
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
                else
                {
                    ViewData["Error"] = "Không có quyền xóa User!";
                    return View("Error");
                }
            }
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewData["Error"] = "Vui lòng nhập tên đăng nhập và mật khẩu.";
                return View();
            }

            ConnectionMySQL connection = new ConnectionMySQL();

            string query = "SELECT * FROM users WHERE username = @username";

            MySqlConnection conn = connection.ConnectionSQL();
            MySqlCommand cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@username", username);

            conn.Open();
            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                string hashedPasswordFromDB = reader["password"].ToString();

                // Verify the entered password with the hashed password from the database
                bool passwordMatch = BCrypt.Net.BCrypt.Verify(password, hashedPasswordFromDB);

                if (passwordMatch)
                {
                    int userType = Convert.ToInt32(reader["usertype_id"]);

                    if (userType == 1 || userType == 2)
                    {
                        // Đăng nhập thành công, lấy fullname từ cơ sở dữ liệu
                        string fullname = reader["fullname"].ToString();

                        // Lưu fullname vào Session
                        Session["fullname"] = fullname;
                        Session["usertype_id"] = userType;

                        conn.Close();
                        return RedirectToAction("Index", "Admin");
                    }
                    else
                    {
                        // Đăng nhập thành công nhưng usertype_id không phù hợp
                        ViewData["Error"] = "Tài khoản của bạn không đủ điều kiện đăng nhập!";
                        conn.Close();
                        return View();
                    }
                }
                else
                {
                    ViewData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
                    conn.Close();
                    return View();
                }
            }
            else
            {
                // Username not found
                ViewData["Error"] = "Tên đăng nhập hoặc mật khẩu không đúng.";
                conn.Close();
                return View();
            }
        }

        public ActionResult GetImage(string fullname)
        {
            string imageUrl = string.Empty;

            // Kết nối cơ sở dữ liệu và truy vấn để lấy đường dẫn hình ảnh dựa trên fullname
            try
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                string query = "SELECT image FROM users WHERE fullname = @fullname";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@fullname", fullname);

                conn.Open();
                var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    imageUrl = reader["image"].ToString();
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ (nếu cần)
            }

            if (!string.IsNullOrEmpty(imageUrl))
            {
                return File("D:\\flutterapp\\appvscode\\app_ontapkienthuc\\" + imageUrl, "image/*");
            }
            else
            {
                return File("D:\\flutterapp\\appvscode\\app_ontapkienthuc\\assets\\images\\avt.jpg", "lỗi ảnh");
            }
        }

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Users");
        }
    }
}
