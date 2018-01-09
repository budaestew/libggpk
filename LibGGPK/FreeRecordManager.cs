using LibGGPK.Records;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibGGPK
{
    internal sealed class FreeRecordManager
    {
        private SortedDictionary<uint, List<LinkedListNode<FreeRecord>>> _freeRecordDic;
        private LinkedList<FreeRecord> _freeRecordList;
        private uint _lastDicKey = 0;

        public LinkedList<FreeRecord> List
        {
            get
            {
                return _freeRecordList;
            }
        }

        public void Initialize()
        {
            _freeRecordList = new LinkedList<FreeRecord>();
            _freeRecordDic = new SortedDictionary<uint, List<LinkedListNode<FreeRecord>>>();
        }

        public void Add(FreeRecord freeRecord)
        {
            var node = _freeRecordList.AddLast(freeRecord);

            AddNode(node);
        }

        public long AllocSpace(BinaryWriter bw, uint length)
        {
            long position = 0;
            var freeRecords = GetFreeRecordsByLength(length);
            if (freeRecords != null)
            {
                LinkedListNode<FreeRecord> oldFreeRecordNode = freeRecords[0];
                var oldFreeRecord = oldFreeRecordNode.Value;
                position = oldFreeRecord.RecordBegin;

                if (oldFreeRecord.Length > length)
                {
                    FreeRecord newFreeRecord = new FreeRecord(
                        oldFreeRecord.Length - length,
                        oldFreeRecord.RecordBegin + length,
                        oldFreeRecord.NextFreeOffset);
                    Add(bw, newFreeRecord, oldFreeRecordNode);
                }

                Remove(bw, oldFreeRecordNode);
            }

            return position > 0 ? position : bw.BaseStream.Length;
        }

        public FreeRecord AddFromFileRecord(BinaryWriter bw, FileRecord fileRecord)
        {
            _lastDicKey = 0;

            var freeRecord = new FreeRecord(fileRecord.Length, fileRecord.RecordBegin, 0);

            Add(bw, freeRecord);

            return freeRecord;
        }

        private List<LinkedListNode<FreeRecord>> GetFreeRecordsByLength(uint length)
        {
            _lastDicKey = 0;
            List<LinkedListNode<FreeRecord>> freeRecords = null;
            // Exact length
            if (_freeRecordDic.ContainsKey(length) && _freeRecordDic[length].Count > 0)
            {
                freeRecords = _freeRecordDic[length];
                _lastDicKey = length;
            }
            // Nearest length
            else
            {
                var foundFreeRecords = _freeRecordDic.FirstOrDefault(x => x.Key >= length + 46 && x.Value != null && x.Value.Count > 0);
                if (foundFreeRecords.Value != null)
                {
                    _lastDicKey = foundFreeRecords.Key;
                    freeRecords = foundFreeRecords.Value;
                }
            }
            return freeRecords;
        }

        private void AddNode(LinkedListNode<FreeRecord> node)
        {
            if (!_freeRecordDic.ContainsKey(node.Value.Length))
                _freeRecordDic[node.Value.Length] = new List<LinkedListNode<FreeRecord>>();
            _freeRecordDic[node.Value.Length].Add(node);
        }

        private void Add(BinaryWriter bw, FreeRecord freeRecord, LinkedListNode<FreeRecord> targetNode = null)
        {
            Write(bw, freeRecord);

            LinkedListNode<FreeRecord> node = null;
            if (targetNode == null)
                node = _freeRecordList.AddLast(freeRecord);
            else
                node = _freeRecordList.AddAfter(targetNode, freeRecord);

            if (node.Previous != null)
            {
                node.Previous.Value.NextFreeOffset = freeRecord.RecordBegin;
                Write(bw, node.Previous.Value);
            }

            AddNode(node);
        }

        private void Remove(BinaryWriter bw, LinkedListNode<FreeRecord> oldFreeRecord)
        {
            var nextFreeRecord = oldFreeRecord.Next;
            var previousFreeRecord = oldFreeRecord.Previous;
            if (previousFreeRecord != null)
            {
                previousFreeRecord.Value.NextFreeOffset = nextFreeRecord != null ? nextFreeRecord.Value.RecordBegin : 0;
                Write(bw, previousFreeRecord.Value);
            }

            if (_lastDicKey > 0 && _freeRecordDic.ContainsKey(_lastDicKey))
                _freeRecordDic[_lastDicKey].Remove(oldFreeRecord);

            _freeRecordList.Remove(oldFreeRecord);
        }

        private void Write(BinaryWriter bw, FreeRecord freeRecord)
        {
            bw.BaseStream.Position = freeRecord.RecordBegin;

            freeRecord.Write(bw, null);
        }
    }
}
