using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace LNMultiPilot.Library
{
    public static class MemUtils
    {

        public static byte[] SerializeToByteArray(object anything)
        {
            byte[] rawdatas;
            if (anything is string)
            {
                rawdatas = StrToByteArray((string)anything);
            }
            else
            {
                int rawsize = Marshal.SizeOf(anything);
                IntPtr buffer = Marshal.AllocHGlobal(rawsize);
                Marshal.StructureToPtr(anything, buffer, false);
                rawdatas = new byte[rawsize];
                Marshal.Copy(buffer, rawdatas, 0, rawsize);
                Marshal.FreeHGlobal(buffer);
            }
            return rawdatas;
        }

        public static string SerializeToString(object anything)
        {
            byte[] rawdatas = SerializeToByteArray(anything);
            return ByteArrayToStr(rawdatas);
        }

        


        /*
        public static object RawDeserialize(byte[] rawdatas, Type anytype)
        {
            int rawsize = Marshal.SizeOf(anytype);
            if (rawsize > rawdatas.Length)
                return null;
            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            Marshal.Copy(rawdatas, 0, buffer, rawsize);
            object retobj = Marshal.PtrToStructure(buffer, anytype);
            Marshal.FreeHGlobal(buffer);
            return retobj;
        }*/

        // C# to convert a string to a byte array.
        public static byte[] StrToByteArray(string str)
        {
            //System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            //return encoding.GetBytes(str);
            byte[] o = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
                o[i] = (byte) str[i];
            return o;
        }


        public static string ByteArrayToStr(byte[] dBytes)
        {
            //System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            //return encoding.GetString(dBytes);
            string o = "";
            for (int i = 0; i < dBytes.Length; i++)
                o = o + ((char)dBytes[i]).ToString();
            return o;
        }


        public static object RawDeserialize(System.IO.Stream fs, Type t)
        {
            byte[] buffer = new byte[Marshal.SizeOf(t)];
            fs.Read(buffer, 0, Marshal.SizeOf(t));
            return RawDeserialize(buffer, t);
        }

        public static object RawDeserialize(string str, Type t)
        {
            byte[] buffer = StrToByteArray(str);
            return RawDeserialize(buffer, t);
        }

        public static object RawDeserialize(byte[] buffer, Type t)
        {
            int rawsize = Marshal.SizeOf(t);
            if (rawsize > buffer.Length)
                return null;
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Object temp = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), t);
            handle.Free();
            return temp;
        }

        public static T TypedDeserialize<T>(System.IO.Stream fs)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            fs.Read(buffer, 0, Marshal.SizeOf(typeof(T)));
            return TypedDeserialize<T>(buffer);
        }


        public static T TypedDeserialize<T>(string str)
        {
            byte[] buffer = StrToByteArray(str);
            return TypedDeserialize<T>(buffer);
        }
        public static T TypedDeserialize<T>(byte[] buffer)
        {
            int rawsize = Marshal.SizeOf(typeof(T));
            if (rawsize > buffer.Length)
                return default(T);
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T temp = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return temp;
        }
    }
}
