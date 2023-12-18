using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using OfficeOpenXml;
using WebQuanLyAppOnTap.Models;

namespace WebQuanLyAppOnTap.Controllers
{
    public class QuestionsController : Controller
    {
        // GET: Questions
        public ActionResult Index(int? selectedQuizId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                List<Quiz> quizList = GetQuizzesFromDatabase();
                ViewBag.Quizzes = new SelectList(quizList, "Id", "NameQuiz", selectedQuizId);

                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT u.id, u.question, u.answer1, u.answer2, u.answer3, u.answer4, u.correctanswer, u.quiz_id, t.namequiz " +
                   "FROM questions u " +
                   "INNER JOIN quizzes t ON u.quiz_id = t.id";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                List<Question> questionList = new List<Question>();

                try
                {
                    conn.Open();
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            Question question = new Question();
                            question.Id = Convert.ToInt32(dataReader["id"]);
                            question.QuestionContent = dataReader["question"].ToString();
                            question.Answer1 = dataReader["answer1"].ToString();
                            question.Answer2 = dataReader["answer2"].ToString();
                            question.Answer3 = dataReader["answer3"].ToString();
                            question.Answer4 = dataReader["answer4"].ToString();
                            question.CorrectAnswer = dataReader["correctanswer"].ToString();
                            question.Quiz_Id = Convert.ToInt32(dataReader["quiz_id"]);
                            question.NameQuiz = dataReader["namequiz"].ToString();
                            questionList.Add(question);
                        }
                    }

                    if (selectedQuizId.HasValue)
                    {
                        questionList = questionList.Where(q => q.Quiz_Id == selectedQuizId).ToList();
                    }

                    return View(questionList);
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
                List<Quiz> quizList = GetQuizzesFromDatabase();

