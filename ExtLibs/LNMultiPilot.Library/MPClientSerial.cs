using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LNMultiPilot.Library;
using System.IO.Ports;

namespace LNMultiPilot.Client
{
    public class MPClientSerial : MPClientBase
    {
        SerialPort m_port;

        public MPClientSerial()
            : base()
        {
            m_port = new SerialPort();
            m_port.DataReceived += new SerialDataReceivedEventHandler(m_port_DataReceived);
            m_port.ErrorReceived += new SerialErrorReceivedEventHandler(m_port_ErrorReceived);
            //m_port.PinChanged += new SerialPinChangedEventHandler(m_port_PinChanged);
        }

        protected string m_ComPort;
        public string ComPort
        {
            get { return (m_ComPort); }
            set { m_ComPort = value; }
        }

        protected int m_BaudRate = 0;
        public int BaudRate
        {
            get { return (m_BaudRate); }
            set { m_BaudRate = value; }
        }

        public override bool Connected
        { get { return m_port.IsOpen; } }

        public override void  Connect()
        {
            if (!m_port.IsOpen)
            {
                m_port.PortName = m_ComPort;
                m_port.BaudRate = m_BaudRate;
                m_port.Parity = Parity.None;
                m_port.DataBits = 8;
                m_port.StopBits = StopBits.One;
                m_port.Handshake = Handshake.None;


                try
                {
                    m_port.Open();
                    Channel_OnConnect();
                }
                catch (Exception ex)
                { }
            }
        }





        public override void Disconnect()
        {
            base.Disconnect();
            if (m_port.IsOpen)
            {
                m_port.Close();
                Channel_OnDisconnect();
            }
        }

        protected override void Channel_Send(string str)
        {
            if (Connected)
            {
                try  {
                    m_port.Write(str);
                } catch { }
            }
        }

        protected override void Channel_Send(byte[] data)
        {
            if ((Connected) && (data != null))
            {
                try  {
                    m_port.Write(data, 0, data.Length);
                } catch { }
            }                
        }


        //void m_port_PinChanged(object sender, SerialPinChangedEventArgs e)
        //{
            //throw new Exception("The method or operation is not implemented.");
        //}

        void m_port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            base.Channel_OnError( e.EventType.ToString(), (int) e.EventType);
            if (!m_port.IsOpen)
                Channel_OnDisconnect();
        }


        void m_port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytes = m_port.BytesToRead;
            //System.Threading.Thread.Sleep(pausa);
            if (bytes > 0)
            {
                //create a byte array to hold the awaiting data
                byte[] comBuffer = new byte[bytes];
                //read the data and store it
                m_port.Read(comBuffer, 0, bytes);
                string rcv = MemUtils.ByteArrayToStr(comBuffer);
                System.Diagnostics.Debug.Write(rcv);
                //Channel_OnRead(rcv);
                Channel_OnRead(comBuffer, 0, bytes);
            }
        }

    }
}
