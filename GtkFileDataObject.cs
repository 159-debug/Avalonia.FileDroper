using Avalonia;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.FileDroper
{
    public class GtkFileDataObject : IDataObject
    {

        public Point Point { get; set; }

        IEnumerable<GtkStorageFile> storageFiles;

        public GtkFileDataObject(string[] files)
        {
            storageFiles = files.Select(x => new GtkStorageFile(new FileInfo(x)));
        }

        public bool Contains(string dataFormat)
        {
            return true;
        }

        public object? Get(string dataFormat)
        {
            return storageFiles.ToArray();
        }

        public IEnumerable<string> GetDataFormats()
        {
            return new List<string>();
        }
    }

    internal class GtkStorageFile : IStorageBookmarkFile
    {
        public GtkStorageFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo ?? throw new ArgumentNullException(nameof(fileInfo));
        }

        public FileInfo FileInfo { get; }

        public string Name => FileInfo.Name;

        public virtual bool CanBookmark => true;

        public Uri Path
        {
            get
            {
                try
                {
                    if (FileInfo.Directory is not null)
                    {
                        return FilePathToUri(FileInfo.FullName);
                    }
                }
                catch (SecurityException)
                {
                }
                return new Uri(FileInfo.Name, UriKind.Relative);
            }
        }

        public static Uri FilePathToUri(string path)
        {
            var uriPath = new StringBuilder(path)
                .Replace("%", $"%{(int)'%':X2}")
                .Replace("[", $"%{(int)'[':X2}")
                .Replace("]", $"%{(int)']':X2}")
                .ToString();

            return new UriBuilder("file", string.Empty) { Path = uriPath }.Uri;
        }

        public Task<StorageItemProperties> GetBasicPropertiesAsync()
        {
            if (FileInfo.Exists)
            {
                return Task.FromResult(new StorageItemProperties(
                    (ulong)FileInfo.Length,
                    FileInfo.CreationTimeUtc,
                    FileInfo.LastAccessTimeUtc));
            }
            return Task.FromResult(new StorageItemProperties());
        }

        public Task<IStorageFolder?> GetParentAsync()
        {
            return Task.FromResult<IStorageFolder?>(null);
        }

        public Task<Stream> OpenReadAsync()
        {
            return Task.FromResult<Stream>(FileInfo.OpenRead());
        }

        public Task<Stream> OpenWriteAsync()
        {
            var stream = new FileStream(FileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.Write);
            return Task.FromResult<Stream>(stream);
        }

        public virtual Task<string?> SaveBookmarkAsync()
        {
            return Task.FromResult<string?>(FileInfo.FullName);
        }

        public Task ReleaseBookmarkAsync()
        {
            // No-op
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        ~GtkStorageFile()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task DeleteAsync()
        {
            FileInfo.Delete();
        }

        public async Task<IStorageItem?> MoveAsync(IStorageFolder destination)
        {
            return null;
        }
    }
}
