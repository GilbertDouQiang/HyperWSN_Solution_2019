using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BootLoaderLibrary
{
    public class BootLoaderEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
