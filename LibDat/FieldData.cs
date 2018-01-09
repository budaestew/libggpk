using System;
using System.Collections.Generic;
using System.IO;
using LibDat.Data;
using LibDat.Types;

namespace LibDat
{
    /// <summary>
    /// contains field data:
    /// 1) field info
    /// 2) actual data read from stream
    /// Basically it's a wrapper around data at field offset
    /// </summary>
    public class FieldData
    {
        public AbstractData Data { get; private set; }

        public FieldInfo FieldInfo { get; private set; }

        public int _width;

        public FieldData(FieldInfo fieldInfo, DatReader reader)
        {
            FieldInfo = fieldInfo;

            var offset = reader.GetDataSectionOffset();
            var dict = new Dictionary<string, object>();
            dict["offset"] = offset;
            Data = TypeFactory.CreateData(fieldInfo.FieldType, reader, dict);
            _width = reader.GetFieldSize(fieldInfo.FieldType);
        }

        /// <summary>
        /// Returns pointer prefix string in format:
        ///     ""                      if field is value type
        ///     "@ = "                  if field's width is 4
        ///     "[length]@offset = "    if field's width is 8
        /// </summary>
        /// <returns></returns>
        public string GetOffsetPrefix()
        {
            if (!FieldInfo.IsPointer) return String.Empty;

            var pData = Data as PointerData;
            if (pData == null)
                throw new Exception("FieldData of pointer type doesn't have data of PointerData class");
            if (_width != 16) return String.Format("@{0}", pData.RefData.Offset);

            var lData = pData.RefData as ListData;
            if (lData == null)
                throw new Exception("Didn't find ListData data at offset of FieldData of pointer to list type");
            return String.Format("[{0}]@{1}", lData.Count, pData.RefData.Offset);
        }
    }
}
