﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace TACT.Net.Common
{
    internal class BoolArrayV2 : ICollection<bool>
    {
        private byte[] _bytes;

        #region Constructors

        public BoolArrayV2(BinaryReader br, uint count)
        {
            Count = (int)count;
            _bytes = br.ReadBytes((Count + 7) / 8);
        }

        public BoolArrayV2(uint count)
        {
            Count = (int)count;
            _bytes = new byte[(Count + 7) / 8];
        }

        #endregion

        #region Methods

        public bool this[int index]
        {
            get => (_bytes[index / 8] & GetMask(index)) != 0;
            set
            {
                // expand the collection automatically
                int diff = ((index + 7) / 8) - _bytes.Length;
                if (diff > 0)
                    Array.Resize(ref _bytes, _bytes.Length + diff);

                // (un)set the bit
                if (value)
                    _bytes[index / 8] |= (byte)GetMask(index);
                else
                    _bytes[index / 8] &= (byte)~GetMask(index);
            }
        }

        public void Add(bool v) => this[++Count] = v;

        public void Remove(int index) => BlitAndRotate(index);

        public void Clear()
        {
            _bytes = new byte[0];
            Count = 0;
        }

        public byte[] ToByteArray() => _bytes;

        #endregion

        #region Interface Methods

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        public bool Contains(bool item) => throw new NotImplementedException();

        public bool Remove(bool item) => throw new NotImplementedException();

        public void CopyTo(bool[] array, int arrayIndex)
        {
            for (int i = 0; i < Count; i++)
                array[arrayIndex + i] = this[i];
        }

        public IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region Helpers

        /// <summary>
        /// Purges a specific bit by rotating the whole array from the specified index
        /// </summary>
        /// <param name="index">Global bit index</param>
        private void BlitAndRotate(int index)
        {
            if (index < 0)
                throw new ArgumentException("Index must be >= 0");

            // blit the specified bit from the target byte
            int mask = ~0 << (7 - (index % 8));
            index /= 8;
            _bytes[index] = (byte)(((_bytes[index] << 1) & ~mask) | ((_bytes[index] << 1) & mask));

            // shift all the proceeding bytes to fill the gap
            for (int i = ++index; i < _bytes.Length; i++)
            {
                // transpose the last bit of the current byte to the first of the previous
                _bytes[i - 1] |= (byte)((_bytes[i] & 0x80) >> 7);
                _bytes[i] <<= 1;
            }

            // update the count and resize the array if necessary
            if ((--Count + 7) / 8 < _bytes.Length)
                Array.Resize(ref _bytes, _bytes.Length - 1);
        }

        private int GetMask(int index) => 1 << 7 - (index % 8);

        #endregion

    }
}
