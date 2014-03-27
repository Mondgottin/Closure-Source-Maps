using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClosureSourceMaps
{
    /// <summary>
    /// A utility class for working with Base64 values
    /// @author johnlenz@google.com (John Lenz)
    /// </summary>
    public static class Base64
    {
        // This is a utility class
        static Base64() 
        {
            Base64DecodeMap = new int[256];
            Fill(Base64DecodeMap, -1);
            for (int i = 0; i < Base64Map.Length; ++i)
                Base64DecodeMap[Base64Map[i]] = i;
        }

        
        /// <summary>
        /// A map used to convert integer values in the range 0-63 to their Base64 values
        /// </summary>
        private const string Base64Map = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                                                "abcdefghijklmnopqrstuvwxyz" +
                                                "0123456789+/";

        private static int[] Base64DecodeMap;

        /// <summary>
        ///  Fill array with value
        /// </summary>
        /// <param name="array"></param>
        /// <param name="value"></param>
        private static void Fill(this int[] array, int value)
        {
            for (int i = 0; i < array.Length; ++i)
                array[i] = value;
        }

        /// <summary>
        /// Convert integer value in the range 0-63 to their Base64 value
        /// </summary>
        /// <param name="value">A value in the range of 0-63</param>
        /// <returns>A Base64 digit</returns>
        public static char ToBase64(int value)
        {
            if (value > 63 && value < 0)
                throw new Exception("value out of range:" + value.ToString());
            return Base64Map[value];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c">A base64 digit</param>
        /// <returns>A value in the range of 0-63</returns>
        public static int FromBase64(char c)
        {
            int result = Base64DecodeMap[c];
            if (result == -1)
                throw new Exception("invalid char");
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value">an integer to Base64 encode</param>
        /// <returns>the six digit long base64 encoded value of the integer</returns>
        public static string Base64EncodeInt(int value)
        {
            char[] c = new char[6];
            for (int i = 0; i < 5; ++i)
            {
                c[i] = Base64.ToBase64((value >> (26 - i * 6)) & 0x3f);
            }
            c[5] = Base64.ToBase64((value << 4) & 0x3f);
            return new string(c);
        }
    }
}
