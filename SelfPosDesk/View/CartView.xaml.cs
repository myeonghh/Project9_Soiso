using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;



namespace SelfPosDesk.View
{
    /// <summary>
    /// CartView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CartView : UserControl
    {

        public CartView()
        {
            InitializeComponent();
            Debug.WriteLine("CartView 생성 및 DataContext 설정 완료");
        }

        private void Separator_Expanded(object sender, RoutedEventArgs e)
        {

        }
    }
}
