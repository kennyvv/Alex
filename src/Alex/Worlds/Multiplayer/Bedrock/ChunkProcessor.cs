using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using Alex.Blocks;
using Alex.Blocks.Mapping;
using Alex.Blocks.Minecraft;
using Alex.Blocks.Minecraft.Fences;
using Alex.Common.Utils.Collections;
using Alex.Entities.BlockEntities;
using Alex.Net.Bedrock;
using Alex.Utils;
using Alex.Utils.Caching;
using Alex.Worlds.Chunks;
using Alex.Worlds.Singleplayer;
using fNbt;
using MiNET;
using MiNET.Net;
using MiNET.Utils;
using NLog;
using BlockCoordinates = Alex.Common.Utils.Vectors.BlockCoordinates;
using BlockState = Alex.Blocks.State.BlockState;
using ChunkCoordinates = Alex.Common.Utils.Vectors.ChunkCoordinates;
using NibbleArray = MiNET.Utils.NibbleArray;

namespace Alex.Worlds.Multiplayer.Bedrock
{
    public class ChunkProcessor : IDisposable
    {
	    private static readonly Logger         Log = LogManager.GetCurrentClassLogger(typeof(ChunkProcessor));
	    public static           ChunkProcessor Instance { get; private set; }
	    static ChunkProcessor()
	    {
		    
	    }
	    
	    //public IReadOnlyDictionary<uint, BlockStateContainer> BlockStateMap { get; set; } =
		//    new Dictionary<uint, BlockStateContainer>();

	    public static Itemstates Itemstates { get; set; } = new Itemstates();
	    
	    private ConcurrentDictionary<uint, uint> _convertedStates = new ConcurrentDictionary<uint, uint>();
	    
	    private CancellationToken CancellationToken  { get; }
	    //public  bool              ClientSideLighting { get; set; } = true;

	    private BedrockClient    Client           { get; }
	    public BlobCache        Cache            { get; }

	    public ChunkProcessor(BedrockClient client, CancellationToken cancellationToken, BlobCache blobCache)
        {
	        Client = client;
	        CancellationToken = cancellationToken;
	        Cache = blobCache;

	        Instance = this;
        }

	    private ConcurrentQueue<KeyValuePair<ulong, byte[]>> _blobQueue =
		    new ConcurrentQueue<KeyValuePair<ulong, byte[]>>();
	    
	    private ConcurrentQueue<ChunkData>            _actionQueue = new ConcurrentQueue<ChunkData>();

        public void HandleChunkData(bool cacheEnabled,
	        ulong[] blobs,
	        uint subChunkCount,
	        byte[] chunkData,
	        int cx,
	        int cz)
        {
	        if (CancellationToken.IsCancellationRequested || subChunkCount < 1)
		        return;

	        var value = new ChunkData(cacheEnabled, blobs, subChunkCount, chunkData, cx, cz);

	        _actionQueue.Enqueue(value);

	        _resetEvent.Set();
	        
	        CheckThreads();
        }
        
        //public void AddAction(ChunkCoordinates co)

