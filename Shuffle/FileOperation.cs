using System;
using System.IO;

namespace Shuffle
{
    public class FileOperation {

        #region Equality 
        protected bool Equals(FileOperation other)
        {
            return Type == other.Type && string.Equals(SourceFilePath, other.SourceFilePath) && string.Equals(DestinationPathFile, other.DestinationPathFile);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileOperation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Type;
                hashCode = (hashCode * 397) ^ (SourceFilePath != null ? SourceFilePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DestinationPathFile != null ? DestinationPathFile.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion


        public FileOperation(FileOperationType type, string source, string destinationPath)
        {
            Type = type;
            SourceFilePath = source;
            DestinationPathFile = destinationPath;
        }

        public override string ToString()
        {
            return $"{Type} [Source: {SourceFileName}, Destination: {DestinationDirectory}]";
        }

        public FileOperationType Type { get; internal set; }

        public string SourceFilePath { get; internal set; }

        public string DestinationPathFile { get; internal set; }

        public string SourceFileName => Path.GetFileName(SourceFilePath);

        public string SourceDirectory => Path.GetDirectoryName(SourceFilePath);

        public string DestinationFileName => Path.GetFileName(DestinationPathFile);

        public string DestinationDirectory => Path.GetDirectoryName(DestinationPathFile);

        public void Execute()
        {
            Console.WriteLine($" --> {this}");

            switch (Type)
            {
                case FileOperationType.Copy:
                    try
                    {

                        if (Directory.Exists(SourceFilePath))
                        {
                            return;
                        }

                        var dir = Path.GetDirectoryName(DestinationPathFile);
                        if (dir != null && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        File.Copy(SourceFilePath, DestinationPathFile, true);
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Failed:{ex.Message}");
                    }
                    break;
                case FileOperationType.Delete:
                    // not supported yet.
                    break;
                case FileOperationType.Rename:
                    // not supported yet.
                    break;
            }

        }

        /// <summary>
        /// True to only perform operation if the file is newer than the target.
        /// </summary>
        public bool CopyIfNewer { get; set; }
    }
}