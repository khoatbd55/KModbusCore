using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KUtilities.ConvertExtentions
{
    public class VersionConvert
    {
        public static bool Compare(Tuple<int, int, int> value, Tuple<int, int, int> compare)
        {
            if (value.Item1 > compare.Item1)
                return true;
            else if ((value.Item1 == compare.Item1)
                      && (value.Item2 > compare.Item2))
                return true;
            else if ((value.Item1 == compare.Item1)
                    && (value.Item2 == compare.Item2)
                    && (value.Item3 >= compare.Item3))
                return true;
            return false;
        }

        public static Tuple<int, int, int> DecodeVersion(string version)
        {
            if (version != null)
            {
                try
                {
                    version = version.Replace("\0", String.Empty);
                    version = version.Replace("V", string.Empty);
                    version = version.Replace("v", string.Empty);
                    var array = version.Split('.');
                    if (array.Length >= 3)
                    {
                        int number0 = 0;
                        int number1 = 0;
                        int number2 = 0;
                        int.TryParse(array[0], out number0);
                        int.TryParse(array[1], out number1);
                        int.TryParse(array[2], out number2);
                        return new Tuple<int, int, int>(number0, number1, number2);
                    }
                    else
                    {
                        int number0 = 0;
                        int number1 = 0;
                        int.TryParse(array[0], out number0);
                        int.TryParse(array[1], out number1);
                        return new Tuple<int, int, int>(number0, number1, 0);
                    }


                }
                catch (Exception)
                {
                    return new Tuple<int, int, int>(1, 0, 0);
                }
            }
            else
            {
                return new Tuple<int, int, int>(0, 0, 0);
            }

        }
    }
}