        private Thread           _workerThread = null;
        private object           _syncObj      = new object();
        private object           _threadSync   = new object();
        private ManualResetEvent _resetEvent   = new ManualResetEvent(false);
        private long             _threadHelper = 0;
        private void CheckThreads()
        {
	        if (_actionQueue.IsEmpty && _blobQueue.IsEmpty)
		        return;
	        
	        if (!Monitor.TryEnter(_syncObj, 0))
		        return;

	        try
	        {
		        if (_actionQueue.IsEmpty && _blobQueue.IsEmpty)
			        return;
		        if (_workerThread == null && Interlocked.CompareExchange(ref _threadHelper, 1, 0) == 0)
		        {
			        ThreadPool.QueueUserWorkItem(
				        o =>
				        {
					        lock (_threadSync)
					        {
						        _workerThread = Thread.CurrentThread;

						        while (!CancellationToken.IsCancellationRequested)
						        {
							        bool handled = false;

							        if (_actionQueue != null && _actionQueue.TryDequeue(out var enqueuedChunk))
							        {
								        //if (_actions.TryRemove(chunkCoordinates, out var chunk))

								        if (enqueuedChunk.CacheEnabled)
								        {
									        HandleChunkCachePacket(enqueuedChunk);

									        continue;
								        }

								        var handledChunk = HandleChunk(enqueuedChunk);

								        Client.World.ChunkManager.AddChunk(
									        handledChunk, new ChunkCoordinates(enqueuedChunk.X, enqueuedChunk.Z), true);

								        handled = true;
							        }

							        if (_blobQueue != null && _blobQueue.TryDequeue(out var kv))
							        {
								        ulong  hash = kv.Key;
								        byte[] data = kv.Value;

								        if (!Cache.Contains(hash))
											Cache.TryStore(hash, data);

								        var chunks = _futureChunks.Where(c => c.SubChunks.Contains(hash) || c.Biome == hash).ToArray();
								        foreach (CachedChunk chunk in chunks)
								        {
									        chunk.TryBuild(Client, this);
								        }

								        handled = true;
							        }
							        
							        if (!handled)
							        {
								        _resetEvent.Reset();
								        
								        if (!_resetEvent.WaitOne(50))
											break;
							        }
						        }

						        Interlocked.CompareExchange(ref _threadHelper, 0, 1);
						      //  _threadHelper = 0;
						        _workerThread = null;
					        }
				        });
		        }
	        }
	        finally
	        {
		        Monitor.Exit(_syncObj);
	        }
        }

        private List<string> Failed     { get; set; } = new List<string>();

        public BlockState GetBlockState(uint p)
        {
	        if (_convertedStates.TryGetValue(p, out var existing))
	        {
		        return BlockFactory.GetBlockState(existing);
	        }
	        
	    //    if (!BlockStateMap.TryGetValue(p, out var bs))
	      //  {
		 //       var a = MiNET.Blocks.BlockFactory.BlockPalette.FirstOrDefault(x => x.RuntimeId == p);
		//        bs = a;
	    //    }

	    var bs = MiNET.Blocks.BlockFactory.BlockPalette.FirstOrDefault(x => x.RuntimeId == p);
	        if (bs != null)
	        {
		        var copy = new BlockStateContainer()
		        {
			        Data = bs.Data,
			        Id = bs.Id,
			        Name = bs.Name,
			        States = bs.States,
			        RuntimeId = bs.RuntimeId
		        };
				        
		        if (TryConvertBlockState(copy, out var convertedState))
		        {
			        if (convertedState != null)
			        {
				        _convertedStates.TryAdd(p, convertedState.ID);
			        }

			        return convertedState;
		        }

		        var t = BlockFactory.GetBlockState(copy.Name);/* TranslateBlockState(
			        BlockFactory.GetBlockState(copy.Name),
			        -1, copy.Data);*/

		        if (t.Name == "Unknown" && !Failed.Contains(copy.Name))
		        {
			        Failed.Add(copy.Name);

			        return t;
			        //  File.WriteAllText(Path.Combine("failed", bs.Name + ".json"), JsonConvert.SerializeObject(bs, Formatting.Indented));
		        }
		        else
		        {
			        _convertedStates.TryAdd(p, t.ID);
			        return t;
		        }
	        }

	        return null;
        }

        public ThreadSafeList<CachedChunk>                 _futureChunks        = new ThreadSafeList<CachedChunk>();
        public ConcurrentDictionary<BlockCoordinates, NbtCompound> _futureBlockEntities = new ConcurrentDictionary<BlockCoordinates, NbtCompound>();
        public void HandleClientCacheMissResponse(McpeClientCacheMissResponse message)
        {
	        foreach (KeyValuePair<ulong, byte[]> kv in message.blobs)
	        {
		        _blobQueue.Enqueue(kv);
	        }
	        
	        _resetEvent.Set();
	        
	        CheckThreads();
        }
        
