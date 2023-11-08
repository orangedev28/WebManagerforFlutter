using System;
using MySql.Data.MySqlClient;

namespace WebQuanLyAppOnTap
{
    public class ConnectionMySQL
    {
        // Khai báo chuỗi kết nối
        string str_mySQL = "Server=localhost;Port=3306;Database=appontapkienthuc;Uid=root;Pwd=;";

        // Hàm kết nối
        public MySqlConnection ConnectionSQL()
        {
            return new MySqlConnection(str_mySQL);
        }
    }
}
