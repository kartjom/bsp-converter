using System.Runtime.InteropServices;

namespace Decompiler
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Header
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Signature;
        public int Version;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CoDLump
    {
        public int Length;
        public int Offset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CoDLumpsList
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 33)]
        public CoDLump[] LumpList;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CoDMaterial
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] Name;

        public uint Flags;

        public uint ContentFlags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CoD1Vertex
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Position;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] UV;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] Unknown1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Normal;

        public float Unknown2;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CoD2Vertex
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Position;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Normal;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] RGBa;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] UV;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] ST;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public float[] Unknown;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct TriangleSoups
    {
        public ushort MaterialID;
        public ushort DrawOrder;
        public int VertexOffset;     // Referred to Vertex
        public ushort VertexLength;
        public ushort TriangleLength;
        public int TriangleOffset;   // Referred to MeshVerts
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MeshVerts
    {
        public ushort Offset;
    };
}
