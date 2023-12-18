using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebQuanLyAppOnTap.Models;

namespace WebQuanLyAppOnTap.Controllers
{
    public class VideosController : Controller
    {
        // GET: Videos
        public ActionResult Index()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT u.id, u.namevideo, u.linkvideo, u.dateupload, t.namesubject " +
                               "FROM videos u " +
                               "LEFT JOIN subjects t ON u.subject_id = t.id";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                List<Video> videoList = new List<Video>();

                try
                {
                    conn.Open();
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            Video video = new Video();
                            video.Id = Convert.ToInt32(dataReader["id"]);
                            video.NameVideo = dataReader["namevideo"].ToString();
                            video.LinkVideo = dataReader["linkvideo"].ToString();
                            video.DateUpload = dataReader.GetDateTime(dataReader.GetOrdinal("dateupload"));
                            video.NameSubject = dataReader["namesubject"].ToString();
                            videoList.Add(video);
                        }
                    }

                    return View(videoList);
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

        private List<Subject> GetSubjectsFromDatabase()
        {
            List<Subject> subjects = new List<Subject>();

            try
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                conn.Open();

                string query = "SELECT * FROM subjects";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (MySqlDataReader dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        Subject subject = new Subject();
                        subject.Id = Convert.ToInt32(dataReader["id"]);
                        subject.NameSubject = dataReader["namesubject"].ToString();
                        subjects.Add(subject);
                    }
                }