        private void HandleChunkCachePacket(ChunkData chunkData)
        {
	        var chunk = new CachedChunk(chunkData.X, chunkData.Z);
	        chunk.SubChunks = new ulong[chunkData.SubChunkCount];
	        chunk.Sections = new byte[chunkData.SubChunkCount][];

	        var hits   = new List<ulong>();
	        var misses = new List<ulong>();

	        ulong biomeHash = chunkData.Blobs.Last();
	        chunk.Biome = biomeHash;
	        if (Cache.Contains(biomeHash))
	        { 
		        hits.Add(biomeHash);
	        }
	        else
	        {
		        misses.Add(biomeHash);
	        }

	        for (int i = 0; i < chunkData.SubChunkCount; i++)
	        {
		        ulong hash = chunkData.Blobs[i];
		        chunk.SubChunks[i] = hash;
		        
		        if (Cache.TryGet(hash, out byte[] subChunkData))
		        {
			        chunk.Sections[i] = subChunkData;
			        hits.Add(hash);
			        //chunk.SubChunkCount--;
		        }
		        else
		        {
			        chunk.Sections[i] = null;
			        misses.Add(hash);
		        }
	        }
	        
	        if (misses.Count > 0)
		        _futureChunks.TryAdd(chunk);
	        
	        var status = McpeClientCacheBlobStatus.CreateObject();
	        status.hashHits = hits.ToArray();
	        status.hashMisses = misses.ToArray();
	        Client.SendPacket(status);

	        using (MemoryStream ms = new MemoryStream(chunkData.Data))
	        {
		        ReadExtra(chunk.Chunk, ms);
	        }

	        if (chunk.IsComplete)
	        {
		        chunk.TryBuild(Client, this);
	        }
        }

        internal ChunkSection ReadSection(Stream stream, NbtBinaryReader defStream)
        {
	        ChunkSection section = null;

	        int version = defStream.ReadByte();

	        if (version == 1 || version == 8)
	        {
		        int storageSize = version == 1 ? 1 : defStream.ReadByte();
		        
		        section = new ChunkSection(storageSize);

		        for (int storage = 0; storage < storageSize; storage++)
		        {
			        int flags = stream.ReadByte();
			        bool isRuntime = (flags & 1) != 0;
			        int bitsPerBlock = flags >> 1;
			        int blocksPerWord = (int) Math.Floor(32f / bitsPerBlock);
			        int wordsPerChunk = (int) Math.Ceiling(4096f / blocksPerWord);

			        long jumpPos = stream.Position;
			        stream.Seek(wordsPerChunk * 4, SeekOrigin.Current);

			        int paletteCount = VarInt.ReadSInt32(stream);
			        var palette = new int[paletteCount];
			        bool allZero = true;

			        for (int j = 0; j < paletteCount; j++)
			        {
				        if (!isRuntime)
				        {
					        var file = new NbtFile {BigEndian = false, UseVarInt = true};
					        file.LoadFromStream(stream, NbtCompression.None);
					        var tag = (NbtCompound) file.RootTag;

					        var block = MiNET.Blocks.BlockFactory.GetBlockByName(tag["name"].StringValue);

					        if (block != null && block.GetType() != typeof(Block) && !(block is Air))
					        {
						        List<IBlockState> blockState = ReadBlockState(tag);
						        block.SetState(blockState);
					        }
					        else
					        {
						        block = new MiNET.Blocks.Air();
					        }

					        palette[j] = block.GetRuntimeId();
				        }
				        else
				        {
					        int runtimeId = VarInt.ReadSInt32(stream);
					        palette[j] = runtimeId;
					        //if (bedrockPalette == null || internalBlockPallet == null) continue;

					        // palette[j] = GetServerRuntimeId(bedrockPalette, internalBlockPallet, runtimeId);
				        }

				        if (palette[j] != 0)
					        allZero = false;
			        }

			        if (!allZero)
			        {
				        long afterPos = stream.Position;
				        stream.Position = jumpPos;
				        int position = 0;

				        for (int w = 0; w < wordsPerChunk; w++)
				        {
					        uint word = defStream.ReadUInt32();

					        for (uint block = 0; block < blocksPerWord; block++)
					        {
						        if (position >= 4096)
							        continue;

						        uint state = (uint) ((word >> ((position % blocksPerWord) * bitsPerBlock))
						                             & ((1 << bitsPerBlock) - 1));

						        int x = (position >> 8) & 0xF;
						        int y = position & 0xF;
						        int z = (position >> 4) & 0xF;

						        int runtimeId = palette[state];

						        if (runtimeId != 0)
						        {
							        var blockState = GetBlockState((uint) runtimeId);

							        if (blockState != null)
								        section.Set(storage, x, y, z, blockState);
						        }

						        position++;
					        }
				        }

				        stream.Position = afterPos;
			        }
		        }
	        }
	        else if (version == 0)
	        {
		        section = new ChunkSection(1);

		        #region OldFormat

		        byte[] blockIds = new byte[4096];
		        defStream.Read(blockIds, 0, blockIds.Length);

		        NibbleArray data = new NibbleArray(4096);
		        defStream.Read(data.Data, 0, data.Data.Length);

		        for (int x = 0; x < 16; x++)
		        {
			        for (int z = 0; z < 16; z++)
			        {
				        for (int y = 0; y < 16; y++)
				        {
					        int idx = (x << 8) + (z << 4) + y;
					        var id = blockIds[idx];
					        var meta = data[idx];

					        //var ruid = BlockFactory.GetBlockStateID(id, meta);

					        var block = MiNET.Blocks.BlockFactory.GetRuntimeId(id, meta);
					        BlockState result = GetBlockState(block);

					        if (result != null)
					        {
						        section.Set(x, y, z, result);
					        }
					        else
					        {
						        Log.Info($"Unknown block: {id}:{meta}");
					        }

				        }
			        }
		        }

		        #endregion
	        }
	        else
	        {
		        Log.Warn($"Unsupported storage version: {version}");
	        }

	        return section;
        }

