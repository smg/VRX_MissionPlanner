using System;
using System.Collections.Generic;
using System.Text;

namespace LNMultiPilot.Library
{
    public class Position
    {
        public double dLon;
        public double dLat;
        public double dAlt;
        public double dHeading;
        public double dGroundSpeed;
        public double dSpeed3D;
        public int iNumSat;
        public int iFix;
        public int iTime;

        
        public void CopyFrom(Position pos)
        {
            dLon = pos.dLon; 
            dLat = pos.dLat; 
            dAlt = pos.dAlt; 
            dHeading = pos.dHeading;
            dGroundSpeed = pos.dGroundSpeed;
            dSpeed3D = pos.dSpeed3D;
            iNumSat = pos.iNumSat;
            iFix = pos.iFix;
            iTime = pos.iTime; 
        }

        public override string ToString()
        {
            string str = "";
            str = str +     "Longitude: " + Utility.Double2Str(dLon);
            str = str + "\r\nLatitude:  " + Utility.Double2Str(dLat);
            str = str + "\r\nAltitude:  " + Utility.Double2Str(dAlt);
            str = str + "\r\nHeading:   " + Utility.Double2Str(dHeading);
            str = str + "\r\nGr. Speed: " + Utility.Double2Str(dGroundSpeed);
            str = str + "\r\nSpeed3D:   " + Utility.Double2Str(dSpeed3D);
            str = str + "\r\nNumSat:    " + Utility.Double2Str(iNumSat);
            str = str + "\r\nFix:       " + Utility.Double2Str(iFix);
            str = str + "\r\nTime:      " + Utility.Double2Str(iTime);
            return str;
        }
    }
}
