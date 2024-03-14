using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViridiX.Linguist;

namespace XboxImageGrabber
{
    public enum D3DFORMAT : int
    {
        /* Swizzled formats */

        D3DFMT_A8R8G8B8             = 0x00000006,
        D3DFMT_X8R8G8B8             = 0x00000007,
        D3DFMT_R5G6B5               = 0x00000005,
        D3DFMT_R6G5B5               = 0x00000027,
        D3DFMT_X1R5G5B5             = 0x00000003,
        D3DFMT_A1R5G5B5             = 0x00000002,
        D3DFMT_A4R4G4B4             = 0x00000004,
        D3DFMT_A8                   = 0x00000019,
        D3DFMT_A8B8G8R8             = 0x0000003A,
        D3DFMT_B8G8R8A8             = 0x0000003B,
        D3DFMT_R4G4B4A4             = 0x00000039,
        D3DFMT_R5G5B5A1             = 0x00000038,
        D3DFMT_R8G8B8A8             = 0x0000003C,
        D3DFMT_R8B8                 = 0x00000029,
        D3DFMT_G8B8                 = 0x00000028,

        D3DFMT_P8                   = 0x0000000B,

        D3DFMT_L8                   = 0x00000000,
        D3DFMT_A8L8                 = 0x0000001A,
        D3DFMT_AL8                  = 0x00000001,
        D3DFMT_L16                  = 0x00000032,

        D3DFMT_V8U8                 = 0x00000028,
        D3DFMT_L6V5U5               = 0x00000027,
        D3DFMT_X8L8V8U8             = 0x00000007,
        D3DFMT_Q8W8V8U8             = 0x0000003A,
        D3DFMT_V16U16               = 0x00000033,

        D3DFMT_D16_LOCKABLE         = 0x0000002C,
        D3DFMT_D16                  = 0x0000002C,
        D3DFMT_D24S8                = 0x0000002A,
        D3DFMT_F16                  = 0x0000002D,
        D3DFMT_F24S8                = 0x0000002B,

        /* YUV formats */

        D3DFMT_YUY2                 = 0x00000024,
        D3DFMT_UYVY                 = 0x00000025,

        /* Compressed formats */

        D3DFMT_DXT1                 = 0x0000000C,
        D3DFMT_DXT2                 = 0x0000000E,
        D3DFMT_DXT3                 = 0x0000000E,
        D3DFMT_DXT4                 = 0x0000000F,
        D3DFMT_DXT5                 = 0x0000000F,

        /* Linear formats */

        D3DFMT_LIN_A1R5G5B5         = 0x00000010,
        D3DFMT_LIN_A4R4G4B4         = 0x0000001D,
        D3DFMT_LIN_A8               = 0x0000001F,
        D3DFMT_LIN_A8B8G8R8         = 0x0000003F,
        D3DFMT_LIN_A8R8G8B8         = 0x00000012,
        D3DFMT_LIN_B8G8R8A8         = 0x00000040,
        D3DFMT_LIN_G8B8             = 0x00000017,
        D3DFMT_LIN_R4G4B4A4         = 0x0000003E,
        D3DFMT_LIN_R5G5B5A1         = 0x0000003D,
        D3DFMT_LIN_R5G6B5           = 0x00000011,
        D3DFMT_LIN_R6G5B5           = 0x00000037,
        D3DFMT_LIN_R8B8             = 0x00000016,
        D3DFMT_LIN_R8G8B8A8         = 0x00000041,
        D3DFMT_LIN_X1R5G5B5         = 0x0000001C,
        D3DFMT_LIN_X8R8G8B8         = 0x0000001E,

        D3DFMT_LIN_A8L8             = 0x00000020,
        D3DFMT_LIN_AL8              = 0x0000001B,
        D3DFMT_LIN_L16              = 0x00000035,
        D3DFMT_LIN_L8               = 0x00000013,

        D3DFMT_LIN_V16U16           = 0x00000036,
        D3DFMT_LIN_V8U8             = 0x00000017,
        D3DFMT_LIN_L6V5U5           = 0x00000037,
        D3DFMT_LIN_X8L8V8U8         = 0x0000001E,
        D3DFMT_LIN_Q8W8V8U8         = 0x00000012,

        D3DFMT_LIN_D24S8            = 0x0000002E,
        D3DFMT_LIN_F24S8            = 0x0000002F,
        D3DFMT_LIN_D16              = 0x00000030,
        D3DFMT_LIN_F16              = 0x00000031,

