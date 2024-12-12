using System.Windows;
using System.Windows.Controls;
using ProjectSoiso.ViewModel;

namespace ProjectSoiso.View
{
    public partial class SalesView : Window
    {
        private SalesViewModel viewModel;

        public SalesView()
        {
            InitializeComponent();
            viewModel = new SalesViewModel();
            DataContext = viewModel;
        }

        private void MonthSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonthSelector.SelectedItem is string selectedMonth)
            {
                // 기존 데이터 로드
                viewModel.LoadDailyRevenue(selectedMonth);

                // 카테고리 데이터 로드
                viewModel.LoadCategoryData(selectedMonth);
            }
        }

    }
}
