using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class FileSendException : ApplicationException
    {
        public FileSendException(string file, Enlister en)
           : base(String.Format(Properties.Resources.ERROR_FILE_CANT_SEND, file, en.Name))
        {
        }
    }
}
