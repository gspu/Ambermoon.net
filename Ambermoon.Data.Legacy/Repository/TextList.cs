﻿using Ambermoon.Data.Legacy.Repository.Entities;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Repository
{
    public class TextList : List<string>
    {
        public TextList()
        {
        }

        public TextList(IEnumerable<string> texts)
            : base(texts)
        {
        }
    }

    public class TextList<T> : TextList, IIndexedEntity where T : IIndexedEntity
    {
        public TextList(T associatedItem)
        {
            AssociatedItem = associatedItem;
            Index = associatedItem.Index;
        }

        public TextList(T associatedItem, IEnumerable<string> texts)
            : base(texts)
        {
            AssociatedItem = associatedItem;
            Index = associatedItem.Index;
        }

        public T AssociatedItem { get; }
        public uint Index { get; set; }
    }
}