        private ChunkColumn HandleChunk(ChunkData chunk)
        {
	        ChunkColumn chunkColumn = new ChunkColumn(chunk.X, chunk.Z);

	        try
	        {
		        using (MemoryStream stream = new MemoryStream(chunk.Data))
		        {
			        using NbtBinaryReader defStream = new NbtBinaryReader(stream, true);
			        for (int s = 0; s < chunk.SubChunkCount; s++)
			        {
				        chunkColumn.Sections[s] = ReadSection(stream, defStream);
			        }
			        
			         byte[] biomeIds = new byte[256];

			         if (defStream.Read(biomeIds, 0, 256) != 256) return chunkColumn;

			        for (int x = 0; x < 16; x++)
			        {
				        for (int z = 0; z < 16; z++)
				        {
					        var biomeId = biomeIds[(z << 4) + (x)];

					        for (int y = 0; y < 255; y++)
					        {
						        chunkColumn.SetBiome(x, y, z, biomeId);
					        }
				        }
			        }

			        if (stream.Position >= stream.Length - 1)
			        {
				        chunkColumn.CalculateHeight();
				       return chunkColumn;
			        }

			        ReadExtra(chunkColumn, stream);

			        chunkColumn.CalculateHeight();
			        
			        return chunkColumn;
		        }

	        }
	        catch (Exception ex)
	        {
		        Log.Error($"Exception in chunk loading: {ex.ToString()}");

		        return chunkColumn;
	        }
	        finally
	        {

	        }
        }

