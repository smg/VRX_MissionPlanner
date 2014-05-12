using System;
using System.Collections.Generic;
using System.Text;

namespace LNMultiPilot.Library
{
    public class HPR
    {
        public HPR()
        {
            Heading = 0;
            Pitch = 0;
            Roll = 0;
        }
        public double Heading;
        public double Pitch;
        public double Roll;

        public void CopyFrom(HPR src)
        {
            Heading = src.Heading;
            Pitch = src.Pitch;
            Roll = src.Roll;
        }
        public string Encode()
        {
            return Utility.Double2Str(Roll) + "," + Utility.Double2Str(Pitch) + "," + Utility.Double2Str(Heading);
        }
        public override string ToString()
        {
            return "ROLL:  " + Utility.Double2Str(Roll) +
               "\r\nPITCH: " + Utility.Double2Str(Pitch) +
               "\r\nYAW:   " + Utility.Double2Str(Heading);
        }
    }

    public class XYZ
    {
        public double X;
        public double Y;
        public double Z;

        public void CopyFrom(XYZ src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
        }

        public string Encode()
        {
            return Utility.Double2Str(X) + "," + Utility.Double2Str(Y) + "," + Utility.Double2Str(Z);
        }

        public override string ToString()
        {
            return "X: " + Utility.Double2Str(X) +
               "\r\nY: " + Utility.Double2Str(Y) +
               "\r\nZ: " + Utility.Double2Str(Z);
        }
    }

    public class MPRemote
    {
        public HPR hpr = new HPR();
        public double throttle;
        public double aux;
        public double aux2;

        public MPRemote()
        {
        }

        public MPRemote(MPRemote src)
        {
            CopyFrom(src);
        }

        public void CopyFrom(MPRemote src)
        {
            hpr.CopyFrom(src.hpr);
            throttle = src.throttle;
            aux = src.aux;
            aux2 = src.aux2;
        }

        public string Encode()
        {
            string strRet = hpr.Encode();
            strRet = strRet + "," + Utility.Double2Str(throttle);
            strRet = strRet + "," + Utility.Double2Str(aux);
            strRet = strRet + "," + Utility.Double2Str(aux2);
            return strRet;
        }
    }

    public class MPData
    {
        public string swVersion = "";
        public int systemtime = 0;
        public HPR gyro_rate = new HPR();
        public XYZ acceleration = new XYZ();
        public HPR euler = new HPR();
        public XYZ magnetometer = new XYZ();
        public double magneticHeading;
        public string motorConfig = "";
        public int[] motors;
        public HPR control = new HPR();
        /*
        public HPR remote_hpr = new HPR();
        public double remote_throttle;
        public double remote_aux;
        public double remote_aux2;
         * */

        public int remote_systemtime = 0;
        public MPRemote remote = new MPRemote();
        public double sonar_val;
        public double sonar_target;
        public double sonar_alt_err;

        public Position position = new Position();
        public Position target_position = new Position();
        public int target_active;
        public XYZ target_diff = new XYZ();
        public HPR command_gps = new HPR();

        public bool acrobatic_active;


        public void CopyFrom(MPData src)
        {
            systemtime = src.systemtime;
            swVersion = src.swVersion;
            gyro_rate.CopyFrom(src.gyro_rate);
            acceleration.CopyFrom(src.acceleration);
            euler.CopyFrom(src.euler);
            magnetometer.CopyFrom(src.magnetometer);
            magneticHeading = src.magneticHeading;
            motorConfig = src.motorConfig;
            if (src.motors != null)
            {
                int n = src.motors.Length;
                motors = new int[n];
                for (int i = 0; i < n; i++)
                    motors[i] = src.motors[i];
            }
            control.CopyFrom(src.control);
            remote_systemtime = src.remote_systemtime;
            remote.CopyFrom(src.remote);
            position.CopyFrom(src.position);
            target_active = src.target_active;
            target_position.CopyFrom(src.target_position);
            target_diff.CopyFrom(src.target_diff);
            command_gps.CopyFrom(src.command_gps);

            acrobatic_active = src.acrobatic_active;
        }