                List<SelectListItem> subjectListItems = quizList.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.NameQuiz
                }).ToList();

                ViewBag.Quizzes = subjectListItems;

                var defaultSubjectId = quizList.FirstOrDefault()?.Id;
                ViewBag.DefaultSubjectId = defaultSubjectId;

                return View();
            }
        }

        private List<Quiz> GetQuizzesFromDatabase()
        {
            List<Quiz> quizList = new List<Quiz>();

            try
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                conn.Open();

                string query = "SELECT * FROM quizzes";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (MySqlDataReader dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        Quiz quiz = new Quiz();
                        quiz.Id = Convert.ToInt32(dataReader["id"]);
                        quiz.NameQuiz = dataReader["namequiz"].ToString();
                        quizList.Add(quiz);
                    }
                }
                conn.Close();
            }
            catch (Exception ex)
            {

            }

            return quizList;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Question question)
        {
            if (ModelState.IsValid)
            {
                if (IsQuestionExists(question.Quiz_Id, question.QuestionContent, question.CorrectAnswer))
                {
                    ViewData["Error"] = "Câu hỏi đã tồn tại cho bài kiểm tra này!";
                    PopulateDropdownList();
                    return View(question);
                }

                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                string query = "INSERT INTO questions (quiz_id, question, answer1, answer2, answer3, answer4, correctanswer) " +
                               "VALUES (@quizId, @question, @answer1, @answer2, @answer3, @answer4, @correctAnswer)";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@quizId", question.Quiz_Id);
                cmd.Parameters.AddWithValue("@question", question.QuestionContent);
                cmd.Parameters.AddWithValue("@answer1", question.Answer1);
                cmd.Parameters.AddWithValue("@answer2", question.Answer2);
                cmd.Parameters.AddWithValue("@answer3", question.Answer3);
                cmd.Parameters.AddWithValue("@answer4", question.Answer4);
                cmd.Parameters.AddWithValue("@correctAnswer", question.CorrectAnswer);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
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

            PopulateDropdownList();
            return View(question);
        }

        private bool IsQuestionExists(int quizId, string questionContent, string correctAnswer)
        {
            ConnectionMySQL connection = new ConnectionMySQL();
            MySqlConnection conn = connection.ConnectionSQL();

            string query = "SELECT COUNT(*) FROM questions WHERE quiz_id = @quizId AND question = @questionContent AND correctanswer = @correctAnswer";

            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@quizId", quizId);
            cmd.Parameters.AddWithValue("@questionContent", questionContent);
            cmd.Parameters.AddWithValue("@correctAnswer", correctAnswer);

            try
            {
                conn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());

                return count > 0;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        private void PopulateDropdownList()
        {
            List<Quiz> quizList = GetQuizzesFromDatabase();
            List<SelectListItem> subjectListItems = quizList.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.NameQuiz
            }).ToList();

            ViewBag.Quizzes = subjectListItems;

            var defaultSubjectId = quizList.FirstOrDefault()?.Id;
            ViewBag.DefaultSubjectId = defaultSubjectId;
        }

        public ActionResult CreateList()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                List<Quiz> quizList = GetQuizzesFromDatabase();

                List<SelectListItem> subjectListItems = quizList.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.NameQuiz
                }).ToList();

                ViewBag.Quizzes = subjectListItems;

                var defaultSubjectId = quizList.FirstOrDefault()?.Id;
                ViewBag.DefaultSubjectId = defaultSubjectId;

                return View();
            }
        }

        [HttpPost]
        public async Task<ActionResult> UploadQuestions(HttpPostedFileBase file, int quiz_Id)
        {
            if (file != null && file.ContentLength > 0)
            {
                if (Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    // Đọc câu hỏi từ Excel
                    List<Question> questions = await ReadQuestionsFromExcel(file.InputStream, quiz_Id);

                    if (questions.Count == 0)
                    {
                        TempData["QuestionExistsError"] = "Bai kiem tra da ton tai cau hoi trong file Excel!";
                        return RedirectToAction("CreateList");
                    }

                    SaveQuestionsToDatabase(questions, quiz_Id);
                }
            }

            return RedirectToAction("Index");
        }

        private async Task<List<Question>> ReadQuestionsFromExcel(Stream inputStream, int quizId)
        {
            List<Question> questions = new List<Question>();

            using (var stream = new MemoryStream())
            {
                await inputStream.CopyToAsync(stream);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.First();
                    int rowCount = worksheet.Dimension.Rows;

                    for (int i = 2; i <= rowCount; i++)
                    {
                        string questionContent = worksheet.Cells[i, 1].Text;
                        string answer1 = worksheet.Cells[i, 2].Text;
                        string answer2 = worksheet.Cells[i, 3].Text;
                        string answer3 = worksheet.Cells[i, 4].Text;
                        string answer4 = worksheet.Cells[i, 5].Text;
                        string correctAnswer = worksheet.Cells[i, 6].Text;

                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(questionContent) &&
                            string.IsNullOrWhiteSpace(answer1) &&
                            string.IsNullOrWhiteSpace(answer2) &&
                            string.IsNullOrWhiteSpace(answer3) &&
                            string.IsNullOrWhiteSpace(answer4) &&
                            string.IsNullOrWhiteSpace(correctAnswer))
                        {
                            continue;
                        }

                        // Check if the question already exists
                        if (IsQuestionExists(quizId, questionContent, correctAnswer))
                        {
                            // Handle duplicate question
                            return new List<Question>(); // Return an empty list to indicate an issue
                        }

                        // Retrieve SubjectId based on the selected quiz
                        int subjectId = GetSubjectIdFromQuiz(quizId);

                        Question question = new Question
                        {
                            QuestionContent = questionContent,
                            Answer1 = answer1,
                            Answer2 = answer2,
                            Answer3 = answer3,
                            Answer4 = answer4,
                            CorrectAnswer = correctAnswer,
                            Quiz_Id = quizId,
                            Subject_Id = subjectId
                        };

                        questions.Add(question);
                    }
                }
            }

            return questions;
        }


        private void SaveQuestionsToDatabase(List<Question> questions, int quizId)
        {
            ConnectionMySQL connection = new ConnectionMySQL();
            MySqlConnection conn = connection.ConnectionSQL();

            conn.Open();

            // Retrieve subject_id based on the selected quiz outside the loop
            int subjectId = GetSubjectIdFromQuiz(quizId);

            foreach (var question in questions)
            {
                // Check if the question already exists
                if (IsQuestionExists(quizId, question.QuestionContent, question.CorrectAnswer))
                {
                    // Handle duplicate question
                    continue; // Skip this iteration and move to the next question
                }
                if (ShouldUpdateHotQuestion(conn, subjectId, question.QuestionContent, question.CorrectAnswer))
                {
                    UpdateHotQuestion(conn, subjectId, question.QuestionContent, question.CorrectAnswer);
                }
                string query = "INSERT INTO questions (quiz_id, subject_id, question, answer1, answer2, answer3, answer4, correctanswer) " +
                               "VALUES (@quizId, @subjectId, @question, @answer1, @answer2, @answer3, @answer4, @correctAnswer)";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@quizId", quizId);
                cmd.Parameters.AddWithValue("@subjectId", subjectId); // subject_id lấy từ liên kết với quiz_id
                cmd.Parameters.AddWithValue("@question", question.QuestionContent);
                cmd.Parameters.AddWithValue("@answer1", question.Answer1);
                cmd.Parameters.AddWithValue("@answer2", question.Answer2);
                cmd.Parameters.AddWithValue("@answer3", question.Answer3);
                cmd.Parameters.AddWithValue("@answer4", question.Answer4);
                cmd.Parameters.AddWithValue("@correctAnswer", question.CorrectAnswer);

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }

            conn.Close();
        }

        private int GetSubjectIdFromQuiz(int quizId)
        {
            ConnectionMySQL connection = new ConnectionMySQL();
            MySqlConnection conn = connection.ConnectionSQL();

            string query = "SELECT subject_id FROM quizzes WHERE id = @quizId";

            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@quizId", quizId);

            try
            {
                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                
            }
            finally
            {
                conn.Close();
            }

            return 0;
        }

        private bool ShouldUpdateHotQuestion(MySqlConnection conn, int subjectId, string questionContent, string correctAnswer)
        {
            string query = "SELECT COUNT(*) FROM questions WHERE subject_id = @subjectId AND question = @questionContent AND correctanswer = @correctAnswer AND hotquestion = 1";

            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@subjectId", subjectId);
            cmd.Parameters.AddWithValue("@questionContent", questionContent);
            cmd.Parameters.AddWithValue("@correctAnswer", correctAnswer);

            try
            {
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count == 0;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void UpdateHotQuestion(MySqlConnection conn, int subjectId, string questionContent, string correctAnswer)
        {
            string query = "SELECT COUNT(*) FROM questions WHERE subject_id = @subjectId AND question = @questionContent AND correctanswer = @correctAnswer";

            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@subjectId", subjectId);
            cmd.Parameters.AddWithValue("@questionContent", questionContent);
            cmd.Parameters.AddWithValue("@correctAnswer", correctAnswer);

            try
            {
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count > 0)
                {
                    string updateQuery = "UPDATE questions SET hotquestion = 1 WHERE subject_id = @subjectId AND question = @questionContent AND correctanswer = @correctAnswer";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@subjectId", subjectId);
                    updateCmd.Parameters.AddWithValue("@questionContent", questionContent);
                    updateCmd.Parameters.AddWithValue("@correctAnswer", correctAnswer);

                    updateCmd.ExecuteNonQuery();
                }

                return;
            }
            catch (Exception ex)
            {

            }
        }

        public ActionResult EditQuestion(int questionId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "User");

            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT id, question, answer1, answer2, answer3, answer4, correctanswer, hotquestion, quiz_id " +
                    "FROM questions " +
                    "WHERE id = @questionId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@questionId", questionId);

                List<Quiz> quizzes = GetQuizzesFromDatabase();

                List<SelectListItem> quizListItems = quizzes.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.NameQuiz
                }).ToList();

                ViewBag.Quizzes = quizListItems;

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Question question = new Question();
                        question.Id = Convert.ToInt32(reader["id"]);
                        question.QuestionContent = reader["question"].ToString();
                        question.Answer1 = reader["answer1"].ToString();
                        question.Answer2 = reader["answer2"].ToString();
                        question.Answer3 = reader["answer3"].ToString();
                        question.Answer4 = reader["answer4"].ToString();
                        question.CorrectAnswer = reader["correctanswer"].ToString();
                        question.Quiz_Id = Convert.ToInt32(reader["quiz_id"]);

                        if (!reader.IsDBNull(reader.GetOrdinal("hotquestion")))
                        {
                            question.HotQuestion = Convert.ToBoolean(reader["hotquestion"]);
                        }
                        else
                        {
                            question.HotQuestion = null;
                        }

                        conn.Close();
                        return View(question);
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
                finally {
                    conn.Close();
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditQuestion(Question question)
        {
            if (ModelState.IsValid)
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(*) FROM questions WHERE quiz_id = @quizId AND question = @questionContent AND correctanswer = @correctAnswer AND id != @questionId";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);

                    checkCmd.Parameters.AddWithValue("@questionContent", question.QuestionContent);
                    checkCmd.Parameters.AddWithValue("@correctAnswer", question.CorrectAnswer);
                    checkCmd.Parameters.AddWithValue("@quizId", question.Quiz_Id);
                    checkCmd.Parameters.AddWithValue("@questionId", question.Id);

                    int questionCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (questionCount > 0)
                    {
                        ViewData["Error"] = "Câu hỏi đã tồn tại cho bài kiểm tra này!";
                        ViewBag.Quizzes = GetQuizzesFromDatabase()
                            .Select(s => new SelectListItem
                            {
                                Value = s.Id.ToString(),
                                Text = s.NameQuiz
                            }).ToList();
                        return View(question);
                    }

                    string updateQuery = "UPDATE questions SET question = @questionContent, answer1 = @answer1, answer2 = @answer2, answer3 = @answer3, answer4 = @answer4, correctanswer = @correctAnswer, hotquestion = @hotQuestion, quiz_id = @quizId WHERE id = @questionId";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);

                    updateCmd.Parameters.AddWithValue("@questionContent", question.QuestionContent);
                    updateCmd.Parameters.AddWithValue("@answer1", question.Answer1);
                    updateCmd.Parameters.AddWithValue("@answer2", question.Answer2);
                    updateCmd.Parameters.AddWithValue("@answer3", question.Answer3);
                    updateCmd.Parameters.AddWithValue("@answer4", question.Answer4);
                    updateCmd.Parameters.AddWithValue("@correctAnswer", question.CorrectAnswer);
                    updateCmd.Parameters.AddWithValue("@hotQuestion", question.HotQuestion ?? (object)DBNull.Value);
                    updateCmd.Parameters.AddWithValue("@quizId", question.Quiz_Id);
                    updateCmd.Parameters.AddWithValue("@questionId", question.Id);

                    updateCmd.ExecuteNonQuery();

                    return RedirectToAction("Index", "Questions");
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

            // If ModelState is not valid, return the view with validation errors
            List<Quiz> quizzes = GetQuizzesFromDatabase();

            List<SelectListItem> quizListItems = quizzes.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.NameQuiz
            }).ToList();

            ViewBag.Quizzes = quizListItems;

            return View(question);
        }

        public ActionResult DeleteQuestion(int questionId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "SELECT u.id, u.question, u.answer1, u.answer2, u.answer3, u.answer4, u.correctanswer, u.hotquestion, t.namequiz " +
                               "FROM questions u " +
                               "INNER JOIN quizzes t ON u.quiz_id = t.id " +
                               "WHERE u.id = @questionId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@questionId", questionId);

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Question question = new Question();
                        question.Id = Convert.ToInt32(reader["id"]);
                        question.QuestionContent = reader["question"].ToString();
                        question.Answer1 = reader["answer1"].ToString();
                        question.Answer2 = reader["answer2"].ToString();
                        question.Answer3 = reader["answer3"].ToString();
                        question.Answer4 = reader["answer4"].ToString();
                        question.CorrectAnswer = reader["correctanswer"].ToString();
                        if (!reader.IsDBNull(reader.GetOrdinal("hotquestion")))
                        {
                            question.HotQuestion = Convert.ToBoolean(reader["hotquestion"]);
                        }
                        else
                        {
                            question.HotQuestion = null;
                        }
                        question.NameQuiz = reader["namequiz"].ToString();

                        conn.Close();
                        return View(question);
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

        public ActionResult DeleteConfirmed(int questionId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "DELETE FROM questions WHERE id = @questionId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@questionId", questionId);

                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return RedirectToAction("Index", "Questions");
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