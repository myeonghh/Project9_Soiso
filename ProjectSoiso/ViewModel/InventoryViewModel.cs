using Microsoft.Win32;
using MySql.Data.MySqlClient;
using ProjectSoiso.Helper;
using ProjectSoiso.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows;

namespace ProjectSoiso.ViewModel
{
    class InventoryViewModel : ViewModelBase
    {
        public ICommand GoBackCommand { get; } // 뒤로가기 명령 (Command)

        // 속성 변경 알림 이벤트 정의
        public event PropertyChangedEventHandler PropertyChanged;

        // DB 작업을 처리하는 DBHelper 객체
        private readonly DBHelper _dbHelper;

        // 검수 완료된 상품 목록
        private ObservableCollection<Inventory> _completedInventories;

        // CompletedInventories 속성: UI에서 검수 완료 상품 목록을 바인딩
        public ObservableCollection<Inventory> CompletedInventories
        {
            get => _completedInventories; // 속성 값을 반환
            set
            {
                _completedInventories = value; // 속성 값 설정
                OnPropertyChanged(nameof(CompletedInventories)); // 속성 변경 알림
            }
        }

        // 검수 대기 중인 상품 목록
        private ObservableCollection<Inventory> _pendingInventories;

        // PendingInventories 속성: UI에서 검수 대기 상품 목록을 바인딩
        public ObservableCollection<Inventory> PendingInventories
        {
            get => _pendingInventories; // 속성 값을 반환
            set
            {
                _pendingInventories = value; // 속성 값 설정
                OnPropertyChanged(nameof(PendingInventories)); // 속성 변경 알림
            }
        }

        // 생성자: ViewModel 초기화 및 데이터 로드
        public InventoryViewModel()
        {
            _dbHelper = new DBHelper(); // DBHelper 객체 초기화
            CompletedInventories = new ObservableCollection<Inventory>(); // 검수 완료 목록 초기화
            PendingInventories = new ObservableCollection<Inventory>(); // 검수 대기 목록 초기화
            LoadDataFromDatabase(); // 데이터베이스에서 초기 데이터 로드
            GoBackCommand = new RelayCommand(ExecuteGoBack); // 뒤로가기 명령 초기화
        }

        private void ExecuteGoBack(object parameter)
        {
            // MainPage.xaml 창 열기
            var mainPage = new ProjectSoiso.View.MainPage();
            mainPage.Show();

            // 현재 창 닫기
            Application.Current.Windows[0]?.Close();
        }

