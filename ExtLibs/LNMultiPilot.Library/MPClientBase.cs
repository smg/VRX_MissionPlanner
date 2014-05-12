using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LNMultiPilot.Library;

namespace LNMultiPilot.Client
{
    public class MPClientBase
    {

        protected System.Threading.ReaderWriterLock rwlockData = new ReaderWriterLock();
        protected MPData m_data = new MPData();
        protected int m_systemtime_interval;
        protected int m_remote_interval;
        protected MPData m_lastdata = new MPData();
        protected int m_last_systemtime_interval;
        protected int m_last_remote_interval;

        protected string m_logLock = "";
        //protected System.IO.TextWriter m_twLog = null;
        protected System.IO.Stream m_strmLog = null;
        protected System.IO.BinaryWriter m_twLog = null;

        public MPClientBase()
        { 
        }

        protected string m_strLogFolder = null;
        public string LogFolder
        {
            get { return (m_strLogFolder); }
            set { m_strLogFolder = value; }
        }

        protected bool m_bConnected = false;
        public virtual bool Connected
        {
            get
            {
                return m_bConnected;
            }
        }

        protected bool m_bConnecting = false;
        public virtual bool Connecting
        {
            get
            {
                return m_bConnecting;
            }
        }

        public virtual void Connect()
        {
            m_bConnecting = true;
        }

        public virtual void Disconnect()
        {
            m_bConnecting = false;
            if (Connected)
            {
                string strCommand = "X";
                Channel_Send(strCommand);
            }
            m_bConnected = false;
        }

        public virtual void SendRestartCommand()
        {
            if (Connected)
            {
                string strCommand = "T";
                Channel_Send(strCommand);
            }
        }

        public virtual void SendStopCommand()
        {
            if (Connected)
            {
                string strCommand = "X";
                Channel_Send(strCommand);
            }
        }

        public virtual void Send(string str)
        {
            if (Connected)
                Channel_Send(str);
        }

        public virtual void Send(byte[] data)
        {
            if (Connected)
                Channel_Send(data);
        }

        public Position GetPosition()
        {
            GetData();
            return m_lastdata.position;
        }

