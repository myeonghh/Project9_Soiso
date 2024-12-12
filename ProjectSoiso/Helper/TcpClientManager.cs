using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProjectSoiso.Helper
{
    internal class TcpClientManager
    {
        private static TcpClientManager _instance; // 싱글톤 인스턴스
        private static readonly object LockObject = new object();

        private TcpClient client; // TCP 클라이언트
        private NetworkStream stream;

        public event Func<string, byte[], Task> OnDataReceived;

        private string serverIp;
        private int serverPort;
        private enum ACT { ITEMINPUT, ITEMLIST };

        // 싱글톤 인스턴스 제공
        public static TcpClientManager Instance
        {
            get
            {
                lock (LockObject)
                {
                    return _instance ??= new TcpClientManager();
                }
            }
        }

        // 생성자를 private으로 제한 (외부에서 인스턴스 생성 불가)
        private TcpClientManager()
        {
        }

        // 서버 연결 설정
        public void Initialize(string serverIp, int serverPort)
        {
            this.serverIp = serverIp;
            this.serverPort = serverPort;
        }

        // 서버 연결
        public async Task<bool> Connect()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(serverIp, serverPort);
                stream = client.GetStream();

                Console.WriteLine($"서버에 연결되었습니다: {serverIp}:{serverPort}");

                // 데이터 수신 비동기로 시작
                _ = ReceiveData();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결 실패: {ex.Message}");
                return false;
            }
        }

        // 데이터 수신
        private async Task ReceiveData()
        {
            try
            {
                while (true) // 연결된 동안 계속 실행
                {
                    // 1. 헤더 읽기 (128바이트)
                    byte[] headerBuffer = new byte[128];
                    int headerBytesRead = 0;
                    while (headerBytesRead < headerBuffer.Length)
                    {
                        int bytesRead = await stream.ReadAsync(headerBuffer, headerBytesRead, headerBuffer.Length - headerBytesRead);
                        if (bytesRead == 0) throw new Exception("서버 연결이 종료되었습니다.");
                        headerBytesRead += bytesRead;
                    }

                    // 헤더 파싱 (actType, senderId, dataLength 추출)
                    string header = Encoding.UTF8.GetString(headerBuffer).TrimEnd('\0');
                    Debug.WriteLine($"{header} 헤더 수신");
                    string[] parts = header.Split('/');
                    int actType = int.Parse(parts[0]);    // 데이터 타입
                    string senderId = parts[1];          // 송신자 ID
                    int dataLength = int.Parse(parts[2]); // 본문 데이터 길이

                    // 2. 본문 데이터 읽기 (dataLength 크기만큼)
                    byte[] bodyBuffer = new byte[dataLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < dataLength)
                    {
                        int bytesRead = await stream.ReadAsync(bodyBuffer, totalBytesRead, dataLength - totalBytesRead);
                        if (bytesRead == 0) throw new Exception("서버 연결이 종료되었습니다.");
                        totalBytesRead += bytesRead;
                    }

                    // 3. 데이터 처리
                    if (OnDataReceived != null)
                    {
                        if (actType == (int)ACT.ITEMLIST) // 이미지 처리
                        {
                            // OnDataReceived에 이미지 경로 전달
                            await OnDataReceived($"{actType}/{senderId}/", bodyBuffer);
                        }
                        else // 일반 텍스트 데이터 처리
                        {
                            string body = Encoding.UTF8.GetString(bodyBuffer); // UTF-8로 디코딩
                            Debug.WriteLine($"{body} 문자열 수신");
                            await OnDataReceived($"{actType}/{senderId}/{body}", null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 연결이 종료되었습니다: {ex.Message}"); // 연결 종료 로그 출력
            }
        }

        // 데이터 전송
        public async Task SendData(int actType, string itemInfo = "", byte[] imgData = null, string msg = "")
        {
            try
            {
                // 1. 메시지 데이터 준비
                byte[] bodyBytes = string.IsNullOrEmpty(msg) ? imgData : Encoding.UTF8.GetBytes(msg);  
                // 메시지가 없으면 이미지 데이터를 본문으로, 메시지가 있으면 메시지를 UTF8로 인코딩 하여 본문으로

                // 2. 헤더 생성 (128바이트: actType, senderId, 데이터 길이 포함)
                string header = $"{actType}/{itemInfo}/{bodyBytes.Length}";
                byte[] headerBytes = Encoding.UTF8.GetBytes(header.PadRight(128, '\0')); // 128바이트로 고정 길이 패딩

                // 3. 헤더와 본문 데이터 결합
                byte[] dataToSend = new byte[headerBytes.Length + bodyBytes.Length];
                Array.Copy(headerBytes, 0, dataToSend, 0, headerBytes.Length); // 헤더 복사
                Array.Copy(bodyBytes, 0, dataToSend, headerBytes.Length, bodyBytes.Length); // 본문 복사

                // 4. 데이터 전송
                await stream.WriteAsync(dataToSend, 0, dataToSend.Length);
                Console.WriteLine($"데이터 전송 완료: {dataToSend.Length}바이트 전송");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"데이터 전송 실패: {ex.Message}");
            }
        }

        // 연결 종료
        public void Disconnect()
        {
            stream?.Close();
            client?.Close();
            Console.WriteLine("서버 연결 종료");
        }
    }
}
