using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LNMultiPilot.Library;

namespace LNMultiPilot.Client
{
    public class MPClient
    {

        protected System.Threading.ReaderWriterLock rwlockData = new ReaderWriterLock();

        protected WinsockDll.WSocket m_socket = null;
        //Position m_position = new Position();
        //Position m_lastposition = new Position();
        MPData m_data = new MPData();
        MPData m_lastdata = new MPData();

        protected string m_logLock = "";
        protected System.IO.TextWriter m_twLog = null;

        public MPClient()
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

        protected string m_strLogFolder = null;
        public string LogFolder
        {
            get { return (m_strLogFolder); }
            set { m_strLogFolder = value; }
        }

        public bool Connected
        {
            get
            {
                bool bRet = false;
                if (m_socket != null)
                    bRet = m_socket.Connected;
                return bRet;
            }
        }

        bool m_bConnecting = false;
        public bool Connecting
        {
            get
            {
                return m_bConnecting;
            }
        }

        public void Connect()
        {
            m_socket = new WinsockDll.WSocket(m_IP, m_Port);
            m_socket.OnConnect += new WinsockDll.WSocket.ConnectionDelegate(m_socket_OnConnect);
            m_socket.OnDisconnect += new WinsockDll.WSocket.ConnectionDelegate(m_socket_OnDisconnect);
            m_socket.OnRead += new WinsockDll.WSocket.ConnectionDelegate(m_socket_OnRead);
            m_socket.OnError += new WinsockDll.WSocket.ErrorDelegate(m_socket_OnError);
            m_bConnecting = true;
            m_socket.Connect();
        }
        
        public void Disconnect()
        {
            m_bConnecting = false;

            if (m_socket != null)
            {
                if (m_socket.Connected)
                {
                    string strCommand = "X";
                    m_socket.SendText(strCommand);
                }
                m_socket.Disconnect();
                m_socket = null;
            }
        }

        public Position GetPosition()
        {
            /*
            if (Monitor.TryEnter(m_data))
            {
                m_lastdata.CopyFrom(m_data);
                Monitor.Exit(m_data);
            }*/
            GetData();
            return m_lastdata.position;
        }

        public MPData GetData()
        {
            /*
            if (Monitor.TryEnter(m_data))
            {
                m_lastdata.CopyFrom(m_data);
                Monitor.Exit(m_data);
            }*/
            try
            {
                rwlockData.AcquireReaderLock(0);
                try
                {
                    m_lastdata.CopyFrom(m_data);
                }
                finally
                {
                    rwlockData.ReleaseReaderLock();
                }
            }
            catch (ApplicationException)
            {
            }
            return m_lastdata;
        }


        public double GetTargetDirection()
        { 
            //calcolo la direzione approssimativa del target
            double dRet = m_lastdata.euler.Heading;
            double dx = m_lastdata.target_diff.X;
            double dy = m_lastdata.target_diff.Y;

            if (dx > 0)
            {
                dRet = 180 * Math.Atan(dy / dx) * Math.PI;
            }
            else if (dx < 0)
            {
                dRet = 360 - 180 * Math.Atan(dy / dx) * Math.PI;
            }
            else 
            {
                if (dy >= 0)
                    dRet = 0;
                else
                    dRet = 180;
            }


            return dRet;
        }


        void UpdateData(MPData data)
        {
            /*
            Monitor.Enter(m_data);
            m_data.CopyFrom(data);
            Monitor.Exit(m_data);*/

            try
            {
                rwlockData.AcquireWriterLock(50);
                try
                {
                    m_data.CopyFrom(data);
                }
                finally
                {
                    rwlockData.ReleaseWriterLock();
                }
            }
            catch (ApplicationException)
            {
            }
        }

