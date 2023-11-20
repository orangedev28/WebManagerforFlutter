using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebQuanLyAppOnTap.Models
{
    public class UserData
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Hãy nhập Username!")]
        public string Username { get; set; }
        public string Password { get; set; }
        [Required(ErrorMessage = "Hãy nhập Email!")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Hãy nhập Họ tên!")]
        public string FullName { get; set; }
        public int UserType_ID { get; set; }
        public string NameType { get; set; }
    }
}