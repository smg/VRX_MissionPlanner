using System;
using System.Runtime.InteropServices;

namespace LNMultiPilot.Library.RPC
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct T_YPR
    {
        public short roll;
        public short pitch;
        public short yaw;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct T_XYZ
    {
        public short X;
        public short Y;
        public short Z;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct T_Remote
    {
        public T_YPR hpr;
        public short throttle;
        public short aux;
        public short aux2;
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        //public short[] extra;
        public short extra0;
        public short extra1;
        public short extra2;
        public short extra3;
        public short extra4;
        public short extra5;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct T_Version
    {
        public short Major;
        public short Minor;
        public short Revision;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct T_Gps
    {
        public int lat;
        public int lon;
        public int alt;
        public short heading;
        public short groundspeed;
        public short ground3D;
        public byte NSat;
        public byte Fix;
        public int Time;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct T_MPData
    {
        public T_Version version;       //6     
        public int systemtime;          //4
        public T_YPR gyro_rate;         //6
        public T_XYZ acceleration;      //6
        public T_YPR euler;             //6
        public T_XYZ magnetometer;      //6
        public short magneticHeading;   //2
        public byte motorCfg0;          //1
        public byte motorCfg1;          //1
        public byte motorCfg2;          //1
        public byte motorCount;         //1
        public short motor00;           //2
        public short motor01;           //2
        public short motor02;           //2
        public short motor03;           //2
        public short motor04;           //2
        public short motor05;           //2
        public short motor06;           //2
        public short motor07;           //2
        public short motor08;           //2
        public short motor09;           //2
        public short motor10;           //2
        public short motor11;           //2
        public T_YPR control;           //6
        public int remote_systemtime;   //4
        public T_Remote remote;         //24
        public int sonar_val;           //4
        public int sonar_target;        //4
        public int sonar_alt_err;       //4
        public byte acrobatic_active;   //1
        public byte target_active;      //1
        public T_Gps position;          //24
        public T_Gps target_position;   //24
        public T_XYZ target_diff;       //6
        public T_YPR command_gps;       //6
    }

}