        bool DecodeData(ref string input, ref MPData data)
        {
            bool bRet = false;
            int inizio = input.IndexOf("!V:");
            int fine = 0;
            if (inizio >= 0)
                fine = input.IndexOf("\r\n", inizio);

            if ((inizio >= 0) && (fine > inizio))
            {
                string message = input.Substring(inizio, fine+2 - inizio);
                //ricalibro inizio se per caso sono concatenati due messaggi incompleti
                int inizio2 = message.LastIndexOf("!V:");
                if (inizio2 > inizio)
                    message = message.Substring(inizio2);


                string[] sepsez = {"!"};
                string[] sephead = { ":" };
                string[] sepval = { "," };
                string[] sezioni = message.Split(sepsez, StringSplitOptions.RemoveEmptyEntries);
                List<string> trovati = new List<string>();
                string trovato = null;
                foreach (string sezione in sezioni)
                {
                    string[] corpo = sezione.Split(sephead, StringSplitOptions.None);
                    if (corpo.Length > 1)
                    {
                        string[] valori = corpo[1].Split(sepval, StringSplitOptions.None);
                        switch (corpo[0])
                        { 
                            case "V":
                                trovato = "V";
                                if (!trovati.Contains(trovato))
                                {
                                    trovati.Add(trovato);
                                    data.swVersion = valori[0];
                                }
                                break;
                            case "T":
                                trovato = "T";
                                if (!trovati.Contains(trovato))
                                {
                                    trovati.Add(trovato);
                                    data.systemtime = (int)Utility.Str2Double(valori[0]);
                                }
                                break;
                            case "A":
                                if (valori.Length >= 6)
                                {
                                    trovato = "A";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        data.gyro_rate.Roll = Utility.Str2Double(valori[0]);
                                        data.gyro_rate.Pitch = Utility.Str2Double(valori[1]);
                                        data.gyro_rate.Heading = Utility.Str2Double(valori[2]);
                                        data.acceleration.X = Utility.Str2Double(valori[3]);
                                        data.acceleration.Y = Utility.Str2Double(valori[4]);
                                        data.acceleration.Z = Utility.Str2Double(valori[5]);
                                    }
                                }
                                break;
                            case "E":
                                if (valori.Length >= 3)
                                {
                                    trovato = "E";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        data.euler.Roll = Utility.Str2Double(valori[0]);
                                        data.euler.Pitch = Utility.Str2Double(valori[1]);
                                        data.euler.Heading = Utility.Str2Double(valori[2]);
                                    }
                                }
                                /*
                                data.position.dRoll = data.euler.Roll;
                                data.position.dPitch = data.euler.Pitch;
                                data.position.dHeading = data.euler.Heading;
                                 */ 
                                break;
                            case "C":
                                if (valori.Length >= 4)
                                {
                                    trovato = "C";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        data.magnetometer.X = Utility.Str2Double(valori[0]);
                                        data.magnetometer.Y = Utility.Str2Double(valori[1]);
                                        data.magnetometer.Z = Utility.Str2Double(valori[2]);
                                        data.magneticHeading = Utility.Str2Double(valori[3]);
                                    }
                                }
                                break;
                            case "M":
                                if (valori.Length >= 2)
                                {
                                    trovato = "M";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        int n = (int)Utility.Str2Double(valori[0]);
                                        data.motorConfig = valori[1];
                                        data.motors = new int[n];
                                        if (valori.Length >= n + 2)
                                        {
                                            for (int i = 0; i < n; i++)
                                            {

                                                data.motors[i] = (int)Utility.Str2Double(valori[i + 2]);
                                            }
                                        }
                                    }
                                }
                                break;
                            case "CH":
                                if (valori.Length >= 6)
                                {
                                    trovato = "CH";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        data.remote.hpr.Roll = Utility.Str2Double(valori[0]);
                                        data.remote.hpr.Pitch = Utility.Str2Double(valori[1]);
                                        data.remote.hpr.Heading = Utility.Str2Double(valori[2]);
                                        data.remote.throttle = Utility.Str2Double(valori[3]);
                                        data.remote.aux = Utility.Str2Double(valori[4]);
                                        data.remote.aux2 = Utility.Str2Double(valori[5]);
                                    }
                                }
                                break;
                            case "CT":
                                if (valori.Length >= 3)
                                {
                                    trovato = "CT";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        data.control.Roll = Utility.Str2Double(valori[0]);
                                        data.control.Pitch = Utility.Str2Double(valori[1]);
                                        data.control.Heading = Utility.Str2Double(valori[2]);
                                    }
                                }
                                break;
                            case "S":
                                if (valori.Length >= 3)
                                {
                                    trovato = "S";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        data.sonar_val = Utility.Str2Double(valori[0]);
                                        data.sonar_target = Utility.Str2Double(valori[1]);
                                        data.sonar_alt_err = Utility.Str2Double(valori[2]);
                                    }
                                }
                                break;
                            case "G":
                                if (valori.Length >= 9)
                                {
                                    trovato = "G";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        data.position.dLat = Utility.Str2Double(valori[0]) / 10000000;
                                        data.position.dLon = Utility.Str2Double(valori[1]) / 10000000;
                                        data.position.dAlt = Utility.Str2Double(valori[2]);
                                        data.position.dGroundSpeed = Utility.Str2Double(valori[3]);
                                        data.position.dSpeed3D = Utility.Str2Double(valori[4]);
                                        data.position.dHeading = Utility.Str2Double(valori[5]);
                                        data.position.iNumSat = (int)Utility.Str2Double(valori[6]);
                                        data.position.iFix = (int)Utility.Str2Double(valori[7]);
                                        if (valori[8].Length != 8)
                                        {
                                            //data.position.iTime = (int)Utility.Str2Double(valori[8]);
                                            System.Diagnostics.Debug.WriteLine(valori[8]);
                                        }
                                        else
                                            data.position.iTime = (int)Utility.Str2Double(valori[8]);
                                    }
                                }
                                break;
                            case "GH":
                                if (valori.Length >= 9)
                                {
                                    trovato = "GH";
                                    if (!trovati.Contains(trovato))
                                    {
                                        trovati.Add(trovato);
                                        data.target_active = (int)Utility.Str2Double(valori[0]);
                                        data.target_position.dLat = Utility.Str2Double(valori[1]) / 10000000;
                                        data.target_position.dLon = Utility.Str2Double(valori[2]) / 10000000;
                                        data.target_position.dAlt = Utility.Str2Double(valori[3]);
                                        data.target_diff.X = Utility.Str2Double(valori[4]);
                                        data.target_diff.Y = Utility.Str2Double(valori[5]);
                                        data.target_diff.Z = Utility.Str2Double(valori[6]);
                                        data.command_gps.Roll = Utility.Str2Double(valori[7]);
                                        data.command_gps.Pitch = Utility.Str2Double(valori[8]);
                                    }
                                }
                                break;
                        }
                    }
                }

                //correzioni
                if (data.target_active != 0)
                {
                    if ((data.target_diff.X == 0) && (data.target_diff.Y == 0))
                    {
                        data.target_diff.X = data.target_position.dLon - data.position.dLon;
                        data.target_diff.Y = data.target_position.dLat - data.position.dLat;
                    }
                }


                input = input.Substring(fine + 2);
                bRet = true;
            }
            else 
            {
                //bRet = Position.DecodePosition(input, ref data.position);
            }
            return bRet;
        }

        void m_socket_OnError(string ErroMessage, System.Net.Sockets.Socket soc, int ErroCode)
        {
            m_bConnecting = false;
        }


        string m_buffer = "";
        void m_socket_OnRead(System.Net.Sockets.Socket soc)
        {
            if (m_socket != null)
            {
                string rcv = m_socket.ReceivedText;
                if ((rcv != null) && (rcv.Length > 0))
                {
                    /*
                    m_iReceivedBytes = m_iReceivedBytes + rcv.Length;
                    m_tmLastRx = DateTime.Now;
                    NotifyReceive(rcv);
                     */

                    rcv = rcv.Replace("\0", "");
                    WriteLogFile(rcv);
                    
                    m_buffer = m_buffer + rcv;

                    MPData data = new MPData();
                    data.CopyFrom(m_data);
                    if (DecodeData(ref m_buffer, ref data))
                        UpdateData(data);
                }
            }
        }

        

        void m_socket_OnDisconnect(System.Net.Sockets.Socket soc)
        {
            m_bConnecting = false;
            CloseLogFile();
        }

        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }


        void m_socket_OnConnect(System.Net.Sockets.Socket soc)
        {
            m_bConnecting = false;

            OpenLogFile();

            string strCommand = "T";
            m_socket.SendText(strCommand);
        }


        //FUNZIONI PER IL LOG
        protected void OpenLogFile()
        {
            if (m_strLogFolder != null)
            {
                if (System.IO.Directory.Exists(m_strLogFolder))
                {
                    DateTime dt = DateTime.Now;
                    string strfile = "LNMultiPilot_" + dt.Year.ToString("0000") + dt.Month.ToString("00") + dt.Day.ToString("00") +
                                                        dt.Hour.ToString("00") + dt.Minute.ToString("00") + dt.Second.ToString("00") + ".log";
                    string fl = System.IO.Path.Combine(m_strLogFolder, strfile);
                    Monitor.Enter(m_logLock);
                    if (m_twLog != null)
                        m_twLog.Close();
                    m_twLog = new System.IO.StreamWriter(fl);
                    Monitor.Exit(m_logLock);
                }
            }
        }


        protected void CloseLogFile()
        {
            Monitor.Enter(m_logLock);
            if (m_twLog != null)
                m_twLog.Close();
            m_twLog = null;
            Monitor.Exit(m_logLock);
        }

        protected void WriteLogFile(string str)
        {
            if (Monitor.TryEnter(m_logLock, 50))
            {
                if (m_twLog != null)
                    m_twLog.Write(str);
                Monitor.Exit(m_logLock);
            }
        }

    }
}
