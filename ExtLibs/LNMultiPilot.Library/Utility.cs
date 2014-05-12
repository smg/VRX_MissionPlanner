using System;
using System.Collections.Generic;
using System.Text;

namespace LNMultiPilot.Library
{
    public class Utility
    {
        public static double Str2Double(string str)
        {
            double dRet = 0;
            try
            {
                
                str = str.Replace(".", System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);
                if (!double.TryParse(str, out dRet))
                    dRet = 0;
                //dRet = Convert.ToDouble(str);
            }
            catch (Exception ex)
            {
            }
            return dRet;
        }

        public static string Double2Str(double d)
        {
            string strRet = d.ToString();
            return strRet.Replace(System.Globalization.NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, ".");
        }

    }
}
