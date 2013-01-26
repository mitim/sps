using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkyPhotoSharing
{
    class SkypeApiException : ApplicationException
    {
        public SkypeApiException(string description)
            : base(description)
        {
            
        }
    }
}