        D3DFMT_VERTEXDATA           = 100,
        D3DFMT_INDEX16              = 101,
    }

    public class D3DResource
    {
        public const int kSizeOf = 12;

        /// <summary>
        /// Refcount and flags
        /// </summary>
        public int Common;
        /// <summary>
        /// GPU address of resource data (mask with 0x80000000 to get CPU accessible address)
        /// </summary>
        public uint Data;
        /// <summary>
        /// 
        /// </summary>
        public int Lock;

        public uint GetCPUAddress()
        {
            return this.Data | 0x80000000;
        }
    }

    public class D3DPixelContainer : D3DResource
    {
        /// <summary>
        /// Encoded format information (contains size info for power-of-2 textures)
        /// </summary>
        public uint Format;
        /// <summary>
        /// Size info for non power-of-2 textures, 0 otherwise
        /// </summary>
        public int Size;

        /// <summary>
        /// Gets the D3DFORMAT of the texture pixel data
        /// </summary>
        public D3DFORMAT TextureFormat { get { return (D3DFORMAT)((this.Format >> 8) & 0xFF); } }
        /// <summary>
        /// Gets the width of the texture
        /// </summary>
        public int Width { get { return GetWidth(); } }
        /// <summary>
        /// Gets the height of the texture
        /// </summary>
        public int Height { get { return GetHeight(); } }
        /// <summary>
        /// Gets the texture depth
        /// </summary>
        public int Depth { get { return GetDepth(); } }
        /// <summary>
        /// Gets the texture pitch in bytes
        /// </summary>
        public int Pitch { get { return GetPitch(); } }

        protected int GetWidth()
        {
            // Check if this is a non power-of-2 texture.
            if (this.Size != 0)
            {
                return (this.Size & 0xFFF) + 1;
            }
            else
            {
                return 1 << ((int)(this.Format >> 20) & 0xF);
            }
        }

        protected int GetHeight()
        {
            // Check if this is a non power-of-2 texture.
            if (this.Size != 0)
            {
                return ((this.Size >> 12) & 0xFFF) + 1;
            }
            else
            {
                return 1 << ((int)(this.Format >> 24) & 0xF);
            }
        }

        protected int GetDepth()
        {
            // Check if this is a non power-of-2 texture.
            if (this.Size != 0)
            {
                return 1;
            }
            else
            {
                return 1 << ((int)(this.Format >> 28) & 0xF);
            }
        }

        protected int GetPitch()
        {
            // Check if this is a non power-of-2 texture.
            if (this.Size != 0)
            {
                return (((int)(this.Size >> 24) & 0xFF) + 1) * 64;
            }
            else
            {
                // Check the format and handle accordingly.
                switch (this.TextureFormat)
                {
                    case D3DFORMAT.D3DFMT_DXT1: return GetWidth() * 2;
                    case D3DFORMAT.D3DFMT_DXT2:
                    case D3DFORMAT.D3DFMT_DXT4: return GetWidth() * 4;
                    default:
                        {
                            return GetWidth() * GetBitsPerPixel(this.TextureFormat) / 8;
                        }
                }
            }
        }

        public int GetSize()
        {
            return GetPitch() * GetHeight();
        }

        public int GetTiledPitch()
        {
            int[] TilePitches = new int[]
            {
                0x200,
                0x300,
                0x400,
                0x500,
                0x600,
                0x700,
                0x800,
                0xA00,
                0xC00,
                0xE00,
                0x1000,
                0x1400,
                0x1800,
                0x1C00,
                0x2000,
                0x2800,
                0x3000,
                0x3800,
                0x4000,
                0x5000,
                0x6000,
                0x7000,
                0x8000,
                0xA000,
                0xC000,
                0xE000
            };

            int bpp = GetBitsPerPixel(this.TextureFormat);
            int pitch = (this.Width * bpp / 8 + 64 - 1) & ~(64 - 1);

            for (int i = 0; i < TilePitches.Length; i++)
            {
                if (pitch <= TilePitches[i])
                {
                    pitch = TilePitches[i];
                    break;
                }
            }

            return pitch;
        }

