using LiveCharts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using ProjectSoiso.Helper;
using ProjectSoiso.Data;
using System.Windows.Input;
using System.Windows;

namespace ProjectSoiso.ViewModel
{
    public class SalesViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand GoBackCommand { get; } // 뒤로가기 명령 (Command)

        // 변경 알림을 위한 메서드
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ChartValues<double> MonthlyValues { get; set; }
        public List<string> Months { get; set; }

        private ChartValues<double> dailyValues;
        public ChartValues<double> DailyValues
        {
            get => dailyValues;
            set
            {
                dailyValues = value;
                OnPropertyChanged();
            }
        }

        private List<string> dates;
        public List<string> Dates
        {
            get => dates;
            set
            {
                dates = value;
                OnPropertyChanged();
            }
        }

        private ChartValues<double> categoryTotalPrices;
        public ChartValues<double> CategoryTotalPrices
        {
            get => categoryTotalPrices;
            set
            {
                categoryTotalPrices = value;
                OnPropertyChanged();
            }
        }

        private ChartValues<int> categoryTotalQuantities;
        public ChartValues<int> CategoryTotalQuantities
        {
            get => categoryTotalQuantities;
            set
            {
                categoryTotalQuantities = value;
                OnPropertyChanged();
            }
        }

        public List<string> Categories { get; set; }

        public Func<double, string> Formatter { get; set; }

        private SalesChartDatabaseHelper db;

        public SalesViewModel()
        {
            GoBackCommand = new RelayCommand(ExecuteGoBack); // 뒤로가기 명령 초기화
            db = new SalesChartDatabaseHelper();

            // 월별 데이터 가져오기
            DataTable monthlyData = db.GetMonthlyRevenue();
            Months = new List<string>();
            MonthlyValues = new ChartValues<double>();

            foreach (DataRow row in monthlyData.Rows)
            {
                Months.Add(row["month"].ToString()); // 월 추가
                MonthlyValues.Add(Convert.ToDouble(row["monthly_revenue"])); // 매출 추가
            }

            // 카테고리 초기화
            Categories = new List<string> { "주방용품", "청소용품", "식품", "문구" };
            CategoryTotalPrices = new ChartValues<double>();
            CategoryTotalQuantities = new ChartValues<int>();

            // Formatter 설정
            Formatter = value => value.ToString("C");
        }

        private void ExecuteGoBack(object parameter)
        {
            // MainPage.xaml 창 열기
            var mainPage = new ProjectSoiso.View.MainPage();
            mainPage.Show();

            // 현재 창 닫기
            Application.Current.Windows[0]?.Close();
        }

        // 특정 월의 일별 데이터를 로드
        public void LoadDailyRevenue(string selectedMonth)
        {
            DataTable dailyData = db.GetDailyRevenueByMonth(selectedMonth);

            DailyValues = new ChartValues<double>();
            Dates = new List<string>();

            foreach (DataRow row in dailyData.Rows)
            {
                Dates.Add(Convert.ToDateTime(row["day"]).ToShortDateString());
                DailyValues.Add(Convert.ToDouble(row["total_revenue"]));
            }
        }

        // 특정 월의 카테고리별 데이터를 로드
        public void LoadCategoryData(string selectedMonth)
        {
            // 카테고리별 합계 가격 로드
            DataTable priceData = db.GetCategoryTotalPrice(selectedMonth);
            CategoryTotalPrices.Clear();
            foreach (DataRow row in priceData.Rows)
            {
                CategoryTotalPrices.Add(Convert.ToDouble(row["total_price"]));
            }

            // 카테고리별 합계 수량 로드
            DataTable quantityData = db.GetCategoryTotalQuantity(selectedMonth);
            CategoryTotalQuantities.Clear();
            foreach (DataRow row in quantityData.Rows)
            {
                CategoryTotalQuantities.Add(Convert.ToInt32(row["total_quantity"]));
            }
        }
    }
}
