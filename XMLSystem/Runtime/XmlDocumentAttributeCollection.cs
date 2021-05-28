using System.Collections.Generic;
using System;

namespace XMLSystem.Xml
{
    public class XmlDocumentAttributeCollection : List<XmlDocumentAttribute>
    {
        public XmlDocumentAttributeCollection(IEnumerable<XmlDocumentAttribute> collection)
            : base(collection)
        { }
        public XmlDocumentAttributeCollection()
            : base()
        { }
        public XmlDocumentAttributeCollection(int capacity)
            : base(capacity)
        { }

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
                for (int i = 0; i < base.Count; i++)
                {
                    if (registered)
                    {
                        if (base[i].Name == name)
                            return base[i];
                    }
                    else
                    {
                        if (base[i].Name.ToLower() == name.ToLower())
                            return base[i];
                    }
                }
                return null;
            }
        }

        [Obsolete("Данный метод не актуален, используйте 'public void Add(XmlParserNode parent, XmlParserAttribute attr)'", true)]
        public new void Add(XmlDocumentAttribute node)
        { }
        /// <summary>
        /// Добавляет аттрибут, указанному узлу
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="attr"></param>
        public void Add(XmlDocumentNode parent, XmlDocumentAttribute attr)
        {
            attr.parent = parent;
            base.Add(attr);
        }
    }
}
