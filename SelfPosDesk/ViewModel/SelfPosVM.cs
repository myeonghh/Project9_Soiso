using SelfPosDesk.View;
using SelfPosDesk.ViewModel.Command;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports; // 시리얼 포트 사용

using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using AForge.Video.DirectShow;
using System.Windows;
using System.Diagnostics;
using ZstdSharp.Unsafe;
using SelfPosDesk.Helper;
using System.Windows.Interop;


namespace SelfPosDesk.ViewModel
{
    public class SelfPosVM : INotifyPropertyChanged
    {
        private object _currentView;
        private decimal _totalAmount;
        private decimal _totalCount;
        private decimal _alltotalAmount;
        private SerialPort _serialPort; // 아두이노 시리얼 포트
        private TcpClientHelper clientManager; // tcp통신 클래스 객체 선언
        private bool connectSuccess;

        private enum ACT { ItemCheck, ItemAble, ItemUnable, BuyItem };

        private readonly TimeSpan _processingDelay = TimeSpan.FromSeconds(3); // QR 처리 지연 시간
        private DateTime _lastProcessedTime = DateTime.MinValue; // 마지막 처리 시간

        // 현재 표시 중인 뷰
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        // 카메라 미리보기
        private BitmapImage _cameraPreview;
        public BitmapImage CameraPreview
        {
            get => _cameraPreview;
            set
            {
                _cameraPreview = value;
                Debug.WriteLine("PropertyChanged 이벤트 발생: CameraPreview"); // 디버깅 메시지
                OnPropertyChanged(); // UI 갱신
            }
        }

