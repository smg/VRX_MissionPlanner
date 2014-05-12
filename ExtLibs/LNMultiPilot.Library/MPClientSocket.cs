using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LNMultiPilot.Library;

namespace LNMultiPilot.Client
{
    public class MPClientSocket : MPClientBase
    {

        protected WinsockDll.WSocket m_socket = null;

        public MPClientSocket(): base()
        { 
        }

        protected string m_IP;
        public string IP
        {
            get { return (m_IP); }
            set { m_IP = value; }
        }

        protected int m_Port = 0;
        public int Port
        {
            get { return (m_Port); }
            set { m_Port = value; }
        }

        public override bool Connected
        {
            get
            {
                bool bRet = false;
                if (m_socket != null)
                    bRet = m_socket.Connected;
                return bRet;
            }
        }

        public override void  Connect()
        {
            m_socket = new WinsockDll.WSocket(m_IP, m_Port);
            m_socket.OnConnect += new WinsockDll.WSocket.ConnectionDelegate(m_socket_OnConnect);
            m_socket.OnDisconnect += new WinsockDll.WSocket.ConnectionDelegate(m_socket_OnDisconnect);
            m_socket.OnRead += new WinsockDll.WSocket.ConnectionDelegate(m_socket_OnRead);
            m_socket.OnError += new WinsockDll.WSocket.ErrorDelegate(m_socket_OnError);
            m_bConnecting = true;
            m_socket.Connect();
        }

        public override void Disconnect()
        {
            base.Disconnect();

            if (m_socket != null)
            {
                m_socket.Disconnect();
                m_socket = null;
            }
        }

        protected override void Channel_Send(string str)
        {
            if (Connected)
                m_socket.SendText(str);
        }


        protected override void Channel_Send(byte[] data)
        {
            if (Connected)
                m_socket.SendBytes(data);
        }

        void m_socket_OnError(string ErroMessage, System.Net.Sockets.Socket soc, int ErroCode)
        {
            base.Channel_OnError(ErroMessage, ErroCode);
        }


        void m_socket_OnRead(System.Net.Sockets.Socket soc)
        {
            if (m_socket != null)
            {
                //string rcv = m_socket.ReceivedText;
                //Channel_OnRead(rcv);
                int l = m_socket.ReceivedBytesCount;
                if (l > 0)
                {
                    Channel_OnRead(m_socket.ReceivedBytes, 0, l);
                }
            }
        }
        

        void m_socket_OnDisconnect(System.Net.Sockets.Socket soc)
        {
            Channel_OnDisconnect();
        }


        void m_socket_OnConnect(System.Net.Sockets.Socket soc)
        {
            Channel_OnConnect();
        }


    }
}
