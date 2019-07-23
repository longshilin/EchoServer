using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ChatServer
{
    /// <summary>
    /// 消息体状态
    /// </summary>
    class MessageState
    {
        public Socket socket;                    // 客户端与服务器的socket连接
        public byte[] readBuff = new byte[1024]; // 承载消息体的字节数组
    }

    class ChatServer
    {
        static Socket listenSocket;                                                               // 监听Socket
        static Dictionary<Socket, MessageState> clients = new Dictionary<Socket, MessageState>(); // 客户端Socket及状态信息

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
                MessageState clientState = new MessageState();
                clientState.socket = clientSocket;
                clients.Add(clientSocket, clientState);

                // 接收数据BeginReceive
                clientSocket.BeginReceive(clientState.readBuff, 0, 1024, 0, ReceiveCallback, clientState);
                // 继续Accept
                listenSocket.BeginAccept(AcceptCallback, listenSocket);
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
                MessageState clientState = (MessageState) ar.AsyncState;
                Socket clientSocket = clientState.socket;
                int count = clientSocket.EndReceive(ar);

                // 关闭客户端
                if (count == 0)
                {
                    clientSocket.Close();
                    clients.Remove(clientSocket);
                    Console.WriteLine("Socket Close");
                    return;
                }

                string recvStr = System.Text.Encoding.Default.GetString(clientState.readBuff, 0, count);
                string sendStr = string.Format("{0} say: {1}", clientSocket.RemoteEndPoint.ToString(), recvStr);

                byte[] sendBytes = System.Text.Encoding.Default.GetBytes(sendStr);

                // 将消息分发给每个客户端
                foreach (MessageState s in clients.Values)
                {
                    s.socket.Send(sendBytes);
                }

                clientSocket.BeginReceive(clientState.readBuff, 0, 1024, 0, ReceiveCallback, clientState);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Socket Receive fail" + e.ToString());
            }
        }
    }
}