using System.Runtime.InteropServices;

namespace Decompiler
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MoHAALumpsList
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public MoHAALump[] LumpList;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MoHAALump
    {
        public int Offset;
        public int Length;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MoHAAMaterial
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] Name;

        public int SurfaceFlags;
        public int ContentFlags;
        public uint Subdivisions;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public char[] FenceMaskImage;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MoHAAVertex
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Position;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] UV;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] Lightmap;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Normal;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Color;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MoHAASurface
    {
        public int MaterialID;
        public int fogNum;
        public int surfaceType;
        public int firstVert;
        public int numVerts;
        public int firstIndex;
        public int numIndexes;
        public int lightmapNum;
        public int lightmapX, lightmapY;
        public int lightmapWidth, lightmapHeight;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] lightmapOrigin;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public float[] lightmapVecs;

        public int patchWidth;
        public int patchHeight;
        public float subdivisions;
    }
}