using iText.StyledXmlParser.Node;
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
                               "LEFT JOIN subjects t ON u.subject_id = t.id";

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
                            doc.Id = Convert.ToInt32(dataReader["id"]);
                            doc.NameDocument = dataReader["namedocument"].ToString();
                            doc.LinkDocument = dataReader["linkdocument"].ToString();
                            doc.DateUpload = dataReader.GetDateTime(dataReader.GetOrdinal("dateupload"));
                            doc.NameSubject = dataReader["namesubject"].ToString();
                            documentList.Add(doc);
                        }
                    }

                    return View(documentList);
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
                List<Subject> subjects = GetSubjectsFromDatabase();

                List<SelectListItem> subjectListItems = subjects.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.NameSubject
                }).ToList();

                ViewBag.Subjects = subjectListItems;

                var defaultSubjectId = subjects.FirstOrDefault()?.Id;
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
                    string filePath = @"D:\flutterapp\appvscode\app_ontapkienthuc\assets\pdf\" + file.FileName;
                    file.SaveAs(filePath);
                    document.LinkDocument = "assets/pdf/" + file.FileName;
                }

                document.DateUpload = DateTime.Now;

                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    // kiểm tra tài liệu đã tồn tại với môn học này chưa
                    string checkQuery = "SELECT COUNT(*) FROM documents WHERE (namedocument = @namedoc OR linkdocument = @linkdoc) AND subject_id = @subjectid";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@namedoc", document.NameDocument);
                    checkCmd.Parameters.AddWithValue("@linkdoc", document.LinkDocument);
                    checkCmd.Parameters.AddWithValue("@subjectid", document.Subject_ID);

                    int documentCount = Convert.ToInt32(checkCmd.ExecuteScalar());

                    if (documentCount > 0)
                    {
                        ViewData["Error"] = "Tài liệu đã tồn tại với môn học đã chọn!";
                        ViewBag.Subjects = GetSubjectsFromDatabase()
                            .Select(s => new SelectListItem
                            {
                                Value = s.Id.ToString(),
                                Text = s.NameSubject
                            }).ToList();
                        return View(document);
                    }

                    string insertQuery = "INSERT INTO documents (namedocument, linkdocument, dateupload, subject_id) VALUES (@namedocument, @linkdocument, @dateupload, @subject_id)";
                    MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);

                    insertCmd.Parameters.AddWithValue("@namedocument", document.NameDocument);
                    insertCmd.Parameters.AddWithValue("@linkdocument", document.LinkDocument);
                    insertCmd.Parameters.AddWithValue("@dateupload", document.DateUpload);
                    insertCmd.Parameters.AddWithValue("@subject_id", document.Subject_ID ?? (object)DBNull.Value);

                    insertCmd.ExecuteNonQuery();

                    return RedirectToAction("Index", "Documents");
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
                        Document document = new Document();
                        document.Id = Convert.ToInt32(reader["id"]);
                        document.NameDocument = reader["namedocument"].ToString();
                        document.LinkDocument = reader["linkdocument"].ToString();
                        document.Subject_ID = reader["subject_id"] is DBNull ? (int?)null : Convert.ToInt32(reader["subject_id"]);

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditDocument(Document document, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                MySqlConnection conn = connection.ConnectionSQL();

                try
                {
                    conn.Open();

                    // Kiểm tra nếu có file mới được chọn
                    if (file != null && file.ContentLength > 0)
                    {
                        // Nếu có, lưu file mới và cập nhật đường dẫn
                        string filePath = @"D:\flutterapp\appvscode\app_ontapkienthuc\assets\pdf\" + file.FileName;
                        file.SaveAs(filePath);
                        document.LinkDocument = "assets/pdf/" + file.FileName;
                    }
                    else
                    {
                        // Nếu không có file mới được chọn, giữ nguyên đường dẫn cũ
                        document.LinkDocument = document.LinkDocument;
                    }

                    document.DateUpload = DateTime.Now;

                    string updateQuerry = "UPDATE documents SET namedocument = @namedoc, linkdocument = @linkdoc, dateupload = @dateUpload WHERE id = @documentId";
                    MySqlCommand updateCmd = new MySqlCommand(updateQuerry, conn);

                    updateCmd.Parameters.AddWithValue("@namedoc", document.NameDocument);
                    updateCmd.Parameters.AddWithValue("@linkdoc", document.LinkDocument);
                    updateCmd.Parameters.AddWithValue("@dateupload", document.DateUpload);
                    updateCmd.Parameters.AddWithValue("@documentId", document.Id);

                    updateCmd.ExecuteNonQuery();

                    return RedirectToAction("Index", "Documents");
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

            List<Subject> subjects = GetSubjectsFromDatabase();

            List<SelectListItem> subjectListItems = subjects.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.NameSubject
            }).ToList();

            ViewBag.Subjects = subjectListItems;

            return View(document);
        }

        public ActionResult DeleteDocument(int documentId)
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
               "LEFT JOIN subjects t ON u.subject_id = t.id " +
               "WHERE u.id = @documentId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@documentId", documentId);

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
                        document.DateUpload = reader.GetDateTime(reader.GetOrdinal("dateupload"));
                        document.NameSubject = reader["namesubject"].ToString();
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
                    return View("Error");
                }
                finally
                {
                    conn.Close();
                }
            }
        }

        public ActionResult DeleteConfirmed(int documentId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "DELETE FROM documents WHERE id = @documentId";
                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@documentId", documentId);

                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return RedirectToAction("Index", "Documents");
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