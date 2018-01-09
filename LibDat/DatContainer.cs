using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LibDat.Data;
using System.Text.RegularExpressions;

namespace LibDat
{
    /// <summary>
    /// Parses and holds all information found in a specific .dat file.
    /// </summary>
    public class DatContainer
    {
        /// <summary>
        /// Bit type of dat file (.dat64 for 64bit)
        /// </summary>
        public enum DatType { Dat32, Dat64 }

        private readonly DatType _datType;

        /// <summary>
        /// Name of the dat file (without .dat extension)
        /// </summary>
        public readonly string DatName;

        /// <summary>
        /// Length of .dat file
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Returns integer value in first 4 bytes of .dat
        /// </summary>
        public int Count { get; private set; }

        public RecordInfo RecordInfo { get; private set; }

        public int RecordSize { get; private set; }

        /// <summary>
        /// Offset of the data section in the .dat file (Starts with 0xbbbbbbbbbbbbbbbb)
        /// </summary>
        public static int DataSectionOffset { get; private set; }

        /// <summary>
        /// Length of data section of .dat file
        /// </summary>
        public int DataSectionDataLength { get; private set; }

        /// <summary>
        /// Contains the entire .dat file
        /// </summary>
        private byte[] _originalData;

        /// <summary>
        /// List of .dat files records' content
        /// </summary>
        public List<RecordData> Records;

        /// <summary>
        /// Mapping of all known strings and other data found in the data section. 
        /// Key = offset with respect to beginning of data section.
        /// 
        /// </summary>
        public static Dictionary<int, AbstractData> DataEntries { get; private set; }

        public static Dictionary<int, PointerData> DataPointers { get; private set; }

        /// <summary>
        /// Parses the .dat file contents from inStream.
        /// </summary>
        /// <param name="inStream">Unicode binary reader containing ONLY the contents of a single .dat file and nothing more</param>
        /// <param name="fileName">Name of the dat file (with extension)</param>
        public DatContainer(Stream inStream, string fileName)
        {
            _datType = fileName.EndsWith("64") ? DatType.Dat64 : DatType.Dat32;
            DatName = Path.GetFileNameWithoutExtension(fileName);
            RecordInfo = RecordFactory.GetRecordInfo(DatName);
            DataEntries = new Dictionary<int, AbstractData>();
            DataPointers = new Dictionary<int, PointerData>();

            using (var datReader = DatReader.CreateDatReader(_datType, inStream, RecordInfo))
            {
                Read(datReader);
            }
        }

        /// <summary>
        /// Parses the .dat file found at path 'fileName'
        /// </summary>
        /// <param name="filePath">Path of .dat file to parse</param>
        public DatContainer(string filePath)
        {
            _datType = filePath.EndsWith("64") ? DatType.Dat64 : DatType.Dat32;
            DatName = Path.GetFileNameWithoutExtension(filePath);
            RecordInfo = RecordFactory.GetRecordInfo(DatName);
            DataEntries = new Dictionary<int, AbstractData>();
            DataPointers = new Dictionary<int, PointerData>();

            var fileBytes = File.ReadAllBytes(filePath);
            using (var datReader = DatReader.CreateDatReader(_datType, new MemoryStream(fileBytes), RecordInfo))
            {
                Read(datReader);
            }
        }

        /// <summary>
        /// Reads the .dat frile from the specified stream
        /// </summary>
        /// <param name="reader">DatReader containing contents of .dat file</param>
        private void Read(DatReader reader)
        {
            Length = (int)reader.BaseStream.Length;
            DataSectionOffset = 0;
            RecordSize = reader.RecordSize;

            // check that record format is defined
            if (RecordInfo == null)
                throw new Exception("Missing dat parser for file " + DatName);

            Count = reader.ReadInt32();

            // find record_length;
            var actualRecordLength = reader.DetermineRecordLength(Count);
            
            // Data section offset
            DataSectionOffset = Count * actualRecordLength + 4;
            DataSectionDataLength = Length - DataSectionOffset - 8;
            reader.BaseStream.Seek(DataSectionOffset, SeekOrigin.Begin);
            // check magic number
            if (reader.ReadUInt64() != 0xBBbbBBbbBBbbBBbb)
                throw new ApplicationException("Missing magic number after records");

            // save entire stream
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            _originalData = reader.ReadBytes(Length);

            // read records
            if (actualRecordLength == 0)
            {
                Records = new List<RecordData>();
                return;
            }

            Records = new List<RecordData>(Count);
            for (var i = 0; i < Count; i++)
            {
                Records.Add(new RecordData(RecordInfo, reader, i));
            }                        
        }


