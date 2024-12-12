using ProjectSoiso.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;
using ProjectSoiso.Model;

namespace ProjectSoiso.View
{
    public partial class InventoryView : Window
    {
        // 생성자: InventoryView 초기화
        public InventoryView()
        {
            InitializeComponent(); // XAML과 연결된 UI 초기화

            // ViewModel을 데이터 컨텍스트로 설정
            DataContext = new InventoryViewModel();

            // ListView의 ItemContainerGenerator 상태 변경 이벤트를 구독
            InventoryListView.ItemContainerGenerator.StatusChanged += OnItemContainerGeneratorStatusChanged;
        }

        // ItemContainerGenerator의 상태가 변경될 때 호출되는 메서드
        private void OnItemContainerGeneratorStatusChanged(object sender, EventArgs e)
        {
            // ItemContainer가 모두 생성되었는지 확인
            if (InventoryListView.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                // CheckBox와 TextBox 상태 동기화 메서드 호출
                SynchronizeCheckBoxAndTextBox();
            }
        }

        // 사용자가 TextBox에서 Enter 키를 눌렀을 때 수량을 업데이트하는 메서드
        private void OnQuantityKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Enter 키가 눌렸는지 확인
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (sender is TextBox textBox && textBox.Tag is string productName)
                {
                    // TextBox의 텍스트를 정수로 변환
                    if (int.TryParse(textBox.Text, out int newQuantity))
                    {
                        // 디버그 로그 출력: 입력된 제품 이름과 새로운 수량
                        Debug.WriteLine($"Quantity updated for ProductName={productName}, NewQuantity={newQuantity}");

                        // ViewModel을 통해 데이터베이스의 수량을 업데이트
                        if (DataContext is InventoryVM viewModel)
                        {
                            viewModel.UpdateInventoryQuantity(productName, newQuantity);
                        }
                    }
                    else
                    {
                        // 변환 실패 시 디버그 로그 출력
                        Debug.WriteLine($"Invalid quantity entered for ProductName={productName}");
                    }
                }

                // Enter 키 이벤트를 처리 완료로 표시
                e.Handled = true;
            }
        }

        // CheckBox와 TextBox 상태를 동기화하는 메서드
        private void SynchronizeCheckBoxAndTextBox()
        {
            foreach (var item in InventoryListView.Items) // ListView의 모든 아이템을 순회
            {
                // 현재 아이템에 대한 ListViewItem 컨테이너 가져오기
                var listViewItem = (ListViewItem)InventoryListView.ItemContainerGenerator.ContainerFromItem(item);
                if (listViewItem != null)
                {
                    // 현재 ListViewItem에서 CheckBox와 TextBox를 검색
                    var checkBox = FindChild<CheckBox>(listViewItem);
                    var textBox = FindChild<TextBox>(listViewItem);

                    if (checkBox != null && textBox != null)
                    {
                        // CheckBox의 상태에 따라 TextBox 활성화/비활성화 설정
                        textBox.IsEnabled = checkBox.IsChecked == true;
                    }
                }
            }
        }

        // ViewModel의 데이터를 새로고침하는 메서드
        private void RefreshData()
        {
            if (DataContext is InventoryVM viewModel) // DataContext가 InventoryVM인지 확인
            {
                Debug.WriteLine("Refreshing data..."); // 디버그 메시지 출력
                viewModel.ReloadData(); // ViewModel의 ReloadData 메서드 호출
            }
        }

        // 검수 완료 CheckBox가 선택되었을 때 호출되는 메서드
        private void OnCompletedCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Completed CheckBox Checked"); // 디버그 메시지 출력
            HandleCompletedCheckBoxChange(sender, true); // 상태를 true로 처리
        }

        // 검수 완료 CheckBox가 선택 해제되었을 때 호출되는 메서드
        private void OnCompletedCheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Completed CheckBox Unchecked"); // 디버그 메시지 출력
            HandleCompletedCheckBoxChange(sender, false); // 상태를 false로 처리
        }

        // 검수 대기 CheckBox가 선택되었을 때 호출되는 메서드
        private void OnPendingCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pending CheckBox Checked"); // 디버그 메시지 출력
            HandlePendingCheckBoxChange(sender, true); // 상태를 true로 처리
        }

        // 검수 대기 CheckBox가 선택 해제되었을 때 호출되는 메서드
        private void OnPendingCheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Pending CheckBox Unchecked"); // 디버그 메시지 출력
            HandlePendingCheckBoxChange(sender, false); // 상태를 false로 처리
        }

        // 검수 완료 CheckBox 상태가 변경될 때 호출되는 메서드
        private void HandleCompletedCheckBoxChange(object sender, bool isChecked)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is string productName)
            {
                Debug.WriteLine($"Completed CheckBox: ProductName={productName}, IsChecked={isChecked}"); // 디버그 로그

                // ViewModel의 UpdateProductState 메서드를 호출하여 상태 업데이트
                if (DataContext is InventoryVM viewModel)
                {
                    viewModel.UpdateProductState(productName, isChecked);
                }

                // CheckBox가 포함된 ListViewItem 가져오기
                var listViewItem = FindParent<ListViewItem>(checkBox);
                if (listViewItem != null)
                {
                    // 같은 행에 있는 TextBox를 검색하여 상태 동기화
                    var textBox = FindChild<TextBox>(listViewItem);
                    if (textBox != null)
                    {
                        textBox.IsEnabled = isChecked; // CheckBox 상태에 따라 TextBox 활성화/비활성화
                        Debug.WriteLine($"TextBox for ProductName={productName} is now {(isChecked ? "Enabled" : "Disabled")}");
                    }
                }
            }
        }

        // 검수 대기 CheckBox 상태가 변경될 때 호출되는 메서드
        private void HandlePendingCheckBoxChange(object sender, bool isChecked)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is string productName)
            {
                Debug.WriteLine($"Pending CheckBox: ProductName={productName}, IsChecked={isChecked}"); // 디버그 로그

                // ViewModel의 ProcessPendingCheckBox 메서드 호출
                if (DataContext is InventoryVM viewModel)
                {
                    viewModel.ProcessPendingCheckBox(productName, isChecked);
                }

                // 데이터 새로고침
                RefreshData();
            }
        }

        // 부모 요소를 재귀적으로 찾는 메서드
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            // 현재 요소의 부모 가져오기
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null; // 부모가 없으면 null 반환

            if (parentObject is T parent) // 부모가 원하는 타입인지 확인
            {
                return parent; // 원하는 타입이면 반환
            }

            // 재귀적으로 부모를 탐색
            return FindParent<T>(parentObject);
        }

        // 자식 요소를 재귀적으로 찾는 메서드
        private T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null; // 부모가 null이면 null 반환

            // 부모의 자식 요소 개수 가져오기
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i); // 현재 자식 가져오기

                if (child is T foundChild) // 자식이 원하는 타입인지 확인
                {
                    return foundChild; // 원하는 타입이면 반환
                }

                // 재귀적으로 자식을 탐색
                var childOfChild = FindChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild; // 원하는 자식이 있으면 반환
                }
            }
            return null; // 원하는 자식이 없으면 null 반환
        }
    }
}