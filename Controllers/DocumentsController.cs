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
    public class DocumentsController : Controller
    {
        // GET: Documents
        public ActionResult Index()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT u.id, u.namedocument, u.linkdocument, u.dateupload, t.namesubject " +
                   "FROM documents u " +
                   "INNER JOIN subjects t ON u.subject_id = t.id";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                List<Document> documentList = new List<Document>();

                try
                {
                    conn.Open();
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            Document doc = new Document();
                            doc.Id = Convert.ToInt32(dataReader["id"]); // document_id
                            doc.NameDocument = dataReader["namedocument"].ToString();
                            doc.LinkDocument = dataReader["linkdocument"].ToString();
                            if (!dataReader.IsDBNull(dataReader.GetOrdinal("dateupload")))
                            {
                                // Lấy giá trị kiểu DateTime từ cột dateupload
                                doc.DateUpload = dataReader.GetDateTime(dataReader.GetOrdinal("dateupload"));
                            }
                            else
                            {
                                // Xử lý nếu giá trị là NULL
                                doc.DateUpload = null;
                            }
                            doc.NameSubject = dataReader["namesubject"].ToString();       
                            documentList.Add(doc);
                        }
                    }

                    return View(documentList);
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

        private List<Subject> GetSubjectsFromDatabase()
        {
            // Kết nối đến cơ sở dữ liệu và truy vấn để lấy danh sách môn học
            List<Subject> subjects = new List<Subject>();

            // Thực hiện truy vấn để lấy danh sách môn học từ cơ sở dữ liệu
            try
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                conn.Open();

                string query = "SELECT * FROM subjects"; // Đảm bảo rằng truy vấn này phù hợp với cấu trúc bảng subjects trong cơ sở dữ liệu của bạn

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (MySqlDataReader dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        Subject subject = new Subject();
                        subject.Id = Convert.ToInt32(dataReader["id"]); // subject_id
                        subject.NameSubject = dataReader["namesubject"].ToString();
                        subjects.Add(subject);
                    }
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                // Xử lý ngoại lệ, ví dụ ghi log lỗi hoặc thông báo lỗi
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
                    Value = s.Id.ToString(), // s.Id là subject_id
                    Text = s.NameSubject // s.Name là namesubject
                }).ToList();

                ViewBag.Subjects = subjectListItems; // Gửi danh sách môn học đến View

                // Set the default selected value to the ID of the first subject
                var defaultSubjectId = subjects.FirstOrDefault()?.Id; // Get the ID of the first subject
                ViewBag.DefaultSubjectId = defaultSubjectId;

                return View();
            }
        }

        [HttpPost]
        public ActionResult Create(Document document, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                if (file != null && file.ContentLength > 0)
                {
                    // Lưu tệp PDF vào đường dẫn '' và cập nhật link tài liệu trong document
                    string filePath = @"D:\flutterapp\appvscode\app_ontapkienthuc\assets\pdf\" + file.FileName;
                    file.SaveAs(filePath);
                    document.LinkDocument = "assets/pdf/" + file.FileName;
                }

                document.DateUpload = DateTime.Now; // Ngày giờ hiện tại

                // Kết nối đến cơ sở dữ liệu
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    // Check if the document already exists by namedocument or linkdocument with the provided subject_id
                    string checkQuery = "SELECT COUNT(*) FROM documents WHERE (namedocument = @namedoc OR linkdocument = @linkdoc) AND subject_id = @subjectid";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@namedoc", document.NameDocument);
                    checkCmd.Parameters.AddWithValue("@linkdoc", document.LinkDocument);
                    checkCmd.Parameters.AddWithValue("@subjectid", document.Subject_ID);

                    int documentCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (documentCount > 0)
                    {
                        // Document already exists with the provided subject_id
                        ViewData["Error"] = "Tài liệu đã tồn tại với môn học đã chọn!";
                        ViewBag.Subjects = GetSubjectsFromDatabase()
                            .Select(s => new SelectListItem
                            {
                                Value = s.Id.ToString(),
                                Text = s.NameSubject
                            }).ToList();
                        return View(document);
                    }

                    // If the document doesn't exist, proceed to insert it
                    string insertQuery = "INSERT INTO documents (namedocument, linkdocument, dateupload, subject_id) VALUES (@namedocument, @linkdocument, @dateupload, @subject_id)";
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);

                    // Thêm các tham số để tránh tấn công SQL Injection
                    insertCmd.Parameters.AddWithValue("@namedocument", document.NameDocument);
                    insertCmd.Parameters.AddWithValue("@linkdocument", document.LinkDocument);
                    insertCmd.Parameters.AddWithValue("@dateupload", document.DateUpload);
                    insertCmd.Parameters.AddWithValue("@subject_id", document.Subject_ID);

                    insertCmd.ExecuteNonQuery(); // Thực hiện lệnh INSERT

                    return RedirectToAction("Index", "Documents"); // Chuyển hướng sau khi thêm thành công
                }
                catch (Exception ex)
                {
                    // Xử lý ngoại lệ
                    return View("Error");
                }
                finally
                {
                    conn.Close();
                }
            }

            return View(document);
        }

        public ActionResult EditDocument(int documentId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT id, namedocument, linkdocument, subject_id " +
                               "FROM documents " +
                               "WHERE id = @documentId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@documentId", documentId);

                // Lấy danh sách môn học từ cơ sở dữ liệu
                List<Subject> subjects = GetSubjectsFromDatabase();

                // Chuyển danh sách môn học thành danh sách SelectListItem
                List<SelectListItem> subjectListItems = subjects.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(), // s.Id là subject_id
                    Text = s.NameSubject // s.Name là namesubject
                }).ToList();

                ViewBag.Subjects = subjectListItems; // Gửi danh sách môn học đến View

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        Document document = new Document();
                        document.Id = Convert.ToInt32(reader["id"]);
                        document.NameDocument = reader["namedocument"].ToString();
                        document.LinkDocument = reader["linkdocument"].ToString();
                        document.Subject_ID = Convert.ToInt32(reader["subject_id"]);

                        conn.Close();
                        return View(document);
                    }
                    else
                    {
                        conn.Close();
                        return View("Error");
                    }
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
    }
}