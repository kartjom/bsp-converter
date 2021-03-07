using System.Runtime.InteropServices;

namespace IWI_DDS_Converter
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct IWIHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public char[] Signature;

        public byte Version;
        public byte DXT;
        public byte Alpha;
        public short Width;
        public short Height;
        public short Unknown;

        public uint FileSize;
        public uint TextureOffset;
        public uint MipMap1Offset;
        public uint MipMap2Offset;
    };
}
