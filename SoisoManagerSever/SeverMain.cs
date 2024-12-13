using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Asn1.X509;
using static Mysqlx.Notice.Warning.Types;
using System.Collections.Concurrent;
using ZstdSharp.Unsafe;
using Mysqlx.Crud;
using QRCoder;
using System.Reflection.PortableExecutable;



namespace SoisoManagerSever
{
    internal class SeverMain
    {
        private List<TcpClient> clients = new List<TcpClient>();


        private TcpListener server;
        private readonly object clientLock = new object();
        private DatabaseManager dbManager = new DatabaseManager();
        private int cnum = 1;

        private enum ACT { ItemCheck, ItemAble, ItemUnable, BuyItem };

        // 서버 시작
        public async Task StartServer(string ip, int port)
        {
            string dbConnectString = "Server=localhost;Port=3306;Database=soiso;Uid=root;Pwd=1234;";
            dbManager.Connect(dbConnectString);

            server = new TcpListener(IPAddress.Parse(ip), port);
            server.Start();
            Console.WriteLine($"서버가 {ip}:{port}에서 시작되었습니다.");

            try
            {
                while (true)
                {
                    TcpClient clientSocket = await server.AcceptTcpClientAsync(); // 비동기로 클라이언트 연결 수락

                    lock (clientLock)
                    {
                        clients.Add(clientSocket);
                    }
                    Console.WriteLine($"클라이언트가 연결되었습니다. 현재 클라이언트 수: {clients.Count}");

                    _ = HandleClient(clientSocket); // 클라이언트 데이터 처리
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"서버 실행 중 오류 발생: {ex.Message}");
            }
        }

        // 클라이언트와 통신
        private async Task HandleClient(TcpClient clientSocket)
        {
            NetworkStream stream = clientSocket.GetStream();
            byte[] headerBuffer = new byte[128]; // 고정 크기 헤더 버퍼

            try
            {
                while (true)
                {
                    // 1. 고정된 128바이트 헤더 읽기
                    int headerBytesRead = 0;
                    while (headerBytesRead < headerBuffer.Length)
                    {
                        int bytesRead = await stream.ReadAsync(headerBuffer, headerBytesRead, headerBuffer.Length - headerBytesRead);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("클라이언트 연결 종료");
                            return;
                        }
                        headerBytesRead += bytesRead;
                    }

                    // 2. 헤더 파싱 (actType, senderId, 데이터 크기 읽기)
                    string header = Encoding.UTF8.GetString(headerBuffer).TrimEnd('\0');
                    string[] parts = header.Split('/');
                    ACT actType = (ACT)int.Parse(parts[0]); // actType
                    string itemInfo = parts[1];             // senderId
                    int dataLength = int.Parse(parts[2]);   // 데이터 크기

                    Console.WriteLine($"헤더 수신: 타입={actType}, 상품 정보={itemInfo}, 데이터 크기={dataLength}");

                    // 3. 데이터 수신 (dataLength 크기만큼 읽기)
                    byte[] dataBuffer = new byte[dataLength];
                    int totalDataBytesRead = 0;

                    while (totalDataBytesRead < dataLength)
                    {
                        int bytesRead = await stream.ReadAsync(dataBuffer, totalDataBytesRead, dataLength - totalDataBytesRead);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("데이터 수신 중 연결 종료");
                            return;
                        }
                        totalDataBytesRead += bytesRead;
                    }
              
                    string receiveMsg = Encoding.UTF8.GetString(dataBuffer);
                    Console.WriteLine($"수신 데이터: {receiveMsg}");

                    // 서버 데이터 수신 작업 처리 메서드 호출
                    await ReceiveDataOperate(clientSocket, actType, itemInfo, receiveMsg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"클라이언트 통신 오류: {ex.Message}");
            }
            finally
            {
                lock (clientLock)
                {
                    clients.Remove(clientSocket);
                }

                Console.WriteLine("클라이언트가 연결을 종료했습니다.");
                clientSocket.Close();
            }
        }

        // 서버 수신 작업 처리
        private async Task ReceiveDataOperate(TcpClient clientSocket, ACT actType, string itemInfo, string receiveMsg)
        {

            switch (actType)
            {
                case ACT.ItemCheck:
                    await ItemPossibleCheck(clientSocket, itemInfo, receiveMsg);
                    break;
                case ACT.BuyItem:
                    await DatabaseUpdate(clientSocket, itemInfo, receiveMsg);
                    break;
                default:
                    break;
            }
        }

