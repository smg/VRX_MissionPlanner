﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;

namespace MissionPlanner.Log
{
    /// <summary>
    /// read log and extract log
    /// </summary>
    public class DFLog
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public struct Label
        {
            public int Id;
            public string Format;
            public string[] FieldNames;

            public int Length;
            public string Name;
        }

        public struct DFItem
        {
            public string msgtype;
            public DateTime time;
            public string[] items;
            public int timems;
            public int lineno;
        }

        public enum error_subsystem
        {
            MAIN = 1,
            RADIO = 2,
            COMPASS = 3,
            OPTFLOW = 4,
            FAILSAFE_RADIO = 5,
            FAILSAFE_BATT = 6,
            FAILSAFE_GPS = 7,
            FAILSAFE_GCS = 8,
            FAILSAFE_FENCE = 9,
            FLIGHT_MODE = 10,
            GPS = 11,
            CRASH_CHECK = 12
        }

        public enum error_code
        {
            ERROR_RESOLVED = 0,
            FAILED_TO_INITIALISE = 1,
            // subsystem specific error codes -- radio
            RADIO_LATE_FRAME = 2,
            // subsystem specific error codes -- failsafe_thr, batt, gps
            FAILSAFE_RESOLVED = 0,
            FAILSAFE_OCCURRED = 1,
            // subsystem specific error codes -- compass
            COMPASS_FAILED_TO_READ = 2,
            // subsystem specific error codes -- gps
            GPS_GLITCH = 2,
            // subsystem specific error codes -- main
            MAIN_INS_DELAY = 1,
            // subsystem specific error codes -- crash checker
            CRASH_CHECK_CRASH = 1,
        }

        public enum events
        {
            DATA_MAVLINK_FLOAT = 1,
            DATA_MAVLINK_INT32 = 2,
            DATA_MAVLINK_INT16 = 3,
            DATA_MAVLINK_INT8 = 4,
            DATA_AP_STATE = 7,
            DATA_INIT_SIMPLE_BEARING = 9,
            DATA_ARMED = 10,
            DATA_DISARMED = 11,
            DATA_AUTO_ARMED = 15,
            DATA_TAKEOFF = 16,
            DATA_LAND_COMPLETE = 18,
            DATA_NOT_LANDED = 28,
            DATA_LOST_GPS = 19,
            DATA_BEGIN_FLIP = 21,
            DATA_END_FLIP = 22,
            DATA_EXIT_FLIP = 23,
            DATA_SET_HOME = 25,
            DATA_SET_SIMPLE_ON = 26,
            DATA_SET_SIMPLE_OFF = 27,
            DATA_SET_SUPERSIMPLE_ON = 29,
            DATA_AUTOTUNE_INITIALISED = 30,
            DATA_AUTOTUNE_OFF = 31,
            DATA_AUTOTUNE_RESTART = 32,
            DATA_AUTOTUNE_COMPLETE = 33,
            DATA_AUTOTUNE_ABANDONED = 34,
            DATA_AUTOTUNE_REACHED_LIMIT = 35,
            DATA_AUTOTUNE_TESTING = 36,
            DATA_AUTOTUNE_SAVEDGAINS = 37,
            DATA_SAVE_TRIM = 38,
            DATA_SAVEWP_ADD_WP = 39,
            DATA_SAVEWP_CLEAR_MISSION_RTL = 40,
            DATA_FENCE_ENABLE = 41,
            DATA_FENCE_DISABLE = 42,
            DATA_ACRO_TRAINER_DISABLED = 43,
            DATA_ACRO_TRAINER_LEVELING = 44,
            DATA_ACRO_TRAINER_LIMITED = 45,
            DATA_EPM_ON = 46,
            DATA_EPM_OFF = 47,
            DATA_EPM_NEUTRAL = 48,
        }

        public static Dictionary<string, Label> logformat = new Dictionary<string, Label>();

        public static void Clear()
        {
            logformat.Clear();
        }