        /// <summary>
        /// Saves parsed data to specified path
        /// </summary>
        /// <param name="filePath">Path to write contents to</param>
        public void Save(string filePath)
        {
            using (var outStream = File.Open(filePath, FileMode.Create))
            {
                Save(outStream);
            }
        }

        public byte[] SaveAsBytes()
        {
            using (var ms = new MemoryStream())
            {
                Save(ms);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Saves possibly changed data to specified stream
        /// </summary>
        /// <param name="rawOutStream">Stream to write contents to</param>
        public void Save(Stream rawOutStream)
        {
            // Mapping of the new string and data offsets
            var changedStringOffsets = new Dictionary<int, int>();

            var writer = DatWriter.CreateDatWriter(_datType, rawOutStream);
            
            // write original data
            writer.Write(_originalData);

            // append changed strings to the end
            foreach (var item in DataEntries)
            {
                if (!(item.Value is StringData)) continue;

                var str = item.Value as StringData;
                if (string.IsNullOrWhiteSpace(str.NewValue)) continue;

                // actually write changed string
                var newOffset = (int)writer.BaseStream.Position - DataSectionOffset;
                str.Save(writer);
                changedStringOffsets[str.Offset] = newOffset;
            }

            // update pointers to changed string
            foreach (var pData in from keyValuePair in DataPointers
                                  select keyValuePair.Value into pData
                                  where pData.RefData is StringData
                                  let pOffset = pData.RefData.Offset
                                  where changedStringOffsets.ContainsKey(pOffset)
                                  select pData)
            {
                // StringData will write pointer to itself to pointer data
                writer.Seek(DataSectionOffset + pData.Offset, SeekOrigin.Begin);
                pData.RefData.WritePointerOffset(writer, changedStringOffsets[pData.RefData.Offset]);
            }
        }
              
        /// <summary>
        /// returns all non empty StringData referenced from fields marked as "user field" 
        /// directly or through lists or other pointers
        /// </summary>
        /// <returns>list of StringData</returns>
        public IList<StringData> GetUserStrings()
        {
            var offsets = GetUserStringOffsets();
            return offsets.Select(offset => DataEntries[offset]).Cast<StringData>().ToList();
        }

        /// <summary>
        /// returns offsets to all non empty StringData referenced from fields marked as "user field" 
        /// directly or through lists or other pointers
        /// </summary>
        /// <returns>list of StringData offsets</returns>
        public IList<int> GetUserStringOffsets()
        {
            var result = new List<int>();

            // Get field which can contain user strings
            var indexes = RecordInfo.Fields.Where(f => f.IsPointer && f.IsUser).Select(f => f.Index).ToList();

            // Replace the actual strings
            foreach (var fieldData in from recordData in Records from index in indexes select recordData.FieldsData[index])
            {
                FindUserStrings(fieldData.Data, result);
            }
            return result;
        }

        private static void FindUserStrings(AbstractData data, List<int> result)
        {
            if (data == null)
                return;

            if (data is PointerData)
            {
                var pData = data as PointerData;
                FindUserStrings(pData.RefData, result);
            }
            else if (data is ListData)
            {
                var lData = data as ListData;
                foreach (var listEntry in lData.List)
                {
                    FindUserStrings(listEntry, result);
                }
            }
            else if (data is StringData)
            {
                var sData = data as StringData;
                if (!String.IsNullOrEmpty(sData.Value))
                    result.Add(data.Offset);
            }
            // skip any other value data
        }
        
        /// <summary>
        /// Returns a CSV table with the contents of this dat container.
        /// </summary>
        /// <returns></returns>
        public string GetCsvFormat()
        {
            const char separator = ',';
            var sb = new StringBuilder();
            var fieldInfos = RecordInfo.Fields;

            if (RecordSize == 0)
            {
                return sb.AppendFormat("Count").AppendLine().Append(Count).AppendLine().ToString();
            }

            // add header
            sb.AppendFormat("Rows{0}", separator);
            foreach (var field in fieldInfos)
            {
                sb.AppendFormat("{0}{1}", field.Id, separator);
            }
            sb.Remove(sb.Length - 1, 1);
            sb.AppendLine();

            // add records
            foreach (var recordData in Records)
            {
                // row index
                sb.AppendFormat("{0}{1}", Records.IndexOf(recordData), separator);

                // add fields
                foreach (var fieldData in recordData.FieldsData)
                {
                    sb.AppendFormat("{0}{1}", GetCsvString(fieldData), separator);
                }

                // finish line
                sb.Remove(sb.Length - 1, 1);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private static string GetCsvString(FieldData fieldData)
        {
            var str = fieldData.Data.GetValueString();
            if (Regex.IsMatch(str, ",") || Regex.IsMatch(str, "\\n"))
                str = String.Format("\"{0}\"", str.Replace("\"", "\"\""));

            return str;
        }
    }
}
