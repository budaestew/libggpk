using LibDat.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace LibDat
{
    /// <summary>
    /// Contains list of <c>FieldData</c> read from single record in .dat file
    /// </summary>
    public class RecordData
    {
        private RecordInfo RecordInfo { get; set; }
        private int Index { get; set; }

        private readonly List<FieldData> _fieldsData;
        public ReadOnlyCollection<FieldData> FieldsData
        {
            get { return _fieldsData.AsReadOnly(); }
        }

        /// <summary>
        /// For easy usage
        /// </summary>
        public FieldData this[int index]
        {
            get
            {
                return _fieldsData[index];
            }
        }

        public FieldData this[string key]
        {
            get
            {
                int index = RecordInfo.GetFieldIndex(key);
                if (index == -1)
                    throw new ArgumentException("No field '" + key + "' in " + RecordInfo.FileName);
                return this[index];
            }
        }

        public RecordData(RecordInfo ri, DatReader reader, int index)
        {
            RecordInfo = ri;
            Index = index;
            _fieldsData = new List<FieldData>();

            // Seek to start of record            
            foreach (var fi in RecordInfo.Fields)
            {
                int lastOffset = -1;
                try
                {
                    lastOffset = reader.Position;
                    reader.SeekToField(index, fi.Index);
                    var fieldData = new FieldData(fi, reader);
                    _fieldsData.Add(fieldData);
                }
                catch (Exception e)
                {
                    var error = String.Format(
                        "Error: Row ID = {0} Field Id={1}, Field Type Name = {2}, Offset = {3}," +
                        "\n Message:{4}\n Stacktrace: {5}",
                        Index, fi.Id, fi.FieldType.Name, lastOffset, e.Message, e.StackTrace);
                    throw new Exception(error);
                }
            }
        }
    }
}
