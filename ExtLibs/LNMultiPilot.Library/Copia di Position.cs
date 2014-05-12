using System;
using System.Collections.Generic;
using System.Text;

namespace LNMultiPilot.Library
{
    public class Position
    {
        const string POSCSV_SEPARATOR = "," ;

        const int POSCSV_FIELDCOUNT = 12;
        const int POSCSV_LONGITUDE = 0;
        const int POSCSV_LATITUDE = 1;
        const int POSCSV_ALTITUDE = 2;
        const int POSCSV_HEADING = 3;
        const int POSCSV_PITCH = 4;
        const int POSCSV_ROLL = 5;


        public double dLon;
        public double dLat;
        public double dAlt;
        public double dHeading;
        public double dPitch;
        public double dRoll;

        
        public void CopyFrom(Position pos)
        {
            dLon = pos.dLon; 
            dLat = pos.dLat; 
            dAlt = pos.dAlt; 
            dHeading = pos.dHeading; 
            dPitch = pos.dPitch;
            dRoll  = pos.dRoll; 
        }

        public override string ToString()
        {
            string str = "";
            str = str +     "Longitude: " + Utility.Double2Str(dLon);
            str = str + "\r\nLatitude:  " + Utility.Double2Str(dLat);
            str = str + "\r\nAltitude:  " + Utility.Double2Str(dAlt);
            str = str + "\r\nHeading:   " + Utility.Double2Str(dHeading);
            str = str + "\r\nPitch:     " + Utility.Double2Str(dPitch);
            str = str + "\r\nRoll:      " + Utility.Double2Str(dRoll);
            return str;
        }

        public static bool DecodePosition(string strInput, ref Position pos)
        {
            bool bRet = false;
            if ((strInput != null) && (strInput.Length >= 7) && (strInput.Substring(0, 7) == "!!!VER:"))
                bRet = DecodePositionASCII(strInput, ref pos);
            else
                //bRet = DecodePositionCSV(strInput, ref pos);
                bRet = DecodePositionQ(strInput, ref pos);
                
            return bRet;
        }

        public static bool DecodePositionCSV(string strInput, ref Position pos)
        {
            bool bRet = false;
            string[] seps = { POSCSV_SEPARATOR };
            string[] fields = strInput.Split(seps, StringSplitOptions.None);
            if (fields.Length >= POSCSV_FIELDCOUNT)
            {
                pos.dLon = Utility.Str2Double(fields[POSCSV_LONGITUDE]);
                pos.dLat = Utility.Str2Double(fields[POSCSV_LATITUDE]);
                pos.dAlt = Utility.Str2Double(fields[POSCSV_ALTITUDE]);
                pos.dHeading = Utility.Str2Double(fields[POSCSV_HEADING]);
                pos.dPitch = Utility.Str2Double(fields[POSCSV_PITCH]);
                pos.dRoll = Utility.Str2Double(fields[POSCSV_ROLL]);
                bRet = true;
            }
            return bRet;
        }

        public static bool DecodePositionQ(string strInput, ref Position pos)
        {
            bool bRet = false;
            string[] seps = { POSCSV_SEPARATOR };
            string[] fields = strInput.Split(seps, StringSplitOptions.None);
            if (fields.Length >= 11)
            {
                pos.dLon = 9; // Utility.Str2Double(fields[POSCSV_LONGITUDE]);
                pos.dLat = 45; // Utility.Str2Double(fields[POSCSV_LATITUDE]);
                pos.dAlt = 200; // Utility.Str2Double(fields[POSCSV_ALTITUDE]);
                pos.dHeading = Utility.Str2Double(fields[10]);
                pos.dPitch = Utility.Str2Double(fields[9]);
                pos.dRoll = Utility.Str2Double(fields[8]);
                bRet = true;
            }
            return bRet;
        }


        public static bool DecodePositionASCII(string strInput, ref Position pos)
        {
            bool bRet = false;
            string[] seps = { POSCSV_SEPARATOR };
            string[] valsep = { ":" };
            string[] fields = strInput.Split(seps, StringSplitOptions.RemoveEmptyEntries);
            bool bYaw = false;
            for (int i = 0; i < fields.Length ; i++)
            {
                string[] val = fields[i].Split(valsep, StringSplitOptions.None);
                switch (val[0])
                { 
                    case "LON":
                        bRet = true;
                        pos.dLon = Utility.Str2Double(val[1]);
                        break;
                    case "LAT":
                        pos.dLat = Utility.Str2Double(val[1]);
                        break;
                    case "ALT":
                        pos.dAlt = Utility.Str2Double(val[1]);
                        break;
                    case "MGH":  //mag heading
                        if (!bYaw)
                            pos.dHeading = Utility.Str2Double(val[1]);
                        break;
                    case "YAW":
                        bYaw = true;
                        pos.dHeading = Utility.Str2Double(val[1]);
                        break;
                    case "PCH":
                        pos.dPitch = Utility.Str2Double(val[1]);
                        break;
                    case "RLL":
                        pos.dRoll = Utility.Str2Double(val[1]);
                        break;
                }
            }
            return bRet;
        }


        public static string EncodePosition(Position pos)
        {
            string[] fields = new string[POSCSV_FIELDCOUNT];
            fields[POSCSV_LONGITUDE] = Utility.Double2Str(pos.dLon);
            fields[POSCSV_LATITUDE] = Utility.Double2Str(pos.dLat);
            fields[POSCSV_ALTITUDE] = Utility.Double2Str(pos.dAlt);
            fields[POSCSV_HEADING] = Utility.Double2Str(pos.dHeading);
            fields[POSCSV_PITCH] = Utility.Double2Str(pos.dPitch);
            fields[POSCSV_ROLL] = Utility.Double2Str(pos.dRoll);

            return String.Join(POSCSV_SEPARATOR, fields);
        }

    }
}
