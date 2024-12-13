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
using System.Reflection;
using System.Data;



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
            byte[] buffer = new byte[2048];

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length); // 데이터를 read
                    if (bytesRead > 0) // 데이터가 있으면
                    {
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"데이터 수신 완료: {data}"); // 수신된 데이터 출력

                        string[] parts = data.Split('/');
                        ACT actType = (ACT)int.Parse(parts[0]);
                        string itemInfo = parts[1];
                        string receiveMsg = parts[2];

                        await ReceiveDataOperate(clientSocket, actType, itemInfo, receiveMsg);
                    }
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

            // SQL 쿼리
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
                        await SendMessage(clientSocket, (int)ACT.ItemUnable, "", $"\"{itemName}\"\n[재고에 등록되지 않은 상품]");
                    }

                    while (reader.Read())
                    {
                        int productState = Convert.ToInt32(reader["state"]);
                        int inventoryQuantity = Convert.ToInt32(reader["quantity"]);

                        if (productState == 0)
                        {
                            // 판매 중지된 상품
                            await SendMessage(clientSocket, (int)ACT.ItemUnable, "", $"\"{itemName}\"\n[판매 중지된 상품]");
                        }
                        else if (inventoryQuantity <= 0)
                        {
                            // 재고 부족
                            await SendMessage(clientSocket, (int)ACT.ItemUnable, "", $"\"{itemName}\"\n[재고 부족]");
                        }
                        else
                        {
                            // 구매 가능
                            await SendMessage(clientSocket, (int)ACT.ItemAble, itemName, itemPrice);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류 발생: {ex.Message}");
                await SendMessage(clientSocket, (int)ACT.ItemUnable, "", $"\"{itemName}\"\n[데이터베이스 처리 오류]");
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

                string updateQuery = @"
                    UPDATE inventory i
                    JOIN products p ON i.product_id = p.id
                    SET i.quantity = i.quantity - @Quantity
                    WHERE p.name = @ProductName;
                ";

                try
                {
                    var updateCommand = dbManager.CreateCommand(updateQuery);
                    updateCommand.Parameters.AddWithValue("@Quantity", itemQuantity);
                    updateCommand.Parameters.AddWithValue("@ProductName", itemName);

                    await updateCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"재고 업데이트 완료");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"재고 업데이트 중 오류 발생: {ex.Message}");
                }
            }

            // 재고 업데이트 후 매출 테이블 업데이트 실행 (한 번만 호출)
            try
            {
                await UpdateRevenue(totalPrice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"매출 업데이트 중 오류 발생: {ex.Message}");
            }

        }

        // 매출 테이블 업데이트 메서드
        private async Task UpdateRevenue(int totalPrice)
        {
            string todayDate = DateTime.Now.ToString("yyyy-MM-dd"); // 오늘 날짜

            try
            {
                // 1. 오늘 날짜의 매출 데이터를 확인
                string selectQuery = @"
                    SELECT id, total_revenue 
                    FROM revenue 
                    WHERE date = @TodayDate;
                ";

                var selectCommand = dbManager.CreateCommand(selectQuery);
                selectCommand.Parameters.AddWithValue("@TodayDate", todayDate);

                using (var reader = await selectCommand.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        // 오늘 날짜의 데이터가 있는 경우
                        while (reader.Read())
                        {
                            int revenueId = reader.GetInt32("id");
                            decimal currentRevenue = reader.GetDecimal("total_revenue");

                            reader.Close(); // Reader 닫기

                            // 2. 기존 매출액에 더하기
                            string updateQuery = @"
                                UPDATE revenue 
                                SET total_revenue = @NewTotalRevenue
                                WHERE id = @RevenueId;
                            ";

                            var updateCommand = dbManager.CreateCommand(updateQuery);
                            updateCommand.Parameters.AddWithValue("@NewTotalRevenue", currentRevenue + totalPrice);
                            updateCommand.Parameters.AddWithValue("@RevenueId", revenueId);

                            await updateCommand.ExecuteNonQueryAsync();
                            Console.WriteLine($"매출 업데이트 완료: {totalPrice} 추가");
                            return; // 이미 업데이트를 완료했으므로 메서드 종료
                        }
                    }
                    else
                    {
                        reader.Close(); // Reader 닫기
                        // 3. 새로운 날짜 생성해서 매출액 입력
                        string insertQuery = @"
                            INSERT INTO revenue (date, total_revenue) 
                            VALUES (@TodayDate, @TotalRevenue);
                        ";

                        var insertCommand = dbManager.CreateCommand(insertQuery);
                        insertCommand.Parameters.AddWithValue("@TodayDate", todayDate);
                        insertCommand.Parameters.AddWithValue("@TotalRevenue", totalPrice);

                        await insertCommand.ExecuteNonQueryAsync();
                        Console.WriteLine($"새 매출 행 추가 완료: {totalPrice}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"매출 업데이트 중 오류 발생: {ex.Message}");
            }
        }


        // 메시지 전송
        private async Task SendMessage(TcpClient clientSocket, int actType, string itemInfo = "", string msg = "")
        {
            try
            {
                NetworkStream stream = clientSocket.GetStream();

                string fullmsg = $"{actType}/{itemInfo}/{msg}";
                byte[] data = Encoding.UTF8.GetBytes(fullmsg); // 메시지를 UTF-8로 인코딩
                await stream.WriteAsync(data, 0, data.Length);
                Console.WriteLine($"데이터 전송 완료: {Encoding.UTF8.GetString(data)}");
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