        // 데이터베이스에서 데이터를 로드하여 CompletedInventories와 PendingInventories를 채움
        private void LoadDataFromDatabase()
        {
            try
            {
                // SQL 쿼리: 제품 및 재고 데이터를 가져오는 쿼리
                string query = @"
                                SELECT 
                                    p.id,
                                    p.name, 
                                    p.img_path, 
                                    p.category, 
                                    COALESCE(i.quantity, 0) AS quantity, 
                                    p.state
                                FROM 
                                    soiso.products p
                                LEFT JOIN 
                                    soiso.inventory i
                                ON 
                                    p.id = i.product_id
                                ORDER BY 
                                    p.category;
                            ";

                // 쿼리를 실행하고 결과를 DataTable로 받음
                DataTable dataTable = _dbHelper.ExecuteQuery(query);

                // 쿼리 결과를 반복하면서 Inventory 객체로 변환하여 목록에 추가
                foreach (DataRow row in dataTable.Rows)
                {
                    var inventory = new Inventory
                    {
                        Name = row["name"].ToString(), // 제품 이름
                        Category = row["category"].ToString(), // 제품 카테고리
                        Quantity = Convert.ToInt32(row["quantity"]), // 수량
                        State = Convert.ToBoolean(row["state"]), // 상태 (검수 완료 여부)
                        Image = LoadImageFromPath(row["img_path"].ToString()) // 이미지 경로를 BitmapImage로 로드
                    };

                    // 수량이 있으면 CompletedInventories에 추가, 없으면 PendingInventories에 추가
                    if (inventory.Quantity > 0)
                    {
                        CompletedInventories.Add(inventory); // 검수 완료 목록에 추가
                    }
                    else
                    {
                        PendingInventories.Add(inventory); // 검수 대기 목록에 추가
                    }
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 메시지 출력
                Console.WriteLine($"데이터 로드 오류: {ex.Message}");
            }
        }

        // 인벤토리 수량 업데이트
        public void UpdateInventoryQuantity(string productName, int newQuantity)
        {
            try
            {
                // 제품 ID를 찾기 위한 SQL 쿼리
                string findProductIdQuery = "SELECT id FROM soiso.products WHERE name = @ProductName LIMIT 1;";
                // 인벤토리 수량을 업데이트하는 SQL 쿼리
                string updateInventoryQuery = "UPDATE soiso.inventory SET quantity = @Quantity WHERE product_id = @ProductId;";

                using (MySqlConnection connection = new MySqlConnection(_dbHelper._connectionString))
                {
                    connection.Open(); // 데이터베이스 연결 열기

                    // Step 1: 제품 ID 찾기
                    int productId;
                    using (MySqlCommand findCommand = new MySqlCommand(findProductIdQuery, connection))
                    {
                        findCommand.Parameters.AddWithValue("@ProductName", productName); // 파라미터 설정
                        object result = findCommand.ExecuteScalar(); // 쿼리 실행

                        if (result == null)
                        {
                            Debug.WriteLine($"Product '{productName}' not found."); // 결과가 없으면 로그 출력
                            return;
                        }
                        productId = Convert.ToInt32(result); // 결과를 정수로 변환하여 제품 ID로 저장
                    }

                    // Step 2: 인벤토리 수량 업데이트
                    using (MySqlCommand updateCommand = new MySqlCommand(updateInventoryQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Quantity", newQuantity); // 새 수량 설정
                        updateCommand.Parameters.AddWithValue("@ProductId", productId); // 제품 ID 설정
                        int affectedRows = updateCommand.ExecuteNonQuery(); // 쿼리 실행 및 영향받은 행 수 확인
                        Debug.WriteLine($"ProductId 인벤토리 업데이트={productId}. Rows affected: {affectedRows}");
                    }
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 디버그 메시지 출력
                Debug.WriteLine($"ProductName의 재고 수량 업데이트 오류={productName}: {ex.Message}");
            }
        }

        // ViewModel의 데이터를 새로고침
        public void ReloadData()
        {
            try
            {
                // 기존 데이터를 초기화 (ObservableCollection을 비움)
                CompletedInventories.Clear(); // 검수 완료 목록 비우기
                PendingInventories.Clear();   // 검수 대기 목록 비우기

                // 데이터베이스에서 새 데이터를 다시 로드
                LoadDataFromDatabase(); // LoadDataFromDatabase 메서드 호출
            }
            catch (Exception ex)
            {
                // 오류 발생 시 메시지 출력
                Console.WriteLine($"데이터 새로고침 오류: {ex.Message}");
            }
        }

        // 제품 상태 업데이트 (검수 완료 여부)
        public void UpdateProductState(string productName, bool isChecked)
        {
            try
            {
                Debug.WriteLine($"제품 상태 업데이트: ProductName={productName}, IsChecked={isChecked}"); // 디버그 메시지 출력
                // 제품 상태를 업데이트하는 SQL 쿼리
                string query = "UPDATE soiso.products SET state = @State WHERE name = @ProductName;";

                using (MySqlConnection connection = new MySqlConnection(_dbHelper._connectionString))
                {
                    connection.Open(); // 데이터베이스 연결 열기

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        // 상태 및 제품 이름 파라미터 설정
                        command.Parameters.AddWithValue("@State", isChecked ? 1 : 0); // 상태 값을 정수로 변환 (1: 완료, 0: 대기)
                        command.Parameters.AddWithValue("@ProductName", productName); // 제품 이름

                        int affectedRows = command.ExecuteNonQuery(); // 쿼리 실행 및 영향받은 행 수 확인
                        Debug.WriteLine($"Product state 업데이트. Rows affected: {affectedRows}"); // 디버그 메시지 출력
                    }
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 디버그 메시지 출력
                Debug.WriteLine($"Product state 업데이트 오류: {ex.Message}");
            }
        }

        // 검수 대기 상태 처리 (검수 대기 목록에서 추가/수정)
        public void ProcessPendingCheckBox(string productName, bool isChecked)
        {
            try
            {
                Debug.WriteLine($"검수대기 체크박스: ProductName={productName}, IsChecked={isChecked}"); // 디버그 메시지 출력

                // 체크가 해제된 경우 작업 종료
                if (!isChecked) return;

                // 제품 ID를 찾는 SQL 쿼리
                string findProductIdQuery = "SELECT id FROM soiso.products WHERE TRIM(name) = TRIM(@ProductName) LIMIT 1;";

                // 인벤토리 테이블에 데이터 삽입 또는 업데이트하는 SQL 쿼리
                string insertInventoryQuery = @"
                                              INSERT INTO soiso.inventory (product_id, quantity) 
                                              VALUES (@ProductId, 100)
                                              ON DUPLICATE KEY UPDATE quantity = quantity + 100;";

                using (MySqlConnection connection = new MySqlConnection(_dbHelper._connectionString))
                {
                    connection.Open(); // 데이터베이스 연결 열기

                    // 트랜잭션 시작
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int productId;

                            // Step 1: 제품 ID 가져오기
                            using (MySqlCommand findCommand = new MySqlCommand(findProductIdQuery, connection, transaction))
                            {
                                findCommand.Parameters.AddWithValue("@ProductName", productName.Trim()); // 파라미터 설정
                                object result = findCommand.ExecuteScalar(); // 쿼리 실행

                                if (result == null)
                                {
                                    // 제품을 찾을 수 없는 경우 롤백
                                    Debug.WriteLine($"ProductName에 일치하는 데이터를 찾을 수 없습니다: '{productName}'");
                                    transaction.Rollback();
                                    return;
                                }

                                productId = Convert.ToInt32(result); // 결과를 정수로 변환하여 제품 ID로 저장
                            }

                            // Step 2: 인벤토리 테이블에 데이터 삽입 또는 업데이트
                            using (MySqlCommand insertCommand = new MySqlCommand(insertInventoryQuery, connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@ProductId", productId); // 제품 ID 설정
                                int affectedRows = insertCommand.ExecuteNonQuery(); // 쿼리 실행 및 영향받은 행 수 확인
                                Debug.WriteLine($"인벤토리 업데이트 완료. 변경된 상품이름: {affectedRows}.");
                            }

                            transaction.Commit(); // 트랜잭션 커밋
                        }
                        catch (Exception ex)
                        {
                            // 트랜잭션 중 오류 발생 시 롤백
                            Debug.WriteLine($"데이터 전송 중 오류: {ex.Message}");
                            transaction.Rollback();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 디버그 메시지 출력
                Debug.WriteLine($"인벤토리 업데이트 에러: {ex.Message}");
            }
        }

        // 이미지 경로에서 BitmapImage 객체를 로드
        private BitmapImage LoadImageFromPath(string imagePath)
        {
            try
            {
                // 이미지 경로가 유효하고 파일이 존재하는지 확인
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    Debug.WriteLine("이미지");
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit(); // BitmapImage 초기화 시작
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute); // 경로를 URI로 설정
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // 로드된 데이터를 캐시
                    bitmap.EndInit(); // BitmapImage 초기화 완료
                    return bitmap; // 로드된 BitmapImage 반환
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 메시지 출력
                Console.WriteLine($"이미지 로드 실패: {ex.Message}");
            }
            return null; // 실패 시 null 반환
        }

        // 속성 변경 알림 메서드 (PropertyChanged 이벤트 호출)
        private void OnPropertyChanged(string propertyName)
        {
            // 속성 이름을 이벤트 핸들러에 전달하여 알림
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
