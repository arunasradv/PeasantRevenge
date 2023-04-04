using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BastardsMod.Helpers
{
    public abstract class XmlCfg
    {
        public virtual void Save(string file_name, object obj)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            TextWriter writer = new StreamWriter(file_name);
            serializer.Serialize(writer, obj);
            writer.Close();
        }

        public virtual object Load(string file_name, Type type)
        {
            object retval = new object();

            var mySerializer = new XmlSerializer(type);
            using (var myFileStream = new FileStream(file_name, FileMode.Open))
            {
                retval = (object)mySerializer.Deserialize(myFileStream);
            }

            return retval;
        }
    }
}
