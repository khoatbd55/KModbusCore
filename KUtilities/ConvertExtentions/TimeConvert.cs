using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.ConvertExtentions
{
    public class TimeConvert
    {

        public static Tuple<bool, int> CheckOnline(DateTime createdAt)
        {
            bool isOnline = false;
            var now_sub_2_minute = DateTime.Now.Subtract(new TimeSpan(0, 2, 0));
            if (DateTime.Compare(createdAt, now_sub_2_minute) < 0)// đã trừ đi rồi mà vẫn sớm hơn
            {
                isOnline = false;
            }
            else
            {
                isOnline = true;
            }
            int deltaSec = (int)DateTime.Now.Subtract(createdAt).TotalSeconds;
            if (deltaSec < 0)
                deltaSec = 0;
            return new Tuple<bool, int>(isOnline, deltaSec);
        }

        public static Tuple<bool, DateTime> DecodeDateTime(byte[] data)
        {
            try
            {
                byte sec = data[0];
                byte minute = data[1];
                byte hour = data[2];
                byte day = data[3];
                byte month = data[4];
                byte year = data[5];
                if (TimeConvert.ValidTime(year, month, day, hour, minute, sec))
                {
                    return new Tuple<bool, DateTime>(true, new DateTime(year + 2000, month, day, hour, minute, sec));
                }
                else
                    return new Tuple<bool, DateTime>(false, DateTime.Now);
            }
            catch (Exception)
            {
                return new Tuple<bool, DateTime>(false, DateTime.Now);
            }
        }

        public static DateTime GetDateTime(byte[] payload, DateTime timeRecv, ref int idx)
        {
            try
            {
                byte sec = payload[idx++];
                byte minute = payload[idx++];
                byte hour = payload[idx++];
                byte day = payload[idx++];
                byte month = payload[idx++];
                byte year = payload[idx++];
                if (TimeConvert.ValidTime(year, month, day, hour, minute, sec))
                {
                    DateTime time = new DateTime(year + 2000, month, day, hour, minute, sec);
                    return time;
                }
                else
                    return timeRecv;
            }
            catch (Exception)
            {
                return timeRecv;
            }
        }

        public static Tuple<DateTime, bool> DecodeBcdDateTime(byte[] payload, DateTime timeRecv, ref int idx)
        {
            int year = BcdToInt(payload[idx++]);
            int month = BcdToInt(payload[idx++]);
            int day = BcdToInt(payload[idx++]);
            int hour = BcdToInt(payload[idx++]);
            int minute = BcdToInt(payload[idx++]);
            int sec = BcdToInt(payload[idx++]);
            try
            {
                if (TimeConvert.ValidTime(year, month, day, hour, minute, sec))
                {
                    DateTime time = new DateTime(year + 2000, month, day, hour, minute, sec, DateTimeKind.Utc);
                    time = time.ToLocalTime();
                    DateTime timeCurrent_3p_Before = timeRecv.Subtract(new TimeSpan(0, 0, 30)); // - trừ thời gian hiện tại đi 5 phút 
                    DateTime timeCurrnet_3p_Last = timeRecv.Add(new TimeSpan(0, 0, 30));
                    if (DateTime.Compare(time, timeCurrent_3p_Before) < 0 || // nếu timeRecieve vẫn sớm hơn timecurrent khi trừ 5 phút
                                    DateTime.Compare(timeCurrnet_3p_Last, time) < 0)
                    {
                        return new Tuple<DateTime, bool>(timeRecv, false);
                    }
                    else
                        return new Tuple<DateTime, bool>(time, true);
                }
                else
                    return new Tuple<DateTime, bool>(timeRecv, false);
            }
            catch (Exception)
            {
                return new Tuple<DateTime, bool>(timeRecv, false);
            }

        }

        public static int BcdToInt(int value)
        {
            return (value >> 4) * 10 + (value & 0x0F);
        }

        public static DateTime DecodeDateTime(byte[] payload, DateTime timeRecv, ref int idx)
        {
            try
            {
                byte sec = payload[idx++];
                byte minute = payload[idx++];
                byte hour = payload[idx++];
                byte day = payload[idx++];
                byte month = payload[idx++];
                byte year = payload[idx++];
                if (TimeConvert.ValidTime(year, month, day, hour, minute, sec))
                {
                    DateTime time = new DateTime(year + 2000, month, day, hour, minute, sec);
                    DateTime timeCurrent_3p_Before = timeRecv.Subtract(new TimeSpan(0, 1, 0)); // - trừ thời gian hiện tại đi 5 phút 
                    DateTime timeCurrnet_3p_Last = timeRecv.Add(new TimeSpan(0, 1, 0));
                    if (DateTime.Compare(time, timeCurrent_3p_Before) < 0 || // nếu timeRecieve vẫn sớm hơn timecurrent khi trừ 5 phút
                                    DateTime.Compare(timeCurrnet_3p_Last, time) < 0)
                    {
                        return timeRecv;
                    }
                    else
                        return time;
                }
                else
                    return timeRecv;
            }
            catch (Exception)
            {
                return timeRecv;
            }
        }
        public static bool ValidTime(byte year, byte month, byte day, byte hour, byte minute, byte sec)
        {
            int err = 0;
            err += year != 0 ? 0 : 1;
            err += (month != 0 && month <= 12) ? 0 : 1;
            err += day > 0 && day <= 31 ? 0 : 1;
            err += hour >= 0 && hour < 24 ? 0 : 1;
            err += minute >= 0 && minute <= 59 ? 0 : 1;
            err += sec >= 0 && sec <= 59 ? 0 : 1;
            if (err == 0)
                return true;
            else
                return false;
        }
        public static bool ValidTime(int year, int month, int day, int hour, int minute, int sec)
        {
            int err = 0;
            err += year != 0 ? 0 : 1;
            err += (month != 0 && month <= 12) ? 0 : 1;
            err += day > 0 && day <= 31 ? 0 : 1;
            err += hour >= 0 && hour < 24 ? 0 : 1;
            err += minute >= 0 && minute <= 59 ? 0 : 1;
            err += sec >= 0 && sec <= 59 ? 0 : 1;
            if (err == 0)
                return true;
            else
                return false;
        }
        public static string ConvertDecToStringHex(int dec)
        {
            string hex = "";
            //hex += "";
            if (dec < 16)
                hex += "0";
            hex += Convert.ToString(dec, 16);

            return hex;
        }
        public static string ConvertArrayToStringHex(byte[] array)
        {
            string str = "";
            for (int i = 0; i < array.Length; i++)
            {
                str += ConvertDecToStringHex(array[i]) + " ";
            }
            string strHoa = "";
            foreach (char item in str)
            {
                if (item >= 'a' && item <= 'z')
                    strHoa += Convert.ToChar((int)item - 32).ToString();
                else
                    strHoa += item.ToString();
            }
            return strHoa;
        }
        public static bool ValidateCheckSum(byte[] frame)
        {
            int sum = 0;
            for (int i = 0; i < frame.Length; i++)
            {
                sum += frame[i];
            }
            if (sum % 256 == 0xFF)
                return true;
            else
                return false;
        }
    }
}
