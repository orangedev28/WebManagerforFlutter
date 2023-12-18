using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using WebQuanLyAppOnTap.Models;

namespace WebQuanLyAppOnTap.Controllers
{
    public class SubjectsController : Controller
    {
        // GET: Subjects
        public ActionResult Index()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT id, namesubject FROM subjects";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                List<Subject> subjectList = new List<Subject>();

                try
                {
                    conn.Open();
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            Subject subject = new Subject();
                            subject.Id = Convert.ToInt32(dataReader["id"]);
                            subject.NameSubject = dataReader["namesubject"].ToString();
                            subjectList.Add(subject);
                        }
                    }

                    return View(subjectList);
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

        public ActionResult Create()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Subject subject)
        {
            if (ModelState.IsValid)
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    // Kiểm tra tên môn học đã tồn tại chưa
                    string checkQuery = "SELECT COUNT(*) FROM subjects WHERE namesubject = @namesubject";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);

                    checkCmd.Parameters.AddWithValue("@namesubject", subject.NameSubject);

                    int subjectCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (subjectCount > 0)
                    {
                        ViewData["Error"] = "Môn học đã tồn tại!";
                        return View(subject);
                    }

                    string insertQuery = "INSERT INTO subjects (namesubject) VALUES (@namesubject)";
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);

                    // Thêm các tham số để tránh tấn công SQL Injection
                    insertCmd.Parameters.AddWithValue("@namesubject", subject.NameSubject);
                    insertCmd.ExecuteNonQuery(); // Thực hiện lệnh INSERT

                    return RedirectToAction("Index", "Subjects");
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
            return View(subject);
        }

        public ActionResult EditSubject(int subjectId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT id, namesubject " +
                               "FROM subjects " +
                               "WHERE id = @subjectId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@subjectId", subjectId);

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Subject subject = new Subject();
                        subject.Id = Convert.ToInt32(reader["id"]);
                        subject.NameSubject = reader["namesubject"].ToString();

                        conn.Close();
                        return View(subject);
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

        // Kiểm tra namesubject đã tồn tại chưa
        private bool IsNameSubjectValid(string namesubject, int subjectId)
        {
            ConnectionMySQL connection = new ConnectionMySQL();

            string query = "SELECT COUNT(*) FROM subjects WHERE namesubject = @namesubject AND id != @subjectId";

            MySqlConnection conn = connection.ConnectionSQL();
            MySqlCommand cmd = new MySqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@namesubject", namesubject);
            cmd.Parameters.AddWithValue("@subjectId", subjectId);

            conn.Open();
            int count = Convert.ToInt32(cmd.ExecuteScalar());
            conn.Close();

            return count == 0; // Nếu count = 0, namesubject hợp lệ
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSubject(Subject subject)
        {
            if (ModelState.IsValid)
            {
                if (IsNameSubjectValid(subject.NameSubject, subject.Id))
                {
                    ConnectionMySQL connection = new ConnectionMySQL();

                    string query = "UPDATE subjects SET namesubject = @namesubject WHERE id = @subjectId";

                    MySqlConnection conn = connection.ConnectionSQL();
                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@namesubject", subject.NameSubject);
                    cmd.Parameters.AddWithValue("@subjectId", subject.Id);

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
                            ViewData["Error"] = "Không thể cập nhật môn học!";
                            return View(subject);
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewData["Error"] = "Đã xảy ra lỗi trong quá trình cập nhật môn học!";
                        return View(subject);
                    }
                    finally
                    {
                        conn.Close();
                    }
                }
                else
                {
                    ViewData["Error"] = "Môn học đã tồn tại!";
                    return View(subject);
                }
            }
            else
            {
                ViewData["Error"] = "Dữ liệu không hợp lệ!";
                return View(subject);
            }
        }

        public ActionResult DeleteSubject(int subjectId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "SELECT id, namesubject FROM subjects WHERE id = @subjectId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@subjectId", subjectId);

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Subject subject = new Subject();
                        subject.Id = Convert.ToInt32(reader["id"]);
                        subject.NameSubject = reader["namesubject"].ToString();

                        conn.Close();
                        return View(subject);
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
        public ActionResult DeleteConfirmed(int subjectId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "DELETE FROM subjects WHERE id = @subjectId";
                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@subjectId", subjectId);

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