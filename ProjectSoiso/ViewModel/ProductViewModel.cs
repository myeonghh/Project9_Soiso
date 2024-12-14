using Microsoft.Win32;
using ProjectSoiso.Model;
using MySql.Data.MySqlClient;
using QRCoder;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ProjectSoiso.Helper;
using LiveCharts.Wpf.Charts.Base;

namespace ProjectSoiso.ViewModel
{
    /// <summary>
    /// ViewModel 클래스 - MVVM 패턴에서 View와 Model 간의 중간 역할 수행
    /// </summary>
    public class ProductViewModel : ViewModelBase
    {
        // MySQL 연결 문자열 (DB 연결 정보)
        private readonly string _connectionString = "Server=127.0.0.1;Database=soiso;Uid=root;Pwd=1234;SslMode=None;";
        private enum ACT { ProductInput };
        // 메시지(등록 상태나 오류 메시지)를 저장하는 속성
        private string _message;
        // 현재 상품 정보 저장 (Model과 연결)
        private Product _currentProduct;
        // TCP Manager 인스턴스
        private readonly TcpClientManager _tcpClientManager;

        private BitmapImage _previewImageSource;

        public BitmapImage PreviewImageSource
        {
            get => _previewImageSource;
            set
            {
                _previewImageSource = value;
                OnPropertyChanged(nameof(PreviewImageSource));
            }
        }

        /// <summary>
        /// 현재 상품 정보 (View와 데이터 바인딩)
        /// </summary>
        public Product CurrentProduct
        {
            get => _currentProduct;
            set
            {
                _currentProduct = value;
                OnPropertyChanged(nameof(CurrentProduct));
            }
        }

        /// <summary>
        /// View에 표시할 메시지 (성공/실패 알림)
        /// </summary>
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        public ICommand SelectImageCommand { get; }    // 이미지 선택 명령 (Command)

        public ICommand RegisterCommand { get; }    // 상품 등록 명령 (Command)

        public ICommand GoBackCommand { get; } // 뒤로가기 명령 (Command)


        public ProductViewModel()
        {
            _tcpClientManager = TcpClientManager.Instance; // 싱글톤으로 TCP 연결 재사용
            CurrentProduct = new Product();    // Product 모델 초기화

            SelectImageCommand = new RelayCommand(ExecuteSelectImage); // 이미지 선택 명령 초기화
            //RegisterCommand = new RelayCommand(ExecuteRegisterProduct, CanExecuteRegisterProduct); // 상품 등록 명령 초기화
            RegisterCommand = new RelayCommand(ExecuteRegisterProduct); // 항상 버튼 활성화
            GoBackCommand = new RelayCommand(ExecuteGoBack); // 뒤로가기 명령 초기화
            ClearInputs();
        }

        private void ExecuteGoBack(object parameter)
        {
            // MainPage.xaml 창 열기
            var mainPage = new ProjectSoiso.View.MainPage();
            mainPage.Show();

            // 현재 창 닫기
            Application.Current.Windows[0]?.Close();
        }

