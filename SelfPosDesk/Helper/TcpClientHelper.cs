using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SelfPosDesk.Helper
{
    internal class TcpClientHelper
    {
        private TcpClient client; // 서버와 연결하는 소켓
        private NetworkStream stream; // 데이터 송수신 스트림

        // 수신된 데이터를 전달하기 위한 이벤트
        public event Func<string, Task> OnDataReceived;

        private string serverIp;
        private int serverPort;
        private enum ACT { };

        // 생성자 (서버 IP와 포트 설정)
        public TcpClientHelper(string serverIp, int serverPort)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;
        }

        // 초기화 메서드 (비동기로 서버 연결)
        public async Task<bool> Connect()
        {
            try
            {
                client = new TcpClient(); // TCP 클라이언트 생성
                await client.ConnectAsync(serverIp, serverPort); // 서버에 비동기로 연결
                stream = client.GetStream(); // 서버와의 데이터 송수신을 위한 스트림 가져오기
                Console.WriteLine($"서버에 연결되었습니다. {serverIp}:{serverPort}");

                // 데이터 수신 시작
                _ = ReceiveData(); // 비동기로 데이터 수신 시작 (Fire-and-forget 방식)
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버에 연결할 수 없습니다: {ex.Message}");
                return false;
            }
        }

        // 데이터를 서버로 비동기로 수신
        private async Task ReceiveData()
        {
            byte[] buffer = new byte[2048]; // 데이터를 저장할 버퍼
            try
            {
                while (true) // 연결된 동안 계속 실행
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length); // 서버로부터 데이터 읽기
                    if (bytesRead > 0) // 데이터가 있으면
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead); // UTF-8로 디코딩

                        // 데이터 처리
                        if (OnDataReceived != null)
                        {
                            Debug.WriteLine($"{data} 문자열 수신");
                            await OnDataReceived(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결이 종료되었습니다: {ex.Message}"); // 연결 종료 로그 출력
            }
        }

        // 데이터를 서버로 전송
        public async Task SendData(int actType, string itemInfo = "", string msg = "")
        {
            try
            {

                string data = $"{actType}/{itemInfo}/{msg}";
                // 본문 데이터 준비
                byte[] bodyBytes = Encoding.UTF8.GetBytes(msg);
                byte[] bytes = Encoding.UTF8.GetBytes(data); // UTF-8로 인코딩
                await stream.WriteAsync(bytes, 0, bytes.Length); // 데이터를 서버로 전송

            }
            catch (Exception ex)
            {
                Console.WriteLine($"데이터 전송 실패: {ex.Message}");
            }
        }

        // 연결 종료
        public void Disconnect()
        {
            try
            {
                stream?.Close(); // 스트림 닫기
                client?.Close(); // 클라이언트 소켓 닫기
                Console.WriteLine("서버와의 연결이 종료되었습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"연결 종료 실패: {ex.Message}");
            }
        }
    }
}
