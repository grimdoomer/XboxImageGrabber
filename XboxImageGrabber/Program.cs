using ViridiX.Linguist.Network;
using ViridiX.Linguist;
using System.Diagnostics;
using ViridiX.Mason.Utilities;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace XboxImageGrabber
{
    internal class Program
    {
        static PageTableUseType[] AllPageTableTypeArray = new PageTableUseType[]
        {
            PageTableUseType.Free,
            PageTableUseType.Allocated,
            PageTableUseType.Kernel,
            PageTableUseType.NV2AInstanceMemory,
            PageTableUseType.PageTableEntries,

            PageTableUseType.BusyType_Stack,
            PageTableUseType.BusyType_VirtualPTE,
            PageTableUseType.BusyType_SystemPTE,
            PageTableUseType.BusyType_Pool,
            PageTableUseType.BusyType_VirtualMemory,
            PageTableUseType.BusyType_SystemMemory,
            PageTableUseType.BusyType_Image,
            PageTableUseType.BusyType_FsCache,
            PageTableUseType.BusyType_Contiguous,
            PageTableUseType.BusyType_Debugger,

            PageTableUseType.HaloRuntimeData,

            PageTableUseType.HaloD3DBackBuffer,
            PageTableUseType.HaloRasterizerBuffer
        };

        static Dictionary<PageTableUseType, Color> SystemPageTableColorDictionary = new Dictionary<PageTableUseType, Color>()
        {
            { PageTableUseType.Kernel, Color.Gold },
            { PageTableUseType.NV2AInstanceMemory, Color.CornflowerBlue },
            { PageTableUseType.PageTableEntries, Color.Purple },
            { PageTableUseType.Allocated, Color.FromArgb(127, 255, 142) },

            { PageTableUseType.BusyType_Stack, Color.Cyan },
            { PageTableUseType.BusyType_VirtualPTE, Color.DarkOrange },
            { PageTableUseType.BusyType_SystemPTE, Color.DarkBlue },
            { PageTableUseType.BusyType_Pool, Color.Red },
            { PageTableUseType.BusyType_VirtualMemory, Color.Orange },
            { PageTableUseType.BusyType_SystemMemory, Color.Blue },
            { PageTableUseType.BusyType_Image, Color.Green },
            { PageTableUseType.BusyType_FsCache, Color.Brown },
            { PageTableUseType.BusyType_Contiguous, Color.FromArgb(178, 0, 255) },
            { PageTableUseType.BusyType_Debugger, Color.HotPink },
        };

        static Dictionary<PageTableUseType, Color> HaloPageTableColorDictionary = new Dictionary<PageTableUseType, Color>()
        {
            { PageTableUseType.HaloRuntimeData, Color.Navy },
            { PageTableUseType.HaloD3DBackBuffer, Color.Black },
            { PageTableUseType.HaloD3DFrontBuffer, Color.White },
            { PageTableUseType.HaloD3DDepthBuffer, Color.Cyan },
            { PageTableUseType.HaloTagData, Color.Gold },
            { PageTableUseType.HaloRasterizerBuffer, Color.Yellow },
            { PageTableUseType.HaloTextureCache, Color.LightGreen },
            { PageTableUseType.HaloLowTextureCache, Color.DarkGreen },
            { PageTableUseType.HaloGeometryCache, Color.Red },
            { PageTableUseType.HaloSoundCache, Color.Purple },
            { PageTableUseType.HaloAnimationCache, Color.HotPink },
            { PageTableUseType.HaloNetSimMiscCache, Color.Coral },
        };

        static readonly Rectangle SystemMemoryBlitBounds = new Rectangle(394, 292, 512, 1024);
        static readonly Rectangle HaloMemoryBlitBounds = new Rectangle(1000, 292, 512, 1024);
        static readonly Rectangle UsedFreeBlitBounds = new Rectangle(1605, 292, 512, 1024);

        static void BlitBitmap(Bitmap source, Rectangle sourceRect, Bitmap target, Rectangle targetRect)
        {
            // Lock both bitmaps to their respective areas.
            BitmapData srcData = source.LockBits(sourceRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = target.LockBits(targetRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* pSrc = (byte*)srcData.Scan0.ToPointer();
                byte* pDst = (byte*)dstData.Scan0.ToPointer();

                for (int y = 0; y < sourceRect.Height; y++)
                {
                    for (int x = 0; x < sourceRect.Width; x++)
                    {
                        pDst[(y * dstData.Stride) + (x * 4) + 0] = pSrc[(y * srcData.Stride) + (x * 4) + 0];
                        pDst[(y * dstData.Stride) + (x * 4) + 1] = pSrc[(y * srcData.Stride) + (x * 4) + 1];
                        pDst[(y * dstData.Stride) + (x * 4) + 2] = pSrc[(y * srcData.Stride) + (x * 4) + 2];
                        pDst[(y * dstData.Stride) + (x * 4) + 3] = pSrc[(y * srcData.Stride) + (x * 4) + 3];
                    }
                }
            }

            target.UnlockBits(dstData);
            source.UnlockBits(srcData);
        }

        static void GetPhysicalMemoryImage(Xbox xbox, bool is128mb, bool readHaloData)
        {
            // Pause execution.
            xbox.DebugMonitor.DmStop();

            // Read page table data from memory.
            PageTableMap pageTableMap = new PageTableMap(is128mb);
            pageTableMap.ReadFromConsole(xbox);

            // Check if we should read halo runtime data.
            if (readHaloData == true)
            {
                HaloParser.ParseHaloData(xbox, pageTableMap);
            }

            // Resume execution on the console.
            xbox.DebugMonitor.DmGo();

            // Create a new bitmap from the template image.
            Bitmap memoryBitmap = (Bitmap)XboxImageGrabber.Properties.Resources.system_memory_template.Clone();

            // Create a bitmap from the system page table data and blit it to the final image.
            Bitmap systemMemBitmap = pageTableMap.CreatePageTableBitmap(SystemPageTableColorDictionary);
            BlitBitmap(systemMemBitmap, new Rectangle(0, 0, systemMemBitmap.Width, systemMemBitmap.Height), memoryBitmap, SystemMemoryBlitBounds);

            // Check if we need to blit the halo memory map.
            if (readHaloData == true)
            {
                // Create a bitmap from the halo page table data and blit it to the final image.
                Bitmap haloMemBitmap = pageTableMap.CreatePageTableBitmap(HaloPageTableColorDictionary);
                BlitBitmap(haloMemBitmap, new Rectangle(0, 0, haloMemBitmap.Width, haloMemBitmap.Height), memoryBitmap, HaloMemoryBlitBounds);
            }

            // Create a bitmap for used/free pages and blit it to the final image.
            Dictionary<PageTableUseType, Color> usedFreeColorDictionary = Enum.GetValues<PageTableUseType>().ToDictionary(k => k, v => v == PageTableUseType.Free ? Color.Gray : Color.FromArgb(127, 255, 142));
            Bitmap usedFreeBitmap = pageTableMap.CreatePageTableBitmap(usedFreeColorDictionary);
            BlitBitmap(usedFreeBitmap, new Rectangle(0, 0, usedFreeBitmap.Width, usedFreeBitmap.Height), memoryBitmap, UsedFreeBlitBounds);

            // Save the image to file.
            string tmpFilePath = Path.GetTempFileName();
            string imageFilePath = tmpFilePath.Replace(".tmp", ".png");
            File.Delete(tmpFilePath);

            memoryBitmap.Save(imageFilePath, ImageFormat.Png);

            // Shell execute the image so it opens.
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = imageFilePath;
            psi.UseShellExecute = true;
            psi.WindowStyle = ProcessWindowStyle.Normal;
            Process.Start(psi);
        }

        static void DumpHaloBackBuffer(Xbox xbox)
        {
            // Create an event wait handle fo synchronizing the snapshot thread with the callback threads.
            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            // Get the address mappings for the running halo xbe.
            HaloAddressMappings addressMappings = HaloParser.ReadAddressMappingFile(xbox);
            if (addressMappings == null)
                return;

            // Add a listener for debug notifications so we can handle breakpoints accordingly.
            xbox.NotificationReceived += new EventHandler<XboxNotificationEventArgs>(delegate (object sender, XboxNotificationEventArgs e)
            {
                // Print the notification for debugging purposes.
                Debug.WriteLine($"Recevied notification: {e.Message}");

                // Check if this is a breakpoint notification.
                if (e.Message.StartsWith("break", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Tokenize the response message.
                    Dictionary<string, string> tokens = Tokenizer.TokenizeResponse(e.Message);

                    // Get the breakpoint address and thread id.
                    uint address = BitUtilities.ParseUintHexString(tokens["addr"]);
                    int threadId = int.Parse(tokens["thread"]);

                    Xbox _xbox = (Xbox)sender;
                    _xbox.DebugMonitor.DmStop();

                    // Get the d3d device pointer.
                    uint d3dDevicePtr = (uint)_xbox.Memory.ReadUInt32(addressMappings.d3d_g_pDevice);

                    // The breakpoint is setup such that we use the (now) old front buffer that was swapped out. If we try to use either of the
                    // back buffer surfaces the screenshot might have "tearing" scanlines on it...
                    uint frontBufferSurfacePtr = (uint)_xbox.Memory.ReadUInt32(d3dDevicePtr + D3DParser.d3d_frame_buffer_offset);

                    // Get the back buffer info.
                    D3DPixelContainer frontBufferInfo = D3DParser.ParsePixelContainer(_xbox, frontBufferSurfacePtr);
                    uint frontBufferAddress = 0x80000000 | frontBufferInfo.Data;
                    int pitch = frontBufferInfo.GetTiledPitch();

                    // Allocate a buffer to hold decoded pixel data.
                    byte[] pixelData = new byte[frontBufferInfo.Width * frontBufferInfo.Height * 4];

                    // Check the surface width to determine if it's swizzled or not.
                    if (frontBufferInfo.Width != 1920)
                    {
                        // Find a tile that covers the range of memory used by the surface.
                        D3DTile tile = null;
                        for (int i = 0; i < 8; i++)
                        {
                            // Deserialize the next tile structure.
                            D3DTile currentTile = D3DParser.ParseD3DTile(_xbox, (uint)(d3dDevicePtr + D3DParser.d3d_tile_array_offset + (i * D3DTile.kSizeOf)));
                            if (currentTile.Address <= frontBufferAddress && currentTile.Address + currentTile.Size >= frontBufferAddress + (pitch * frontBufferInfo.Height))
                            {
                                // Found the encapsulating tile.
                                tile = currentTile;
                                break;
                            }
                        }

                        // Check if a tile structure was found and handle accordingly.
                        if (tile != null)
                        {
                            // Read the memory from the console in one go.
                            int roundedSize = ((pitch * frontBufferInfo.Height) + 4095) & ~4095;
                            byte[] tiledPixelData = _xbox.Memory.ReadBytes(0x80000000 | frontBufferInfo.Data, roundedSize);

                            // Read the pixel buffer.
                            for (int y = 0; y < frontBufferInfo.Height; y++)
                            {
                                for (int x = 0; x < frontBufferInfo.Width; x++)
                                {
                                    // Calculate the offsets for the current pixel in the tile image buffer and non-tiled buffer.
                                    int offset = ((y * frontBufferInfo.Width) + x) * 4;
                                    int tiledPtr = (y * pitch) + (x * 4);

                                    // Convert the tiled offset to linear, this lets us copy the pixel data as "un-tiled".
                                    int tiledOffset = D3DParser.TiledAddressToLinear(pitch, tiledPtr, false);

                                    // Copy the current pixel value and ignore alpha channel so the png file doesn't end up semi-transparent
                                    pixelData[offset + 0] = tiledPixelData[tiledOffset + 0];
                                    pixelData[offset + 1] = tiledPixelData[tiledOffset + 1];
                                    pixelData[offset + 2] = tiledPixelData[tiledOffset + 2];
                                    pixelData[offset + 3] = 0xFF;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Detected non-tiled back buffer surface");
                            throw new NotSupportedException();
                        }
                    }
                    else
                    {
                        // 1080p surface is never tiled but does use a tiled scan stride.
                        byte[] rawPixelData = _xbox.Memory.ReadBytes(0x80000000 | (uint)frontBufferInfo.Data, pitch * frontBufferInfo.Height);

                        // Copy the pixel data accounting for tiled scan stride.
                        for (int y = 0; y < frontBufferInfo.Height; y++)
                        {
                            Array.Copy(rawPixelData, y * pitch, pixelData, y * frontBufferInfo.Width * 4, frontBufferInfo.Width * 4);

                            // Ignore alpha channel so the png file doesn't end up semi-transparent.
                            for (int x = 0; x < frontBufferInfo.Width; x++)
                            {
                                pixelData[((y * frontBufferInfo.Width) + x) * 4 + 3] = 0xFF;
                            }
                        }
                    }

                    // Resume exection.
                    _xbox.DebugMonitor.DmRemoveBreakpoint(addressMappings.D3DSwapCall);
                    _xbox.DebugMonitor.DmContinueThread(threadId);
                    _xbox.DebugMonitor.DmGo();

                    // Create a new file to write the image to.
                    Bitmap snapshot = new Bitmap(frontBufferInfo.Width, frontBufferInfo.Height, PixelFormat.Format32bppArgb);

                    // Copy the pixel data to the image.
                    BitmapData bitmapData = snapshot.LockBits(new Rectangle(0, 0, frontBufferInfo.Width, frontBufferInfo.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                    {
                        Marshal.Copy(pixelData, 0, bitmapData.Scan0, pixelData.Length);
                    }
                    snapshot.UnlockBits(bitmapData);

                    // Save the image to file.
                    string filePath = Path.GetTempFileName().Replace(".tmp", ".png");
                    snapshot.Save(filePath, ImageFormat.Png);

                    // Shell execute the image so it opens.
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = filePath;
                    psi.UseShellExecute = true;
                    psi.WindowStyle = ProcessWindowStyle.Normal;
                    Process.Start(psi);

                    waitHandle.Set();
                }
            });

            xbox.DebugMonitor.DmSetBreakpoint(addressMappings.D3DSwapCall);
            waitHandle.WaitOne();
        }

        static void Main(string[] args)
        {
            // Run the discovery protocol to get a list of debug consoles on the network.
            List<XboxConnectionInformation> xboxConsoles = Xbox.Discover(100);
            if (xboxConsoles.Count == 0)
            {
                // No xbox consoles found.
                Console.WriteLine("No xbox consoles found...");
                return;
            }

            // Open a connection to the first debug console found.
            Xbox xbox = new Xbox();
            xbox.Connect(xboxConsoles[0].Ip);

            // Check args to determine what to do.
            if (args.Length > 0 && args[0] == "-memory")
            {
                // Determine if the console has 128MB of RAM or not.
                bool has128RAM = xbox.Memory.Statistics.TotalPages > 0x4000;

                // Determine if halo 2 is running.
                bool haloIsRunning = xbox.Process.Modules.Last().Name.StartsWith("halo2");

                // Create the memory stats bitmap.
                GetPhysicalMemoryImage(xbox, has128RAM, haloIsRunning);
            }
            else
            {
                // Dump the back buffer to a image locally.
                DumpHaloBackBuffer(xbox);
            }

            xbox.Disconnect();
        }
    }
}