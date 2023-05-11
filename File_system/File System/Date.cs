using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace File_system.File_System
{
    class Date
    {
        public byte Day { private set; get; }
        public byte Month { private set; get; }
        public ushort Year { private set; get; }
        public byte Hour { private set; get; }
        public byte Minute { private set; get; }
        public byte Second { private set; get; }

        public enum Date_Mode
        {
            empty = 0, now_all,
        }

        public Date(Date_Mode mode)
        {           
            switch (mode)
            {
                case Date_Mode.empty:
                    {
                        Day = 0;
                        Month = 0;
                        Year = 0;
                        Hour = 0;
                        Minute = 0;
                        Second = 0;
                        break;
                    }

                case Date_Mode.now_all:
                    {
                        DateTime dt = DateTime.Now;
                        Day = (byte)dt.Day;
                        Month = (byte)dt.Month;
                        Year = (ushort)dt.Year;
                        Hour = (byte)dt.Hour;
                        Minute = (byte)dt.Minute;
                        Second = (byte)dt.Second;
                        break;
                    }
            }
        }

        public static byte[] Date_To_Byte(Date date)
        {
            byte[] bytes = new byte[7];
            bytes[0] = date.Day;
            bytes[1] = date.Month;
            Array.Copy(BitConverter.GetBytes(date.Year), 0, bytes, 2, 2);
            bytes[4] = date.Hour;
            bytes[5] = date.Minute;
            bytes[6] = date.Second;
            return bytes;
        }

        public static Date Byte_To_Date(byte[] bytes, int start_index)
        {
            Date date = new Date(Date_Mode.empty);
            date.Day = bytes[start_index];
            date.Month = bytes[start_index + 1];
            date.Year = BitConverter.ToUInt16(bytes, start_index + 2);
            date.Hour = bytes[start_index + 4];
            date.Minute = bytes[start_index + 5];
            date.Second = bytes[start_index + 6];
            return date;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2} {3}:{4}:{5}", Day, Month, Year, Hour, Minute, Second);
        }
    }
}
