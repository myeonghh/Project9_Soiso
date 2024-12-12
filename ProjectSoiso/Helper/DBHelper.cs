using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace ProjectSoiso.Helper
{
    public class DBHelper
    {
        // 데이터베이스 연결 문자열
        public readonly string _connectionString;

        // 생성자: DB 연결 문자열 초기화
        public DBHelper()
        {
            // MySQL 서버 연결 설정
            _connectionString = "Server=127.0.0.1;Database=soiso;User Id=root;Password=123;SslMode=None;CharSet=utf8mb4;";
        }

        // 데이터베이스에서 SELECT 쿼리를 실행하고 결과를 DataTable로 반환
        public DataTable ExecuteQuery(string query)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    // 데이터베이스 연결 열기
                    connection.Open();
                    Console.WriteLine("데이터베이스 연결 성공");

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // 쿼리 실행 후 결과를 DataAdapter를 통해 DataTable로 반환
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            DataTable dataTable = new DataTable();
                            adapter.Fill(dataTable); // 결과를 DataTable에 채우기
                            Console.WriteLine($"쿼리 실행 성공: {query}");
                            return dataTable;
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                // MySQL 관련 오류 처리
                Console.WriteLine($"MySQL 오류: {ex.Message}");
                throw; // 오류를 호출자로 다시 던짐
            }
            catch (Exception ex)
            {
                // 일반적인 오류 처리
                Console.WriteLine($"오류: {ex.Message}");
                throw; // 오류를 호출자로 다시 던짐
            }
        }


    }
}
