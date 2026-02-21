using System;
using System.Buffers.Binary;

namespace laba1New
{
    public class ProdNodeHelper(byte[] rawProdFileData, byte[] rawSpecFileData)
    {
        readonly private byte[] RawProdFileData = rawProdFileData;
        readonly private byte[] RawSpecFileData = rawSpecFileData;
        private int offset = 0;

        private int SpecFreeSpacePtr => SpecHeaderData.FreeSpacePtr(RawSpecFileData);
        private int ProdFreeSpacePtr => ProdHeaderData.FreeSpacePtr(RawProdFileData);
        private int DataSpaceSize => ProdHeaderData.DataSpaceSize(RawProdFileData);

        public void ValidateSpecPtr(int ptr)
        {
            if (ptr < -1 || ptr > SpecFreeSpacePtr)
                throw new InvalidOperationException("Incorrect pointer!");

            if ((ptr - SpecHeaderOffset.TotalOffset) % SpecNodeOffset.TotalOffset != 0)
                throw new ArgumentException("Invalid pointer alignment. The pointer must be exactly at the start of a node.");
        }

        public void ValidateProdPtr(int ptr)
        {
            if (ptr < -1 || ptr > ProdFreeSpacePtr)
                throw new InvalidOperationException("Incorrect pointer!");

            if ((ptr - ProdHeaderOffset.TotalOffset) % ProdNodeOffset.TotalOffset(RawProdFileData) != 0)
                throw new ArgumentException("Invalid pointer alignment. The pointer must be exactly at the start of a node.");
        }

        public ProdNodeHelper SetOffset(int _offset)
        {
            if (_offset == -1)
                throw new ArgumentNullException("Null pointer access. Can't represent node with -1 address");

            ValidateProdPtr(_offset);

            offset = _offset;
            return this;
        }

        public ProdNodeHelper this[int _offset]
        {
            get => SetOffset(_offset);
        }

        public sbyte CanBeDel
        {
            get
            {
                return (sbyte)RawProdFileData[offset + ProdNodeOffset.CanBeDel];
            }
            set
            {
                RawProdFileData[offset + ProdNodeOffset.CanBeDel] = (byte)value;
            }
        }

        public int SpecNodePtr
        {
            get
            {
                return BinaryPrimitives.ReadInt32LittleEndian(RawProdFileData.AsSpan(offset + ProdNodeOffset.SpecNodePtr));
            }
            set
            {
                ValidateSpecPtr(value);

                BinaryPrimitives.WriteInt32LittleEndian(RawProdFileData.AsSpan(offset + ProdNodeOffset.SpecNodePtr), value);
            }
        }

        public SpecNodeHelper? Spec
        {
            get
            {
                if (SpecNodePtr == -1)
                    return null;
                return new SpecNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(SpecNodePtr);
            }
        }

        public int NextNodePtr
        {
            get
            {
                return BinaryPrimitives.ReadInt32LittleEndian(RawProdFileData.AsSpan(offset + ProdNodeOffset.NextNodePtr));
            }
            set
            {
                ValidateProdPtr(value);

                BinaryPrimitives.WriteInt32LittleEndian(RawProdFileData.AsSpan(offset + ProdNodeOffset.NextNodePtr), value);
            }
        }

        public ProdNodeHelper? Next
        {
            get
            {
                if (NextNodePtr == -1)
                    return null;
                return new ProdNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(NextNodePtr);
            }
        }

        public ReadOnlySpan<byte> Data
        {
            get
            {
                return RawProdFileData.AsSpan(offset + ProdNodeOffset.Data, DataSpaceSize);
            }
            set
            {
                if (value.Length > DataSpaceSize)
                    throw new ArgumentException("Name to long!");

                value.CopyTo(RawProdFileData.AsSpan(offset + ProdNodeOffset.Data));

                if (value.Length < DataSpaceSize)
                    RawProdFileData
                        .AsSpan(
                            offset + ProdHeaderOffset.CompDataSize + value.Length,
                            DataSpaceSize - value.Length
                        ).Fill(0);
            }
        }
    }

    public class SpecNodeHelper(byte[] rawProdFileData, byte[] rawSpecFileData)
    {
        readonly private byte[] RawProdFileData = rawProdFileData;
        readonly private byte[] RawSpecFileData = rawSpecFileData;
        private int offset = 0;

        private int SpecFreeSpacePtr => SpecHeaderData.FreeSpacePtr(RawSpecFileData);
        private int ProdFreeSpacePtr => ProdHeaderData.FreeSpacePtr(RawProdFileData);

