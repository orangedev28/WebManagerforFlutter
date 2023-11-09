using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebQuanLyAppOnTap.Models
{
    public class Subject
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Hãy nhập Tên môn học!")]
        public string NameSubject { get; set; }
    }
}