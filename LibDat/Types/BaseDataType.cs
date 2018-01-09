using System.Collections.Generic;
using System.IO;

namespace LibDat.Types
{
    /// <summary>
    /// represents type of any data found in .dat file
    /// </summary>
    public class BaseDataType
    {
        /// <summary>
        /// Type name for identification
        /// </summary>
        public string Name { get; private set; }

        public BaseDataType(string name)
        {
            Name = name;
        }

        /// <summary>
        /// reads offset parameters to data of this type
        /// this method is called from constructor of PointerData instance
        /// </summary>
        /// <param name="reader">stream to read from</param>
        /// <returns>List of parameters required to read data of this type</returns>
        public virtual Dictionary<string, object> ReadPointer(DatReader reader)
        {
            var dict = new Dictionary<string, object>();
            var offset = reader.ReadPointer();
            dict["offset"] = offset;
            return dict;
        }
    }
}