        public MPData GetData()
        {
            try
            {
                //rwlockData.AcquireReaderLock(0);
                rwlockData.AcquireReaderLock(1);
                try
                {
                    m_lastdata.CopyFrom(m_data);
                    m_last_remote_interval = m_remote_interval;
                    m_last_systemtime_interval = m_systemtime_interval;
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

        public int SystemTimeInterval
        { get { return m_last_systemtime_interval; } }
        public int RemoteControlInterval
        { get { return m_last_remote_interval; } }

        double cos(double degrees)
        {
            return Math.Cos(Math.PI * degrees / 180.0);
        }

        double sin(double degrees)
        {
            return Math.Sin(Math.PI * degrees / 180.0);
        }

        double greatcircle(double lon1, double lat1, double lon2, double lat2)
        {
            double r = 6371009000;
            double phi2 = (lat2 - lat1) / 2.0;
            double lam2 = (lon2 - lon1) / 2.0;
            //return r * Math.Acos(sin(lat1) * sin(lat2) + cos(lat1) * cos(lat2) * cos(lon2 - lon1));
            return 2.0 * r * Math.Asin(Math.Sqrt(sin(phi2) * sin(phi2) + cos(lat2) * cos(lat1) * sin(lam2) * sin(lam2) ));
        }

        public double GetTargetDirection()
        {
            //calcolo la direzione approssimativa del target
            double dRet = m_lastdata.euler.Heading;
            double dx = m_lastdata.target_diff.X;
            double dy = m_lastdata.target_diff.Y;
            double r = 6371009000;
            /*
            dx = cos(m_lastdata.target_position.dLat) * cos(m_lastdata.target_position.dLon) -
                cos(m_lastdata.position.dLat) * cos(m_lastdata.position.dLon);
            dx = 2 * r * Math.Asin(dx / 2);

            dy = cos(m_lastdata.target_position.dLat) * sin(m_lastdata.target_position.dLon) -
                cos(m_lastdata.position.dLat) * sin(m_lastdata.position.dLon);
            dy = 2 * r * Math.Asin(dy / 2);
            */

            dx = Math.Sign(m_lastdata.target_position.dLon - m_lastdata.position.dLon) * 
                    Math.Abs( greatcircle(m_lastdata.position.dLon, m_lastdata.position.dLat,m_lastdata.target_position.dLon, m_lastdata.position.dLat));

            dy = Math.Sign(m_lastdata.target_position.dLat - m_lastdata.position.dLat) * 
                Math.Abs(greatcircle(m_lastdata.position.dLon, m_lastdata.position.dLat,
                    m_lastdata.position.dLon, m_lastdata.target_position.dLat));

            if (dy > 0)
            {
                dRet = 180 * Math.Atan(dx / dy) / Math.PI;
            }
            else if (dy < 0)
            {
                dRet = 180 + 180 * Math.Atan(dx / dy) / Math.PI;
            }
            else
            {
                if (dx == 0)
                    dRet = 0;
                else if (dx >= 0)
                    dRet = 90;
                else
                    dRet = 270;
            }


            return dRet;
        }

        protected void UpdateData(MPData data)
        {
            try
            {
                rwlockData.AcquireWriterLock(50);
                try
                {
                    //aggiorno alcuni dati di misura del ritardo di trasmissione
                    if (m_data.systemtime != data.systemtime)
                    {
                        m_systemtime_interval = data.systemtime - m_data.systemtime;
                        m_remote_interval = data.remote_systemtime - m_data.remote_systemtime;
                    }

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


        protected double Str2Coord(string str)
        {
            return Math.Round(Utility.Str2Double(str) / 10000000, 7);
        }




        protected bool DecodeData(ref string input, ref MPData data)
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

                                        data.acrobatic_active = (data.remote.aux2 > 1500);
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
                                        data.position.dLat = Str2Coord(valori[0]);
                                        data.position.dLon = Str2Coord(valori[1]);
                                        data.position.dAlt = Utility.Str2Double(valori[2]) / 1000;
                                        data.position.dGroundSpeed = Utility.Str2Double(valori[3]);
                                        data.position.dSpeed3D = Utility.Str2Double(valori[4]);
                                        data.position.dHeading = Utility.Str2Double(valori[5]) / 10;
                                        //System.Diagnostics.Debug.WriteLine(valori[5]);
                                        data.position.iNumSat = (int)Utility.Str2Double(valori[6]);
                                        data.position.iFix = (int)Utility.Str2Double(valori[7]);
                                        if (valori[8].Length != 8)
                                        {
                                            //data.position.iTime = (int)Utility.Str2Double(valori[8]);
                                            //System.Diagnostics.Debug.WriteLine(valori[8]);
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
                                        data.target_position.dLat = Str2Coord(valori[1]);
                                        data.target_position.dLon = Str2Coord(valori[2]);
                                        data.target_position.dAlt = Utility.Str2Double(valori[3]);
                                        if (data.target_position.dAlt != -99999)
                                            data.target_position.dAlt = data.target_position.dAlt / 1000;
                                        data.target_diff.X = Utility.Str2Double(valori[4]);
                                        data.target_diff.Y = Utility.Str2Double(valori[5]);
                                        data.target_diff.Z = Utility.Str2Double(valori[6]);
                                        //if (data.target_diff.Z != -99999)
                                        //    data.target_diff.Z = data.target_diff.Z / 1000;
                                        data.command_gps.Roll = Utility.Str2Double(valori[7]);
                                        data.command_gps.Pitch = Utility.Str2Double(valori[8]);
                                    }
                                }
                                break;
                        }
                    }
                }

                if (data.systemtime == 0)
                {
                    //System.Diagnostics.Debug.WriteLine(message);
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
                bRet = (data.systemtime != 0);
                
            }
            else 
            {
                //bRet = Position.DecodePosition(input, ref data.position);
            }
            return bRet;
        }

        protected virtual void Channel_OnError(string ErroMessage, int ErroCode)
        {
            m_bConnecting = false;
        }


        string m_buffer = "";
        protected virtual void Channel_OnRead(string rcv)
        {
            if ((rcv != null) && (rcv.Length > 0))
            {

                rcv = rcv.Replace("\0", "");
                WriteLogFile(rcv);

                m_buffer = m_buffer + rcv;

                MPData data = new MPData();
                data.CopyFrom(m_data);
                if (DecodeData(ref m_buffer, ref data))
                    UpdateData(data);
            }
        }

        protected RPCChannel m_ch = new RPCChannel();
        protected virtual void Channel_OnRead(byte[] rcv, int offset, int length)
        {
            if ((rcv != null) && (rcv.Length > 0))
            {
                /*
                rcv = rcv.Replace("\0", "");
                WriteLogFile(rcv);

                m_buffer = m_buffer + rcv;

                MPData data = new MPData();
                data.CopyFrom(m_data);
                if (DecodeData(ref m_buffer, ref data))
                    UpdateData(data);
                 */
                WriteLogFile(rcv);
                m_ch.OnData(rcv, offset, length);
                if (m_ch.HasNewData)
                {
                    RPCChannel.DataToken dt = m_ch.Data;
                    if (dt.messageType == (byte)'T')
                    {
                        LNMultiPilot.Library.RPC.T_MPData data = new LNMultiPilot.Library.RPC.T_MPData();
                        if (dt.PayloadLength == System.Runtime.InteropServices.Marshal.SizeOf(data))
                        {
                            data = LNMultiPilot.Library.MemUtils.TypedDeserialize<LNMultiPilot.Library.RPC.T_MPData>(dt.payload);
                            MPData msg = new MPData();
                            msg.CopyFromStruct(data);
                            //System.Diagnostics.Debug.WriteLine(data.systemtime);
                            //System.Diagnostics.Debug.WriteLine(msg.euler.ToString());
                            UpdateData(msg);
                        }
                        //else
                            //System.Diagnostics.Debug.WriteLine(dt.PayloadLength);
                    }
                }
            }
        }



        protected virtual void Channel_OnDisconnect()
        {
            m_bConnecting = false;
            CloseLogFile();
        }

        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }


