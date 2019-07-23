using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FinalChatServer
{
    /// <summary>
    /// 消息体状态
    /// </summary>
    class EchoState
    {
        public Socket socket;                    // 客户端与服务器的socket连接
        public byte[] readBuff = new byte[1024]; // 承载消息体的字节数组
    }

    class EchoServer
    {
        static Socket listenSocket;                                                             // 监听Socket
        static Dictionary<Socket, EchoState> clients = new Dictionary<Socket, EchoState>(); // 客户端Socket及状态信息

        static void Main1(string[] args)
        {
            Console.WriteLine("Hello World!");

            // Socket
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Bind
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 13333);
            listenSocket.Bind(iPEndPoint);

            // Listen
            listenSocket.Listen(0);
            Console.WriteLine("[服务器] 启动成功");
            listenSocket.BeginAccept(AcceptCallback, listenSocket);

            // 等待
            Console.Read();
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Console.WriteLine("[服务器] Accept");
                Socket listenSocket = (Socket) ar.AsyncState;
                Socket clientSocket = listenSocket.EndAccept(ar);

                // clients列表
                EchoState echoState = new EchoState();
                echoState.socket = clientSocket;
                clients.Add(clientSocket, echoState);

                // 接收数据BeginReceive
                clientSocket.BeginReceive(echoState.readBuff, 0, 1024, 0, ReceiveCallback, echoState);
            }
            catch (Exception e)
            {
                Console.WriteLine("Socket Accept fail" + e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                EchoState echoState = (EchoState) ar.AsyncState;
                Socket clientSocket = echoState.socket;
                int count = clientSocket.EndReceive(ar);

                // 关闭客户端
                if (count == 0)
                {
                    clientSocket.Close();
                    clients.Remove(clientSocket);
                    Console.WriteLine("Socket Close");
                    return;
                }

                string readStr = System.Text.Encoding.Default.GetString(echoState.readBuff, 0, count);
                byte[] sendBytes = System.Text.Encoding.Default.GetBytes(readStr);
                clientSocket.Send(sendBytes);
                clientSocket.BeginReceive(echoState.readBuff, 0, 1024, 0, ReceiveCallback, echoState);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Socket Receive fail" + e.ToString());
            }
        }
    }
}