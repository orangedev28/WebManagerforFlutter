using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebQuanLyAppOnTap.Models
{
    public class Question
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Hãy nhập Nội dung câu hỏi!")]
        public string QuestionContent { get; set; }
        [Required(ErrorMessage = "Hãy nhập Đáp án 1!")]
        public string Answer1 { get; set; }
        [Required(ErrorMessage = "Hãy nhập Đáp án 2!")]
        public string Answer2 { get; set; }
        [Required(ErrorMessage = "Hãy nhập Đáp án 3!")]
        public string Answer3 { get; set; }
        [Required(ErrorMessage = "Hãy nhập Đáp án 4!")]
        public string Answer4 { get; set; }
        [Required(ErrorMessage = "Hãy nhập Đáp án đúng!")]
        public string CorrectAnswer { get; set; }
        [Required(ErrorMessage = "Hãy chọn Bài kiểm tra!")]
        public int Quiz_Id { get; set; }
        public string NameQuiz { get; set; }
        public int? Subject_Id { get; set; }
        public string SubjectName { get; set; }
        public bool? HotQuestion { get; set; }
    }
}