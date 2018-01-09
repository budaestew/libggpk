using LibDat.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibDat
{
    public abstract class DatReader : BinaryReader
    {
        public abstract int PointerSize { get; }

        public int RecordSize { get; private set; }

        private Dictionary<int, int> _fieldOffset;

        public int Position
        {
            get { return (int)BaseStream.Position; }
            set { BaseStream.Position = value; }
        }

        protected DatReader(Stream stream, RecordInfo recordInfo)
            : base(stream, Encoding.Unicode)
        {
            CalcRecordSize(recordInfo);
        }

        public static DatReader CreateDatReader(DatContainer.DatType datType, Stream stream, RecordInfo recordInfo)
        {
            if (datType == DatContainer.DatType.Dat32)
                return new DatReader32(stream, recordInfo);
            else
                return new DatReader64(stream, recordInfo);
        }

        public int GetFieldSize(BaseDataType dataType)
        {
            if (dataType is PointerDataType pointerDataType)
                return GetFieldSize(pointerDataType.RefType);

            else if (dataType is ListDataType)
                return PointerSize * 2;

            switch (dataType.Name)
            {
                case "string":
                    return PointerSize;
                case "int":
                    return 4;
                case "uint":
                    return PointerSize;
                case "long":
                case "ulong":
                    return PointerSize * 2;
                case "bool":
                case "byte":
                    return 1;
                case "short":
                    return 2;
            }

            throw new InvalidOperationException("you shouldn't be here");
        }

        private void CalcRecordSize(RecordInfo recordInfo)
        {
            RecordSize = 0;
            _fieldOffset = new Dictionary<int, int>();

            int index = 0;
            foreach (var fieldInfo in recordInfo.Fields)
            {
                _fieldOffset.Add(index++, RecordSize);

                RecordSize += GetFieldSize(fieldInfo.FieldType);
            }
        }

        public override string ReadString()
        {
            var sb = new StringBuilder();
            char ch;
            while ((ch = ReadChar()) != 0) { sb.Append(ch); }
            ch = ReadChar();
            if (ch != 0)    // The string must end with int(0)
                throw new Exception("Not found int(0) value at the end of the string");
            return sb.ToString();
        }

        public int DetermineRecordLength(int numberOfEntries)
        {
            if (numberOfEntries == 0)
                return 0;

            BaseStream.Seek(4, SeekOrigin.Begin);
            var stringLength = BaseStream.Length;
            var recordLength = 0;
            for (var i = 0; BaseStream.Position <= stringLength - 8; i++)
            {
                var ul = ReadUInt64();
                if (ul == 0xBBbbBBbbBBbbBBbb)
                {
                    recordLength = i;
                    break;
                }
                BaseStream.Seek(-8 + numberOfEntries, SeekOrigin.Current);
            }
            return recordLength;
        }

        public int GetDataSectionOffset()
        {
            return (int)BaseStream.Position - DatContainer.DataSectionOffset;
        }

        public void SeekToField(int row, int col)
        {
            BaseStream.Position = (4 + RecordSize * row) + _fieldOffset[col];
        }

        public abstract int ReadPointer();
        public abstract uint ReadUInt();
    }

    public class DatReader32 : DatReader
    {
        public override int PointerSize { get { return 4; } }

        public DatReader32(Stream stream, RecordInfo recordInfo)
            : base(stream, recordInfo)
        {

        }

        public override int ReadPointer()
        {
            return ReadInt32();
        }

        public override uint ReadUInt()
        {
            return ReadUInt32();
        }
    }

    public class DatReader64 : DatReader
    {
        public override int PointerSize { get { return 8; } }

        public DatReader64(Stream stream, RecordInfo recordInfo)
            : base(stream, recordInfo)
        {

        }

        public override int ReadPointer()
        {
            long value = ReadInt64();
            return (int)(value != -72340172838076674 ? value : 0);
        }

        public override uint ReadUInt()
        {
            long value = ReadInt64();
            return (uint)(value != -72340172838076674 ? value : 0);
        }
    }

}
