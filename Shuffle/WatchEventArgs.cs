using System.Collections.Generic;
using System.IO;

namespace Shuffle
{
    public class WatchEventArgs
    {

        public WatchEventArgs(FileSystemEventArgs eventArgs)
        {
            Event = eventArgs;
        }

        public FileSystemEventArgs Event { get; set; }

        public List<FileOperation> Operations { get; set; } = new List<FileOperation>();
        
    }
}