        protected virtual void Channel_OnConnect()
        {
            m_bConnecting = false;

            OpenLogFile();

            string strCommand = "T";
            Channel_Send(strCommand);
        }

        protected virtual void Channel_Send(string str)
        { 
        }

        protected virtual void Channel_Send(byte[] data)
        {
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
                    {
                        m_twLog.Close();
                        m_strmLog.Close();
                    }
                    m_strmLog = new System.IO.FileStream(fl, System.IO.FileMode.Create);
                    m_twLog = new System.IO.BinaryWriter(m_strmLog);
                    Monitor.Exit(m_logLock);
                }
            }
        }


        protected void CloseLogFile()
        {
            Monitor.Enter(m_logLock);
            if (m_twLog != null)
            {
                m_twLog.Close();
                m_strmLog.Close();
            }
            m_strmLog = null;
            m_twLog = null;
            Monitor.Exit(m_logLock);
        }

        protected void WriteLogFile(string str)
        {
            if (Monitor.TryEnter(m_logLock, 50))
            {
                //System.Diagnostics.Debug.Write(str);
                if (m_twLog != null)
                    m_twLog.Write(str);
                Monitor.Exit(m_logLock);
            }
        }
        protected void WriteLogFile(byte[] buffer)
        {
            if (Monitor.TryEnter(m_logLock, 50))
            {
                //string strlog = MemUtils.ByteArrayToStr(buffer);
                //strlog = strlog.Replace('\0', '.');
                //System.Diagnostics.Debug.Write(strlog);
                if (m_twLog != null)
                    m_twLog.Write(buffer);
                Monitor.Exit(m_logLock);
            }
        }
    }
}
