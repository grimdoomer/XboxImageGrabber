using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViridiX.Linguist;

namespace XboxImageGrabber
{
    public class HaloAddressMappings
    {
        // DirectX globals:
        public uint d3d_g_pDevice;                                  // Global d3d device pointer
        public uint D3DSwapCall;                                    // Address of the call to IDirect3DDevice8_Swap for presenting the frame

        // Physical memory globals:
        public uint physical_memory_globals_low_stage_address;
        public uint physical_memory_globals_hi_stage_address;

        // Rasterizer:
        public uint _g_rasterizer_render_targets_array;

        // Texture cache:
        public uint xbox_texture_cache_globals_standard_cache;                   // lruv cache
        public uint xbox_texture_cache_globals_standard_cache_base_address;      // Physical memory cache
        public uint xbox_texture_cache_globals_low_detail_cache_base_address;    // Physical memory cache

        // Geometry cache:
        public uint xbox_geometry_cache_globals_cache;                           // lruv cache
        public uint xbox_geometry_cache_globals_cache_base_address;              // Physical memory cache

        // Sound cache:
        public uint xbox_sound_cache_globals_cache;                              // lruv cache
        public uint xbox_sound_cache_globals_base_address;                       // Physical memory cache

        // Animation cache:
        public uint xbox_animation_cache_globals_cache;                          // lruv cache
        public uint xbox_animation_cache_globals_cache_base_address;             // Physical memory cache
        public uint xbox_animation_cache_globals_animation_cache_size_bytes;     // Size of cache in bytes

        // Tag cache:
        public uint g_cache_file_globals_header;                 // Address of the active map header

        // Network/simulation/misc:
        public uint _g_something_count;                          // Number of elements in various networking arrays
        public uint _g_something_count2;                         // Same as above?
        public uint _g_network_channels_array;
        public int _g_network_channels_array_entry_size;
        public uint _g_network_connections;
        public int _g_network_connections_entry_size;
        public uint _g_network_message_queues;
        public int _g_network_message_queues_entry_size;
        public uint _g_simulation_view_data_array;
        public int _g_simulation_view_data_array_entry_size;
        public uint _g_simulation_distributed_view_data_array;
        public int _g_simulation_distributed_view_data_array_entry_size;
        public uint _g_simulation_distributed_world;
        public int _g_simulation_distributed_world_size;

        public int _g_simulation_data_view_array_modifier;

        public uint _g_network_heap_size;
        public uint _g_network_heap;

        public uint _g_webstats_size;
        public uint _g_webstats;
    }

