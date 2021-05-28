using System.Collections.Generic;
using System;

namespace XMLSystem.Xml
{
    public class XmlDocumentNodeCollection : List<XmlDocumentNode>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        public XmlDocumentNodeCollection(IEnumerable<XmlDocumentNode> collection)
            : base(collection)
        { }
        /// <summary>
        /// 
        /// </summary>
        public XmlDocumentNodeCollection()
            : base()
        { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        public XmlDocumentNodeCollection(int capacity)
            : base(capacity)
        { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public XmlDocumentNode this[string name]
        {
            get
            {
                if (Count == 0)
                    return null;
                return this[name, false];
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="registered"></param>
        /// <returns></returns>
        public XmlDocumentNode this[string name, bool registered]
        {
            get
            {
                if (Count == 0)
                    return null;
                if (registered)
                {
                    for (int i = 0; i < base.Count; i++)
                    {
                        if (base[i].Name == name)
                            return base[i];
                    }
                }
                else
                {
                    for (int i = 0; i < base.Count; i++)
                    {
                        if (base[i].Name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                            return base[i];
                    }
                }
                return null;
            }
        }

        [Obsolete("Use 'Add(XmlParserNode parent, XmlParserNode node)'", true)]
        public new void Add(XmlDocumentNode node)
        { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="node"></param>
        public void Add(XmlDocumentNode parent, XmlDocumentNode node)
        {
            node.Parent = parent;
            base.Add(node);
        }
    }
}
