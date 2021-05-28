
namespace XMLSystem.Xml
{
    public class XmlDocumentAttribute
    {
        internal XmlDocumentNode parent;
        internal string name;
        internal string value;
        internal object objValue;

        /// <summary>
        /// Возвращает узел, к которому пренадлежит аттрибут
        /// </summary>
        public XmlDocumentNode Node { get { return parent; } }

        /// <summary>
        /// Возвращает или задает имя аттрибута (! без проверки на правильный формат)
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    (Node as IXmlDocumentNode).OnRaiseChangeEvent(Node);
                    name = value;  
                }
            }
        }
        //&amp; (&), &lt; (<), &gt; (>), &apos; (') и &quot; (") 
        /// <summary>
        /// Принимает строку и преобразует ее в правильный вид, если это необходимо.<para/>
        /// Возвращает уже отформатированную строку
        /// <para>res = res.Replace("&amp;amp;", "&amp;");</para>
        /// <para>res = res.Replace("&amp;lt;", "&lt;");</para>
        /// <para>res = res.Replace("&amp;gt;", "&gt;");</para>
        /// <para>res = res.Replace("&amp;apos;", "'");</para>
		///	<para>res = res.Replace("&amp;quot;", "\"");</para>
        /// <para>res = res.Replace("&amp;nl;", "\n");</para>
        /// <para>res = res.Replace("&amp;#10;", "\n");</para>
        /// <para>res = res.Replace("&amp;#160;", " ");</para>
        /// </summary>
        public string Value
        {
            get
            {
                string res = value;

                if (value.Contains("&"))
                {
                    res = res.Replace("&amp;", "&");
                    res = res.Replace("&lt;", "<");
                    res = res.Replace("&gt;", ">");
                    res = res.Replace("&apos;", "'");
					res = res.Replace("&quot;", "\"");
                    res = res.Replace("&nl;", "\n");
                    res = res.Replace("&#10;", "\n");
                    res = res.Replace("&#160;", " ");
                }

                return res;
            }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    this.value = this.value.Replace("<", "&lt;");
                    this.value = this.value.Replace(">", "&gt;");
                    this.value = this.value.Replace("'", "&apos;");
                    this.value = this.value.Replace("\"", "&quot;");
                    this.value = this.value.Replace("\n", "&#10;");
                    this.value = this.value.Replace(" ", "&#160;");
                    if(Node != null)
                        (Node as IXmlDocumentNode).OnRaiseChangeEvent(Node);
                }
            }
        }

        /// <summary>
        /// JSON тип
        /// </summary>
        public object ObjValue
        {
            get { return objValue; } 
            set 
            {
                if (objValue != value)
                {
                    objValue = value;
                    (Node as IXmlDocumentNode).OnRaiseChangeEvent(Node);
                }
            } 
        }

        public XmlDocumentAttribute(XmlDocumentNode parent, string name, string value)
            //: this(name, value)
        {
            this.parent = parent;
            this.name = name;
            this.Value = value;
        }
        public XmlDocumentAttribute(XmlDocumentNode parent, string name, object value)
        //: this(name, value)
        {
            this.parent = parent;
            this.name = name;
            this.Value = value == null ? "" : value.ToString();
        }
        //public XmlDocumentAttribute(string name, string value)
        //{
        //    this.name = name;
        //    this.Value = value;
        //}
        //public XmlDocumentAttribute(string name, object value)
        //{
        //    this.name = name;
        //    this.Value = value == null ? "" : value.ToString();
        //}

        public override string ToString()
        {
            return string.Format("{0}='{1}'", name, value);
        }

    }
}
