using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebQuanLyAppOnTap.Models
{
    public class Document
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Hãy nhập Tên tài liệu!")]
        public string NameDocument { get; set; }
        public string LinkDocument { get; set; }
        public DateTime? DateUpload { get; set; }
        public int Subject_ID { get; set; }
        public string NameSubject { get; set; }
    }
}