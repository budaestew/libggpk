using System.Collections.Generic;
using System.IO;

namespace LibDat.Types
{
    /// <summary>
    /// Type that represents "list" data: sequence of one ore more data of the same BaseDataType derived type
    /// </summary>
    public class ListDataType : BaseDataType
    {
        /// <summary>
        /// type of data in the list
        /// </summary>
        public BaseDataType ListType { get; private set; }

        public ListDataType(string name, BaseDataType listType)
            : base(name)
        {
            ListType = listType;
        }

        public override Dictionary<string, object> ReadPointer(DatReader reader)
        {
            var dict = new Dictionary<string, object>();
            var count = reader.ReadPointer();
            var offset = reader.ReadPointer();
            dict["count"] = count;
            dict["offset"] = offset;
            return dict;
        }
    }
}
