using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebQuanLyAppOnTap.Models
{
    public class UserData
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public int UserType_ID { get; set; }
        public string NameType { get; set; }
    }
}