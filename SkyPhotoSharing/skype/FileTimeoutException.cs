using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class FileTimeoutException : FileTransactionException
    {
        public FileTimeoutException(Enlister en, string mode) :
            base(String.Format(Properties.Resources.ERROR_FILE_TRANSFER_TIMEOUT, en.Name, mode))
        {
        }
    }
}
