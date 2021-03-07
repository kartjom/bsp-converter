using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;

namespace BinaryFileReader
{
    public static class BinLib
    {
        public static void SetStreamOffset(BinaryReader stream, long offset)
        {
            stream.BaseStream.Position = offset;
        }

        public static long GetStreamOffset(BinaryReader stream)
        {
            return stream.BaseStream.Position;
        }

        public static T ReadBinary<T>(BinaryReader stream) where T : struct
        {
            unsafe
            {
                int size = Marshal.SizeOf<T>();
                byte[] data = stream.ReadBytes(size);

                fixed (byte* ptr = &data[0])
                {
                    return (T)Marshal.PtrToStructure(new IntPtr(ptr), typeof(T));
                }
            }
        }

        public static T ReadFromArray<T>(byte[] array) where T : struct
        {
            using (MemoryStream memoryStream = new MemoryStream(array))
            {
                using (BinaryReader stream = new BinaryReader(memoryStream))
                {
                    T obj = ReadBinary<T>(stream);

                    return obj;
                }
            }
        }

        public static T[] ReadMultiple<T>(BinaryReader stream, int count) where T : struct
        {
            T[] objects = new T[count];

            for (int i = 0; i < count; i++)
            {
                T obj = ReadBinary<T>(stream);
                objects[i] = obj;
            }

            return objects;
        }

        public static T OpenReadCloseBinary<T>(string filePath, long offset = 0) where T : struct
        {
            using ( BinaryReader stream = new BinaryReader(File.Open(filePath, FileMode.Open)) )
            {
                SetStreamOffset(stream, offset);
                return ReadBinary<T>(stream);
            }
        }

        public static string BytesToString(byte[] str)
        {
            return Encoding.ASCII.GetString(str);
        }

        public static int SizeOf<T>()
        {
            return Marshal.SizeOf<T>();
        }
    }
}
