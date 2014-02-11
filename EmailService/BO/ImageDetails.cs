using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmailService.BO
{
    public class ImageDetails
    {
        public string CompressedName { get; set; }
        public int Height { get; set; }
        public string FTPImageURL { get; set; }
        //public string LocalImageURL { get; set; }
    }
}
