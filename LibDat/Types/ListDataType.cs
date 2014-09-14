﻿namespace LibDat.Types
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

        public ListDataType(string name, int width, int pointerWidth, BaseDataType listType) 
            : base(name, width, pointerWidth)
        {
            ListType = listType;
        }
    }
}
