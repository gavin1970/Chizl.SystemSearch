using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;

namespace Chizl.SystemSearch
{
    public class FileDetails : IDisposable
    {
        #region Constructor / Deconstructor
        private bool disposedValue;
        internal FileDetails(string fullPath, string md5Hash)
        {
            var fe = new FileInfo(fullPath);
            this.FullPath = fe.FullName;
            this.DirectoryName = fe.DirectoryName;
            this.Filename = fe.Name;
            this.Extension = fe.Extension;

            this.MD5Hash = md5Hash;
            this.MD5Id = CreateId(md5Hash);

            if (fe.Exists)
            {
                this.FileSizeBytes = fe.Length;
                this.CreationTimeUtc = fe.CreationTimeUtc;
                this.ModifiedDateUtc = fe.LastWriteTimeUtc;
            }

            Debug.WriteLine($"{md5Hash}: '{MD5Id}' {fullPath}");
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { }
                disposedValue = true;
            }
        }
        ~FileDetails() => Dispose(disposing: false);
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Public Generated Properties
        [JsonIgnore]
        public bool Exists { get { return File.Exists(FullPath); } }
        [JsonIgnore]
        public DateTime CreationTime { get { return CreationTimeUtc.ToLocalTime(); } }
        [JsonIgnore]
        public DateTime ModifiedDate { get { return ModifiedDateUtc.ToLocalTime(); } }
        #endregion

        #region Public Properties
        public string FullPath { get; } = string.Empty;
        public string DirectoryName { get; } = string.Empty;
        public string Filename { get; } = string.Empty;
        public string Extension { get; } = string.Empty;
        public long FileSizeBytes { get; } = 0;
        public DateTime ModifiedDateUtc { get; } = DateTime.MinValue;
        public DateTime CreationTimeUtc { get; } = DateTime.MinValue;
        public int MD5Id { get; } = 0;
        public string MD5Hash { get; } = string.Empty;
        #endregion

        #region Public Method
        public bool SaveToFile()
        {
            var dir = $"{GlobalSettings.SavePath}{MD5Hash.Substring(0, 2)}\\";
            
            if(!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists($"{dir}{MD5Hash}.json"))
                return true;

            var json = JsonConvert.SerializeObject(this, Formatting.Indented); // Indented for readability
            File.WriteAllText($"{dir}{MD5Hash}.json", json);
            return true;
        }
        #endregion
        #region Private Method
        private int CreateId(string input)
        {
            int id = 0;
            var test = (input.Length % 8);
            while (!string.IsNullOrWhiteSpace(input) && ((input.Length % 8) == 0))
            {
                var part = input.Take(8).ToArray();
                input = input.Substring(8);
                id += int.Parse(string.Join("", part), NumberStyles.HexNumber);
            }
            return id;
        }
        #endregion
    }
}
