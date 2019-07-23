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
    class ClientState
    {
        public Socket socket;                    // 客户端与服务器的socket连接
        public byte[] readBuff = new byte[1024]; // 承载消息体的字节数组
    }

    class FinalServer
    {
        static Socket listenSocket;                                                             // 监听Socket
        static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>(); // 客户端Socket及状态信息
        static List<Socket> checkRead = new List<Socket>();                                     // 检测是否有可读的Socket列表

        static void Main1(string[] args)
        {
            Console.WriteLine("Hello World!");

            // Socket
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Bind
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint iPEndPoint = new IPEndPoint(ipAddress, 14444);
            listenSocket.Bind(iPEndPoint);

            // Listen
            listenSocket.Listen(0);
            Console.WriteLine("[服务器] 启动成功");


            // 主循环
            while (true)
            {
                checkRead.Clear();
                checkRead.Add(listenSocket);
                foreach (ClientState s in clients.Values)
                {
                    checkRead.Add(s.socket);
                }

                // 多路复用，返回可读的Socket列表
                Socket.Select(checkRead, null, null, 1000);
                foreach (Socket s in checkRead)
                {
                    if (s == listenSocket)
                    {
                        ReadListenSocket(s);
                    }
                    else
                    {
                        ReadClientSocket(s);
                    }
                }

                // 防止CPU占用率过高
                System.Threading.Thread.Sleep(1);
            }
        }

        private static bool ReadClientSocket(Socket clientSocket)
        {
            ClientState clientState = clients[clientSocket];
            int count;

            try
            {
                count = clientSocket.Receive(clientState.readBuff);
            }
            catch (SocketException e)
            {
                clientSocket.Close();
                clients.Remove(clientSocket);
                Console.WriteLine("Receive SocketException " + e.ToString());
                return false;
            }

            // 关闭客户端
            if (count == 0)
            {
                clientSocket.Close();
                clients.Remove(clientSocket);
                Console.WriteLine("Socket Close");
                return false;
            }

            string recvStr = System.Text.Encoding.Default.GetString(clientState.readBuff, 0, count);
            string sendStr = string.Format("{0} say: {1}", clientSocket.RemoteEndPoint.ToString(), recvStr);
            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);

            // 将消息分发给每个客户端
            foreach (ClientState s in clients.Values)
            {
                s.socket.Send(sendBytes);
            }

            return true;
        }

        private static void ReadListenSocket(Socket listenSocket)
        {
            Console.WriteLine("Socket Accept");
            Socket clientSocket = listenSocket.Accept();
            ClientState clientState = new ClientState();
            clientState.socket = clientSocket;
            clients.Add(clientSocket, clientState);
        }
    }
}