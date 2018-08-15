using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;

namespace Hyperwsn.Comm
{
    public class SaveObject2File
    {
        public void Save(object source,string filename)
         {
            
           using (FileStream fs = new FileStream(filename, FileMode.CreateNew))
           {
                XmlSerializer serializer = new XmlSerializer(source.GetType());
                serializer.Serialize(fs, source);
            }
        }


    }
}
