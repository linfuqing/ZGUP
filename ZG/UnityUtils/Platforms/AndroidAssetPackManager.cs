using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using System.Xml.Linq;

namespace ZG
{
    public struct AndroidAssetPackLocation
    {
        public ulong offset;

        public string path;

        public bool isVail => !string.IsNullOrEmpty(path);

        public AndroidAssetPackLocation(string path)
        {
            offset = 0;
            this.path = AndroidAssetPacks.GetAssetPackPath(path);
        }
    }

    public class AndroidAssetPackEnumerator : IAssetPackEnumerator
    {
        public const ulong MAX_BYTES_TO_COPY_PER_TIME = 32 * 1024;
        public readonly Task Task;

        public bool isSuccessful
        {
            get
            {
                var exception = Task.Exception;
                if (exception != null)
                {
                    Debug.LogException(exception.InnerException ?? exception);

                    return false;
                }

                return true;
            }
        }

        public float progress
        {
            get;

            private set;
        }

        public AndroidAssetPackEnumerator(ulong locationSize, AndroidAssetPackLocation location, string targetPath)
        {
            Task = Task.Run(() =>
            {
                using (var reader = File.OpenRead(location.path))
                using (var writer = File.OpenWrite(targetPath))
                {
                    ulong offset = 0, step;
                    byte[] bytes = null;

                    reader.Position = (long)location.offset;

                    do
                    {
                        progress = (float)(offset * 1.0 / locationSize);

                        step = Math.Min(MAX_BYTES_TO_COPY_PER_TIME, locationSize - offset);
                        if (bytes == null)
                            bytes = new byte[step];

                        reader.Read(bytes, 0, (int)step);

                        writer.Write(bytes, 0, (int)step);

                        offset += step;
                    } while (offset < locationSize);
                }
            });
        }

        public bool MoveNext()
        {
            return !Task.IsCompleted;
        }

        void IEnumerator.Reset()
        {

        }

        object IEnumerator.Current => null;
    }

    public class AndroidAssetPackHeader : IAssetPackHeader
    {
        public const string NAME_PREFIX = "Android@";

        public readonly string Name;
        public readonly GetAssetPackStateAsyncOperation Operation;

        public bool isDone
        {
            get
            {
                return Operation.isDone;
            }
        }

        public ulong fileSize
        {
            get
            {
                return Operation.size;
            }
        }

        public string filePath => null;

        public string name => GetName(Name);

        public static string GetName(string name) => NAME_PREFIX + name;

        public AndroidAssetPackHeader(GetAssetPackStateAsyncOperation operation)
        {
            Name = name;
            Operation = operation;
        }
    }

    public class AndroidAssetPack : IAssetPack, IAssetPackLocator
    {
        public static RequestToUseMobileDataAsyncOperation __userConfirmationOperation = null;

        public readonly bool IsOverridePath;
        public readonly string Path;
        public readonly string Name;

        private AndroidAssetPackHeader __header;

        public bool isDone
        {
            get;

            private set;
        }

        public AndroidAssetPackStatus status
        {
            get;

            private set;
        }

        public ulong size
        {
            get;

            private set;
        }

        public float downloadProgress
        {
            get;

            private set;
        }

        public IAssetPackHeader header
        {
            get
            {
                if (__header == null)
                    __header = new AndroidAssetPackHeader(AndroidAssetPacks.GetAssetPackStateAsync(new string[] { Name }));

                return __header;
            }
        }

        public bool Contains(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.IndexOf(AndroidAssetPackHeader.NAME_PREFIX) == 0;
        }

        public bool GetFileInfo(
            string name,
            out ulong fileOffset,
            out string filePath)
        {
            string path;
            if (IsOverridePath)
            {
                path = System.IO.Path.GetFileName(name);
                if (!string.IsNullOrEmpty(Path))
                    path = System.IO.Path.Combine(Path, path);
            }
            else
                path = name;

            var location = new AndroidAssetPackLocation(path);
            if (!location.isVail)
            {
                Debug.LogError($"Get Asset Location Failed: {status}, {path}");

                fileOffset = 0;
                filePath = null;

                return false;
            }

            fileOffset = location.offset;
            filePath = path;// location.Path;

            return true;
        }

