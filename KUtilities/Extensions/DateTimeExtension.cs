using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.Extentions
{
    public static class DateTimeExtension
    {
        public static long ConvertToTimestamp(this DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return (long)Math.Floor(diff.TotalSeconds);
        }

        public static string ToCustomDateTimeString(this DateTime date)
        {
            return $"{date.Year}/{date.Month.ToTwoIntergerDigi()}/{date.Day.ToTwoIntergerDigi()} {date.Hour.ToTwoIntergerDigi()}:{date.Minute.ToTwoIntergerDigi()}:{date.Second.ToTwoIntergerDigi()}";
        }

        public static string ToCustomDateTime2LineString(this DateTime date)
        {
            string nl = Environment.NewLine; // new line variable
            return $"{date.Year}/{date.Month.ToTwoIntergerDigi()}/{date.Day.ToTwoIntergerDigi()}{nl}{date.Hour.ToTwoIntergerDigi()}:{date.Minute.ToTwoIntergerDigi()}:{date.Second.ToTwoIntergerDigi()}";
        }

        public static string ToCustomDateString(this DateTime date)
        {
            return $"{date.Year}/{date.Month.ToTwoIntergerDigi()}/{date.Day.ToTwoIntergerDigi()}";
        }

        public static string ToCustomTimeString(this DateTime date)
        {
            return $"{date.Hour.ToTwoIntergerDigi()}:{date.Minute.ToTwoIntergerDigi()}:{date.Second.ToTwoIntergerDigi()}";
        }


    }
}
