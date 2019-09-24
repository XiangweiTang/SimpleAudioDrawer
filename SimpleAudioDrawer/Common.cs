using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.ComponentModel.Design.Serialization;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;

namespace SimpleAudioDrawer
{
    static class Common
    {
        public static byte[] ReadBytes(string path, int n)
        {
            Sanity.Requires(File.Exists(path), "The file doesn't exist.");
            Sanity.Requires(n >= 0, "The bytes required cannot be negative.");
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    return br.ReadBytes(n);
                }
            }
        }
        public static T[] ArraySkip<T>(this T[] inputArray, int skip)
        {
            int inputLength = inputArray.Length;
            if (skip < 0)
                skip = skip % inputLength + inputLength;
            skip = Math.Min(inputLength, skip);
            int outputLength = inputLength - skip;
            T[] outputArray = new T[outputLength];
            Array.Copy(inputArray, skip, outputArray, 0, outputLength);
            return outputArray;
        }
        public static string ToStringContext(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd HH:mm:ss");
        }

        public static int ToInt24(byte b0, byte b1, byte b2)
        {
            int int32 = b0 | (b1 << 8) | (b2 << 16);
            return int32 >Constants. INT24_MAX
                ? int32 -Constants. UINT24_MAX
                : int32;
        }

        public static int ToInt24(byte[] bytes, int offset)
        {
            Sanity.Requires(offset + 3 <= bytes.Length, "Offset exceeds array length.");
            return ToInt24(bytes[offset], bytes[offset + 1], bytes[offset + 2]);
        }
    }
}
