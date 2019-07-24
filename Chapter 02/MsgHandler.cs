using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    /// <summary>
    /// 消息处理模块
    /// </summary>
    class MsgHandler
    {
        /// <summary>
        /// 服务端收到Enter协议之后，记录玩家信息到ClientState再广播出去
        /// </summary>
        public static void MsgEnter(ClientState c, string msgArgs)
        {
            // 解析参数
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float x = float.Parse(split[1]);
            float y = float.Parse(split[2]);
            float z = float.Parse(split[3]);
            float eulY = float.Parse(split[4]);

            // 赋值
            c.hp = 100;
            c.x = x;
            c.y = y;
            c.z = z;
            c.eulY = eulY;

            // 广播
            string sendStr = "Enter|" + msgArgs;
            foreach (ClientState cs in MainClass.Clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
        }

        /// <summary>
        /// 服务端收到List协议之后，组装房间所有玩家信息为消息体，将玩家列表广播给所有客户端
        /// </summary>
        public static void MsgList(ClientState c, string msgArgs)
        {
            string sendStr = "List|";
            foreach (ClientState cs in MainClass.Clients.Values)
            {
                sendStr += string.Format("{0},", cs.socket.RemoteEndPoint.ToString());
                sendStr += string.Format("{0},", cs.x.ToString());
                sendStr += string.Format("{0},", cs.y.ToString());
                sendStr += string.Format("{0},", cs.z.ToString());
                sendStr += string.Format("{0},", cs.eulY.ToString());
                sendStr += string.Format("{0},", cs.hp.ToString());
            }

            MainClass.Send(c, sendStr);
        }

        /// <summary>
        /// 服务端收到Move协议之后，解析参数，记录坐标信息到ClientState，然后广播Move协议各所有端
        /// </summary>
        public static void MsgMove(ClientState c, string msgArgs)
        {
            // 解析参数
            string[] split = msgArgs.Split(',');
            string desc = split[0];
            float x = float.Parse(split[1]);
            float y = float.Parse(split[2]);
            float z = float.Parse(split[3]);

            // 解析参数
            c.x = x;
            c.y = y;
            c.z = z;

            // 广播
            string sendStr = "Move|" + msgArgs;
            foreach (ClientState cs in MainClass.Clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
        }

        /// <summary>
        /// 转发Attack协议内容
        /// </summary>
        public static void MsgAttack(ClientState c, string msgArgs)
        {
            // 广播
            string sendStr = "Attack|" + msgArgs;
            foreach (ClientState cs in MainClass.Clients.Values)
            {
                MainClass.Send(cs, sendStr);
            }
        }

        /// <summary>
        /// 找出收到攻击的角色，然后扣血；
        /// 当被攻击的角色血量小于等于0，代表角色死亡，服务端会广播Die协议，通知客户端清除该角色
        /// </summary>
        public static void MsgHit(ClientState c, string msgArgs)
        {
            // 解析参数
            string[] split = msgArgs.Split(',');
            string attDesc = split[0];
            string hitDesc = split[1];

            // 找出被攻击的角色
            ClientState hitCS = null;
            foreach (ClientState cs in MainClass.Clients.Values)
            {
                if (cs.socket.RemoteEndPoint.ToString() == hitDesc)
                {
                    hitCS = cs;
                }
            }

            if (hitCS == null)
            {
                return;
            }

            // 扣血
            hitCS.hp -= 25;
            Console.WriteLine(hitDesc.ToString() + " HP: " + hitCS.hp);
            // 死亡
            if (hitCS.hp <= 0)
            {
                string sendStr = "Die|" + hitCS.socket.RemoteEndPoint.ToString();
                foreach (ClientState cs in MainClass.Clients.Values)
                {
                    MainClass.Send(cs, sendStr);
                }

                MainClass.Clients.Remove(hitCS.socket); // 从玩家列表中清除掉
            }
        }
    }
}