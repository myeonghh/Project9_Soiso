
namespace SoisoManagerSever
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 소이소 서버 시작
            SeverMain server = new SeverMain();
            await server.StartServer("10.10.20.105", 12345);
        }
    }
}
