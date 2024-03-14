using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViridiX.Linguist;

namespace XboxImageGrabber
{
    public enum PageTableUseType : byte
    {
        Free = 0,
        Allocated,
        Kernel,
        NV2AInstanceMemory,
        PageTableEntries,
        
        BusyType_Stack,
        BusyType_VirtualPTE,
        BusyType_SystemPTE,
        BusyType_Pool,
        BusyType_VirtualMemory,
        BusyType_SystemMemory,
        BusyType_Image,
        BusyType_FsCache,
        BusyType_Contiguous,
        BusyType_Debugger,

        HaloRuntimeData,

        HaloD3DBackBuffer,
        HaloD3DFrontBuffer,
        HaloD3DDepthBuffer,
        HaloTagData,
        HaloRasterizerBuffer,
        HaloTextureCache,
        HaloLowTextureCache,
        HaloGeometryCache,
        HaloSoundCache,
        HaloAnimationCache,
        HaloNetSimMiscCache,
    }

    public class PageTableMap
    {
        const int PAGE_SHIFT = 12;

        const uint MM_DATABASE_PHYSICAL_PAGE = 0x03FF0;

        const int PTES_PER_64MB = (64 * 1024 * 1024) / 4096;

        const int MM_BYTES_IN_PHYSICAL_MAP = 256 * 1024 * 1024;

        static uint MI_CONVERT_PFN_TO_PHYSICAL(uint pfn)
        {
            return 0x80000000 | (pfn << PAGE_SHIFT);
        }

        static uint MI_CONVERT_PHYSICAL_TO_PFN(uint address)
        {
            return (address & (MM_BYTES_IN_PHYSICAL_MAP - 1)) >> PAGE_SHIFT;
        }

        /// <summary>
        /// Indicates if the page table map is 64MB or 128MB
        /// </summary>
        public bool Is128MB { get; private set; }

        private PageTableUseType[] pageTableStatusArray;

        public PageTableMap(bool is128Mb)
        {
            // Initialize fields.
            this.Is128MB = is128Mb;

            // Allocate an array to mark pages in use.
            this.pageTableStatusArray = new PageTableUseType[this.Is128MB == true ? 2 * PTES_PER_64MB : PTES_PER_64MB];
        }

        public void ReadFromConsole(Xbox xbox)
        {
            // Loop and read page table entries. The xbox kernel does some additional math to make sure the page table entry is aligned on a certain
            // interval, but the PTE structure is just a DWORD so I think we're fine to ignore that.
            int ptesPerPage = 4096 / 4;
            int ptePageCount = pageTableStatusArray.Length / ptesPerPage;
            for (int i = 0; i < ptePageCount; i++)
            {
                // Read the next page of PTEs.
                uint pteAddress = MI_CONVERT_PFN_TO_PHYSICAL(MM_DATABASE_PHYSICAL_PAGE + (uint)i);
                byte[] pteData = xbox.Memory.ReadBytes(pteAddress, 4096);

                // Loop and update PTE array status.
                for (int x = 0; x < ptesPerPage; x++)
                {
                    // Check if the PTE is marked as valid.
                    uint pteValue = BitConverter.ToUInt32(pteData, x * 4);
                    if ((pteValue & 1) != 0)
                    {
                        // Physical allocation, mark as allocated.
                        pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.Allocated;
                    }
                    else if ((pteValue & 0x10000) != 0)
                    {
                        // Busy bit set, check for busy type.
                        int busyType = (int)(pteValue >> 28) & 0xF;
                        switch (busyType)
                        {
                            case 1: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_Stack; break;
                            case 2: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_VirtualPTE; break;
                            case 3: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_SystemPTE; break;
                            case 4: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_Pool; break;
                            case 5: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_VirtualMemory; break;
                            case 6: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_SystemMemory; break;
                            case 7: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_Image; break;
                            case 8: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_FsCache; break;
                            case 9: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_Contiguous; break;
                            case 10: pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.BusyType_Debugger; break;
                            default:
                                {
                                    throw new Exception("Unsupported busy type");
                                }
                        }
                    }
                    else
                    {
                        // Page is free.
                        pageTableStatusArray[(i * ptesPerPage) + x] = PageTableUseType.Free;
                    }
                }
            }

            // Mark various reserved memory ranges.
            MarkPTERangeAsType(0x80000000, 0x80060000, PageTableUseType.Kernel);
            if (this.Is128MB == true)
            {
                // 128MB ram configuration:
                MarkPTERangeAsType2(MI_CONVERT_PFN_TO_PHYSICAL(MM_DATABASE_PHYSICAL_PAGE), 32, PageTableUseType.PageTableEntries, true);
                MarkPTERangeAsType2(MI_CONVERT_PFN_TO_PHYSICAL(0x07FF0), 16, PageTableUseType.NV2AInstanceMemory, true);
            }
            else
            {
                // 64MB ram configuration:
                MarkPTERangeAsType2(MI_CONVERT_PFN_TO_PHYSICAL(MM_DATABASE_PHYSICAL_PAGE), 16, PageTableUseType.PageTableEntries, true);
                MarkPTERangeAsType2(MI_CONVERT_PFN_TO_PHYSICAL(0x03FE0), 16, PageTableUseType.NV2AInstanceMemory, true);
            }
        }