        // 이미지 선택 기능
        private void ExecuteSelectImage(object parameter)
        // 이미지 선택 명령 구현
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "상품 이미지 선택",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp", // 파일 확장자
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) // 초기 경로
            };

            // 파일 선택 후 처리
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedImagePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(selectedImagePath).ToLower();
                // 이미지 미리보기 업데이트

                // 허용된 확장자인지 확인
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };

                if (Array.Exists(allowedExtensions, ext => ext == fileExtension))
                {
                    // 이미지 미리보기에 이미지 적용
                    PreviewImageSource = new BitmapImage(new Uri(selectedImagePath));

                    //// D:\ProductImages 디렉토리로 복사
                    //string directoryPath = @"C:\ProductImages";
                    //if (!Directory.Exists(directoryPath))
                    //{
                    //    Directory.CreateDirectory(directoryPath); // 디렉토리가 없으면 생성
                    //}

                    string exeDirectory = AppDomain.CurrentDomain.BaseDirectory; // 실행 파일 위치
                    // 프로젝트 루트 폴더 내 ProductImages 경로 생성
                    string productImagesDirectory = Path.Combine(exeDirectory, "ProductImages");
                    // 로그 디렉토리 생성
                    if (!Directory.Exists(productImagesDirectory))
                    {
                        Directory.CreateDirectory(productImagesDirectory);
                    }

                    // 새 파일 경로 설정
                    string newFilePath = Path.Combine(productImagesDirectory, Path.GetFileName(selectedImagePath));
                    File.Copy(selectedImagePath, newFilePath, true); // 파일 복사

                    // 복사된 이미지 경로를 모델에 저장
                    CurrentProduct.ImgPath = newFilePath;
                    Message = "이미지가 성공적으로 저장되었습니다.";
                }
                else
                {
                    // 잘못된 확장자 선택 시 알림
                    MessageBox.Show("허용되지 않은 파일 형식입니다. 이미지만 선택해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteRegisterProduct(object parameter)
        // 상품 등록 명령 구현
        {
            // 입력값 확인
            if (string.IsNullOrEmpty(CurrentProduct.Name) ||
                string.IsNullOrEmpty(CurrentProduct.Category) ||
                CurrentProduct.Price <= 0 ||
                string.IsNullOrEmpty(CurrentProduct.ImgPath))
            {
                // 입력값이 누락된 경우 메시지 설정
                Message = "모든 입력값을 올바르게 입력하세요.";
                return;
            }

            // 상품명이 중복인지 확인
            if (IsProductNameDuplicate(CurrentProduct.Name))
            {
                Message = "상품명이 이미 존재합니다.";
                return;
            }

            // QR 코드 생성
            string qrPath = GenerateQRCode($"상품명:{CurrentProduct.Name},가격:{CurrentProduct.Price}", CurrentProduct.Name);
            if (string.IsNullOrEmpty(qrPath))
            {
                Message = "QR 코드 생성 실패.";
                return;
            }

            CurrentProduct.QrPath = qrPath; // QR 코드 경로 저장

            // 데이터베이스에 상품 정보 삽입
            bool dbResult = InsertProductIntoDatabase(CurrentProduct);
            if (dbResult)
            {
                MessageBox.Show("상품 등록이 완료되었습니다!", "등록 완료", MessageBoxButton.OK, MessageBoxImage.Information);

                // 입력값 초기화
                ClearInputs();
            }
            else
            {
                Message = "상품 등록 실패.";
            }
        }

        private bool IsProductNameDuplicate(string name)
        // 상품명이 중복인지 확인
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT COUNT(*) FROM Products WHERE name = @name";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Message = $"DB 연결 실패: {ex.Message}";
                return false;
            }
        }

        private string GenerateQRCode(string ProductInfo, string productName)
        // QR 코드 생성
        {
            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(ProductInfo, QRCodeGenerator.ECCLevel.Q);
                    using (QRCode qrCode = new QRCode(qrCodeData))
                    {
                        using (Bitmap qrBitmap = qrCode.GetGraphic(20))
                        {

                            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory; // 실행 파일 위치
                            // 프로젝트 루트 폴더 내 QRCodeImages 경로 생성
                            string QRCodeImagesDirectory = Path.Combine(exeDirectory, "QRCodeImages");
                            // 로그 디렉토리 생성
                            if (!Directory.Exists(QRCodeImagesDirectory))
                            {
                                Directory.CreateDirectory(QRCodeImagesDirectory);
                            }

                            string filePath = Path.Combine(QRCodeImagesDirectory, $"{productName}.png");
                            qrBitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                            return filePath; // QR 코드 경로 반환
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Message = $"QR 코드 생성 중 오류 발생: {ex.Message}";
                return null;
            }
        }

        private bool InsertProductIntoDatabase(Product product)
        // 데이터베이스에 상품 정보 삽입
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                    INSERT INTO Products (name, category, price, img_path, qr_path, state, created_at)
                    VALUES (@name, @category, @price, @imgPath, @qrPath, @state, NOW());
                ";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", product.Name);
                        command.Parameters.AddWithValue("@category", product.Category);
                        command.Parameters.AddWithValue("@price", product.Price);
                        command.Parameters.AddWithValue("@imgPath", product.ImgPath);
                        command.Parameters.AddWithValue("@qrPath", product.QrPath);
                        command.Parameters.AddWithValue("@state", 0); // 1 : 판매 중

                        return command.ExecuteNonQuery() > 0; // 성공 여부 반환
                    }
                }
            }
            catch (Exception ex)
            {
                Message = $"DB 저장 실패: {ex.Message}";
                return false;
            }
        }

        private bool CanExecuteRegisterProduct(object parameter)
        // 상품 등록 버튼 활성화 조건(모든 필수 입력값이 채워졌는지 확인 & 이미지가 등록되었는지 확인)
        {
            return !string.IsNullOrEmpty(CurrentProduct.Name) &&
                           !string.IsNullOrEmpty(CurrentProduct.Category) &&
                           CurrentProduct.Price > 0 &&
                           !string.IsNullOrEmpty(CurrentProduct.ImgPath);
        }

        private void ClearInputs()
        {
            CurrentProduct = new Product(); // 모델 초기화
            Message = string.Empty;         // 메시지 초기화        
            PreviewImageSource = new BitmapImage(new Uri("/image/default.jpg", UriKind.Relative));  // 이미지 미리보기 초기화
        }

        // INotifyPropertyChanged 구현
        public event PropertyChangedEventHandler PropertyChanged;

    }
}
