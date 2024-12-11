using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoisoManagerSever
{
    internal class DatabaseManager
    {
        private MySqlConnection connection;

        public void Connect(string connectionString)
        {
            try
            {
                connection = new MySqlConnection(connectionString);
                connection.Open();
                Console.WriteLine("데이터베이스에 연결되었습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
                connection.Dispose();
                Console.WriteLine("데이터베이스 연결이 해제되었습니다.");
            }
        }

        public MySqlCommand CreateCommand(string query)
        {
            if (connection == null || connection.State != System.Data.ConnectionState.Open)
            {
                Console.WriteLine("데이터베이스 연결이 열려 있지 않습니다.");
            }

            return new MySqlCommand(query, connection);
        }
    }
}
