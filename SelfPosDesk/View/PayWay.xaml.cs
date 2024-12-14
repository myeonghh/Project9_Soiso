using SelfPosDesk.ViewModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SelfPosDesk.View
{
    public partial class PayWay : UserControl
    {
        private string _selectedButtonName; // 현재 선택된 버튼 이름 저장
        private List<Button> _buttons;     // 모든 버튼 리스트
        string borderName;
        string paymentMethod;

        public PayWay()
        {
            InitializeComponent();
            InitializeButtonList();
        }

        private void InitializeButtonList()
        {
            // 모든 버튼을 리스트에 추가
            _buttons = new List<Button>
            {
                CardButton,
                PayButton,
                PointButton,
                VoucherButton,
                SmallButton1,
                SmallButton2,
                SmallButton3,
                SmallButton4,
                SmallButton5,
                SmallButton6,
                SmallButton7,
                SmallButton8,
                SmallButton9,
                SmallButton10
            };
        }

        private string GetPaymentMethodName(string borderName)
        {
            // 버튼 이름에 따른 결제 수단 이름을 반환
            switch (borderName)
            {
                case "CardBorder":
                    return "카드 결제";
                case "PayBorder":
                    return "Pay 결제";
                case "PointBorder":
                    return "포인트 결제";
                case "VoucherBorder":
                    return "상품권 결제";
                case "SmallBorder1":
                    return "카카오페이";
                case "SmallBorder2":
                    return "네이버페이";
                case "SmallBorder3":
                    return "1Q페이";
                case "SmallBorder4":
                    return "제로페이";
                case "SmallBorder5":
                    return "BC카드";
                case "SmallBorder6":
                    return "SOL페이";
                case "SmallBorder7":
                    return "유니온페이";
                case "SmallBorder8":
                    return "알리페이";
                case "SmallBorder9":
                    return "티머니";
                case "SmallBorder10":
                    return "페이코";
                default:
                    return "알 수 없음"; // 해당하는 버튼 이름이 없을 경우
            }
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as Button;
            if (clickedButton == null) return;

            // 현재 선택된 버튼 이름 저장
            _selectedButtonName = clickedButton.Name;

            // 버튼 테두리 업데이트
            UpdateButtonBorders();

            // 선택된 버튼에 해당하는 Border 이름과 결제 수단 이름 가져오기
            borderName = FindButtonBorder(clickedButton)?.Name;

        }

        private void UpdateButtonBorders()
        {
            foreach (var button in _buttons)
            {
                // 큰 버튼의 Template 또는 작은 버튼의 Template에서 Border를 찾음
                var border = FindButtonBorder(button);
                if (border != null)
                {
                    // 클릭된 버튼이면 빨간색 테두리, 아니면 투명색
                    border.BorderBrush = button.Name == _selectedButtonName ? Brushes.Red : Brushes.Transparent;
                }
            }
        }

        private Border FindButtonBorder(Button button)
        {
            if (button == null) return null;

            // Template에서 Border 찾기
            var border = button.Template?.FindName($"{button.Name.Replace("Button", "Border")}", button) as Border;

            return border;
        }

        public void OnSelectClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(borderName))
            {
                paymentMethod = GetPaymentMethodName(borderName);
                //MessageBox.Show($"선택된 결제 수단: {paymentMethod}");

                // ViewModel에 결제 수단 전달
                if (DataContext is SelfPosVM vm)
                {
                    vm.SelectedPaymentMethod = paymentMethod;

                    // CurrentView 변경 
                    vm.ChangePage(new CartView
                    {
                        DataContext = vm // ViewModel 공유
                    });
                }
            }
        }
    }
}
