using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace WinsockDll
{
    public class WSocket
    {
        #region Delegates
        public delegate void ConnectionDelegate(Socket soc);
        public delegate void ErrorDelegate(string ErroMessage, Socket soc, int ErroCode);
        #endregion

        #region Events
        public event ConnectionDelegate OnConnect;
        public event ConnectionDelegate OnDisconnect;
        public event ConnectionDelegate OnRead;
        public event ConnectionDelegate OnWrite;
        public event ErrorDelegate OnError;
        #endregion

        #region Variables
        private AsyncCallback WorkerCallBack;
        private Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private IPEndPoint ipLocal;
        private byte[] dataBuffer = new byte[1024];
        private byte[] mBytesReceived;
        private int mBytesCount = 0;
        private string mTextReceived = "";
        private string mTextSent = "";
        private string mRemoteAddress = "";
        private string mRemoteHost = "";

        private string mIndirizzo = "";
        private int mPorta = 0;
        #endregion

        #region Properties

        public string Address
        {
            get
            {
                return (mIndirizzo);
            }
            set
            {
                if (!Connected)
                    mIndirizzo = value;
            }
        }
        public int Port
        {
            get
            {
                return (mPorta);
            }
            set 
            {
                if (!Connected)
                    mPorta = value;
            }
        }

        public byte[] ReceivedBytes
        {
            get
            {
                byte[] temp = null;
                if (mBytesReceived != null)
                {
                    temp = mBytesReceived;
                    mBytesReceived = null;
                    mBytesCount = 0;
                }
                return (temp);
            }
        }

        public int ReceivedBytesCount
        {
            get
            {
                return mBytesCount;
            }
        }

        public string ReceivedText
        {
            get
            {
                string temp = mTextReceived;
                mTextReceived = "";
                return (temp);
            }
        }

        public string WriteText
        {
            get
            {
                string temp = mTextSent;
                mTextSent = "";
                return (temp);
            }
        }

        public string RemoteAddress
        {
            get
            {
                if (mainSocket.Connected)
                    return (mRemoteAddress);
                else
                    return "";
            }
        }

        public string RemoteHost
        {
            get
            {
                if (mainSocket.Connected)
                    return (mRemoteHost);
                else
                    return "";
            }
        }

        public bool Connected
        {
            get
            {
                return (mainSocket.Connected);
            }
        }
        #endregion

        #region Construtor

        public WSocket(string IP, int port)
        {
            mIndirizzo = IP;
            mPorta = port;

            //SetRemoteConnection(IP, port);

            //try
            //{
            //    mPorta = port;
            //    IPAddress ipAddress = IPAddress.Parse(IP);
            //    mRemoteAddress = ipAddress.ToString();
            //    IPHostEntry ipss = Dns.GetHostEntry(mRemoteAddress);
            //    mRemoteHost = ipss.HostName;
            //    ipLocal = new IPEndPoint(ipAddress, port);
            //}
            //catch (Exception se)
            //{
            //    if (OnError != null)
            //        OnError(se.Message, null, 0);
            //}
        }
        #endregion

        #region Functions and Events

        private bool SetRemoteConnection(string host, int port)
        {
            bool bRet = false;

            if (mainSocket.Connected)
                return false;

            Exception ee = null;
            mPorta = port;
            mRemoteAddress = null;
            mRemoteHost = null;

            IPAddress ipAddress = null;

            //1. cerco di fare il parsing dell'host come IP
            try
            {
                ipAddress = IPAddress.Parse(host);
                mRemoteAddress = ipAddress.ToString();
                //TEO 20090612 per evitare lungaggini non recupero il nome host
                //IPHostEntry ipss = Dns.GetHostEntry(mRemoteAddress);
                //mRemoteHost = ipss.HostName;
                //TEO 20090612 X
            }
            catch (Exception ae)
            {
                ee = ae;
                ipAddress = null;
            }

            if (ipAddress == null) 
            {
                //2. cerco di risolvere il nome host
                try
                {
                    IPHostEntry ipss2 = Dns.GetHostEntry(host);
                    if (ipss2.AddressList.Length > 0) {
                        ipAddress = ipss2.AddressList[0];
                        mRemoteHost = ipss2.HostName;
                        mRemoteAddress = ipAddress.ToString();
                    }
                }
                catch (Exception dnse)
                {
                    ee = dnse;
                    ipAddress = null;
                }
            }

            if (ipAddress != null)
            {
                try
                {
                    //ipAddress = IPAddress.Parse(mRemoteAddress);
                    ipLocal = new IPEndPoint(ipAddress, port);
                    bRet = true;
                }
                catch (Exception se)
                {
                    ee = se;
                }
            }

            if (bRet == false) 
            {
                if (ee != null)
                {
                    if (OnError != null)
                        OnError(ee.Message, null, 0);
                }
            }
            return bRet;
        }

        public bool Connect()
        {
            return _Connect(true);
            
            //try
            //{
            //    //cerco di risolvere il nome host
            //    SetRemoteConnection(mIndirizzo, mPorta);

            //    //Connect to the server
            //    mainSocket.BeginConnect(ipLocal, new AsyncCallback(ConfirmConnect), null);
            //    return true;
            //}
            //catch (ArgumentException se)
            //{
            //    if (OnError != null)
            //        OnError(se.Message, null, 0);
            //    return false;
            //}
            //catch (System.ObjectDisposedException se) 
            //{
            //    if (OnError != null)
            //        OnError(se.Message, null, 0);

            //    //strano errore...
            //    //tento di recuperare
            //    mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //    return false;
            //}
            //catch (InvalidOperationException se)
            //{
            //    if (OnError != null)
            //        OnError(se.Message, null, 0);
            //    return false;
            //}
            //catch (SocketException se)
            //{
            //    if (OnError != null)
            //        OnError(se.Message, mainSocket, se.ErrorCode);
            //    return false;
            //}
        }

        public bool _Connect(bool bRetry)
        {
            try
            {
                //cerco di risolvere il nome host
                SetRemoteConnection(mIndirizzo, mPorta);

                //Connect to the server
                mainSocket.BeginConnect(ipLocal, new AsyncCallback(ConfirmConnect), null);
                return true;
            }
            catch (ArgumentException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
                return false;
            }
            catch (System.ObjectDisposedException se)
            {
                //strano errore...
                //tento di recuperare
                mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                if (bRetry)
                { 
                    //faccio 1 tentativo 
                    bool bConn = _Connect(false);
                    return bConn;
                }
                else
                {
                    if (OnError != null)
                        OnError(se.Message, null, 0);
                    return false;
                }
            }
            catch (InvalidOperationException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
                return false;
            }
            catch (SocketException se)
            {
                if (OnError != null)
                    OnError(se.Message, mainSocket, se.ErrorCode);
                return false;
            }
        }

        private void ConfirmConnect(IAsyncResult asyn)
        {
            try
            {
                mainSocket.EndConnect(asyn);
                WaitForData(mainSocket);
                if (OnConnect != null)
                    OnConnect(mainSocket);
            }
            catch (ObjectDisposedException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
            }
            catch (SocketException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
            }
        }

        private void WaitForData(Socket soc)
        {
            try
            {
                if (WorkerCallBack == null)
                    WorkerCallBack = new AsyncCallback(OnDataReceived);
                soc.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, WorkerCallBack, null);
            }
            catch (SocketException se)
            {
                if (OnError != null)
                    OnError(se.Message, soc, se.ErrorCode);
            }
        }

        private void OnDataReceived(IAsyncResult asyn)
        {
            try
            {
                int iRx = mainSocket.EndReceive(asyn);
                if (iRx < 1)
                {
                    mainSocket.Close();
                    if (!mainSocket.Connected)
                        if (OnDisconnect != null)
                            OnDisconnect(mainSocket);
                }
                else
                {
                    mBytesReceived = dataBuffer;
                    mBytesCount = iRx;
                    char[] chars = new char[iRx + 1];
                    Decoder d = Encoding.UTF8.GetDecoder();
                    d.GetChars(dataBuffer, 0, iRx, chars, 0);
                    mTextReceived = new String(chars);
                    if (OnRead != null)
                        OnRead(mainSocket);
                    WaitForData(mainSocket);
                }
            }
            catch (ArgumentException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
            }
            catch (InvalidOperationException se)
            {
                mainSocket.Close();
                if (!mainSocket.Connected)
                    if (OnDisconnect != null)
                        OnDisconnect(mainSocket);
                else
                    if (OnError != null)
                        OnError(se.Message, null, 0);
            }
            catch (SocketException se)
            {
                if (OnError != null)
                    OnError(se.Message, mainSocket, se.ErrorCode);
                if (!mainSocket.Connected)
                    if (OnDisconnect != null)
                        OnDisconnect(mainSocket);
            }
        }

        public bool SendText(string mens)
        {
            try
            {
                byte[] byData = System.Text.Encoding.ASCII.GetBytes(mens);
                int NumBytes = mainSocket.Send(byData);
                if (NumBytes == byData.Length)
                {
                    if (OnWrite != null)
                    {
                        mTextSent = mens;
                        OnWrite(mainSocket);
                    }
                    return true;
                }
                else
                    return false;
            }
            catch (ArgumentException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
                return false;
            }
            catch (ObjectDisposedException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
                return false;
            }
            catch (SocketException se)
            {
                if (OnError != null)
                    OnError(se.Message, mainSocket, se.ErrorCode);
                return false;
            }
        }

        public bool SendBytes(byte[] byData)
        {
            try
            {
                int NumBytes = mainSocket.Send(byData);
                if (NumBytes == byData.Length)
                {
                    if (OnWrite != null)
                    {
                        mTextSent = System.Text.Encoding.ASCII.GetString(byData);
                        OnWrite(mainSocket);
                    }
                    return true;
                }
                else
                    return false;
            }
            catch (ArgumentException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
                return false;
            }
            catch (ObjectDisposedException se)
            {
                if (OnError != null)
                    OnError(se.Message, null, 0);
                return false;
            }
            catch (SocketException se)
            {
                if (OnError != null)
                    OnError(se.Message, mainSocket, se.ErrorCode);
                return false;
            }
        }

        public bool Disconnect()
        {
            mainSocket.Close();
            if (!mainSocket.Connected)
                return true;
            else
                return false;
        }
        #endregion
    }
}