        public void MarkPTERangeAsType(uint startAddress, uint endAddress, PageTableUseType type, bool forceAllocate = false)
        {
            // Calculate the number of pages in the range.
            int pageCount = (int)(endAddress - startAddress) / 4096;
            if ((endAddress % 4096) != 0)
                pageCount += 1;

            MarkPTERangeAsType2(startAddress, pageCount, type, forceAllocate);
        }

        public void MarkPTERangeAsType2(uint startAddress, int pageCount, PageTableUseType type, bool forceAllocate = false)
        {
            Debug.WriteLine($"Marking 0x{startAddress:X8} for 0x{(pageCount * 4096):X} bytes as {type}");

            int pfn = (int)MI_CONVERT_PHYSICAL_TO_PFN(startAddress);
            for (int i = 0; i < pageCount; i++)
            {
                // Only mark the page as type if it's allocated.
                if (forceAllocate == true || this.pageTableStatusArray[pfn + i] != PageTableUseType.Free)
                    this.pageTableStatusArray[pfn + i] = type;
            }
        }

        public Bitmap CreatePageTableBitmap(Dictionary<PageTableUseType, Color> colorDictionary)
        {
            // Create a new bitmap to hold the memory pixel buffer.
            int pixelScale = 4;
            int width = 128;
            int height = this.Is128MB == true ? 256 : 128;
            Bitmap memoryMapBitmap = new Bitmap(width * pixelScale, height * pixelScale, PixelFormat.Format32bppArgb);

            // Lock the bitmap pixel buffer.
            BitmapData bitmapData = memoryMapBitmap.LockBits(new Rectangle(0, 0, width * pixelScale, height * pixelScale), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* pPixelData = (byte*)bitmapData.Scan0.ToPointer();

                void SetPixelColor(int x, int y, Color primaryColor, Color secondaryColor)
                {
                    x *= pixelScale;
                    y *= pixelScale;

                    for (int yrun = 0; yrun < pixelScale; yrun++)
                    {
                        for (int xrun = 0; xrun < pixelScale; xrun++)
                        {
                            pPixelData[((y + yrun) * bitmapData.Stride) + ((x + xrun) * 4) + 0] = yrun >= (pixelScale / 2) ? secondaryColor.B : primaryColor.B;
                            pPixelData[((y + yrun) * bitmapData.Stride) + ((x + xrun) * 4) + 1] = yrun >= (pixelScale / 2) ? secondaryColor.G : primaryColor.G;
                            pPixelData[((y + yrun) * bitmapData.Stride) + ((x + xrun) * 4) + 2] = yrun >= (pixelScale / 2) ? secondaryColor.R : primaryColor.R;
                            pPixelData[((y + yrun) * bitmapData.Stride) + ((x + xrun) * 4) + 3] = yrun >= (pixelScale / 2) ? secondaryColor.A : primaryColor.A;
                        }
                    }
                }

                // Loop and fill in pixel colors.
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Get the page table entry and check the type is valid for this bitmap.
                        PageTableUseType type = pageTableStatusArray[(y * width) + x];
                        if (colorDictionary.ContainsKey(type) == true)
                        {
                            // Use the color for this type.
                            SetPixelColor(x, y, colorDictionary[type], colorDictionary[type]);
                        }
                        else
                        {
                            // Treat the page as free.
                            SetPixelColor(x, y, Color.Gray, Color.Gray);
                        }
                    }
                }

                //int ptesPerChip = PTES_PER_64MB / 4;

                //// Mark where each RAM chip starts.
                //for (int i = 1; i < 8; i++)
                //{
                //    int pos = (i * ptesPerChip) / 128;
                //    SetPixelColor(0, pos, Color.Black, Color.Black);
                //}
            }
            memoryMapBitmap.UnlockBits(bitmapData);

            return memoryMapBitmap;
        }
    }
}