    public class HaloParser
    {
        public static void ParseHaloData(Xbox xbox, PageTableMap pageTableMap)
        {
            // Read the address mapping file.
            HaloAddressMappings addressMapping = ReadAddressMappingFile(xbox);
            if (addressMapping == null)
                return;

            // Map the entire runtime region:
            uint lowAddress = (uint)xbox.Memory.ReadUInt32(addressMapping.physical_memory_globals_low_stage_address) | 0x80000000;
            uint highAddress = (uint)xbox.Memory.ReadUInt32(addressMapping.physical_memory_globals_hi_stage_address) | 0x80000000;
            pageTableMap.MarkPTERangeAsType(lowAddress, highAddress, PageTableUseType.HaloRuntimeData);

            // Map d3d buffers:
            uint d3dDevicePtr = (uint)xbox.Memory.ReadUInt32(addressMapping.d3d_g_pDevice);
            D3DParser.ParseAndMapD3DRenderTargets(xbox, d3dDevicePtr, pageTableMap);

            // Loop and map all rasterizer target textures.
            for (int i = 0; i < 39; i++)
            {
                // Parse and map the rasterizer target.
                uint textureAddress = (uint)(addressMapping._g_rasterizer_render_targets_array + (i * 0x98) + 4);
                D3DParser.ParseAndMapD3dTexture(xbox, d3dDevicePtr, textureAddress, pageTableMap, PageTableUseType.HaloRasterizerBuffer);
            }

            // Map texture cache:
            ParseAndMapCacheRegion(xbox, addressMapping.xbox_texture_cache_globals_standard_cache, addressMapping.xbox_texture_cache_globals_standard_cache_base_address, 0, pageTableMap, PageTableUseType.HaloTextureCache);

            // Map low detail texture cache:
            int lowTextureCacheSize = xbox.Memory.ReadInt32(addressMapping.g_cache_file_globals_header + 0x15C);
            int lowTexturePageCount = (lowTextureCacheSize + 4095) / 4096;
            uint lowTextureCacheAddress = (uint)xbox.Memory.ReadUInt32(addressMapping.xbox_texture_cache_globals_low_detail_cache_base_address);
            pageTableMap.MarkPTERangeAsType2(lowTextureCacheAddress, lowTexturePageCount, PageTableUseType.HaloLowTextureCache);

            // Map geometry cache:
            ParseAndMapCacheRegion(xbox, addressMapping.xbox_geometry_cache_globals_cache, addressMapping.xbox_geometry_cache_globals_cache_base_address, 0, pageTableMap, PageTableUseType.HaloGeometryCache);

            // Map sound cache:
            ParseAndMapCacheRegion(xbox, addressMapping.xbox_sound_cache_globals_cache, addressMapping.xbox_sound_cache_globals_base_address, 0, pageTableMap, PageTableUseType.HaloSoundCache);

            // Map animation cache:
            ParseAndMapCacheRegion(xbox, addressMapping.xbox_animation_cache_globals_cache, addressMapping.xbox_animation_cache_globals_cache_base_address, addressMapping.xbox_animation_cache_globals_animation_cache_size_bytes, pageTableMap, PageTableUseType.HaloAnimationCache);

            // Map network, simulation, and misc caches:
            int arrayCount = xbox.Memory.ReadInt32(addressMapping._g_something_count);
            int networkChannelsPageCount = ((arrayCount * addressMapping._g_network_channels_array_entry_size) + 4095) / 4096;
            uint networkChannelsAddress = (uint)xbox.Memory.ReadUInt32(addressMapping._g_network_channels_array);
            pageTableMap.MarkPTERangeAsType2(networkChannelsAddress, networkChannelsPageCount, PageTableUseType.HaloNetSimMiscCache);

            int networkConnectionsPageCount = ((arrayCount * addressMapping._g_network_connections_entry_size) + 4095) / 4096;
            uint networkConnectionsAddress = (uint)xbox.Memory.ReadUInt32(addressMapping._g_network_connections);
            pageTableMap.MarkPTERangeAsType2(networkConnectionsAddress, networkConnectionsPageCount, PageTableUseType.HaloNetSimMiscCache);

            int networkMessageQueuesPageCount = ((arrayCount * addressMapping._g_network_message_queues_entry_size) + 4095) / 4096;
            uint networkMessageQueuesAddress = (uint)xbox.Memory.ReadUInt32(addressMapping._g_network_message_queues);
            pageTableMap.MarkPTERangeAsType2(networkMessageQueuesAddress, networkMessageQueuesPageCount, PageTableUseType.HaloNetSimMiscCache);

            int arrayCount2 = xbox.Memory.ReadInt32(addressMapping._g_something_count2);
            int simulationViewDataArrayPageCount = CalculateUnknownArrayPageCount(arrayCount2, addressMapping._g_simulation_view_data_array_entry_size, 0, addressMapping._g_simulation_data_view_array_modifier);
            uint simulationViewDataArrayAddress = (uint)xbox.Memory.ReadUInt32(addressMapping._g_simulation_view_data_array);
            pageTableMap.MarkPTERangeAsType2(simulationViewDataArrayAddress, simulationViewDataArrayPageCount, PageTableUseType.HaloNetSimMiscCache);

            int simDistViewDataArrayPageCount = CalculateUnknownArrayPageCount(arrayCount2, addressMapping._g_simulation_distributed_view_data_array_entry_size, 0, addressMapping._g_simulation_data_view_array_modifier);
            uint simDistViewDataArrayAddress = (uint)xbox.Memory.ReadUInt32(addressMapping._g_simulation_distributed_view_data_array);
            pageTableMap.MarkPTERangeAsType2(simDistViewDataArrayAddress, simDistViewDataArrayPageCount, PageTableUseType.HaloNetSimMiscCache);

            uint simulationDistWorldAddress = (uint)xbox.Memory.ReadUInt32(addressMapping._g_simulation_distributed_world);
            pageTableMap.MarkPTERangeAsType2(simulationDistWorldAddress, (addressMapping._g_simulation_distributed_world_size + 4095) / 4096, PageTableUseType.HaloNetSimMiscCache);

            int networkHeapSize = xbox.Memory.ReadInt32(addressMapping._g_network_heap_size);
            networkHeapSize = (networkHeapSize + 4095) / 4096;
            uint networkHeapAddress = (uint)xbox.Memory.ReadUInt32(addressMapping._g_network_heap);
            pageTableMap.MarkPTERangeAsType2(networkHeapAddress, networkHeapSize, PageTableUseType.HaloNetSimMiscCache);

            int webstatsPageCount = xbox.Memory.ReadInt32(addressMapping._g_webstats_size);
            webstatsPageCount = (webstatsPageCount + 4095) / 4096;
            uint webstatsAddress = (uint)xbox.Memory.ReadUInt32(addressMapping._g_webstats);
            pageTableMap.MarkPTERangeAsType2(webstatsAddress, webstatsPageCount, PageTableUseType.HaloNetSimMiscCache);

            // Map tag data last as we don't know which raster buffers are still in use and some may only be used in the main menu which has smaller tag data size:
            int totalTagsSize = xbox.Memory.ReadInt32(addressMapping.g_cache_file_globals_header + 0x1C);
            totalTagsSize = (totalTagsSize + 4095) / 4096;
            pageTableMap.MarkPTERangeAsType2(lowAddress, totalTagsSize, PageTableUseType.HaloTagData);
        }

