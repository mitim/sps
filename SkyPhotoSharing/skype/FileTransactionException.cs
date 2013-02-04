using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    abstract class FileTransactionException : ApplicationException
    {
        public FileTransactionException(string message) :
            base(message)
        {
        }
    }
}