        public static DateTime GetFirstGpsTime(string fn)
        {
            using (StreamReader sr = new StreamReader(fn)) 
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (line.StartsWith("FMT"))
                    {
                        FMTLine(line);
                    }
                    else if (line.StartsWith("GPS"))
                    {
                        DateTime answer = GetTimeGPS(line);
                        if (answer != DateTime.MinValue)
                            return answer;
                    }
                }
            }

            return DateTime.MinValue;
        }

        public static List<DFItem> ReadLog(string fn)
        {
            List<DFItem> answer = new List<DFItem>();

            using (Stream st = File.OpenRead(fn))
            {
                answer = ReadLog(st);
            }

            return answer;
        }

        public static List<DFItem> ReadLog(Stream fn)
        {
            Clear();
            GC.Collect();

            List<DFItem> answer = new List<DFItem>();
            // current gps time
            DateTime gpstime = DateTime.MinValue;
            // last time of message
            DateTime lasttime = DateTime.MinValue;
            // first valid gpstime
            DateTime gpsstarttime = DateTime.MinValue;

            int lineno = 0;
            int msoffset = 0;


            log.Info("loading log " + (GC.GetTotalMemory(false) / 1024.0/1024.0));
           

            using (StreamReader sr = new StreamReader(fn))
            {
                while (!sr.EndOfStream)
                {
                    try
                    {
                        string line = sr.ReadLine();

                        lineno++;

                        //line = line.Replace(",", ",");
                        //line = line.Replace(":", ":");

                        string[] items = line.Split(new char[] { ',', ':' }, StringSplitOptions.RemoveEmptyEntries);

                        if (line.StartsWith("FMT"))
                        {
                            FMTLine(line);
                        }
                        else if (line.StartsWith("GPS"))
                        {
                           // if (gpsstarttime == DateTime.MinValue)
                            {
                                gpsstarttime = GetTimeGPS(line);
                                lasttime = gpsstarttime;

                                int indextimems = FindMessageOffset("GPS", "T");

                                if (indextimems != -1)
                                {
                                    try
                                    {
                                        msoffset = int.Parse(items[indextimems]);
                                    }
                                    catch { }
                                }
                            }
                        }
                        else if (line.StartsWith("ERR"))
                        {
                            Array.Resize(ref items, items.Length + 2);
                            try
                            {
                                int index = FindMessageOffset("ERR", "Subsys");
                                if (index == -1)
                                {
                                    throw new ArgumentNullException();
                                }

                                int index2 = FindMessageOffset("ERR", "ECode");
                                if (index2 == -1)
                                {
                                    throw new ArgumentNullException();
                                }

                                items[items.Length - 2] = ""+(DFLog.error_subsystem)int.Parse(items[index]);
                            }
                            catch { }
                        }
                        else if (line.StartsWith("EV"))
                        {
                            Array.Resize(ref items, items.Length + 1);
                            try
                            {
                                int index = FindMessageOffset("EV", "Id");
                                if (index == -1)
                                {
                                    throw new ArgumentNullException();
                                }

                                items[items.Length - 1] = "" + (DFLog.events)int.Parse(items[index]);
                            }
                            catch { }
                        }

                        DFItem item = new DFItem();
                        try
                        {
                            item.lineno = lineno;

                            if (items.Length > 0)
                            {
                                item.msgtype = items[0];
                                item.items = items;

                                if (line.StartsWith("GPS"))
                                {
                                    item.time = GetTimeGPS(line);
                                }
                                else
                                {
                                    if (logformat.ContainsKey(item.msgtype))
                                    {
                                        int indextimems = FindMessageOffset(item.msgtype, "TimeMS");

                                        if (indextimems != -1)
                                        {
                                            item.timems = int.Parse(items[indextimems]);

                                            item.time = gpsstarttime.AddMilliseconds(item.timems - msoffset);

                                            lasttime = item.time;
                                        }
                                        else
                                        {
                                            item.time = lasttime;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }

                        answer.Add(item);
                    }
                    catch { }
                }
            }

            log.Info("loaded log " + (GC.GetTotalMemory(false) / 1024.0 / 1024.0));

            return answer;
        }

        public static void FMTLine(string strLine)
        {
            try
            {
                if (strLine.StartsWith("FMT"))
                {
                    strLine = strLine.Replace(", ", ",");
                    strLine = strLine.Replace(": ", ":");

                    string[] items = strLine.Split(',', ':');

                    string[] names = new string[items.Length - 5];
                    Array.ConstrainedCopy(items, 5, names, 0, names.Length);

                    Label lbl = new Label() { Name = items[3], Id = int.Parse(items[1]), Format = items[4], Length = int.Parse(items[2]), FieldNames = names };

                    logformat[lbl.Name] = lbl;
                }
            }
            catch { }
        }

        //FMT, 130, 45, GPS, BIHBcLLeeEefI, Status,TimeMS,Week,NSats,HDop,Lat,Lng,RelAlt,Alt,Spd,GCrs,VZ,T
        //GPS, 3, 130040903, 1769, 10, 0.00, -35.3547178, 149.1696673, 885.52, 870.45, 24.56, 321.44, 2.450000, 127615
        public static DateTime GetTimeGPS(string gpsline)
        {
            if (gpsline.StartsWith("GPS") && logformat.Count > 0)
            {
                string strLine = gpsline.Replace(", ", ",");
                strLine = strLine.Replace(": ", ":");

                string[] items = strLine.Split(',', ':');

                // check its a valid lock
                int indexstatus = FindMessageOffset("GPS", "Status");

                if (indexstatus != -1)
                {
                    if (items[indexstatus].Trim() != "3" && items[indexstatus].Trim() != "2")
                        return DateTime.MinValue;
                }

                // get time since start of week
                int indextimems = FindMessageOffset("GPS", "TimeMS");

                // get week number
                int indexweek = FindMessageOffset("GPS", "Week");

                if (indextimems == -1 || indexweek == -1)
                    return DateTime.MinValue;

                return gpsTimeToTime(int.Parse(items[indexweek]), int.Parse(items[indextimems]) / 1000.0);
            }

            return DateTime.MinValue;
        }

        public static DateTime gpsTimeToTime(int week, double sec)
        {
            int leap = 16;

            // not correct for leap seconds                   day   days  weeks  seconds
            var basetime = new DateTime(1980, 1, 6, 0, 0, 0, DateTimeKind.Utc);
            basetime = basetime.AddDays(week * 7);
            basetime = basetime.AddSeconds((sec - leap));

            return basetime.ToLocalTime();
        }

        public static int FindMessageOffset(string linetype,string find)
        {
            if (logformat.ContainsKey(linetype))
                return Log.DFLog.FindInArray(logformat[linetype].FieldNames, find);

            return -1;
        }

        public static int FindInArray(string[] array, string find)
        {
            int a = 1;
            foreach (string item in array)
            {
                if (item == find)
                {
                    return a;
                }
                a++;
            }
            return -1;
        }
    }
}
