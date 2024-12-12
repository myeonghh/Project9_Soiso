using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace ProjectSoiso.Data
{
    public class SalesChartDatabaseHelper
    {
        private const string ConnectionString = "Server=localhost;Database=soiso;User Id=root;Password=1234;";

        // 월별 매출 데이터 조회
        public DataTable GetMonthlyRevenue()
        {
            string query = @"SELECT DATE_FORMAT(date, '%Y-%m') AS month, SUM(total_revenue) AS monthly_revenue
                             FROM revenue
                             GROUP BY DATE_FORMAT(date, '%Y-%m')
                             ORDER BY DATE_FORMAT(date, '%Y-%m');";

            return ExecuteQuery(query);
        }

        // 일별 매출 데이터 전체 조회
        public DataTable GetDailyRevenue()
        {
            string query = @"SELECT date, total_revenue
                             FROM revenue
                             ORDER BY date;";

            return ExecuteQuery(query);
        }

        // 특정 월의 일별 매출 데이터 조회
        public DataTable GetDailyRevenueByMonth(string selectedMonth)
        {
            string query = @"SELECT DATE(date) AS day, total_revenue
                             FROM revenue
                             WHERE DATE_FORMAT(date, '%Y-%m') = @SelectedMonth
                             ORDER BY day;";

            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                DataTable table = new DataTable();
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@SelectedMonth", selectedMonth);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(table);
                return table;
            }
        }

        // 공통 쿼리 실행 메서드
        private DataTable ExecuteQuery(string query)
        {
            DataTable table = new DataTable();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(table);
            }
            return table;
        }

        public DataTable GetCategoryTotalPrice(string selectedMonth)
        {
            string query = @"
        SELECT 
            CASE 
                WHEN product_id BETWEEN 1 AND 10 THEN '주방용품'
                WHEN product_id BETWEEN 11 AND 20 THEN '청소용품'
                WHEN product_id BETWEEN 21 AND 30 THEN '식품'
                WHEN product_id BETWEEN 31 AND 40 THEN '문구'
            END AS category,
            SUM(total_price) AS total_price
        FROM sales
        WHERE DATE_FORMAT(sale_date, '%Y-%m') = @SelectedMonth
        GROUP BY category;";
            return ExecuteQueryWithParameter(query, selectedMonth);
        }

        public DataTable GetCategoryTotalQuantity(string selectedMonth)
        {
            string query = @"
        SELECT 
            CASE 
                WHEN product_id BETWEEN 1 AND 10 THEN '주방용품'
                WHEN product_id BETWEEN 11 AND 20 THEN '청소용품'
                WHEN product_id BETWEEN 21 AND 30 THEN '식품'
                WHEN product_id BETWEEN 31 AND 40 THEN '문구'
            END AS category,
            SUM(quantity) AS total_quantity
        FROM sales
        WHERE DATE_FORMAT(sale_date, '%Y-%m') = @SelectedMonth
        GROUP BY category;";
            return ExecuteQueryWithParameter(query, selectedMonth);
        }

        // Helper method to execute queries with parameters
        private DataTable ExecuteQueryWithParameter(string query, string selectedMonth)
        {
            DataTable table = new DataTable();
            using (MySqlConnection connection = new MySqlConnection(ConnectionString))
            {
                connection.Open();
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@SelectedMonth", selectedMonth);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(table);
            }
            return table;
        }

    }
}