        private void ReadExtra(ChunkColumn chunkColumn, MemoryStream stream)
        {
	        int borderBlock = VarInt.ReadSInt32(stream);// defStream.ReadByte();// defStream.ReadVarInt(); //VarInt.ReadSInt32(stream);
	        if (borderBlock != 0)
	        {
		        int len   = (int) (stream.Length - stream.Position);
		        var bytes = new byte[len];
		        stream.Read(bytes, 0, len);
	        }

	        if (stream.Position < stream.Length - 1)
	        {
		        while (stream.Position < stream.Length - 1)
		        {
			        try
			        {
				        NbtFile file = new NbtFile() {BigEndian = false, UseVarInt = true};

				        file.LoadFromStream(stream, NbtCompression.None);
				        var blockEntityTag = file.RootTag;

				        int x = blockEntityTag["x"].IntValue;
				        int y = blockEntityTag["y"].IntValue;
				        int z = blockEntityTag["z"].IntValue;

				        chunkColumn.AddBlockEntity(new BlockCoordinates(x, y, z), (NbtCompound) file.RootTag);
			        }catch(EndOfStreamException){}

			        // if (Log.IsTraceEnabled()) Log.Trace($"Blockentity:\n{file.RootTag}");
		        }
	        }

	        if (stream.Position < stream.Length - 1)
	        {
		        int len   = (int) (stream.Length - stream.Position);
		        var bytes = new byte[len];
		        stream.Read(bytes, 0, len);
		        Log.Warn($"Still have data to read\n{Packet.HexDump(new ReadOnlyMemory<byte>(bytes))}");
	        }
        }

        private static List<IBlockState> ReadBlockState(NbtCompound tag)
        {
	        //Log.Debug($"Palette nbt:\n{tag}");

	        var states    = new List<IBlockState>();
	        var nbtStates = (NbtCompound) tag["states"];
	        foreach (NbtTag stateTag in nbtStates)
	        {
		        IBlockState state = stateTag.TagType switch
		        {
			        NbtTagType.Byte => (IBlockState) new BlockStateByte()
			        {
				        Name = stateTag.Name,
				        Value = stateTag.ByteValue
			        },
			        NbtTagType.Int => new BlockStateInt()
			        {
				        Name = stateTag.Name,
				        Value = stateTag.IntValue
			        },
			        NbtTagType.String => new BlockStateString()
			        {
				        Name = stateTag.Name,
				        Value = stateTag.StringValue
			        },
			        _ => throw new ArgumentOutOfRangeException()
		        };
		        states.Add(state);
	        }

	        return states;
        }

        private bool TryConvertBlockState(BlockStateContainer record, out BlockState result)
        {
	        if (_convertedStates.TryGetValue((uint) record.RuntimeId, out var alreadyConverted))
	        {
		        result = BlockFactory.GetBlockState(alreadyConverted);
		        return true;
	        }

	        if (BlockFactory.BedrockStates.TryGetValue(record.Name, out var bedrockStates))
	        {
		        var defaultState = bedrockStates.GetDefaultState();

		        if (defaultState != null)
		        {
			        if (record.States != null)
			        {
				        foreach (var prop in record.States)
				        {
					        defaultState = defaultState.WithProperty(
						        prop.Name, prop.Value());
				        }
			        }

			        if (defaultState is PeBlockState pebs)
			        {
				        result = pebs.Original;
				        return true;
			        }

			        result = defaultState;
			        return true;
		        }
	        }

	        result = null;
	        return false;
        }

        public void Dispose()
		{
			_actionQueue?.Clear();
			_actionQueue = null;
			
			_blobQueue?.Clear();
			_blobQueue = null;
			
			_convertedStates?.Clear();
			_convertedStates = null;
			
			if (Instance != this)
			{
				//BackgroundWorker?.Dispose();
				//BackgroundWorker = null;
			}
		}

		private class ChunkData
		{
			public readonly bool                CacheEnabled;
			public readonly ulong[]             Blobs;
			public readonly uint                SubChunkCount;
			public readonly byte[]              Data;
			public readonly int                 X;
			public readonly int                 Z;

			public ChunkData(bool cacheEnabled,
				ulong[] blobs,
				uint subChunkCount,
				byte[] chunkData,
				int cx,
				int cz)
			{
				CacheEnabled = cacheEnabled;
				Blobs = blobs;
				SubChunkCount = subChunkCount;
				Data = chunkData;
				X = cx;
				Z = cz;
			}
		}
    }
}