        public static int GetBitsPerPixel(D3DFORMAT format)
        {
            // Check the format and handle accordingly.
            switch (format)
            {
                case D3DFORMAT.D3DFMT_DXT1:
                    {
                        return 4;
                    }
                case D3DFORMAT.D3DFMT_L8:
                case D3DFORMAT.D3DFMT_AL8:
                case D3DFORMAT.D3DFMT_P8:
                case D3DFORMAT.D3DFMT_DXT2:
                case D3DFORMAT.D3DFMT_DXT4:
                case D3DFORMAT.D3DFMT_LIN_L8:
                case D3DFORMAT.D3DFMT_A8:
                case D3DFORMAT.D3DFMT_LIN_AL8:
                case D3DFORMAT.D3DFMT_LIN_A8:
                    {
                        return 8;
                    }
                case D3DFORMAT.D3DFMT_A8R8G8B8:
                case D3DFORMAT.D3DFMT_X8R8G8B8:
                case D3DFORMAT.D3DFMT_LIN_A8R8G8B8:
                case D3DFORMAT.D3DFMT_LIN_X8R8G8B8:
                case D3DFORMAT.D3DFMT_D24S8:
                case D3DFORMAT.D3DFMT_F24S8:
                case D3DFORMAT.D3DFMT_LIN_D24S8:
                case D3DFORMAT.D3DFMT_LIN_F24S8:
                case D3DFORMAT.D3DFMT_V16U16:
                case D3DFORMAT.D3DFMT_A8B8G8R8:
                case D3DFORMAT.D3DFMT_B8G8R8A8:
                case D3DFORMAT.D3DFMT_R8G8B8A8:
                case D3DFORMAT.D3DFMT_LIN_A8B8G8R8:
                case D3DFORMAT.D3DFMT_LIN_B8G8R8A8:
                case D3DFORMAT.D3DFMT_LIN_R8G8B8A8:
                    {
                        return 32;
                    }
                default:
                    {
                        return 16;
                    }
            }
        }
    }

    public class D3DTile
    {
        public const int kSizeOf = 24;

        public uint Flags;
        public uint Address;
        public uint Size;
        public uint Pitch;
        public uint ZStartTag;
        public uint ZOffset;
    }

    public class D3DParser
    {
        public const uint d3d_frame_buffer_count_offset = 0x1A10;           // Offset into g_pDevice
        public const uint d3d_frame_buffer_offset = 0x1A14;                 // Offset into g_pDevice
        public const uint d3d_depth_buffer_offset = 0x1A20;                 // Offset into g_pDevice
        public const uint d3d_tile_array_offset = 0x1AC0;                   // Offset into g_pDevice

        public static void ParseAndMapD3DRenderTargets(Xbox xbox, uint d3dDeviceAddress, PageTableMap pageTableMap)
        {
            // Read the frame buffer info from the d3d device structure.
            int frameBufferCount = xbox.Memory.ReadInt32(d3dDeviceAddress + d3d_frame_buffer_count_offset);

            // Map the front and back buffers.
            for (int i = 0; i < frameBufferCount; i++)
            {
                uint textureAddress = (uint)xbox.Memory.ReadUInt32(d3dDeviceAddress + d3d_frame_buffer_offset + (uint)(i * 4));
                ParseAndMapD3dTexture(xbox, d3dDeviceAddress, textureAddress, pageTableMap, i == 1 ? PageTableUseType.HaloD3DFrontBuffer : PageTableUseType.HaloD3DBackBuffer);
            }

            // Map the depth buffer.
            uint depthBufferAddress = (uint)xbox.Memory.ReadUInt32(d3dDeviceAddress + d3d_depth_buffer_offset);
            ParseAndMapD3dTexture(xbox, d3dDeviceAddress, depthBufferAddress, pageTableMap, PageTableUseType.HaloD3DDepthBuffer);
        }

        public static void ParseAndMapD3dTexture(Xbox xbox, uint d3dDeviceAddress, uint address, PageTableMap pageTableMap, PageTableUseType type)
        {
            // Parse the texture from memory.
            D3DPixelContainer texture = ParsePixelContainer(xbox, address);
            if (texture.Data == 0)
                return;

            // If a device address was passed in check if the surface is tiled.
            bool tiled = false;
            if (d3dDeviceAddress != 0)
            {
                // Determine if the texture is tiled by checking all active tiles set.
                for (int i = 0; i < 8; i++)
                {
                    // Get the tile buffer address.
                    uint tileBufferAddress = (uint)xbox.Memory.ReadUInt32(d3dDeviceAddress + d3d_tile_array_offset + (i * 24) + 4);
                    if (texture.GetCPUAddress() == tileBufferAddress)
                    {
                        tiled = true;
                        break;
                    }
                }
            }

            // Calculate the number of pages the texture spans.
            int textureSize = 0;
            if (tiled == false)
                textureSize = texture.GetSize();
            else
                textureSize = texture.GetTiledPitch() * texture.Height;

            // Map the texture in the page table map.
            int pageCount = (textureSize + 4095) / 4096;
            pageTableMap.MarkPTERangeAsType2(texture.GetCPUAddress(), pageCount, type);
        }

