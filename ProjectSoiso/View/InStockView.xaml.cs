using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProjectSoiso.View
{
    public partial class InStockView : Window
    {
        public InStockView()
        {
            InitializeComponent();
        }

        // 가격 입력 필드를 숫자만 입력 가능하도록 설정
        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}
