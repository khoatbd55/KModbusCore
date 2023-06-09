using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.Extentions
{
    public static class NumberExtention
    {
        public static string ToCustomString(this List<float> list)
        {
            var str = "";
            foreach (var item in list)
            {
                // str += ((int)(item*1000)).ToString() + " ";
                str += item.ToThreeDigi() + " ";
            }
            return str;
        }
        public static string ToTwoIntergerDigi(this int number)
        {
            if (number < 10)
                return "0" + number.ToString();
            else
                return number.ToString();
        }
        public static string ToTwoDigi(this double number)
        {
            return number.ToString("F2");
        }

        public static string ToTwoDigi(this float number)
        {
            return number.ToString("F2");
        }

        public static string ToThreeDigi(this float number)
        {
            return number.ToString("F3");
        }

        public static string ToThreeDigi(this double number)
        {
            return number.ToString("F3");
        }


    }
}
