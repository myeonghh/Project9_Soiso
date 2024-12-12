using System.Windows.Input;
using ProjectSoiso.Helper;

namespace ProjectSoiso.ViewModel
{
    public class MainPageViewModel : ViewModelBase
    {
        public ICommand OpenInStockViewCommand { get; }
        public ICommand OpenSalesViewCommand { get; }

        public ICommand OpenInventoryViewCommand { get; }

        private string _connectionStatus;

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged(nameof(ConnectionStatus));
            }
        }

        public MainPageViewModel()
        {
            OpenInStockViewCommand = new RelayCommand(typeof(View.InStockView));
            OpenSalesViewCommand = new RelayCommand(typeof(View.SalesView));
            OpenInventoryViewCommand = new RelayCommand(typeof(View.InventoryView));
            ConnectionStatus = "서버 연결 중...";
            _ = InitializeTcpConnection(); // 비동기로 TCP 연결 초기화
        }

        private async Task InitializeTcpConnection()
        {
            try
            {
                string ip = "10.10.20.105";
                int port = 12345;

                var tcpManager = TcpClientManager.Instance;
                tcpManager.Initialize(ip, port);

                bool connected = await tcpManager.Connect();
                ConnectionStatus = connected ? $"서버연결 성공  [ IP: {ip} PORT: {port} ]" : "서버 연결 실패";
            }
            catch (Exception ex)
            {
                ConnectionStatus = $"연결 오류: {ex.Message}";
            }
        }

    }
}