        public AndroidAssetPack(bool isOverridePath, string path, string name)
        {
            IsOverridePath = isOverridePath;

            Path = path;

            Name = name;

            if(new AndroidAssetPackLocation(name).isVail)
            {
                downloadProgress = 1.0f;

                isDone = true;
            }
            else
                AndroidAssetPacks.DownloadAssetPackAsync(new string[] { name }, __Callback);

            AssetUtility.Register(AndroidAssetPackHeader.GetName(name), this);
        }

        public bool Update(ref string filePath, ref ulong fileOffset)
        {
            var location = new AndroidAssetPackLocation(filePath);
            if (!location.isVail)
            {
                Debug.LogError($"Get Asset Location Failed: {status} : {filePath}");

                return false;
            }

            filePath = location.path;
            fileOffset = location.offset;

            return true;
        }

        public IAssetPackEnumerator Copy(string targetPath, string filePath, ulong fileOffset)
        {
            var location = new AndroidAssetPackLocation(filePath);
            if (!location.isVail)
            {
                Debug.LogError($"Get Asset Location Failed: {status} : {filePath}");

                return null;
            }

            return new AndroidAssetPackEnumerator(size, location, targetPath);
        }

        private void __Callback(AndroidAssetPackInfo androidAssetPackInfo)
        {
            var error = androidAssetPackInfo.error;
            if (error != AndroidAssetPackError.NoError)
                Debug.LogError(error);
            else
            {
                switch (status = androidAssetPackInfo.status)
                {
                    case AndroidAssetPackStatus.Pending:
                    case AndroidAssetPackStatus.Downloading:
                    case AndroidAssetPackStatus.Transferring:
                        size = androidAssetPackInfo.size;

                        downloadProgress = (float)(androidAssetPackInfo.bytesDownloaded * 1.0 / androidAssetPackInfo.size);
                        break;
                    case AndroidAssetPackStatus.Completed:
                        size = androidAssetPackInfo.size;

                        downloadProgress = 1.0f;

                        isDone = true;
                        break;
                    case AndroidAssetPackStatus.WaitingForWifi:

                        if (__userConfirmationOperation == null)
                            __userConfirmationOperation = AndroidAssetPacks.RequestToUseMobileDataAsync();

                        if (__userConfirmationOperation.isDone)
                        {
                            var result = __userConfirmationOperation.result;
                            if(result == null)
                            {
                                // userConfirmationOperation finished with an error. Something went
                                // wrong when displaying the prompt to the user, and they weren't
                                // able to interact with the dialog. In this case, we recommend
                                // developers wait for Wi-Fi before attempting to download again.
                                // You can get more info by calling GetError() on the operation.
                                Application.Quit();
                            }
                            else if(result.allowed)
                            {
                                // User accepted the confirmation dialog - download will start
                                // automatically (no action needed).
                                __userConfirmationOperation = null;
                            }
                            else
                            {
                                // User canceled or declined the dialog. Await Wi-Fi connection, or
                                // re-prompt the user.
                                Application.Quit();
                            }
                        }
                        break;
                    default:

                        Debug.LogError(status);

                        Application.Quit();

                        break;
                }
            }

        }
    }

    public class AndroidAssetPackManager : MonoBehaviour
    {
        [Serializable]
        public struct Pack
        {
            public string name;

            public bool isOverridePath;

            public string packPath;

            public string[] filePaths;
        }

        private class Factory : IAssetPackFactory
        {
            public readonly bool IsOverridePath;
            public readonly string PackPath;
            public readonly string PackName;

            private AndroidAssetPack __pack;

            public Factory(bool isOverridePath, string packPath, string packName)
            {
                IsOverridePath = isOverridePath;
                PackPath = packPath;
                PackName = packName;
            }

            public IAssetPack Retrieve()
            {
                if (__pack == null)
                    __pack = new AndroidAssetPack(IsOverridePath, PackPath, PackName);

                return __pack;
            }
        }

        public Pack[] packs;

        void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if(packs != null)
            {
                Factory factory;
                foreach (var pack in packs)
                {
                    factory = new Factory(pack.isOverridePath, pack.packPath, pack.name);
                    foreach(var filePath in pack.filePaths)
                        AssetUtility.Register(filePath, factory);
                }
            }
#endif
        }
    }
}