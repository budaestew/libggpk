using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibDat
{
    public abstract class DatWriter : BinaryWriter
    {
        public int Position
        {
            get { return (int)BaseStream.Position; }
            set { BaseStream.Position = value; }
        }

        protected DatWriter(Stream stream)
            : base(stream, Encoding.Unicode)
        {

        }

        public static DatWriter CreateDatWriter(DatContainer.DatType datType, Stream stream)
        {
            if (datType == DatContainer.DatType.Dat32)
                return new DatWriter32(stream);
            else
                return new DatWriter64(stream);
        }

        public override void Write(string value)
        {
            foreach (var ch in value)
            {
                Write(ch);
            }
            Write(0);
        }

        public abstract void WritePointer(long offset);

    }

    public class DatWriter64 : DatWriter
    {
        public DatWriter64(Stream stream)
            : base(stream)
        {

        }

        public override void WritePointer(long offset)
        {
            Write(offset);
        }
    }

    public class DatWriter32 : DatWriter
    {
        public DatWriter32(Stream stream)
            : base(stream)
        {

        }

        public override void WritePointer(long offset)
        {
            Write((int)offset);
        }
    }
}
