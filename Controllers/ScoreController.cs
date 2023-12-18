using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebQuanLyAppOnTap.Models;

namespace WebQuanLyAppOnTap.Controllers
{
    public class ScoreController : Controller
    {
        // GET: Score
        public ActionResult Index()
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();

                string query = "SELECT u.id, u.score, u.dateadd, t.namequiz, e.fullname " +
                               "FROM score u " +
                               "LEFT JOIN quizzes t ON u.quiz_id = t.id " + // Added space here
                               "INNER JOIN users e ON u.user_id = e.id";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                List<ScoreData> scoreList = new List<ScoreData>();

                try
                {
                    conn.Open();
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            ScoreData score = new ScoreData();
                            score.Id = Convert.ToInt32(dataReader["id"]);
                            score.Score = Convert.ToSingle(dataReader["score"]);
                            score.DateAdd = dataReader.GetDateTime(dataReader.GetOrdinal("dateadd"));
                            score.NameQuiz = dataReader["namequiz"].ToString();
                            score.FullName = dataReader["fullname"].ToString();
                            scoreList.Add(score);
                        }
                    }

                    return View(scoreList);
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

        public ActionResult DeleteScore(int scoreId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "SELECT u.id, u.score, u.dateadd, t.namequiz, e.fullname " +
                               "FROM score u " +
                               "LEFT JOIN quizzes t ON u.quiz_id = t.id " + // Added space here
                               "INNER JOIN users e ON u.user_id = e.id " +
                               "WHERE u.id = @scoreId";

                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@scoreId", scoreId);

                try
                {
                    conn.Open();
                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        ScoreData score = new ScoreData();
                        score.Id = Convert.ToInt32(reader["id"]);
                        score.Score = Convert.ToSingle(reader["score"]);
                        score.DateAdd = reader.GetDateTime(reader.GetOrdinal("dateadd"));
                        score.NameQuiz = reader["namequiz"].ToString();
                        score.FullName = reader["fullname"].ToString();

                        conn.Close();
                        return View(score);
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

        public ActionResult DeleteConfirmed(int scoreId)
        {
            if (Session["fullname"] == null)
            {
                return RedirectToAction("Login", "Users");
            }
            else
            {
                ConnectionMySQL connection = new ConnectionMySQL();
                string query = "DELETE FROM score WHERE id = @scoreId";
                MySqlConnection conn = connection.ConnectionSQL();
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@scoreId", scoreId);

                try
                {
                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return RedirectToAction("Index", "Score");
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