                conn.Close();
            }
            catch (Exception ex)
            {

            }

            return subjects;
        }

        public ActionResult Create()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                // Lấy danh sách môn học từ cơ sở dữ liệu
                List<Subject> subjects = GetSubjectsFromDatabase();

                // Chuyển danh sách môn học thành danh sách SelectListItem
                List<SelectListItem> subjectListItems = subjects.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.NameSubject
                }).ToList();

                ViewBag.Subjects = subjectListItems;

                return View(new Video());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Video video)
        {
            if (ModelState.IsValid)
            {
                video.DateUpload = DateTime.Now; // Ngày giờ hiện tại

                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    // Kiểm tra bài kiểm tra đã tồn tại với môn học này chưa
                    string checkQuery = "SELECT COUNT(*) FROM videos WHERE (namevideo = @nameVideo OR linkvideo = @linkVideo) AND subject_id = @subjectId";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@nameVideo", video.NameVideo);
                    checkCmd.Parameters.AddWithValue("@linkVideo", video.LinkVideo);
                    checkCmd.Parameters.AddWithValue("@subjectId", video.Subject_ID);

                    int videoCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (videoCount > 0)
                    {
                        ViewData["Error"] = "Video đã tồn tại với môn học đã chọn!";
                        ViewBag.Subjects = GetSubjectsFromDatabase()
                            .Select(s => new SelectListItem
                            {
                                Value = s.Id.ToString(),
                                Text = s.NameSubject
                            }).ToList();
                        return View(video);
                    }

                    // Nếu video chưa tồn tại
                    string insertQuery = "INSERT INTO videos (namevideo, linkvideo, dateupload, subject_id) VALUES (@nameVideo, @linkVideo, @dateUpload, @subjectId)";
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);

                    // Thêm các tham số để tránh tấn công SQL Injection
                    insertCmd.Parameters.AddWithValue("@nameVideo", video.NameVideo);
                    insertCmd.Parameters.AddWithValue("@linkVideo", video.LinkVideo);
                    insertCmd.Parameters.AddWithValue("@dateUpload", video.DateUpload);
                    insertCmd.Parameters.AddWithValue("@subjectId", video.Subject_ID);
                    //insertCmd.Parameters.AddWithValue("@subject_id", quiz.Subject_ID ?? (object)DBNull.Value);

                    insertCmd.ExecuteNonQuery();

                    return RedirectToAction("Index", "Videos");
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

            return View(video);
        }

        public ActionResult EditVideo(int videoId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT id, namevideo, linkvideo, subject_id " +
                               "FROM videos " +
                               "WHERE id = @videoId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@videoId", videoId);

                List<Subject> subjects = GetSubjectsFromDatabase();

                List<SelectListItem> subjectListItems = subjects.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.NameSubject
                }).ToList();

                ViewBag.Subjects = subjectListItems;

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Video video = new Video();
                        video.Id = Convert.ToInt32(reader["id"]);
                        video.NameVideo = reader["namevideo"].ToString();
                        video.LinkVideo = reader["linkvideo"].ToString();

                        if (reader["subject_id"] != DBNull.Value)
                        {
                            video.Subject_ID = Convert.ToInt32(reader["subject_id"]);
                        }
                        else
                        {
                            video.Subject_ID = null; // Handle DBNull.Value as null
                        }

                        conn.Close();
                        return View(video);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditVideo(Video video)
        {
            if (ModelState.IsValid)
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    // Check if the updated quiz already exists for the selected subject
                    string checkQuery = "SELECT COUNT(*) FROM videos WHERE (namevideo = @nameVideo OR linkvideo = @linkVideo) AND subject_id = @subjectId AND id != @videoId";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@nameVideo", video.NameVideo);
                    checkCmd.Parameters.AddWithValue("@linkVideo", video.LinkVideo);
                    checkCmd.Parameters.AddWithValue("@subjectId", video.Subject_ID);
                    checkCmd.Parameters.AddWithValue("@videoId", video.Id);

                    int videoCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (videoCount > 0)
                    {
                        ViewData["Error"] = "Video bài giảng đã tồn tại với môn học đã chọn!";
                        ViewBag.Subjects = GetSubjectsFromDatabase()
                            .Select(s => new SelectListItem
                            {
                                Value = s.Id.ToString(),
                                Text = s.NameSubject
                            }).ToList();
                        return View(video);
                    }

                    video.DateUpload = DateTime.Now;

                    // Update the quiz
                    string updateQuery = "UPDATE videos SET namevideo = @nameVideo,linkvideo = @linkVideo, dateupload = @dateUpload, subject_id = @subjectId WHERE id = @videoId";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);

                    // Add parameters to prevent SQL Injection
                    updateCmd.Parameters.AddWithValue("@nameVideo", video.NameVideo);
                    updateCmd.Parameters.AddWithValue("@linkVideo", video.LinkVideo);
                    updateCmd.Parameters.AddWithValue("@dateUpload", video.DateUpload);
                    updateCmd.Parameters.AddWithValue("@subjectId", video.Subject_ID);
                    updateCmd.Parameters.AddWithValue("@videoId", video.Id);

                    updateCmd.ExecuteNonQuery();

                    return RedirectToAction("Index", "Videos");
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

            // If ModelState is not valid, return to the Edit view with the quiz model
            List<Subject> subjects = GetSubjectsFromDatabase();

            List<SelectListItem> subjectListItems = subjects.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.NameSubject
            }).ToList();

            ViewBag.Subjects = subjectListItems;

            return View(video);
        }

        public ActionResult DeleteVideo(int videoId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "SELECT u.id, u.namevideo, u.linkvideo, u.dateupload, t.namesubject " +
               "FROM videos u " +
               "LEFT JOIN subjects t ON u.subject_id = t.id " +
               "WHERE u.id = @videoId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@videoId", videoId);

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Video video = new Video();
                        video.Id = Convert.ToInt32(reader["id"]);
                        video.NameVideo = reader["namevideo"].ToString();
                        video.LinkVideo = reader["linkvideo"].ToString();
                        video.DateUpload = reader.GetDateTime(reader.GetOrdinal("dateupload"));
                        video.NameSubject = reader["namesubject"].ToString();
                        conn.Close();
                        return View(video);
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

        public ActionResult DeleteConfirmed(int videoId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "DELETE FROM videos WHERE id = @videoId";
                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@videoId", videoId);

                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return RedirectToAction("Index", "Videos");
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
        }
    }
}