        public void CopyToStruct(ref RPC.T_MPData dest)
        {
            char [] sep = {'.'};
            string[] vers = swVersion.Split(sep);
            if (vers.Length > 0)
                dest.version.Major = (short) Utility.Str2Double(vers[0]);
            if (vers.Length > 1)
                dest.version.Minor = (short)Utility.Str2Double(vers[1]);
            if (vers.Length > 2)
                dest.version.Revision = (short)Utility.Str2Double(vers[2]);
            
            dest.systemtime = systemtime;

            dest.gyro_rate.roll = (short) gyro_rate.Roll;
            dest.gyro_rate.pitch = (short)gyro_rate.Pitch;
            dest.gyro_rate.yaw = (short)gyro_rate.Heading;
            dest.acceleration.X = (short)acceleration.X;
            dest.acceleration.Y = (short)acceleration.Y;
            dest.acceleration.Z = (short)acceleration.Z;
            dest.euler.roll = (short)euler.Roll;
            dest.euler.pitch = (short)euler.Pitch;
            dest.euler.yaw = (short)euler.Heading;
            dest.magnetometer.X = (short)magnetometer.X;
            dest.magnetometer.Y = (short)magnetometer.Y;
            dest.magnetometer.Z = (short)magnetometer.Z;
            dest.magneticHeading = (short)magneticHeading;
            if (motorConfig.Length > 0)
                dest.motorCfg0 = (byte)motorConfig[0];
            if (motorConfig.Length > 1)
                dest.motorCfg1 = (byte)motorConfig[1];
            if (motorConfig.Length > 2)
                dest.motorCfg2 = (byte)motorConfig[2];
            dest.motorCount = 0;
            if (motors != null)
            {
                dest.motorCount = (byte)motors.Length;
                int i = 0;
                if (dest.motorCount > i)
                    dest.motor00 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor01 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor02 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor03 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor04 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor05 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor06 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor07 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor08 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor09 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor10 = (short)motors[i];
                i++;
                if (dest.motorCount > i)
                    dest.motor11 = (short)motors[i];
                i++;
            }
            dest.control.roll = (short)control.Roll;
            dest.control.pitch = (short)control.Pitch;
            dest.control.yaw = (short)control.Heading;

            dest.remote.hpr.roll = (short)remote.hpr.Roll;
            dest.remote.hpr.pitch = (short)remote.hpr.Pitch;
            dest.remote.hpr.yaw = (short)remote.hpr.Heading;
            dest.remote.throttle = (short)remote.throttle;
            dest.remote.aux = (short)remote.aux;
            dest.remote.aux2 = (short)remote.aux2;

            dest.acrobatic_active = (byte) (acrobatic_active? 1 : 0);
            dest.target_active = (byte) target_active;
            dest.position.lat = (int) (position.dLat * 10000000);
            dest.position.lon = (int) (position.dLon * 10000000);
            dest.position.alt = (int)(position.dAlt * 1000);

            dest.position.groundspeed = (short) position.dGroundSpeed;
            dest.position.ground3D = (short)position.dSpeed3D;
            dest.position.heading = (short)position.dHeading;
            dest.position.NSat = (byte)position.iNumSat;
            dest.position.Fix = (byte)position.iFix;
            dest.position.Time = position.iTime;
            
            /*
            target_position.CopyFrom(src.target_position);
            target_diff.CopyFrom(src.target_diff);
            command_gps.CopyFrom(src.command_gps);
            */
            
        }


        public void CopyFromStruct(RPC.T_MPData src)
        {
            swVersion = src.version.Major + "." + src.version.Minor;
            if (src.version.Revision > 0)
                swVersion = swVersion + "." + src.version.Revision;


            systemtime = src.systemtime;

            gyro_rate.Roll = src.gyro_rate.roll;
            gyro_rate.Pitch = src.gyro_rate.pitch;
            gyro_rate.Heading = src.gyro_rate.yaw;
            acceleration.X = src.acceleration.X;
            acceleration.Y = src.acceleration.Y;
            acceleration.Z = src.acceleration.Z;
            euler.Roll = src.euler.roll;
            euler.Pitch = src.euler.pitch;
            euler.Heading = src.euler.yaw;
            magnetometer.X = src.magnetometer.X;
            magnetometer.Y = src.magnetometer.Y;
            magnetometer.Z = src.magnetometer.Z;
            magneticHeading = src.magneticHeading;

            motorConfig = "";
            if (src.motorCfg0 != 0)
                motorConfig = motorConfig + (char)src.motorCfg0;
            if (src.motorCfg1 != 0)
                motorConfig = motorConfig + (char)src.motorCfg1;
            if (src.motorCfg2 != 0)
                motorConfig = motorConfig + (char)src.motorCfg2;

            //System.Diagnostics.Debug.WriteLine(motorConfig);
            if (motorConfig != "4X")
            {
                int p = 0;
            }
            motors = null;
            if (src.motorCount > 0)
            {
                motors = new int[src.motorCount];

                int i = 0;
                if (src.motorCount > i)
                    motors[i] = src.motor00;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor01;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor02;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor03;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor04;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor05;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor06;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor07;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor08;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor09;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor10;
                i++;
                if (src.motorCount > i)
                    motors[i] = src.motor11;
                i++;
            }
            control.Roll = src.control.roll;
            control.Pitch = src.control.pitch;
            control.Heading = src.control.yaw;

            remote_systemtime = src.remote_systemtime;
            remote.hpr.Roll = src.remote.hpr.roll;
            remote.hpr.Pitch = src.remote.hpr.pitch;
            remote.hpr.Heading = src.remote.hpr.yaw;
            remote.throttle = src.remote.throttle;
            remote.aux = src.remote.aux;
            remote.aux2 = src.remote.aux2;
            acrobatic_active = (src.acrobatic_active == 1);

            target_active = src.target_active;
            position.dLat = src.position.lat / 10000000.0;
            position.dLon = src.position.lon / 10000000.0;
            position.dAlt = src.position.alt / 1000.0;
            position.dGroundSpeed = src.position.groundspeed;
            position.dSpeed3D = src.position.ground3D;
            position.dHeading = src.position.heading;
            position.iNumSat = src.position.NSat;
            position.iFix = src.position.Fix;
            position.iTime = src.position.Time;

            sonar_val = src.sonar_val;
            sonar_target = src.sonar_target;
            sonar_alt_err = src.sonar_alt_err;

            /*
            target_position.CopyFrom(src.target_position);
            target_diff.CopyFrom(src.target_diff);
            command_gps.CopyFrom(src.command_gps);
            */

        }


