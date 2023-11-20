using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebQuanLyAppOnTap.Models;
using NPOI.XWPF.UserModel;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace WebQuanLyAppOnTap.Controllers
{
    public class QuestionsController : Controller
    {
        // GET: Questions
        public ActionResult Index()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT u.id, u.question, u.answer1, u.answer2, u.answer3, u.answer4, u.correctanswer, t.namequiz " +
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
                            question.NameQuiz = dataReader["namequiz"].ToString();
                            questionList.Add(question);
                        }
                    }

                    return View(questionList);
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

        public ActionResult Create()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                List<Quiz> quizList = GetQuizzesFromDatabase();

                // Chuyển danh sách bài kiểm tra thành danh sách SelectListItem
                List<SelectListItem> subjectListItems = quizList.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(), // s.Id là quizid
                    Text = s.NameQuiz // s.Name là namequiz
                }).ToList();

                ViewBag.Quizzes = subjectListItems; // Gửi danh sách bài kiểm tra đến View

                // Set the default selected value to the ID of the first quiz
                var defaultSubjectId = quizList.FirstOrDefault()?.Id; // Get the ID of the first quiz
                ViewBag.DefaultSubjectId = defaultSubjectId;

                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Question question)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem câu hỏi đã tồn tại cho bài kiểm tra này chưa
                if (IsQuestionExists(question.Quiz_Id, question.QuestionContent))
                {
                    ViewData["Error"] = "Câu hỏi đã tồn tại cho bài kiểm tra này.";
                    PopulateDropdownList();
                    return View(question);
                }

                // Nếu câu hỏi chưa tồn tại, thêm vào cơ sở dữ liệu
                AddQuestionToDatabase(question);

                return RedirectToAction("Index");
            }

            // Nếu ModelState không hợp lệ, tái tạo danh sách rơi xuống và hiển thị trang Create
            PopulateDropdownList();
            return View(question);
        }

        private bool IsQuestionExists(int quizId, string questionContent)
        {
            // Kiểm tra xem câu hỏi đã tồn tại cho bài kiểm tra này chưa
            ConnectionMySQL connection = new ConnectionMySQL();
            MySqlConnection conn = connection.ConnectionSQL();

            string query = "SELECT COUNT(*) FROM questions WHERE quiz_id = @quizId AND question = @questionContent";

            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@quizId", quizId);
            cmd.Parameters.AddWithValue("@questionContent", questionContent);

            try
            {
                conn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());

                return count > 0;
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        private void AddQuestionToDatabase(Question question)
        {
            // Thêm câu hỏi vào cơ sở dữ liệu
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
                // Handle exceptions
            }
            finally
            {
                conn.Close();
            }
        }

        private void PopulateDropdownList()
        {
            // Hàm này để tải lại danh sách bài kiểm tra cho dropdownlist nếu cần
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


        [HttpPost]
        public ActionResult UploadQuestions(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                if (Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    // Đọc câu hỏi từ PDF
                    List<Question> questions = ReadQuestionsFromPdf(file.InputStream);
                    SaveQuestionsToDatabase(questions);
                }
                else if (Path.GetExtension(file.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    // Đọc câu hỏi từ Word
                    List<Question> questions = ReadQuestionsFromWord(file.InputStream);
                    SaveQuestionsToDatabase(questions);
                }
            }

            return RedirectToAction("Index");
        }

        private List<Question> ReadQuestionsFromPdf(Stream inputStream)
        {
            List<Question> questions = new List<Question>();

            using (PdfReader pdfReader = new PdfReader(inputStream))
            using (PdfDocument pdfDoc = new PdfDocument(pdfReader))
            {
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    var page = pdfDoc.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    string text = PdfTextExtractor.GetTextFromPage(page, strategy);

                    // Process and extract questions from the PDF text
                    // ...

                    // Add the question to the list
                    Question question = new Question
                    {
                        // Set the properties of the question from the PDF text
                    };
                    questions.Add(question);
                }
            }

            return questions;
        }



        private List<Question> ReadQuestionsFromWord(Stream inputStream)
        {
            List<Question> questions = new List<Question>();

            using (var doc = new XWPFDocument(inputStream))
            {
                foreach (var paragraph in doc.Paragraphs)
                {
                    // Đọc văn bản từ đoạn văn
                    string text = paragraph.ParagraphText;

                    // Xử lý và trích xuất câu hỏi và đáp án từ văn bản
                    ProcessQuestionText(text, questions);
                }
            }

            return questions;
        }

        private void ProcessQuestionText(string text, List<Question> questions)
        {
            // Xử lý và trích xuất câu hỏi và đáp án từ văn bản
            // Giả sử mỗi câu hỏi được phân tách bằng dấu chấm câu
            string[] parts = text.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 1)
            {
                // Phần đầu tiên là câu hỏi
                string questionContent = parts[0].Trim();

                // Các phần còn lại là đáp án, giả sử lưu trữ đáp án trong mảng
                string[] answers = parts.Skip(1).Select(answer => answer.Trim()).ToArray();

                // Tạo đối tượng Question và thêm vào danh sách
                Question question = new Question
                {
                    QuestionContent = questionContent,
                    Answer1 = answers.Length > 0 ? answers[0] : null,
                    Answer2 = answers.Length > 1 ? answers[1] : null,
                    Answer3 = answers.Length > 2 ? answers[2] : null,
                    Answer4 = answers.Length > 3 ? answers[3] : null,
                    
                    // Các thuộc tính khác của câu hỏi
                };

                questions.Add(question);
            }
        }


        private void SaveQuestionsToDatabase(List<Question> questions)
        {
            ConnectionMySQL connection = new ConnectionMySQL();
            MySqlConnection conn = connection.ConnectionSQL();

            conn.Open();

            foreach (var question in questions)
            {
                // Thực hiện lưu câu hỏi vào cơ sở dữ liệu
                // ...
            }

            conn.Close();
        }


    }
}