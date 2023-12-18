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
                   "LEFT JOIN subjects t ON u.subject_id = t.id";

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
                    //insertCmd.Parameters.AddWithValue("@subject_id", quiz.Subject_ID ?? (object)DBNull.Value);

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

                        if (reader["subject_id"] != DBNull.Value)
                        {
                            quiz.Subject_ID = Convert.ToInt32(reader["subject_id"]);
                        }
                        else
                        {
                            quiz.Subject_ID = null; // Handle DBNull.Value as null
                        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditQuiz(Quiz quiz)
        {
            if (ModelState.IsValid)
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    // Check if the updated quiz already exists for the selected subject
                    string checkQuery = "SELECT COUNT(*) FROM quizzes WHERE (namequiz = @namequiz) AND subject_id = @subject_id AND id != @quizId";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@namequiz", quiz.NameQuiz);
                    checkCmd.Parameters.AddWithValue("@subject_id", quiz.Subject_ID);
                    checkCmd.Parameters.AddWithValue("@quizId", quiz.Id);

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

                    // Update the quiz
                    string updateQuery = "UPDATE quizzes SET namequiz = @namequiz, subject_id = @subject_id WHERE id = @quizId";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);

                    // Add parameters to prevent SQL Injection
                    updateCmd.Parameters.AddWithValue("@namequiz", quiz.NameQuiz);
                    updateCmd.Parameters.AddWithValue("@subject_id", quiz.Subject_ID);
                    updateCmd.Parameters.AddWithValue("@quizId", quiz.Id);

                    updateCmd.ExecuteNonQuery();

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

            // If ModelState is not valid, return to the Edit view with the quiz model
            List<Subject> subjects = GetSubjectsFromDatabase();

            List<SelectListItem> subjectListItems = subjects.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.NameSubject
            }).ToList();

            ViewBag.Subjects = subjectListItems;

            return View(quiz);
        }

        public ActionResult DeleteQuiz(int quizId)
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
               "LEFT JOIN subjects t ON u.subject_id = t.id " +
               "WHERE u.id = @quizId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@quizId", quizId);

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Quiz quiz = new Quiz();
                        quiz.Id = Convert.ToInt32(reader["id"]);
                        quiz.NameQuiz = reader["namequiz"].ToString();
                        if (!reader.IsDBNull(reader.GetOrdinal("dateupload")))
                        {
                            // Lấy giá trị kiểu DateTime từ cột dateupload
                            quiz.DateUpload = reader.GetDateTime(reader.GetOrdinal("dateupload"));
                        }
                        else
                        {
                            // Xử lý nếu giá trị là NULL
                            quiz.DateUpload = null;
                        }
                        quiz.NameSubject = reader["namesubject"].ToString();
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

        public ActionResult DeleteConfirmed(int quizId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "DELETE FROM quizzes WHERE id = @quizId";
                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@quizId", quizId);

                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Cập nhật lại danh sách câu hỏi (những câu trùng hotquestion) sau khi xóa bài
                        UpdateQuestionList();
                        return RedirectToAction("Index", "Quizzes");
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

        public ActionResult UpdateQuestionList()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    string updateQuery = @"
                UPDATE questions AS q
                SET hotquestion = CASE 
                                    WHEN hotquestion = 1 AND q.id = (
                                        SELECT id 
                                        FROM questions AS q2
                                        WHERE q.question = q2.question
                                            AND q.correctanswer = q2.correctanswer
                                            AND q.subject_id = q2.subject_id
                                            AND q2.hotquestion = 1
                                        LIMIT 1
                                    ) THEN 1 
                                    ELSE NULL 
                                  END
            ";

                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {

                }
                finally
                {
                    conn.Close();
                }

                return RedirectToAction("Index");
            }
        }
    }
}