        // 총 금액
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                _totalAmount = value;
                OnPropertyChanged();
            }
        }

        public decimal TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged();
            }
        }
        public decimal AlltotalAmount
        {
            get => _alltotalAmount;
            set
            {
                _alltotalAmount = value;
                OnPropertyChanged();
            }
        }

        // 상품 리스트
        public ObservableCollection<Product> Products { get; set; }

        // 명령어들
        public ICommand StartCommand { get; }
        public ICommand StartCameraCommand { get; }
        public ICommand StopCameraCommand { get; }
        public ICommand PaymentCommand { get; }


        private QRCodeProcessor _qrProcessor;

        public SelfPosVM()
        {
            // 초기 화면 설정
            CurrentView = new IntroView();

            // 상품 리스트 초기화
            Products = new ObservableCollection<Product>();
            
            CheckWebcamDevices();
                        
            _qrProcessor = new QRCodeProcessor(); // QR 코드 프로세서 초기화
            _qrProcessor.CameraFrameUpdated += OnPreviewFrameAvailable; // 카메라 프레임 이벤트 연결
            _qrProcessor.QRCodeScanned += OnQRCodeScanned; // QR 코드 스캔 이벤트 연결
                       
            InitializeSerialPort(); // 아두이노 시리얼 포트 초기화

            // 명령어 초기화
            StartCommand = new RelayCommand(ChangeToCartView);
            StartCameraCommand = new RelayCommand(_ => StartCamera());
            StopCameraCommand = new RelayCommand(_ => StopCamera());
            PaymentCommand = new RelayCommand(_ => SendToServer());

            // 카메라 자동 시작
            StartCamera();

            // 서버 연결
            SeverConnect();
        }

        private async void SeverConnect()
        {
            string ip = "10.10.20.105"; // 서버 IP
            int port = 12345; // 서버 포트

            clientManager = new TcpClientHelper(ip, port); // TcpClientManager 객체 생성
            connectSuccess = await clientManager.Connect(); // 비동기로 초기화

            if (connectSuccess)
            {
                //severConnectTextBlock.Text = $"서버연결 성공  [ IP: {ip} PORT: {port} ]";
                clientManager.OnDataReceived += HandleServerData; // 서버에서 수신한 데이터 처리
            }
            else
            {
                //severConnectTextBlock.Text = "서버연결 실패";
            }
        }

        private async Task HandleServerData(string data)
        {

        }

        private void InitializeSerialPort()
        {
            try
            {
                _serialPort = new SerialPort("COM4", 9600)
                {
                    Encoding = System.Text.Encoding.ASCII
                };
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"시리얼 포트를 열 수 없습니다: {ex.Message}");
            }
        }

        private void ChangeToCartView(object parameter)
        {
            // CartView 화면으로 변경
            CurrentView = new CartView();
        }
        private void CheckWebcamDevices()
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                Console.WriteLine("웹캠이 연결되지 않았습니다.");
                MessageBox.Show("웹캠이 연결되지 않았습니다.");
                return;
            }

            foreach (FilterInfo device in videoDevices)
            {
                Console.WriteLine($"웹캠 발견: {device.Name}");
            }
        }

        private void StartCamera()
        {
            _qrProcessor.StartCamera();
        }

        private void StopCamera()
        {
            _qrProcessor.StopCamera();
        }

        private async void SendToServer()
        {
            if (Products == null || Products.Count == 0)
            {
                MessageBox.Show("장바구니에 상품이 없습니다.");
                return;
            }

            // 상품 정보를 직렬화
            string serializedProducts = string.Join("|", Products.Select(p => $"{p.ProductName},{p.Price},{p.Quantity}"));

            // TCP 통신을 이용해 서버로 데이터 전송
            if (connectSuccess && clientManager != null)
            {
                try
                {
                    // 결제한 상품 정보 서버로 전송
                    await clientManager.SendData((int)ACT.BuyItem, AlltotalAmount.ToString(), serializedProducts);
                    MessageBox.Show("상품 정보를 서버로 전송했습니다.");

                    // 장바구니 초기화
                    Products = new ObservableCollection<Product>();
                    OnPropertyChanged(nameof(Products)); // PropertyChanged 이벤트 발생
                    UpdateTotalAmount(); // 총 금액 초기화
                    UpdateTotalcount();  // 총 수량 초기화
                    UpdateTotalAllAmount(); // 총 금액 초기화
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"상품 정보를 전송하는 데 실패했습니다: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("서버와 연결되어 있지 않습니다.");
            }
        }

        private async void OnQRCodeScanned(object sender, string qrCodeData)
        {
            // QR 코드 처리 지연 시간 확인
            if (DateTime.Now - _lastProcessedTime < _processingDelay)
            {
                return; // 지정된 대기 시간 이내라면 처리하지 않음
            }

            _lastProcessedTime = DateTime.Now; // 마지막 처리 시간 업데이트

            // QR 코드 데이터 처리
            var data = qrCodeData.Split(',');
            var productName = data.FirstOrDefault(d => d.StartsWith("상품명:"))?.Replace("상품명:", "").Trim();
            var priceText = data.FirstOrDefault(d => d.StartsWith("가격:"))?.Replace("가격:", "").Trim();

            if (!string.IsNullOrEmpty(productName) && decimal.TryParse(priceText, out decimal price))
            {
                await clientManager.SendData((int)ACT.ItemCheck, productName, priceText);


                var existingProduct = Products.FirstOrDefault(p => p.ProductName == productName); 
                
                if (existingProduct != null)
                {
                    // 동일 상품이 이미 있는 경우 수량 증가 및 금액 계산
                    existingProduct.Quantity++;
                    existingProduct.TotalPrice = existingProduct.Price * existingProduct.Quantity;
                    UpdateTotalAllAmount();
                }
                else
                {
                    // 새로운 상품 추가
                    Products.Add(new Product
                    {
                        ProductName = productName,
                        Price = price,
                        Quantity = 1,
                        TotalPrice = price
                    });
                }

                // 총 금액 업데이트
                UpdateTotalAmount();
                UpdateTotalcount();
                UpdateTotalAllAmount();
                // 아두이노 부저 울리기
                SendBuzzSignal();
            }
        }

        private void UpdateTotalcount()
        {
            TotalCount = Products.Sum(p => p.Quantity);
        }

        private void UpdateTotalAmount()
        {
            TotalAmount = Products.Sum(p => p.TotalPrice);
        }

        private void UpdateTotalAllAmount()
        {
            AlltotalAmount = Products.Sum(p => p.TotalPrice);
        }
        private void OnPreviewFrameAvailable(object sender, BitmapImage frame)
        {
            Debug.WriteLine("OnPreviewFrameAvailable 호출됨");
            CameraPreview = frame;
            Debug.WriteLine($"CameraPreview 설정됨: {CameraPreview}");
            OnPropertyChanged(nameof(CameraPreview)); // 강제 갱신
        }

        private void SendBuzzSignal()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.WriteLine("BUZZ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BUZZ 신호 전송 실패: {ex.Message}");
            }
        }

        public void ForceUpdate()
        {
            OnPropertyChanged(nameof(CameraPreview)); // 강제 갱신
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