        public string Encode()
        {
            string strRet = "";
            strRet = strRet + "!V:" + this.swVersion;
            strRet = strRet + "!T:" + this.systemtime;
            strRet = strRet + "!A:" + gyro_rate.Encode() + "," + acceleration.Encode();
            strRet = strRet + "!E:" + euler.Encode();
            strRet = strRet + "!C:" + magnetometer.Encode() + "," + Utility.Double2Str(magneticHeading);
            int n = 0;
            if (motors != null)
                n = motors.Length;
            strRet = strRet + "!M:" + n + "," + motorConfig;
            for (int i = 0; i < n; i++)
            {
                strRet = strRet + "," + Utility.Double2Str(motors[i]);
            }
            strRet = strRet + "!CH:" + remote.Encode();
            strRet = strRet + "!CT:" + control.Encode();
            strRet = strRet + "!S:" + Utility.Double2Str(sonar_val);
            strRet = strRet + "," + Utility.Double2Str(sonar_target);
            strRet = strRet + "," + Utility.Double2Str(sonar_alt_err);
            strRet = strRet + "!G:" + Utility.Double2Str(position.dLat * 10000000);
            strRet = strRet + "," + Utility.Double2Str(position.dLon * 10000000);
            strRet = strRet + "," + Utility.Double2Str(position.dAlt * 1000);
            strRet = strRet + "," + Utility.Double2Str(position.dGroundSpeed);
            strRet = strRet + "," + Utility.Double2Str(position.dSpeed3D);
            strRet = strRet + "," + Utility.Double2Str(position.dHeading * 10);
            strRet = strRet + "," + Utility.Double2Str(position.iNumSat);
            strRet = strRet + "," + Utility.Double2Str(position.iFix);
            strRet = strRet + "," + Utility.Double2Str(position.iTime);
            strRet = strRet + "!GH:" + Utility.Double2Str(target_active);
            strRet = strRet + "," + Utility.Double2Str(target_position.dLat * 10000000);
            strRet = strRet + "," + Utility.Double2Str(target_position.dLon * 10000000);
            strRet = strRet + "," + Utility.Double2Str(target_position.dAlt * 1000);
            strRet = strRet + "," + target_diff.Encode();
            strRet = strRet + "," + Utility.Double2Str(command_gps.Roll);
            strRet = strRet + "," + Utility.Double2Str(command_gps.Pitch);
            
            strRet = strRet + "\r\n";
            return strRet;
        }

        public override string ToString()
        {
            string s = "";
            s = s + "SW: " + swVersion;
            s = s + "\r\n";
            s = s + "Time: " + systemtime.ToString();
            s = s + "\r\n";
            s = s + position.ToString();
            s = s + "\r\n";
            s = s + "EULER:\r\n";
            s = s + euler.ToString();
            s = s + "\r\n";
            s = s + "ACCEL:\r\n";
            s = s + acceleration.ToString();
            s = s + "\r\n";
            s = s + "GYRO:\r\n";
            s = s + gyro_rate.ToString();
            s = s + "\r\n";
            return s;
        }

    }
}
