using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// 消息体状态
    /// </summary>
    class ClientState
    {
        public Socket socket; // 客户端与服务器的socket连接

        public byte[] readBuff = new byte[1024]; // 承载消息体的字节数组

        // 玩家属性
        public int hp = 100;
        public float x = 0;
        public float y = 0;
        public float z = 0;
        public float eulY = 0;
    }

    internal class MainClass
    {
        private static Socket listenSocket;                                                             // 监听Socket
        private static Dictionary<Socket, ClientState> clients = new Dictionary<Socket, ClientState>(); // 客户端Socket及状态信息
        private static List<Socket> checkRead = new List<Socket>();                                     // 检测是否有可读的Socket列表

        public static Dictionary<Socket, ClientState> Clients
        {
            get
            {
                return clients;
            }
        }

        private static void Main(string[] args)
        {
            // Console.WriteLine("Hello World!");

            // Socket
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Bind
            var ipAddress = IPAddress.Parse("127.0.0.1");
            var iPEndPoint = new IPEndPoint(ipAddress, 14444);
            listenSocket.Bind(iPEndPoint);

            // Listen
            listenSocket.Listen(0);
            Console.WriteLine("[服务器] 启动成功");


            // 主循环
            while (true)
            {
                checkRead.Clear();
                checkRead.Add(listenSocket);
                foreach (var s in clients.Values) checkRead.Add(s.socket);

                // 多路复用，返回可读的Socket列表
                Socket.Select(checkRead, null, null, 1000);
                foreach (var s in checkRead)
                    if (s == listenSocket)
                        ReadListenSocket(s);
                    else
                        ReadClientSocket(s);

                // 防止CPU占用率过高
                System.Threading.Thread.Sleep(1);
            }
        }

        /// <summary>
        /// 处理监听socket
        /// </summary>
        /// <param name="listenSocket">用于服务器监听Socket</param>
        private static void ReadListenSocket(Socket listenSocket)
        {
            Console.WriteLine("Socket Accept");
            var clientSocket = listenSocket.Accept();
            var clientState = new ClientState
            {
                socket = clientSocket
            };
            clients.Add(clientSocket, clientState);
        }

        /// <summary>
        /// 处理已经建立连接socket的读取
        /// </summary>
        /// <param name="clientSocket">客户端Socket</param>
        /// <returns>是否读取成功</returns>
        private static bool ReadClientSocket(Socket clientSocket)
        {
            var clientState = clients[clientSocket];
            int count;

            try
            {
                count = clientSocket.Receive(clientState.readBuff);
            }
            catch (SocketException e)
            {
                // 调用断开连接的EventHandler事件处理方法
                var methodInfo = typeof(EventHandler).GetMethod("OnDisconnect");
                object[] obj = {clientState};
                methodInfo.Invoke(null, obj);

                clientSocket.Close();
                clients.Remove(clientSocket);
                Console.WriteLine("Receive SocketException " + e.ToString());
                return false;
            }

            // 关闭客户端
            if (count == 0)
            {
                // 调用断开连接的EventHandler事件处理方法
                var methodInfo = typeof(EventHandler).GetMethod("OnDisconnect");
                object[] obj = {clientState};
                methodInfo.Invoke(null, obj);

                clientSocket.Close();
                clients.Remove(clientSocket);
                Console.WriteLine("Socket Close");
                return false;
            }

            var recvStr = System.Text.Encoding.Default.GetString(clientState.readBuff, 0, count);
            Console.WriteLine("Receive Frame: " + recvStr);
            // 切分游戏内消息体
            string[] split = recvStr.Split('|');
            string msgName = split[0];
            string msgArgs = split[1];
            string funName = "Msg" + msgName;
            // 根据消息体的内容调用MsgHandler中的事件处理方法
            MethodInfo mi = typeof(MsgHandler).GetMethod(funName);
            object[] o = {clientState, msgArgs};
            mi.Invoke(null, o);

            return true;
        }

        /// <summary>
        /// 服务器向客户端发送消息
        /// </summary>
        /// <param name="clientState">客户端socket状态</param>
        /// <param name="recvStr">需要发送的消息体</param>
        public static void Send(ClientState clientState, string recvStr)
        {
            if (clientState.socket == null)
            {
                return;
            }

            if (!clientState.socket.Connected)
            {
                return;
            }

            byte[] sendBytes = System.Text.Encoding.Default.GetBytes(recvStr);
            clientState.socket.Send(sendBytes);
            // Console.WriteLine("Server Send: " + recvStr);
        }
    }
}