        private async Task ItemPossibleCheck(TcpClient clientSocket, string itemInfo, string receiveMsg)
        {
            string itemName = itemInfo;
            string itemPrice = receiveMsg;

            // SQL 쿼리 (INNER JOIN 사용)
            string query = @"
                    SELECT 
                        p.name,
                        p.state,
                        i.quantity
                    FROM 
                        products p
                    INNER JOIN 
                        inventory i ON p.id = i.product_id
                    WHERE 
                        p.name = @ProductName;
                ";

            try
            {
                var command = dbManager.CreateCommand(query);
                command.Parameters.AddWithValue("@ProductName", itemName);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {
                        // 결과가 없는 경우 등록되지 않은 상품
                        await SendMessage(clientSocket, (int)ACT.ItemUnable, $"{itemName}:재고에 등록되지 않은 상품");
                    }

                    while (reader.Read())
                    {
                        int productState = Convert.ToInt32(reader["state"]);
                        int inventoryQuantity = Convert.ToInt32(reader["quantity"]);

                        if (productState == 0)
                        {
                            // 판매 중지된 상품
                            await SendMessage(clientSocket, (int)ACT.ItemUnable, $"{itemName}:재고에 등록되지 않은 상품");
                        }
                        else if (inventoryQuantity < itemQuantity)
                        {
                            // 재고 부족
                            buyDeniedItems.Add($"{itemName}:재고 부족");
                        }
                        else
                        {
                            // 구매 가능
                            successfulPurchases.Add((itemName, itemQuantity));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
                buyDeniedItems.Add($"{itemName}: 데이터베이스 처리 오류");
            }

        }



        private async Task DatabaseUpdate(TcpClient clientSocket, string itemInfo, string receiveMsg)
        {
            int totalPrice = int.Parse(itemInfo);
            List<string> buyDeniedItems = new List<string>(); // 구매가 거절된 상품 리스트
            List<(string ItemName, int Quantity)> successfulPurchases = new List<(string, int)>(); // 성공적으로 구매된 상품

            string[] buyItems = receiveMsg.Split('|');

            foreach (string item in buyItems)
            {
                string[] itemInfoParts = item.Split(',');
                string itemName = itemInfoParts[0]; // 상품 이름
                int itemPrice = int.Parse(itemInfoParts[1]); // 상품 가격
                int itemQuantity = int.Parse(itemInfoParts[2]); // 상품 수량

                // SQL 쿼리 (INNER JOIN 사용)
                string query = @"
                    SELECT 
                        p.name,
                        p.state,
                        i.quantity
                    FROM 
                        products p
                    INNER JOIN 
                        inventory i ON p.id = i.product_id
                    WHERE 
                        p.name = @ProductName;
                ";

                try
                {
                    var command = dbManager.CreateCommand(query);
                    command.Parameters.AddWithValue("@ProductName", itemName);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            // 결과가 없는 경우 등록되지 않은 상품
                            buyDeniedItems.Add($"{itemName}:재고에 등록되지 않은 상품");
                            continue;
                        }

                        while (reader.Read())
                        {
                            int productState = Convert.ToInt32(reader["state"]);
                            int inventoryQuantity = Convert.ToInt32(reader["quantity"]);

                            if (productState == 0)
                            {
                                // 판매 중지된 상품
                                buyDeniedItems.Add($"{itemName}:판매 중지된 상품");
                            }
                            else if (inventoryQuantity < itemQuantity)
                            {
                                // 재고 부족
                                buyDeniedItems.Add($"{itemName}:재고 부족");
                            }
                            else
                            {
                                // 구매 가능
                                successfulPurchases.Add((itemName, itemQuantity));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"오류 발생: {ex.Message}");
                    buyDeniedItems.Add($"{itemName}: 데이터베이스 처리 오류");
                }
            }

            // 성공적으로 구매된 상품의 재고 차감
            foreach (var purchase in successfulPurchases)
            {
                string updateQuery = @"
                    UPDATE inventory i
                    JOIN products p ON i.product_id = p.id
                    SET i.quantity = i.quantity - @Quantity
                    WHERE p.name = @ProductName;
                ";

                try
                {
                    var updateCommand = dbManager.CreateCommand(updateQuery);
                    updateCommand.Parameters.AddWithValue("@Quantity", purchase.Quantity);
                    updateCommand.Parameters.AddWithValue("@ProductName", purchase.ItemName);

                    await updateCommand.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"재고 업데이트 중 오류 발생: {ex.Message}");
                }
            }

            if (buyDeniedItems.Count > 0)
            {
                // 예외 상품 목록 클라이언트로 전송
                string deniedMessage = string.Join("\n", buyDeniedItems);
                Console.WriteLine($"구매 실패 항목:\n{deniedMessage}");
                // 클라이언트로 전송 로직 추가

            }
            else
            {
                Console.WriteLine("모든 상품 구매");
            }
        }


        // 메시지 전송
        private async Task SendMessage(TcpClient clientSocket, int actType, string msg = "", string itemInfo = "")
        {
            try
            {
                NetworkStream stream = clientSocket.GetStream();

                // 1. 메시지 데이터 준비
                byte[] bodyBytes;

                bodyBytes = Encoding.UTF8.GetBytes(msg); // UTF-8로 인코딩
                
                // 2. 헤더 준비 (128바이트: actType, senderId, 데이터 길이 포함)
                string header = $"{actType}/{itemInfo}/{bodyBytes.Length}";
                byte[] headerBytes = Encoding.UTF8.GetBytes(header.PadRight(128, '\0')); // 128바이트 고정 길이로 패딩

                // 3. 헤더와 데이터 결합
                byte[] fullData = new byte[headerBytes.Length + bodyBytes.Length];
                Array.Copy(headerBytes, 0, fullData, 0, headerBytes.Length); // 헤더 복사
                Array.Copy(bodyBytes, 0, fullData, headerBytes.Length, bodyBytes.Length); // 본문 데이터 복사

                // 4. 데이터 전송
                await stream.WriteAsync(fullData, 0, fullData.Length);

                Console.WriteLine($"데이터 전송 완료: {Encoding.UTF8.GetString(fullData)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"메시지 전송 오류: {ex.Message}");
            }
        }

        ~SeverMain()
        {
            dbManager.Disconnect();
        }
    }
}
