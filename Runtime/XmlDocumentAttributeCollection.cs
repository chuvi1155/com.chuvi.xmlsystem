using System.Collections.Generic;
using System;
using System.Collections;

namespace XMLSystem.Xml
{
    public class XmlDocumentAttributeCollection : IList<XmlDocumentAttribute>
    {
        List<XmlDocumentAttribute> list;

        public int Count => ((ICollection<XmlDocumentAttribute>)list).Count;

        public bool IsReadOnly => ((ICollection<XmlDocumentAttribute>)list).IsReadOnly;

        public XmlDocumentAttribute this[int index] { get => ((IList<XmlDocumentAttribute>)list)[index]; set => ((IList<XmlDocumentAttribute>)list)[index] = value; }

        public XmlDocumentAttribute this[string name]
        {
            get
            {
                return this[name, false];
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="registered">Учитывать регист?</param>
        /// <returns></returns>
        public XmlDocumentAttribute this[string name, bool registered]
        {
            get
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (registered)
                    {
                        if (list[i].Name == name)
                            return list[i];
                    }
                    else
                    {
                        if (list[i].Name.ToLower() == name.ToLower())
                            return list[i];
                    }
                }
                return null;
            }
        }

        public XmlDocumentAttributeCollection(IEnumerable<XmlDocumentAttribute> collection)
            //: base(collection)
        { list = new List<XmlDocumentAttribute>(collection); }
        public XmlDocumentAttributeCollection()
            //: base()
        { list = new List<XmlDocumentAttribute>(); }
        public XmlDocumentAttributeCollection(int capacity)
            //: base(capacity)
        { list = new List<XmlDocumentAttribute>(capacity); }

        /// <summary>
        /// Добавляет аттрибут, указанному узлу
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="attr"></param>
        public void Add(XmlDocumentNode parent, XmlDocumentAttribute attr)
        {
            attr.parent = parent;
            list.Add(attr);
        }

        public void AddRange(IEnumerable<XmlDocumentAttribute> attrs)
        {
            list.AddRange(attrs);
        }
        public List<TOutput> ConvertAll<TOutput>(Converter<XmlDocumentAttribute, TOutput> converter)
        {
            return list.ConvertAll(converter);
        }

        public List<XmlDocumentAttribute> FindAll(Predicate<XmlDocumentAttribute> match)
        {
            return list.FindAll(match);
        }
        public bool Remove(XmlDocumentAttribute attr)
        {
            if (list.Remove(attr))
            {
                attr.parent.OnRaiseRemoveEvent(attr.parent, attr);
                return true;
            }
            return false;
        }

        public int IndexOf(XmlDocumentAttribute item)
        {
            return list.IndexOf(item);
        }

        void IList<XmlDocumentAttribute>.Insert(int index, XmlDocumentAttribute item)
        {
            list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            var attr = list[index];
            list.RemoveAt(index);
            attr.parent.OnRaiseRemoveEvent(attr.parent, attr);
        }

        void ICollection<XmlDocumentAttribute>.Add(XmlDocumentAttribute item)
        { }

        public void Clear()
        {
            var temp = new List<XmlDocumentAttribute>(list);
            list.Clear();
            foreach (var attr in temp)
            {
                attr.parent.OnRaiseRemoveEvent(attr.parent, attr);
            }
        }

        public bool Contains(XmlDocumentAttribute item)
        {
            return list.Contains(item);
        }

        public void CopyTo(XmlDocumentAttribute[] array, int arrayIndex)
        {
            ((ICollection<XmlDocumentAttribute>)list).CopyTo(array, arrayIndex);
        }

        bool ICollection<XmlDocumentAttribute>.Remove(XmlDocumentAttribute item)
        {
            return Remove(item);
        }

        public IEnumerator<XmlDocumentAttribute> GetEnumerator()
        {
            return ((IEnumerable<XmlDocumentAttribute>)list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)list).GetEnumerator();
        }
    }
}
