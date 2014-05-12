using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace LNMultiPilot.Library
{
    public class RPCChannel
    {
        const byte START_BYTE = 0x24; //$
        const byte STOP_BYTE = 0x23; //#

        const byte STARTSIZE = 2 * sizeof(byte);
        const byte HEADERSIZE = 2 * sizeof(byte);
        const byte CRCSIZE = 2 * sizeof(byte);
        const byte STOPSIZE = 1 * sizeof(byte);

        enum ChannelStatus
        { 
            STATUS_WAIT_START = 0,
            STATUS_READY_START = 1,
            STATUS_READ_DATA = 2
        }

        [StructLayout(LayoutKind.Sequential,Pack = 1)]
        struct ControlToken
        {
            public byte messageType;
            public byte messageSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DataToken
        {
            public byte messageType;
            public byte messageSize;
            //public string payload;

            public byte[] payload;

            public void Reset()
            {
                messageSize = 0;
                messageType = 0;
                //payload = IntPtr.Zero;
                payload = null;
            }

            public int PayloadLength
            {
                get 
                {
                    int iRet = 0;
                    if (payload != null) 
                        iRet = payload.Length;
                    return iRet;
                }
            }
        }

        const int DATATOKENSIZE = 6; //Marshal.SizeOf(DataToken);
        const int CONTROLTOKENSIZE = 2; //Marshal.SizeOf(ControlToken);


        ChannelStatus ssStatus;
        byte rxByte;
        bool hExit;
        bool hDecode;
        bool newData;
        //byte buff[BUF_LEN];
        int buffCount;
        //int min_buff;
        DataToken dataTok;

        public RPCChannel()
        {
            Reset();
        }
        
        public string EncodeStruct(char msgType, object data)
        {
            string start = ((char)START_BYTE).ToString();
            string stop = ((char)STOP_BYTE).ToString();
            
            string encodedstruct = MemUtils.SerializeToString(data);
            string len = ((char)encodedstruct.Length).ToString();

            string msgcontent = msgType.ToString() + len + encodedstruct;
            UInt16 crc = CRC(msgcontent, 0, msgcontent.Length);
            string strcrc = MemUtils.SerializeToString(crc);
            
            string output = start + start + msgcontent + strcrc + stop;
            return output;
        }
        
        /*
        public byte[] EncodeStruct(byte msgType, object data)
        {
            byte[] msgcontent = MemUtils.SerializeToByteArray(data);
            if (msgcontent.Length > 255)
                return null;

            byte len = (byte)msgcontent.Length;
            int totlen = msgcontent.Length + STARTSIZE + HEADERSIZE + CRCSIZE + STOPSIZE;
            byte[] output = new byte[totlen];

            output[0] = START_BYTE;
            output[1] = START_BYTE;
            output[2] = msgType;
            output[3] = len;
            System.Buffer.BlockCopy(msgcontent, 0, output, STARTSIZE + HEADERSIZE, msgcontent.Length);

            UInt16 crc = CRC(output, STARTSIZE, msgcontent.Length + HEADERSIZE);
            byte[] strcrc = MemUtils.SerializeToByteArray(crc);
            System.Buffer.BlockCopy(strcrc, 0, output, STARTSIZE + HEADERSIZE + msgcontent.Length, CRCSIZE);
            output[STARTSIZE + HEADERSIZE + msgcontent.Length + CRCSIZE] = STOP_BYTE;
            return output;
        }
        */
        public void Send(byte[] msg, int size)
        {
            //write(START_BYTE);
            //write(START_BYTE);
            //write((byte*)data, size);
            //write(STOP_BYTE);

        }

        public bool HasNewData
        { get { return newData; } }

        public DataToken Data
        { 
            get {
                newData = false;
                return dataTok; 
            }
        }

        void Reset()
        {
            ssStatus = ChannelStatus.STATUS_WAIT_START;
            rxByte = 0;
            hExit = false;
            hDecode = false;
            buffCount = 0;
            //dataTok.Reset();
        }
        void CutBuffer(int newstart)
        {
            //m_strBuffer = m_strBuffer.Substring(newstart);
            m_lstBuffer.RemoveRange(0, newstart);
        }



        public static UInt16 swap(UInt16 input)
        {
            return ((UInt16)(
            ((0xFF00 & input) >> 8) |
            ((0x00FF & input) << 8)));
        }


        public delegate void D_dataCallback(DataToken dt);
        public D_dataCallback dataCallback = null;


        List<byte> m_lstBuffer = new List<byte>();
        
        public void OnData(byte[] buff, int offset, int length)
        {
            //OnData(MemUtils.ByteArrayToStr(buff));

            //System.Diagnostics.Debug.WriteLine(length.ToString());

            if ((offset == 0) && (length == buff.Length))
            {
                m_lstBuffer.AddRange(buff);
            }
            else
            {
                for (int b = offset; b < offset + length; b++)
                    m_lstBuffer.Add(buff[b]);
            }

            if (m_lstBuffer.Count >180)// HEADERSIZE + CRCSIZE + STOPSIZE)
            {

                int i = 0;
                for (i = 0; i < m_lstBuffer.Count - 2; i++)
                {

                    bool bTryDecode = false;

                    if (m_lstBuffer[i] == START_BYTE)
                    {
                        if (m_lstBuffer[i + 1] == START_BYTE)
                        {
                            bTryDecode = true;
                            i = i + 2; 
                        }
                        else
                        {
                            CutBuffer(i);
                            i = 0;
                            Reset();
                        }
                    }
                    byte ch = m_lstBuffer[i];
                    if (bTryDecode)
                    {
                        //inizia il messaggio
                        int posfine = -1;
                        if (ch == STOP_BYTE)
                        {
                            //il messaggio è vuoto, si chiude già qui
                            posfine = i;
                            System.Diagnostics.Debug.WriteLine("Empty");
                        }
                        else if (m_lstBuffer.Count >= i + 2)
                        {
                            //il messaggio può contenere info tipo e lunghezza
                            byte msgtype = (byte)ch;
                            int len = (int)m_lstBuffer[i + 1];
                            int totlen = len + HEADERSIZE + CRCSIZE + STOPSIZE;
                            if (m_lstBuffer.Count >= i + totlen)
                            {
                                //ho il messaggio completo
                                //verifico il crc
                                int crcpos = i + HEADERSIZE + len;
                                byte[] msgcrc = new byte[CRCSIZE];
                                m_lstBuffer.CopyTo(crcpos, msgcrc, 0, CRCSIZE);
                                UInt16 crc = MemUtils.TypedDeserialize<UInt16>(msgcrc);
                                UInt16 crccalc = CRC(m_lstBuffer, i, len + HEADERSIZE);

                                int stoppos = crcpos + CRCSIZE;
                                byte stopbyte = m_lstBuffer[stoppos];

                                if ((crc == crccalc) && (stopbyte == STOP_BYTE))
                                {
                                    dataTok.messageType = (byte)m_lstBuffer[i];
                                    dataTok.messageSize = (byte)m_lstBuffer[i + 1];
                                    dataTok.payload = new byte[len];
                                    m_lstBuffer.CopyTo(i + HEADERSIZE, dataTok.payload, 0, len);
                                    newData = true;
                                    if (dataCallback != null)
                                    {
                                        dataCallback(dataTok);
                                    }

                                    posfine = i + totlen;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("CRC Error");
                                }
                            }
                        }
                        if (posfine >= 0)
                        {
                            CutBuffer(posfine);
                            i = 0;
                            Reset();
                        }
                    }
                }
            }
        }
        /*
        public void OnData(byte[] buff, int offset, int length)
        {
            //OnData(MemUtils.ByteArrayToStr(buff));

            if ((offset == 0) && (length == buff.Length))
            {
                m_lstBuffer.AddRange(buff);
            }
            else
            {
                for (int b = offset; b < offset + length; b++)
                    m_lstBuffer.Add(buff[b]);
            }

            ssStatus = ChannelStatus.STATUS_WAIT_START;

            int i = 0;
            for (i = 0; i < m_lstBuffer.Count; i++)
            {
                byte ch = m_lstBuffer[i];
                switch (ssStatus)
                {
                    case ChannelStatus.STATUS_WAIT_START:
                        if (ch == START_BYTE)
                            ssStatus = ChannelStatus.STATUS_READY_START;
                        break;
                    case ChannelStatus.STATUS_READY_START:
                        if (ch == START_BYTE)
                            ssStatus = ChannelStatus.STATUS_READ_DATA;
                        else
                        {
                            CutBuffer(i);
                            i = 0;
                            Reset();
                        }
                        break;
                    case ChannelStatus.STATUS_READ_DATA:
                        //inizia il messaggio
                        int posfine = -1;
                        if (ch == STOP_BYTE)
                        {
                            //il messaggio è vuoto, si chiude già qui
                            posfine = i;
                        }
                        else if (m_lstBuffer.Count >= i + 2)
                        {
                            //il messaggio può contenere info tipo e lunghezza
                            byte msgtype = (byte)ch;
                            int len = (int)m_lstBuffer[i + 1];
                            int totlen = len + HEADERSIZE + CRCSIZE + STOPSIZE;
                            if (m_lstBuffer.Count >= i + totlen)
                            {
                                //ho il messaggio completo
                                //verifico il crc
                                int crcpos = i + HEADERSIZE + len;
                                byte[] msgcrc = new byte[CRCSIZE];
                                m_lstBuffer.CopyTo(crcpos, msgcrc, 0, CRCSIZE);
                                UInt16 crc = MemUtils.TypedDeserialize<UInt16>(msgcrc);
                                UInt16 crccalc = CRC(m_lstBuffer, i, len + HEADERSIZE);

                                int stoppos = crcpos + CRCSIZE;
                                byte stopbyte = m_lstBuffer[stoppos];

                                if ((crc == crccalc) && (stopbyte == STOP_BYTE))
                                {
                                    dataTok.messageType = (byte)m_lstBuffer[i];
                                    dataTok.messageSize = (byte)m_lstBuffer[i + 1];
                                    dataTok.payload = new byte[len];
                                    m_lstBuffer.CopyTo(i + HEADERSIZE, dataTok.payload, 0, len);
                                    newData = true;
                                    if (dataCallback != null)
                                    {
                                        dataCallback(dataTok);
                                    }

                                    posfine = i + totlen;
                                }
                                else {
                                    System.Diagnostics.Debug.WriteLine("CRC Error");
                                }
                            }
                        }
                        if (posfine >= 0)
                        {
                            CutBuffer(posfine);
                            i = 0;
                            Reset();
                        }
                        break;
                }
            }
        }
        */

        //string m_strBuffer = "";
        public void OnData(string str)
        {
            byte[] rcv = MemUtils.StrToByteArray(str);
            OnData(rcv, 0, rcv.Length);
            /*
            m_strBuffer = m_strBuffer + str;

            //System.Diagnostics.Debug.WriteLine(m_strBuffer);

            int i = 0;
            for (i = 0; i < m_strBuffer.Length; i++ )
            {
                char ch = m_strBuffer[i];
                switch (ssStatus)
                {
                    case ChannelStatus.STATUS_WAIT_START:
                        if (ch == START_BYTE)
                            ssStatus = ChannelStatus.STATUS_READY_START;
                        break;
                    case ChannelStatus.STATUS_READY_START:
                        if (ch == START_BYTE)
                            ssStatus = ChannelStatus.STATUS_READ_DATA;
                        else
                        {
                            CutBuffer(i);
                            i = 0;
                            Reset();
                        }
                        break;
                    case ChannelStatus.STATUS_READ_DATA:
                        //inizia il messaggio
                        int posfine = -1;
                        if (ch == STOP_BYTE)
                        {
                            //il messaggio è vuoto, si chiude già qui
                            posfine = i;
                        } 
                        else if (m_strBuffer.Length >= i + 2)
                        {
                            //il messaggio può contenere info tipo e lunghezza
                            byte msgtype = (byte)ch;
                            int len = (int)m_strBuffer[i+1];
                            int totlen = len + HEADERSIZE + CRCSIZE + STOPSIZE;
                            if (m_strBuffer.Length >= i + totlen)
                            { 
                                //ho il messaggio completo
                                //verifico il crc
                                int crcpos = i + HEADERSIZE + len;
                                //UInt16 crc = (UInt16)(Convert.ToUInt16((byte)m_strBuffer[crcpos]) + 255 *  Convert.ToUInt16((byte)m_strBuffer[crcpos + 1]));
                                string strcrc = m_strBuffer.Substring(crcpos, CRCSIZE);
                                UInt16 crc = MemUtils.TypedDeserialize<UInt16>(strcrc);
                                UInt16 crccalc = CRC(m_strBuffer, i, len + HEADERSIZE);
                                if ((true) || (crc == crccalc))
                                {
                                    dataTok.messageType = (byte)m_strBuffer[i];
                                    dataTok.messageSize = (byte)m_strBuffer[i + 1];
                                    dataTok.payload = m_strBuffer.Substring(i + HEADERSIZE, len);
                                    newData = true;
                                    if (dataCallback != null)
                                    {
                                        dataCallback(dataTok);
                                    }

                                    posfine = i + totlen;
                                }
                            }
                        }
                        if (posfine >= 0)
                        {
                            CutBuffer(posfine);
                            i = 0;
                            Reset();
                        }
                        break;
                }
                
            }*/
        }



        UInt16 CRC(List<byte> str, int startpos, int len)
        {
            UInt16 crc_t = 0;
            for (int i = startpos; i < startpos + len; i++)
            {
                crc_t += (UInt16)str[i];
            }
            return crc_t;
        }

        UInt16 CRC(byte[] str, int startpos, int len)
        {
            UInt16 crc_t = 0;
            for (int i = startpos; i < startpos + len; i++)
            {
                crc_t += (UInt16)str[i];
            }
            return crc_t;
        }

        UInt16 CRC(string str, int startpos, int len)
        {
            UInt16 crc_t = 0;
            for (int i = startpos; i < startpos + len; i++)
            {
                byte b = (byte)str[i];
                crc_t += (UInt16)b;
            }
            return crc_t;
        }
    }
}
