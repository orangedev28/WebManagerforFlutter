using iText.StyledXmlParser.Node;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebQuanLyAppOnTap.Models
{
    public class ScoreData
    {
        public int Id { get; set; }
        public float Score { get; set; }
        public DateTime DateAdd { get; set; }
        public int Quiz_Id { get; set; }
        public string NameQuiz { get; set; }
        public int User_Id { get; set; }    
        public string FullName { get; set; }
    }
}