        private static void ParseAndMapCacheRegion(Xbox xbox, uint lruvAddress, uint regionAddress, uint sizeAddress, PageTableMap pageTableMap, PageTableUseType type)
        {
            // Sanity check: make sure lruv cache signature is valid.
            uint lruvTableAddress = (uint)xbox.Memory.ReadUInt32(lruvAddress);
            uint signature = (uint)xbox.Memory.ReadUInt32(lruvTableAddress + 0x68);
            Debug.Assert(signature == 0x77656565);

            // Get the size of the lruv cache region.
            int cachePageCount = 0;
            if (sizeAddress == 0)
                cachePageCount = (xbox.Memory.ReadInt32(lruvTableAddress + 0x30) << 12) / 4096;
            else
                cachePageCount = xbox.Memory.ReadInt32(sizeAddress) / 4096;

            // Map the cache region in the page table map.
            uint regionBaseAddress = (uint)xbox.Memory.ReadUInt32(regionAddress);
            pageTableMap.MarkPTERangeAsType2(regionBaseAddress, cachePageCount, type);
        }

        private static int CalculateUnknownArrayPageCount(int maximum_count, int size, int alignment, int modifier)
        {
            int roundedSize = (maximum_count + 31) / 32;
            int alignedSize = (maximum_count * size) + (1 << alignment);

            int finalSize = alignedSize + (roundedSize * 4) + modifier;
            return (finalSize + 4095) / 4096;
        }

        public static HaloAddressMappings ReadAddressMappingFile(Xbox xbox)
        {
            Dictionary<uint, string> AddressMappingFileDictionary = new Dictionary<uint, string>()
            {
                { 0x545C211F, "HaloAddressMappings_v1.0.ini" },     // v1.0

                { 0xEB7D2DD9, "HaloAddressMappings_Test.ini" }      // Test xbe
            };

            // Get a list of fields in the address mapping class.
            HaloAddressMappings addressMappings = new HaloAddressMappings();
            Dictionary<string, FieldInfo> addressMappingFieldInfo = typeof(HaloAddressMappings).GetFields(BindingFlags.Public | BindingFlags.Instance).ToDictionary(k => k.Name, v => v);

            // Read the first 4 bytes of the xbe signature to determine which version it is.
            uint signatureBytes = (uint)xbox.Memory.ReadUInt32(0x10004);
            if (AddressMappingFileDictionary.ContainsKey(signatureBytes) == false)
            {
                // No address mappings found for this xbe.
                Console.WriteLine("Unrecognized Halo 2 xbe!");
                return null;
            }

            // Do some janky file parsing to read the address mapping file.
            string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] lines = File.ReadAllLines($"{appDirectory}\\{AddressMappingFileDictionary[signatureBytes]}");

            // Parse each line, removing any lines that are empty or start with a ';' character.
            lines = lines.Where(l => string.IsNullOrEmpty(l) == false && string.IsNullOrWhiteSpace(l) == false && l.StartsWith(';') == false).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                // Split the line and cleanup the name and address parts.
                string[] parts = lines[i].Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                string name = parts[0];
                uint address = Convert.ToUInt32(parts[1].Substring(0, Math.Min(10, parts[1].Length)), 16);

                // If a field with matching name isn't found in the field info dictionary skip it.
                if (addressMappingFieldInfo.ContainsKey(name) == false)
                {
                    Console.WriteLine($"Field '{name}' not recognized, ignoring...");
                    continue;
                }

                // Check the field type and set the value accordingly.
                if (addressMappingFieldInfo[name].FieldType == typeof(uint))
                    addressMappingFieldInfo[name].SetValue(addressMappings, address);
                else if (addressMappingFieldInfo[name].FieldType == typeof(int))
                    addressMappingFieldInfo[name].SetValue(addressMappings, (int)address);
                else
                    throw new Exception($"Field '{name}' has unsupported field type {addressMappingFieldInfo[name].FieldType}");
            }

            return addressMappings;
        }
    }
}
