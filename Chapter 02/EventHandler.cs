using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// 事件处理模块
    /// </summary>
    class EventHandler
    {
        /// <summary>
        /// 当客户端掉线时，会触发服务端的Disconnect事件，对外广播掉线的客户端。
        /// </summary>
        /// <param name="clientState">玩家客户端装备信息</param>
        public static void OnDisconnect(ClientState c)
        {
            string desc = c.socket.RemoteEndPoint.ToString();
            string sendStr = string.Format("{0}{1},", "Leave|", desc);
            foreach (ClientState cs in MainClass.Clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
            Console.WriteLine(desc + " Leave");
        }
    }
}