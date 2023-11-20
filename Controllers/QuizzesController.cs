using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebQuanLyAppOnTap.Models;

namespace WebQuanLyAppOnTap.Controllers
{
    public class QuizzesController : Controller
    {
        // GET: Quizzes
        public ActionResult Index()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT u.id, u.namequiz, u.dateupload, t.namesubject " +
                   "FROM quizzes u " +
                   "INNER JOIN subjects t ON u.subject_id = t.id";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                List<Quiz> quizList = new List<Quiz>();

                try
                {
                    conn.Open();
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            Quiz quiz = new Quiz();
                            quiz.Id = Convert.ToInt32(dataReader["id"]);
                            quiz.NameQuiz = dataReader["namequiz"].ToString();
                            if (!dataReader.IsDBNull(dataReader.GetOrdinal("dateupload")))
                            {
                                // Lấy giá trị kiểu DateTime từ cột dateupload
                                quiz.DateUpload = dataReader.GetDateTime(dataReader.GetOrdinal("dateupload"));
                            }
                            else
                            {
                                // Xử lý nếu giá trị là NULL
                                quiz.DateUpload = null;
                            }
                            quiz.NameSubject = dataReader["namesubject"].ToString();
                            quizList.Add(quiz);
                        }
                    }

                    return View(quizList);
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

                return View(new Quiz());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Quiz quiz)
        {
            if (ModelState.IsValid)
            {
                quiz.DateUpload = DateTime.Now; // Ngày giờ hiện tại

                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    // Kiểm tra bài kiểm tra đã tồn tại với môn học này chưa
                    string checkQuery = "SELECT COUNT(*) FROM quizzes WHERE (namequiz = @namequiz) AND subject_id = @subject_id";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@namequiz", quiz.NameQuiz);
                    checkCmd.Parameters.AddWithValue("@subject_id", quiz.Subject_ID);

                    int quizCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (quizCount > 0)
                    {
                        ViewData["Error"] = "Bài kiểm tra đã tồn tại với môn học đã chọn!";
                        ViewBag.Subjects = GetSubjectsFromDatabase()
                            .Select(s => new SelectListItem
                            {
                                Value = s.Id.ToString(),
                                Text = s.NameSubject
                            }).ToList();
                        return View(quiz);
                    }

                    // Nếu bài kiểm tra chưa tồn tại
                    string insertQuery = "INSERT INTO quizzes (namequiz, dateupload, subject_id) VALUES (@namequiz, @dateupload, @subject_id)";
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);

                    // Thêm các tham số để tránh tấn công SQL Injection
                    insertCmd.Parameters.AddWithValue("@namequiz", quiz.NameQuiz);
                    insertCmd.Parameters.AddWithValue("@dateupload", quiz.DateUpload);
                    insertCmd.Parameters.AddWithValue("@subject_id", quiz.Subject_ID);

                    insertCmd.ExecuteNonQuery();

                    return RedirectToAction("Index", "Quizzes");
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

            return View(quiz);
        }

        public ActionResult EditQuiz(int quizId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT id, namequiz, subject_id " +
                               "FROM quizzes " +
                               "WHERE id = @quizId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@quizId", quizId);

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
                        Quiz quiz = new Quiz();
                        quiz.Id = Convert.ToInt32(reader["id"]);
                        quiz.NameQuiz = reader["namequiz"].ToString();
                        quiz.Subject_ID = Convert.ToInt32(reader["subject_id"]);

                        conn.Close();
                        return View(quiz);
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
    }
}