        public void ValidateSpecPtr(int ptr)
        {
            if (ptr < -1 || ptr > SpecFreeSpacePtr)
                throw new InvalidOperationException("Incorrect pointer!");

            if ((ptr - SpecHeaderOffset.TotalOffset) % SpecNodeOffset.TotalOffset != 0)
                throw new ArgumentException("Invalid pointer alignment. The pointer must be exactly at the start of a node.");
        }

        public void ValidateProdPtr(int ptr)
        {
            if (ptr < -1 || ptr > ProdFreeSpacePtr)
                throw new InvalidOperationException("Incorrect pointer!");

            if ((ptr - ProdHeaderOffset.TotalOffset) % ProdNodeOffset.TotalOffset(RawProdFileData) != 0)
                throw new ArgumentException("Invalid pointer alignment. The pointer must be exactly at the start of a node.");
        }

        public SpecNodeHelper SetOffset(int _offset)
        {
            if (_offset == -1)
                throw new ArgumentNullException("Null pointer access. Can't represent node with -1 address");

            ValidateSpecPtr(_offset);

            offset = _offset;
            return this;
        }

        public SpecNodeHelper this[int _offset]
        {
            get => SetOffset(_offset);
        }

        public sbyte CanBeDel
        {
            get
            {
                return (sbyte)RawSpecFileData[offset + SpecNodeOffset.CanBeDel];
            }
            set
            {
                RawSpecFileData[offset + SpecNodeOffset.CanBeDel] = (byte)value;
            }
        }

        public int ProdNodePtr
        {
            get
            {
                return BinaryPrimitives.ReadInt32LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.ProdNodePtr));
            }
            set
            {
                ValidateProdPtr(value);

                if (value < -1 || value > ProdFreeSpacePtr)
                    throw new InvalidOperationException("Incorrect pointer!");

                BinaryPrimitives.WriteInt32LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.ProdNodePtr), value);
            }
        }

        public ProdNodeHelper? Prod
        {
            get
            {
                if (ProdNodePtr == -1)
                    return null;
                return new ProdNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(ProdNodePtr);
            }
        }

        public ushort Mentions
        {
            get
            {
                return BinaryPrimitives.ReadUInt16LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.Mentions));
            }

            set
            {
                BinaryPrimitives.WriteUInt16LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.Mentions), value);
            }
        }

        public int NextNodePtr
        {
            get
            {
                return BinaryPrimitives.ReadInt32LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.NextNodePtr));
            }
            set
            {
                ValidateSpecPtr(value);

                BinaryPrimitives.WriteInt32LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.NextNodePtr), value);
            }
        }

        public SpecNodeHelper? Next
        {
            get
            {
                if (NextNodePtr == -1)
                    return null;
                return new SpecNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(NextNodePtr);
            }
        }
    }


    public static class SpecHeaderData
    {
        public static int FreeSpacePtr(byte[] specFileData) => BinaryPrimitives.ReadInt32LittleEndian(specFileData.AsSpan(SpecHeaderOffset.FreeSpacePtr));
    }

    public static class ProdHeaderData
    {
        public static int DataSpaceSize(byte[] prodFileData) => BinaryPrimitives.ReadInt32LittleEndian(prodFileData.AsSpan(ProdHeaderOffset.CompDataSize));
        public static int FreeSpacePtr(byte[] prodFileData) => BinaryPrimitives.ReadInt32LittleEndian(prodFileData.AsSpan(ProdHeaderOffset.FreeSpacePtr));
    }

    public static class ProdHeaderOffset
    {
        public const int Signature = 0;
        public const int CompDataSize = 2;
        public const int FirstNodePtr = 4;
        public const int FreeSpacePtr = 8;
        public const int SpecFileName = 12;

        public const int TotalOffset = 28;
    }

    public static class ProdNodeOffset
    {
        public const int CanBeDel = 0;
        public const int SpecNodePtr = 1;
        public const int NextNodePtr = 5;
        public const int Data = 9;

        public static int TotalOffset(byte[] prodFileData) => 9 + ProdHeaderData.DataSpaceSize(prodFileData);
    }

    public static class SpecHeaderOffset
    {
        public const int Signature = 0;
        public const int FirstNodePtr = 3;
        public const int FreeSpacePtr = 7;

        public const int TotalOffset = 11;
    }

    public static class SpecNodeOffset
    {
        public const int CanBeDel = 0;
        public const int ProdNodePtr = 1;
        public const int Mentions = 5;
        public const int NextNodePtr = 7;

        public const int TotalOffset = 11;
    }
}