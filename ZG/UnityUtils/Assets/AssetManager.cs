using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace ZG
{
    public partial class AssetManager : IEnumerable<KeyValuePair<string, AssetManager.Asset>>
    {
        public enum AssetType
        {
            //只读且无法解压
            Uncompressed,
            //已运行时解压
            UncompressedRuntime,
            //只读，运行时完全解压
            Compressed,
            //只读，运行时以LZ4解压
            LZ4,
            //只读，运行时拷贝不解压
            Stream
        }

        [Serializable]
        public class AssetList
        {
            [Serializable]
            public struct Asset
            {
                public string name;
                public AssetType type;
            }

            public List<Asset> assets;
        }

        public struct AssetPack
        {
            public static readonly AssetPack Default = default;

            public string name;

            public string filePath;

            public ulong fileOffset;

            public bool isVail => !string.IsNullOrEmpty(filePath);

            //public bool canRecompress => true;// string.IsNullOrEmpty(filePath) || fileOffset == 0;

            public AssetPack(string name, string filePath, ulong fileOffset)
            {
                this.name = name;
                this.filePath = filePath;
                this.fileOffset = fileOffset;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(fileOffset);
                writer.Write(string.IsNullOrEmpty(filePath) ? string.Empty : filePath);
                writer.Write(string.IsNullOrEmpty(name) ? string.Empty : name);
            }

            public static AssetPack Read(BinaryReader reader, uint version)
            {
                AssetPack pack;
                if (version > 3)
                {
                    pack.fileOffset = reader.ReadUInt64();
                    pack.filePath = reader.ReadString();

                    if (version > 6)
                        pack.name = reader.ReadString();
                    else
                        pack.name = string.IsNullOrEmpty(pack.filePath) ? null : DefaultAssetPackHeader.NAME;
                }
                else
                {
                    pack.fileOffset = 0;
                    pack.filePath = null;
                    pack.name = null;
                }

                return pack;
            }

        }

        public struct AssetInfo
        {
            public uint version;
            public uint size;
            public byte[] md5;

            public static AssetInfo Read(BinaryReader reader, uint version)
            {
                AssetInfo assetInfo;
                assetInfo.version = reader.ReadUInt32();
                assetInfo.size = reader.ReadUInt32();

                switch (version)
                {
                    case 0:
                        assetInfo.md5 = null;
                        break;
                    default:
                        assetInfo.md5 = reader.ReadBytes(16);
                        break;
                }

                return assetInfo;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write(version);
                writer.Write(size);

                UnityEngine.Assertions.Assert.AreEqual(16, md5.Length);

                writer.Write(md5);
            }
        }

        public struct AssetData
        {
            public AssetType type;

            public AssetInfo info;

            /*public ulong fileOffset;

            public string filePath;*/
            public AssetPack pack;

            public string[] dependencies;

            public bool isReadOnly => type != AssetType.UncompressedRuntime && pack.isVail;

            public static AssetData Read(BinaryReader reader, uint version)
            {
                AssetData data;
                if (version > 5)
                    data.type = (AssetType)reader.ReadByte();
                else
                    data.type = AssetType.Uncompressed;

                data.info = AssetInfo.Read(reader, version);
                data.pack = AssetPack.Read(reader, version);

                int numDependencies = reader.ReadInt32();
                data.dependencies = numDependencies > 0 ? new string[numDependencies] : null;
                for (int i = 0; i < numDependencies; ++i)
                    data.dependencies[i] = reader.ReadString();

                return data;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write((byte)type);

                info.Write(writer);

                pack.Write(writer);

                int numDependencies = dependencies == null ? 0 : dependencies.Length;
                writer.Write(numDependencies);
                for (int i = 0; i < numDependencies; ++i)
                    writer.Write(dependencies[i]);
            }
        }

        public struct Asset
        {
            public long offset;

            public AssetData data;
        }

        /*public class AssetDownloadHandler : DownloadHandlerScript, IDisposable
        {
            private ulong __streamOffset;
            private ulong __streamLength;
            private ulong __startBytes;
            private ulong __overrideOffset;
            private string __overridePath;
            private MemoryStream __stream;
            private AssetManager __manager;
            private IReadOnlyList<KeyValuePair<string, Asset>> __assets;

            private ReaderWriterLockSlim __lock;

            public bool isDownloading
            {
                get;

                private set;
            }

            public string assetName
            {
                get;

                private set;
            }

            public float assetProgress
            {
                get;

                private set;
            }

            public int assetCount
            {
                get;

                private set;
            }

            public uint bytesDownloaded
            {
                get;

                private set;
            }

            public ulong fileBytesDownloaded
            {
                get;

                private set;
            }

            public AssetDownloadHandler(
                ulong maxSize,
                AssetManager manager) : base(new byte[maxSize])
            {
                isDownloading = true;

                __manager = manager;

                __lock = new ReaderWriterLockSlim();
            }

            public new void Dispose()
            {
                if (__stream != null)
                    __stream.Dispose();

                base.Dispose();
            }

            public void Clear()
            {
                isDownloading = true;

                __lock.EnterWriteLock();

                __streamOffset = 0;
                __streamLength = 0;

                if (__stream != null)
                    __stream.Position = 0L;

                __lock.ExitWriteLock();
            }

            public void Init(
                ulong startBytes,
                ulong overrideOffset,
                string overridePath,
                IReadOnlyList<KeyValuePair<string, Asset>> assets)
            {
                __startBytes = startBytes;

                __overrideOffset = overrideOffset;
                __overridePath = overridePath;

                bytesDownloaded = 0;
                fileBytesDownloaded = 0;

                assetCount = 0;
                __assets = assets;
            }

            public bool ThreadUpdate(bool isDone, int count)
            {
                if (isDownloading)
                    isDownloading = __Update(isDone, count);

                return isDownloading;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                if (isDownloading)
                {
                    ulong streamLength = __streamLength + (ulong)dataLength;
                    if (string.IsNullOrEmpty(__overridePath))
                    {
                        __lock.EnterUpgradeableReadLock();

                        ulong streamOffset = this.fileBytesDownloaded + __startBytes;
                        if (streamOffset < streamLength)
                        {
                            bool isWrite = false;
                            try
                            {
                                if (__stream == null)
                                {
                                    isWrite = true;

                                    __lock.EnterWriteLock();

                                    __stream = new MemoryStream();
                                }

                                if (streamOffset > __streamOffset)
                                {
                                    if (streamOffset < __streamLength)
                                    {
                                        int count = (int)(__streamLength - streamOffset);
                                        var buffer = new byte[count];
                                        __stream.Seek(-count, SeekOrigin.Current);
                                        __stream.Read(buffer, 0, count);

                                        if (!isWrite)
                                        {
                                            isWrite = true;

                                            __lock.EnterWriteLock();
                                        }

                                        __stream.Position = 0;
                                        __stream.Write(buffer, 0, count);

                                        __stream.Write(data, 0, dataLength);
                                    }
                                    else
                                    {
                                        if (!isWrite)
                                        {
                                            isWrite = true;

                                            __lock.EnterWriteLock();
                                        }

                                        __stream.Position = 0;
                                        int offset = (int)(streamOffset - __streamLength);
                                        __stream.Write(data, offset, dataLength - offset);
                                    }

                                    __streamOffset = streamOffset;
                                }
                                else
                                {
                                    if (!isWrite)
                                    {
                                        isWrite = true;

                                        __lock.EnterWriteLock();
                                    }

                                    __stream.Write(data, 0, dataLength);
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e.InnerException ?? e);

                                streamLength = __streamLength;

                                isDownloading = false;
                            }
                            finally
                            {
                                if (isWrite)
                                    __lock.ExitWriteLock();
                            }
                        }
                        else
                        {
                            __streamOffset += (ulong)(dataLength + __stream.Position);

                            __stream.Position = 0L;
                        }

                        __lock.ExitUpgradeableReadLock();
                    }

                    __streamLength = streamLength;

                    //Debug.LogError($"ReceiveData {dataLength}");
                }

                return isDownloading;
            }

            //TODO: Save Mem
            public bool __Update(bool isDone, int count)
            {
                KeyValuePair<string, Asset> pair;
                //string assetName;
                AssetData data;
                int offset;
                uint assetBytesDownloaded;//, bytesDownloaded;
                ulong fileBytesDownloaded;
                byte[] buffer;
                for (int i = 0; i < count; ++i)
                {
                    fileBytesDownloaded = this.fileBytesDownloaded + __startBytes;
                    if (__streamLength <= fileBytesDownloaded)
                        return !isDone;

                    assetBytesDownloaded = (uint)(__streamLength - fileBytesDownloaded);

                    //Debug.LogError($"__Update {assetBytesDownloaded}");

                    pair = __assets[assetCount];
                    data = pair.Value.data;

                    assetName = pair.Key;

                    bytesDownloaded = Math.Min(assetBytesDownloaded, data.info.size);

                    assetProgress = (float)(bytesDownloaded * 1.0 / data.info.size);

                    if (assetBytesDownloaded < data.info.size)
                        return !isDone;

                    if (string.IsNullOrEmpty(__overridePath))
                    {
                        __lock.EnterReadLock();

                        offset = (int)(fileBytesDownloaded - __streamOffset);
                        buffer = __stream.GetBuffer();
                        using (var md5 = new MD5CryptoServiceProvider())
                        {
                            var md5Hash = md5.ComputeHash(buffer, offset, (int)data.info.size);
                            if (data.info.md5 == null)
                                data.info.md5 = md5Hash;
                            else if (!MemoryEquals(md5Hash, data.info.md5))
                            {
                                __lock.ExitReadLock();

                                Debug.LogError($"{assetName} MD5 Fail.Offset : {__streamOffset}");

                                return false;
                            }
                        }

                        try
                        {
                            __manager.Create(assetName, data, buffer, offset, data.info.size);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);

                            return false;
                        }
                        finally
                        {
                            __lock.ExitReadLock();
                        }
                    }
                    else
                    {
                        data.fileOffset = __overrideOffset + fileBytesDownloaded;
                        data.filePath = __overridePath;

                        try
                        {
                            __manager.__Create(assetName, data);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);

                            return false;
                        }
                    }

                    this.fileBytesDownloaded += data.info.size;
                    //assetBytesDownloaded = 0;

                    //++__totalAssetIndex;

                    if (++assetCount >= __assets.Count)
                        return false;
                }

                return true;
            }
        }*/

        public const uint VERSION = 8;
        public const string FILE_SUFFIX_ASSET_INFOS = ".info";

        private string __path;
        private Dictionary<string, Asset> __assets;

        public static string GetPlatformPath(string path)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "file:///" + path;
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "file://" + path;
            }

            return path;
        }

        public static bool MemoryEquals(byte[] source, byte[] destination)
        {
            int length = source.Length;
            if (length != destination.Length)
                return false;

            for (int i = 0; i < length; ++i)
            {
                if (source[i] != destination[i])
                    return false;
            }

            return true;
        }

        /*public static AssetManager Create(string path, IEnumerable<KeyValuePair<string, Asset>> assets)
        {
            AssetManager assetManager = new AssetManager();
            assetManager.__path = path;

            if (assets != null)
            {
                assetManager.__assets = new Dictionary<string, Asset>();
                foreach (KeyValuePair<string, Asset> asset in assets)
                    assetManager.__assets.Add(asset.Key, asset.Value);
            }

            assetManager.Save();

            return assetManager;
        }*/

        public uint version
        {
            get;

            private set;
        }

        public int assetCount
        {
            get
            {
                return __assets == null ? 0 : __assets.Count;
            }
        }

        public string path
        {
            get
            {
                return __path;
            }
        }

        private AssetManager()
        {

        }

        public AssetManager(string path)
        {
            __path = path;

            LoadFrom(null);
        }

        public bool Contains(string folder)
        {
            if (folder == null)
                return false;

            Dictionary<string, Asset>.KeyCollection keys = __assets == null ? null : __assets.Keys;
            if (keys == null)
                return false;

            folder = folder.Replace('\\', '/') + '/';
            foreach (string key in keys)
            {
                if (key != null && key.Contains(folder))
                    return true;
            }

            return false;
        }

        public int CountOf(string folder)
        {
            var keys = __assets == null ? null : __assets.Keys;
            if (keys == null)
                return 0;

            if (!string.IsNullOrEmpty(folder))
                folder = folder.Replace('\\', '/');

            int result = 0;
            string name;
            foreach (string key in keys)
            {
                if (key == null)
                    continue;

                name = Path.GetDirectoryName(key);
                if (string.IsNullOrEmpty(folder) ? string.IsNullOrEmpty(name) : name.Replace('\\', '/') == folder)
                    ++result;
            }

            return result;
        }

        public bool Get(string name, out Asset asset)
        {
            if (__assets == null)
            {
                asset = default(Asset);

                return false;
            }

            return __assets.TryGetValue(name, out asset);
        }

        public bool GetAssetPath(string name, out Asset asset, out ulong fileOffset, out string filePath)
        {
            if (!__assets.TryGetValue(name, out asset))
            {
                fileOffset = 0U;
                filePath = null;

                return false;
            }

            if (asset.data.isReadOnly)
            {
                fileOffset = asset.data.pack.fileOffset;
                filePath = asset.data.pack.filePath;

                AssetUtility.UpdatePack(asset.data.pack.name, ref filePath, ref fileOffset);
            }
            else
            {
                fileOffset = 0;
                filePath = __GetAssetPath(name);
            }

            return true;
        }

        /*public IEnumerator LoadAll(
            IEnumerable<AssetPath> urlAndFolders,
            Func<ulong, IEnumerator> confirm,
            DownloadHandler handler)
        {
            bool isForce = confirm != null;
            string folder;
            AssetManager assetManager;
            List<(string, string, AssetManager)> results = null;
            foreach (var urlAndFolder in urlAndFolders)
            {
                assetManager = new AssetManager();
                yield return assetManager.__Load(urlAndFolder.url, urlAndFolder.folder, isForce);

                if (assetManager.__assets == null)
                    continue;

                folder = urlAndFolder.folder;
                if (!string.IsNullOrEmpty(folder))
                    folder = folder.Replace('\\', '/');

                if (results == null)
                    results = new List<(string, string, AssetManager)>();

                results.Add((urlAndFolder.filePath, folder, assetManager));
            }

            if (results == null)
                yield break;

            List<string> assetNamesToDelete = null;
            if (__assets != null)
            {
                string directory;
                foreach (string key in __assets.Keys)
                {
                    directory = Path.GetDirectoryName(key);
                    if (!string.IsNullOrEmpty(directory))
                        directory = directory.Replace('\\', '/');

                    foreach (var result in results)
                    {
                        folder = result.Item2;
                        if (string.IsNullOrEmpty(folder) ? !string.IsNullOrEmpty(directory) : folder != directory)
                            continue;

                        assetManager = result.Item3;
                        if (!assetManager.__assets.ContainsKey(key))
                        //Delete(name);
                        {
                            if (assetNamesToDelete == null)
                                assetNamesToDelete = new List<string>();

                            assetNamesToDelete.Add(key);
                        }

                        break;
                    }
                }
            }

            int numAssets = 0, assetCount, assetIndex, i;
            ulong maxSize = 0, size = 0, startBytes;
            AssetData destination;
            Asset source;
            string assetName;
            KeyValuePair<string, Asset> asset;
            var assets = new List<KeyValuePair<string, Asset>>();
            Dictionary<string, ulong> folderAssetStartBytes = null;
            foreach (var result in results)
            {
                assetManager = result.Item3;

                assetCount = assetManager.__assets.Count;
                if (assetCount > 0)
                {
                    assets.Clear();
                    assets.AddRange(assetManager.__assets);

                    startBytes = 0;
                    assetIndex = 0;

                    for (i = 0; i < assetCount; ++i)
                    {
                        asset = assets[i];
                        assetName = asset.Key;
                        destination = asset.Value.data;
                        if (__assets == null || !__assets.TryGetValue(assetName, out source) || source.data.info.version < destination.info.version)
                        {
                            ++numAssets;

                            maxSize = Math.Max(maxSize, destination.info.size);

                            size += destination.info.size;
                        }
                        else
                        {
                            if (assetIndex == i)
                            {
                                startBytes += destination.info.size;

                                ++assetIndex;
                            }
                            else
                                assetIndex = -1;

                            assetManager.__assets.Remove(assetName);
                        }
                    }

                    if (assetIndex >= 0)
                    {
                        if (folderAssetStartBytes == null)
                            folderAssetStartBytes = new Dictionary<string, ulong>();

                        folderAssetStartBytes.Add(result.Item2, startBytes);
                    }
                }
            }

            if (numAssets < 1)
                yield break;

            if (confirm != null)
                yield return confirm(size);

            if (assetNamesToDelete != null)
            {
                foreach (var assetNameToDelete in assetNamesToDelete)
                    Delete(assetNameToDelete);
            }

            bool isDone;
            int totalAssetIndex = 0, index, count, length;
            ulong totalBytesDownload = 0, downloadedBytes;
            long responseCode;
            string url, fullURL, filePath;
            KeyValuePair<string, Asset> pair;
            byte[] md5hash;
            //AssetDownloadHandler assetDownloadHandler;
            Task task;
            //using (var assetDownloadHandler = new AssetDownloadHandler(maxSize, size, numAssets, this, handler))
            using (var md5 = new MD5CryptoServiceProvider())
            {
                foreach (var result in results)
                {
                    assetManager = result.Item3;
                    if (assetManager.__assets.Count < 1)
                        continue;

                    url = assetManager.__path;
                    url = url.Remove(url.LastIndexOf(Path.GetFileName(url)));

                    assets.Clear();
                    assets.AddRange(assetManager.__assets);

                    assetCount = assets.Count;
                    //assetIndex = 0;

                    filePath = result.Item1;
                    folder = result.Item2;
                    if (folderAssetStartBytes != null && folderAssetStartBytes.TryGetValue(folder, out startBytes))
                    {
                        fullURL = assetManager.__path + FILE_SUFFIX_ASSET_PACKAGE;
                        using (var www = UnityWebRequest.Get(fullURL))
                        {
                            var assetDownloadHandler = new AssetDownloadHandler(maxSize, this);

                            //assetDownloadHandler.Clear();
                            www.downloadHandler = assetDownloadHandler;

                            www.SetRequestHeader("Range", $"bytes={startBytes}-");

                            www.SendWebRequest();

                            do
                            {
                                yield return null;

                                responseCode = www.responseCode;
                            } while (responseCode == -1);

                            assetDownloadHandler.Init(
                                responseCode == 206 ? 0 : startBytes,
                                responseCode == 206 ? startBytes : 0,
                                string.IsNullOrEmpty(filePath) ? null : filePath + FILE_SUFFIX_ASSET_PACKAGE,
                                assets);

                            do
                            {
                                isDone = www.isDone;
                                task = Task.Run(() => assetDownloadHandler.ThreadUpdate(isDone, int.MaxValue));
                                do
                                {
                                    yield return null;

                                    if (handler != null)
                                    {
                                        downloadedBytes = assetDownloadHandler.bytesDownloaded;

                                        handler(
                                            assetDownloadHandler.assetName,
                                            assetDownloadHandler.assetProgress,
                                            (uint)downloadedBytes,
                                            totalBytesDownload + assetDownloadHandler.fileBytesDownloaded + downloadedBytes,
                                            size,
                                            totalAssetIndex + assetDownloadHandler.assetCount,
                                            numAssets);
                                    }

                                    var exception = task.Exception;
                                    if (exception != null)
                                    {
                                        Debug.LogException(exception);

                                        break;
                                    }

                                } while (!task.IsCompleted);

                            } while (assetDownloadHandler.isDownloading);

                            assetIndex = assetDownloadHandler.assetCount;
                            totalAssetIndex += assetIndex;
                            totalBytesDownload += assetDownloadHandler.fileBytesDownloaded;
                        }

                        GC.Collect();
                    }
                    else
                        assetIndex = 0;

                    if (!string.IsNullOrEmpty(folder))
                    {
                        length = url.Length;
                        count = folder.Length;
                        if (length > count)
                        {
                            index = length - count - 1;
                            if (url.Substring(index, count).Replace('\\', '/') == folder)
                                url = url.Remove(index);
                        }
                    }

                    if (string.IsNullOrEmpty(filePath))
                    {
                        while (assetIndex < assetCount)
                        {
                            pair = assets[assetIndex++];
                            assetName = pair.Key;
                            destination = pair.Value.data;

                            fullURL = url + assetName;

                            filePath = __GetAssetPath(assetName);
                            CreateDirectory(filePath);

                            while (true)
                            {
                                using (var www = new UnityWebRequest(fullURL, UnityWebRequest.kHttpVerbGET, new DownloadHandlerFile(filePath), null))
                                {
                                    if (handler == null)
                                    {
                                        yield return www.SendWebRequest();

                                        downloadedBytes = www.downloadedBytes;
                                    }
                                    else
                                    {
                                        var asyncOperation = www.SendWebRequest();

                                        do
                                        {
                                            yield return null;

                                            downloadedBytes = www.downloadedBytes;
                                            try
                                            {
                                                handler(
                                                    assetName,
                                                    www.downloadProgress,
                                                    (uint)downloadedBytes,
                                                    totalBytesDownload + downloadedBytes,
                                                    size,
                                                    totalAssetIndex,
                                                    numAssets);
                                            }
                                            catch (Exception e)
                                            {
                                                Debug.LogError(e.InnerException ?? e);
                                            }
                                        } while (!asyncOperation.isDone);
                                    }

                                    if (www.result != UnityWebRequest.Result.Success)
                                    {
                                        Debug.LogError($"{url}{assetName} : { www.error}");

                                        continue;
                                    }
                                }

                                //data = www.downloadHandler?.data;
                                if (!File.Exists(filePath))
                                    continue;

                                md5hash = md5.ComputeHash(File.OpenRead(filePath));

                                if (destination.info.md5 == null)
                                    destination.info.md5 = md5hash;
                                else if (!MemoryEquals(md5hash, destination.info.md5))
                                {
                                    Debug.LogError($"{url}{assetName} MD5 Fail.");

                                    continue;
                                }

                                try
                                {
                                    __Create(assetName, destination);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(e.InnerException ?? e);

                                    continue;
                                }
                                finally
                                {
                                    GC.Collect();
                                }

                                break;
                            }

                            totalBytesDownload += downloadedBytes;

                            ++totalAssetIndex;
                        }
                    }
                    else
                    {
                        filePath = Path.GetDirectoryName(filePath);

                        while (assetIndex < assetCount)
                        {
                            assetName = null;
                            downloadedBytes = 0;

                            if (__assets == null)
                                __assets = new Dictionary<string, Asset>();

                            task = Task.Run(() =>
                            {
                                while (assetIndex < assetCount)
                                {
                                    pair = assets[assetIndex];
                                    assetName = pair.Key;
                                    destination = pair.Value.data;

                                    downloadedBytes = destination.info.size;

                                    destination.overrideOffset = 0;
                                    destination.overridePath = Path.Combine(filePath, Path.GetFileName(assetName));

                                    //__assets[assetName] = destination;
                                    __Create(assetName, destination);

                                    totalBytesDownload += downloadedBytes;

                                    ++totalAssetIndex;

                                    ++assetIndex;
                                }
                            });

                            do
                            {
                                yield return null;

                                var exception = task.Exception;
                                if (exception != null)
                                {
                                    Debug.LogException(exception);

                                    break;
                                }

                                try
                                {
                                    handler(
                                        assetName,
                                        1.0f,
                                        (uint)downloadedBytes,
                                        totalBytesDownload + downloadedBytes,
                                        size,
                                        totalAssetIndex,
                                        numAssets);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogError(e.InnerException ?? e);
                                }
                            } while (!task.IsCompleted);

                            //Save();
                        }
                    }
                }
            }
        }*/

        public IEnumerator<KeyValuePair<string, Asset>> GetEnumerator()
        {
            if (__assets == null)
                __assets = new Dictionary<string, Asset>();

            return __assets.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /*private IEnumerator __Load(string url, string folder, bool isForce)
        {
            __path = url;

            byte[] bytes = null;
            do
            {
                using (UnityWebRequest www = UnityWebRequest.Get(url))
                {
                    yield return www.SendWebRequest();

                    if (www.result == UnityWebRequest.Result.Success)
                        bytes = www.downloadHandler?.data;
                    else
                    {
                        Debug.LogError(url + ':' + www.error);

                        if (!isForce)
                            yield break;
                    }
                }
            } while (bytes == null);

            var assetBundle = AssetBundle.LoadFromMemory(bytes);
            if (assetBundle == null)
            {
                using (MemoryStream memoryStream = new MemoryStream(bytes))
                {
                    __Load(memoryStream, folder, out uint version, ref __assets);

                    this.version = version;
                }

                yield break;
            }

            var assetBundleManifest = assetBundle.LoadAsset<AssetBundleManifest>("assetBundleManifest");
            string[] assetBundleNames = assetBundleManifest == null ? null : assetBundleManifest.GetAllAssetBundles();
            if (assetBundleNames == null)
            {
                assetBundle.Unload(true);

                UnityEngine.Object.Destroy(assetBundle);

                yield break;
            }

            Dictionary<string, AssetInfo> assetInfos = null;
            do
            {
                using (var uwr = UnityWebRequest.Get(url + ".info"))
                {
                    yield return uwr.SendWebRequest();

                    if (uwr.result == UnityWebRequest.Result.Success)
                        assetInfos = LoadAssetInfos(uwr.downloadHandler?.data, out _);
                    else
                        Debug.LogError(url + ':' + uwr.error);
                }
            } while (assetInfos == null);

            Asset asset;
            if (string.IsNullOrEmpty(folder))
            {
                foreach (string assetBundleName in assetBundleNames)
                {
                    if (!assetInfos.TryGetValue(assetBundleName, out asset.data.info))
                    {
                        Debug.LogError($"Missing Asset Bundle {assetBundleName}");

                        continue;
                    }

                    asset.offset = -1L;
                    asset.data.type = AssetType.Compressed;
                    asset.data.fileOffset = 0;
                    asset.data.filePath = null;
                    asset.data.dependencies = assetBundleManifest.GetDirectDependencies(assetBundleName);

                    if (__assets == null)
                        __assets = new Dictionary<string, Asset>();

                    __assets.Add(assetBundleName, asset);
                }
            }
            else
            {
                folder = folder.Replace('\\', '/') + '/';

                int i, numDependencies;
                string dependency;
                foreach (string assetBundleName in assetBundleNames)
                {
                    if (!assetInfos.TryGetValue(assetBundleName, out asset.data.info))
                    {
                        Debug.LogError($"Missing Asset Bundle {assetBundleName}");

                        continue;
                    }

                    asset.offset = -1L;
                    asset.data.type = AssetType.Compressed;
                    asset.data.fileOffset = 0;
                    asset.data.filePath = null;
                    asset.data.dependencies = assetBundleManifest.GetDirectDependencies(assetBundleName);
                    numDependencies = asset.data.dependencies == null ? 0 : asset.data.dependencies.Length;
                    for (i = 0; i < numDependencies; ++i)
                    {
                        dependency = asset.data.dependencies[i];
                        if (!string.IsNullOrEmpty(dependency))
                            asset.data.dependencies[i] = folder + dependency;
                    }

                    if (__assets == null)
                        __assets = new Dictionary<string, Asset>();

                    __assets.Add(folder + assetBundleName, asset);
                }
            }

            assetBundle.Unload(true);
        }*/

        private string __GetManagerPath(string folder)
        {
            return string.IsNullOrEmpty(folder) ? __path : Path.Combine(Path.GetDirectoryName(__path), folder, Path.GetFileName(folder));
        }

        private string __GetAssetPath(string name)
        {
            return Path.Combine(Path.GetDirectoryName(__path), name);
        }
    }
}