        public static D3DPixelContainer ParsePixelContainer(Xbox xbox, uint address)
        {
            D3DPixelContainer texture = new D3DPixelContainer();
            texture.Common = xbox.Memory.ReadInt32(address);
            texture.Data = (uint)xbox.Memory.ReadUInt32(address + 4);
            texture.Lock = xbox.Memory.ReadInt32(address + 8);
            texture.Format = (uint)xbox.Memory.ReadUInt32(address + 12);
            texture.Size = xbox.Memory.ReadInt32(address + 16);

            return texture;
        }

        public static D3DTile ParseD3DTile(Xbox xbox, uint address)
        {
            D3DTile tile = new D3DTile();
            tile.Flags = (uint)xbox.Memory.ReadUInt32(address);
            tile.Address = (uint)xbox.Memory.ReadUInt32(address + 4);
            tile.Size = (uint)xbox.Memory.ReadUInt32(address + 8);
            tile.Pitch = (uint)xbox.Memory.ReadUInt32(address + 12);
            tile.ZStartTag = (uint)xbox.Memory.ReadUInt32(address + 16);
            tile.ZOffset = (uint)xbox.Memory.ReadUInt32(address + 20);

            return tile;
        }

        public static int GetField(int value, int upper, int lower)
        {
            return ((value >> lower) & ((2 << (upper - lower)) - 1));
        }

        public static int TiledAddressToLinear(int pitch, int offset, bool isDepthBuffer)
        {
            int scan = offset / pitch;
            int column = offset % pitch;
            int width_in_pages = pitch / 256;
            int page_row = scan / 16;
            int page = (page_row * width_in_pages) + (column / 256);

            // Handle special pitch sizes of 0x300, 0x500, and 0x700.
            int odd_number_page_slots = (pitch / 256) & 1;

            if (odd_number_page_slots != 0)
            {
                page ^= (isDepthBuffer == true ? 1 : 0);
            }
            else
            {
                int odd_page_row = page_row & 1;

                if ((isDepthBuffer == false && odd_page_row != 0) || (isDepthBuffer == true && odd_page_row == 0))
                {
                    page ^= 1;
                }
            }

            int scan_within_page = (offset / pitch) % 16;
            int column_within_page = offset % 256;

            int scan23 = (GetField(scan_within_page, 2, 2) << 1) + GetField(scan_within_page, 3, 3);

            int linear_offset_within_page = (GetField(scan_within_page, 3, 2) << 10) | (GetField(column_within_page, 7, 6) << 8) | (GetField(scan_within_page, 1, 0) << 6) | (((GetField(column_within_page, 5, 4) + scan23) & 0x3) << 4) | GetField(column_within_page, 3, 0);

            int linear_address_of_page_start = page * 0x1000;

            int linear_memory_address = linear_address_of_page_start + linear_offset_within_page;

            return linear_memory_address;
        }

        public static int LinearAddressToTiled(int pitch, int offset, int tiled_base_address, bool isDepthBuffer)
        {
            // Page relative to the start of the tiled region.
            int page = offset / 0x1000;

            int width_in_pages = pitch / 256;
            int page_row = page / width_in_pages;

            // Handle special pitch sizes of 0x300, 0x500, 0x700.
            int odd_number_page_slots = (pitch / 256) & 1;

            if (odd_number_page_slots != 0)
            {
                page ^= (isDepthBuffer == true ? 1 : 0);
            }
            else
            {
                int odd_page_row = page_row & 1;

                if ((isDepthBuffer == false && odd_page_row != 0) || (isDepthBuffer == true && odd_page_row == 0))
                {
                    page ^= 1;
                }
            }

            int column = (page % width_in_pages) * 256;
            int tiled_offset_of_page_start = (page_row * pitch * 16) + column;

            int scan_within_page = (GetField(offset, 11, 10) << 2) | GetField(offset, 7, 6);

            int linear1011 = (GetField(offset, 10, 10) << 1) + GetField(offset, 11, 11);

            int column_within_page = (GetField(offset, 9, 8) << 6) | (((GetField(offset, 5, 4) - linear1011) & 3) << 4) | GetField(offset, 3, 0);

            int tiled_memory_address = tiled_base_address + tiled_offset_of_page_start + (scan_within_page * pitch);

            return tiled_memory_address;
        }
    }
}
