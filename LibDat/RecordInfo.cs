using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LibDat
{
    /// <summary>
    /// contains inforamtion about format of record of .dat file
    /// </summary>
    public class RecordInfo
    {
        /// <summary>
        /// Returns Name (without extension) of .dat file record of which this record describes
        /// </summary>
        public string FileName { get; private set; }

        private readonly List<FieldInfo> _fields;
        public ReadOnlyCollection<FieldInfo> Fields
        {
            get { return _fields.AsReadOnly(); }
        }

        /// <summary>
        /// returns true if record has fields of pointer type
        /// </summary>
        public bool HasPointers { get; private set; }
                
        /// <summary>
        /// Ensure uniqueness of the field names and make it access to field by name quickly
        /// </summary>
        private readonly Dictionary<string, int> _fieldIndexes;

        public int GetFieldIndex(string key)
        {
            return _fieldIndexes.ContainsKey(key) ? _fieldIndexes[key] : -1;
        }

        public RecordInfo(string fileName, List<FieldInfo> fields = null)
        {
            FileName = fileName;
            _fields = fields ?? new List<FieldInfo>();
            HasPointers = _fields.Any(x => x.IsPointer);

            _fieldIndexes = new Dictionary<string, int>(_fields.Count);
            foreach (var field in _fields)
            {
                if (_fieldIndexes.ContainsKey(field.Id))
                    throw new System.ArgumentException("Duplicated key for " + fileName + ":" + field.Id);

                _fieldIndexes.Add(field.Id, field.Index);
            }
        }
    }
}
