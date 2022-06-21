using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace XMLSystem.Xml
{
    public enum Conditions
    {
        Equal,
        NotEqual,
        Great,
        Less,
        GreatOrEqual,
        LessOrEqual,
        StartWith,
        EndWith,
        None
    }
    /// <summary>
    /// 
    /// </summary>
    public class XmlDocumentNode : IXmlDocumentNode, IEnumerable<XmlDocumentNode>
    {
        protected string name;
        protected XmlDocumentAttributeCollection attributes;
        protected XmlDocumentNodeCollection childNodes;
        protected string innerText = "";
        protected string comments = "";
        protected XmlDocumentNode parent;
        protected XNodeType nodeType = XNodeType.None;
        static Regex patternTag;
        static Regex patternTagName;
        static Regex patternAttr;
        static Regex patternCloseTag;
        static Regex patternComment;
        static Regex patternInnerText;
        static Regex patternJSONTag;
        static Regex patternJSONInnerText;
        private Dictionary<string, string> mathes;
        private int key = 0;
        private bool isHTML = false;
        private static ICustomConverter converter;

        public delegate void NodeAction(IXmlDocumentNode node);
        public delegate void ElementNodeAction(IXmlDocumentNode node, object element);

        public event NodeAction AddNode;
        public event ElementNodeAction AddNodeElement;
        public event NodeAction RemoveNode;
        public event ElementNodeAction RemoveNodeElement;
        public event NodeAction ChangeNode;
        public event ElementNodeAction ChangeNodeElement;

        public static CultureInfo CurrentCulture { get; set; } = new CultureInfo("en-US");

        /// <summary>
        /// 
        /// </summary>
        public object Tag;


        /// <summary>
        /// Возвращает этотже узел, если его имя совпадает с указанным(без учета регистра),
        /// в противном случае ищет в дочерних узлах
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public XmlDocumentNode this[string name]
        {
            get
            {
                //if (this.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                //    return this;
                //return ChildNodes[name];
                for (int i = 0; i < ChildNodes.Count; i++)
                {
                    if (ChildNodes[i].name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return ChildNodes[i];
                }
                return null;
            }
        }
        /// <summary>
        /// Возвращает дочерний узел по индексу
        /// </summary>
        /// <param name="indx"></param>
        /// <returns></returns>
        public XmlDocumentNode this[int indx]
        {
            get
            {
                return ChildNodes[indx];
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string InnerText
        {
            get
            {
                return XmlDocument.DecodeToXML(innerText);
            }
            set
            {
                if (innerText != value)
                {
                    innerText = value;
                    OnRaiseChangeEvent(this, null);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public XNodeType NodeType 
        {
            get { return nodeType; } 
            set 
            {
                if (nodeType != value)
                {
                    nodeType = value;
                    OnRaiseChangeEvent(this, null);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public bool IsHTML
        {
            get { return isHTML; }
        }
        public IXmlDocumentNode Root
        {
            get 
            {
                var parent = this.parent;
                if (parent == null) return null;
                while (parent.parent != null)
                {
                    parent = parent.parent;
                }
                return parent as IXmlDocumentNode;
            }
        }
        /// <summary>
        /// Возвращает родительский узел
        /// </summary>
        public XmlDocumentNode Parent 
        {
            get { return parent; } 
            internal set 
            {
                parent = value; 
            } 
        }
        /// <summary>
        /// Возвращает или задает имя узла
        /// </summary>
        public string Name { get { return name; } set { name = value; } }
        /// <summary>
        /// Возвращает список дочерних узлов
        /// </summary>
        public XmlDocumentNodeCollection ChildNodes { get { return childNodes; } }
        /// <summary>
        /// Возвращает список атрибутов текущего узла
        /// </summary>
        public XmlDocumentAttributeCollection Attributes { get { return attributes; } }
        /// <summary>
        /// Возвращает коментарии в данном узле
        /// </summary>
        public string Comentaries { get { return comments; } }
        /// <summary>
        /// Возвращает количество дочерних узлов
        /// </summary>
        public int Count
        {
            get { return ChildNodes.Count; }
        }
        /// <summary>
        /// Текущий конвертер, который используется в методах 'GetInnerTextValue' и 'GetValue'
        /// </summary>
        public static ICustomConverter Converter { get { return converter; } set { converter = value; } }

        static XmlDocumentNode()
        {
            if(converter == null)
                converter = new UnityConverter();
            patternTag = new Regex(@"[\s\n\t\r]*\<(?<val>.+?)\>");
            patternTagName = new Regex(@"^[\s\n\t\r]*(?<val>\w+)\s?");
            patternAttr = new Regex("(?<name>\\w*)\\s*=\\s*(?:\"(?<val>[^\"]*)\"|(?<val>\\S+))");
            patternCloseTag = new Regex(@"\<(?<val>/.*?)\>|\<(?<val1>.+\s*/)\>");
            patternInnerText = new Regex(@"\G(?<val>.*?)\<");
            patternComment = new Regex(@"\G[\s\n\t\r]*\<!--(?<val>.+?)--\>");

            // JSON
            patternJSONTag = new Regex("[\\s\\n\\t\\r\\[\\{]*\"(?<val>\\w+?)\"");
            patternJSONInnerText = new Regex("(:\"(?<val>.+)\",+[^\\}\\]\\)])|(:\"(?<val1>.+)\")|(?<val2>\\w+)"); // :((?<val>\"[\\w\\s]*\")|(?<val1>\"\\[\\{.*\\}\\]\")|(?<val2>\\w+))
            //patternJSONInnerTextArr = new Regex(":((?<val>\"[\\w\\s]*\")|(?<val1>\"\\[(?<val3>(\\{.*\\}))*\\]\\\")|(?<val2>\\w+))");
        }

        internal XmlDocumentNode(XmlDocumentNode parent)
        {
            this.parent = parent;
            attributes = new XmlDocumentAttributeCollection(10);
            childNodes = new XmlDocumentNodeCollection(10);
        }
        int indent = 0;
        internal bool Parse(string nodeText, ref int start)
        {
            // ищем текст находящийся в тэгах
            Match itemFullTag = patternTag.Match(nodeText, start);
            if (itemFullTag.Success)
            {
                string tag = itemFullTag.Groups["val"].Value;

                Match itemTagName = patternTagName.Match(tag);
                if (itemTagName.Success)// нашли открытый тэг
                {
                    string nameTag = itemTagName.Groups["val"].Value; // получаем имя тэга
                    this.name = nameTag.Replace(";::nl::;", "").Replace(";::tab::;", "");
                    MatchCollection itemsAttr = patternAttr.Matches(tag); // ищем атрибуты
                    if (itemsAttr.Count > 0)
                    {
                        attributes = new XmlDocumentAttributeCollection(itemsAttr.Count);
                        foreach (Match itemAttr in itemsAttr)
                        {
                            if (itemAttr.Success)
                            {
                                string name = itemAttr.Groups["name"].Value.Trim().Replace(";::nl::;", "\n").Replace(";::tab::;", "\t");
                                string value = itemAttr.Groups["val"].Value.Replace(";::nl::;", "\n").Replace(";::tab::;", "\t");
                                AddAttribute(new XmlDocumentAttribute(this, name, value));
                            }
                        }
                    }
                    if (patternCloseTag.IsMatch(itemFullTag.Value)) // нашли закрывающий тэг
                    {
                        start = itemFullTag.Index + itemFullTag.Length;
                        return true;
                    }
                    else
                    {
                        start = itemFullTag.Index + itemFullTag.Length;
                        //string t = nodeText.Substring(start);
                        Match itemInnerText = patternInnerText.Match(nodeText, start);
                        if (itemInnerText.Success)
                        {
                            Group g = itemInnerText.Groups["val"];
                            //nodeText = nodeText.Substring(g.Index + g.Length).TrimStart();
                            start = g.Index + g.Length;
                            if (g.Value.Trim() != string.Empty)
                            {
                                string[] lines = g.Value.Split('\n');
                                string escaped_space = new string(' ', 2 * (indent + 1));
                                for (int i = 0; i < lines.Length; i++)
                                {
#if NET_STANDARD_2_0
                                    string[] splitLine = lines[i].Split(new string[] { escaped_space }, StringSplitOptions.RemoveEmptyEntries);
#else
                                    string[] splitLine = lines[i].Split(escaped_space); 
#endif
                                    for (int i1 = 0; i1 < splitLine.Length; i1++)
                                    {
                                        splitLine[i1] = splitLine[i1].TrimStart().Replace(";::tab::;", "");
                                    }
                                    lines[i] = string.Join("\n", splitLine);
                                }
                                string xml = XmlDocument.DecodeToXML(string.Join("\n", lines));
                                innerText = XmlDocument.EncodeToInnerText(xml);
                                if (string.IsNullOrEmpty(innerText.Trim()))
                                    innerText = "";
                            }
                        }


                        XmlDocumentNode child = new XmlDocumentNode(this);
                        while (child.Parse(nodeText, ref start))
                        {
                            if (child.nodeType != XNodeType.Coment)
                                AddChildNode(child);
                            child = new XmlDocumentNode(this);
                            child.indent = indent + 1;
                        }
                        return true;
                    }
                }
                else // ищем любые другие строки не похожие на открытый тэг
                {
                    Match itemComment = patternComment.Match(nodeText, start);
                    if (itemComment.Success)
                    {
                        Group g = itemComment.Groups["val"];
                        comments += g.Value.Replace(";--nl--;", "").Replace(";--tab--;", "");
                        nodeType = XNodeType.Coment;
                        name = "Comment";
                        start = itemComment.Index + itemComment.Length;
                        return true;
                    }
                    else if (patternCloseTag.IsMatch(itemFullTag.Value)) // нашли закрывающий тэг
                    {
                        start = itemFullTag.Index + itemFullTag.Length;
                        return false;
                    }
                    else
                    {
                        start = itemFullTag.Index + itemFullTag.Length;
                    }
                }
            }
            return false;
        }
        internal bool ParseJSON(string nodeText, ref int start)
        {
            // ищем текст находящийся в тэгах
            Match itemFullTag = patternJSONTag.Match(nodeText, start);
            if (itemFullTag.Success)
            {
                string tag = itemFullTag.Groups["val"].Value;

                start = itemFullTag.Index + itemFullTag.Length;
                Match matchValue = patternJSONInnerText.Match(nodeText, start);
                if (matchValue.Success)
                {
                    this.name = tag;// нашли открытый тэг
                    Group val = matchValue.Groups["val"];
                    Group val1 = matchValue.Groups["val1"];
                    Group val2 = matchValue.Groups["val2"];
                    if (val.Success)
                    {
                        start = val.Index + val.Length;
                        innerText = val.Value;
                        nodeType = XNodeType.JSONString;
                    }
                    else if (val1.Success)
                    {
                        start = matchValue.Index + matchValue.Length;
                        innerText = val1.Value;
                        nodeType = XNodeType.JSONString;
                    }
                    else if (val2.Success)
                    {
                        start = matchValue.Index + matchValue.Length;
                        innerText = val2.Value;
                        nodeType = XNodeType.JSONOther;
                    }
                    nodeText = nodeText.Substring(start);
                    return true;
                }
            }
            return false;
        }
        internal bool ParseHtml(ref string nodeText)
        {
            isHTML = true;
            if (nodeText.StartsWith("</"))
            {
                nodeText = nodeText.Remove(0, nodeText.IndexOf(">") + 1).Trim();
                return false;
            }
            int indxStartNode = nodeText.IndexOf('<');
            int indxCloseNode = nodeText.IndexOf('>');

            string attrStr = nodeText.Substring(indxStartNode + 1, indxCloseNode - indxStartNode - 1);
            if (attrStr.Contains(" "))
            {
                int indx = nodeText.IndexOf(' ');
                name = attrStr.Substring(0, indx - 1).ToLower();
                attrStr = attrStr.Remove(0, name.Length).Trim();
                // find attributes
                while (attrStr.Contains("="))
                {
                    int indexEq = attrStr.IndexOf('=');
                    string nameAttr = attrStr.Substring(0, indexEq).Trim();
                    int indexQuote1 = attrStr.IndexOf('"');
                    int indexQuote2 = attrStr.IndexOf('"', indexQuote1 + 1);
                    string valAttr = attrStr.Substring(indexQuote1 + 1, indexQuote2 - indexQuote1 - 1);
                    AddAttribute(new XmlDocumentAttribute(this, nameAttr, valAttr));
                    attrStr = attrStr.Remove(0, indexQuote2 + 1);
                }
            }
            else name = attrStr.Trim('/', ' ', '\n', '\r', '\t', '\b');
            // find child nodes
            if (name.ToLower() == "br") // в html этот тэг часто идет без закрытия, поэтому считаем его закрытым
            {
                nodeText = nodeText.Remove(indxStartNode, indxCloseNode - indxStartNode + 1).Trim();
                return true;
            }
            if (!attrStr.Contains("/")) // если не нашли закрывающий тэг, то разбираем дочерние строки
            {
                nodeText = nodeText.Remove(indxStartNode, indxCloseNode - indxStartNode + 1).Trim();
                while (!nodeText.StartsWith("</" + name + ">")) // если это закрывающий тэг
                {
                    if (nodeText.StartsWith("<")) // новый узел
                    {
                        if (nodeText.StartsWith("<!--")) // коментарий
                        {
                            XmlDocumentNode childNode = new XmlDocumentNode(this);
                            childNode.nodeType = XNodeType.Coment;
                            childNode.name = "Coment";
                            childNode.comments = nodeText.Substring(4, nodeText.IndexOf("-->"));
                            AddChildNode(childNode);

                            nodeText = nodeText.Remove(0, nodeText.IndexOf("-->") + 3).Trim();
                        }
                        else
                        {
                            XmlDocumentNode childNode = new XmlDocumentNode(this);
                            if (childNode.ParseHtml(ref nodeText))
                                AddChildNode(childNode);
                        }
                    }
                    else // inner text
                    {
                        XmlDocumentNode childNode = new XmlDocumentNode(this);
                        childNode.nodeType = XNodeType.InnerText;
                        childNode.name = "InnerText";
                        childNode.innerText = nodeText.Substring(0, nodeText.IndexOf("<")).Trim('\n', '\r', '\t');
                        AddChildNode(childNode);

                        nodeText = nodeText.Remove(0, nodeText.IndexOf("<")).Trim();
                    }
                }
                nodeText = nodeText.Remove(0, ("</" + name + ">").Length).Trim();
            }
            else
            {
                nodeText = nodeText.Remove(indxStartNode, indxCloseNode - indxStartNode + 1).Trim();
            }
            return true;
        }

        /// <summary>
        /// Добавляет дочерний узел
        /// </summary>
        /// <param name="node"></param>
        public void AddChildNode(XmlDocumentNode node)
        {
            childNodes.Add(this, node);
            OnRaiseAddEvent(this, null);
        }
        /// <summary>
        /// Добавляет дочерний узел
        /// </summary>
        /// <param name="node"></param>
        /// <param name="type">Тип узла, указывается только в случае если после узел должен вернуть запись в формате JSON или HTML</param>
        public void AddChildNode(XmlDocumentNode node, XNodeType type)
        {
            node.nodeType = type;
            childNodes.Add(this, node);
            node.AddNode += (n) => OnRaiseAddEvent(n, null);
            node.AddNodeElement += OnRaiseAddEvent;
            OnRaiseAddEvent(this, null);
        }
        /// <summary>
        /// Вставляет дочерний узел
        /// </summary>
        /// <param name="index"></param>
        /// <param name="node"></param>
        public void InsertChildNode(int index, XmlDocumentNode node)
        {
            node.parent = this;
            childNodes.Insert(index, node);
            OnRaiseAddEvent(this, null);
        }
        /// <summary>
        /// Добавляет атрибут
        /// </summary>
        /// <param name="attribute"></param>
        public void AddAttribute(XmlDocumentAttribute attribute)
        {
            attributes.Add(this, attribute);
            OnRaiseAddEvent(this, attribute);
        }
        /// <summary>
        /// Добавляет атрибут
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddAttribute(string key, string value)
        {
            var attr = new XmlDocumentAttribute(this, key, value);
            attributes.Add(this, attr);
            OnRaiseAddEvent(this, attr);
        }
        /// <summary>
        /// Добавляет атрибут
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">будет вызван метод ToString()</param>
        public void AddAttribute(string key, object value)
        {
            var attr = new XmlDocumentAttribute(this, key, value);
            attributes.Add(this, attr);
            OnRaiseAddEvent(this, attr);
        }
        /// <summary>
        /// Осуществляет поиск узлов по пути XPath
        /// </summary>
        /// <param name="xPath"></param>
        /// <returns>В случае, если ни один узел не найден, возвращает список нулевой длины</returns>
        public List<XmlDocumentNode> SelectNodes(string xPath)
        {
            mathes = new Dictionary<string, string>();
            List<XmlDocumentNode> list = new List<XmlDocumentNode>();
            Regex regex = new Regex(@"\[.*?\]");
            string replace = regex.Replace(xPath, MatchEvaluatorMethod);
            replace = replace.Replace("//", "/*/");
            string[] split = replace.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            bool all = false;

            list.Add(this);

            for (int i = 0; i < split.Length; i++)
            {
                foreach (var item in mathes)
                    split[i] = split[i].Replace(item.Key, item.Value);

                // parse
                if (!all && split[i].StartsWith("*")) all = true;

                ParseXPath(list, split[i], all);
                all = split[i].StartsWith("*");
            }
            mathes.Clear();
            mathes = null;
            return list;
        }
        /// <summary>
        /// Осуществляет поиск узла по пути XPath и возвращает первый попавшийся
        /// </summary>
        /// <param name="xPath"></param>
        /// <returns>Если узел не найден, возвращает null</returns>
        public XmlDocumentNode SelectNode(string xPath)
        {
            List<XmlDocumentNode> collection = SelectNodes(xPath);
            if (collection.Count > 0) return collection[0];
            else return null;
        }
        /// <summary>
        /// Осуществляет поиск узла по пути XPath и возвращает первый попавшийся
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="node"></param>
        /// <returns>Если узел не найден, возвращает false</returns>
        public bool TrySelectNode(string xPath, out XmlDocumentNode node)
        {
            node = null;
            List<XmlDocumentNode> collection = SelectNodes(xPath);
            if (collection.Count > 0)
            {
                node = collection[0];
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Осуществляет поиск узлов по пути XPath
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="node"></param>
        /// <returns>Если узел не найден, возвращает false</returns>
        public bool TrySelectNodes(string xPath, out List<XmlDocumentNode> collection)
        {
            collection = SelectNodes(xPath);
            if (collection.Count > 0) return true;
            else return false;
        }
        /// <summary>
        /// Осуществляет поиск узлов по имени в текущем узле
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        /// <param name="name"></param>
        public void GetAllNodes(List<XmlDocumentNode> list, List<XmlDocumentNode> result, string name)
        {
            int n = 0;
            if (isHTML)
            {
                while (n < list.Count)
                {
                    if (list[n].name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        result.Add(list[n]);
                    GetAllNodes(list[n].childNodes, result, name);
                    n++;
                }
            }
            else
            {
                while (n < list.Count)
                {
                    if (list[n].name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        result.Add(list[n]);
                    GetAllNodes(list[n].childNodes, result, name);
                    n++;
                }
            }
        }
        /// <summary>
        /// Осуществляет поиск дочерних узлов, которые содержат атрибуты
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        public void GetAllAttributes(List<XmlDocumentNode> list, List<XmlDocumentNode> result)
        {
            int n = 0;
            while (n < list.Count)
            {
                if (list[n].attributes.Count > 0)
                    result.Add(list[n]);
                GetAllAttributes(list[n].childNodes, result);
                n++;
            }
        }
        /// <summary>
        /// Осуществляет поиск дочерних узлов, которые содержат атрибуты с указанным значением
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void GetAllAttributes(List<XmlDocumentNode> list, List<XmlDocumentNode> result, string name, string value, Conditions condition)
        {
            if (string.IsNullOrEmpty(name) || name == "*")
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (string.IsNullOrEmpty(value) && list[i].Attributes.Count > 0) 
                        result.Add(list[i]);
                    else
                    {
                        bool isadd = true;
                        for (int i1 = 0; i1 < list[i].Attributes.Count; i1++)
                        {
                            #region check condition
                            if (condition == Conditions.Equal)
                            {
                                isadd = false;
                                if (list[i].Attributes[i1].Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                                {
                                    isadd = true;
                                    break;
                                }
                            }
                            else if (condition == Conditions.NotEqual)
                            {
                                if (list[i].Attributes[i1].Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                                {
                                    isadd = false;
                                    break;
                                }
                            }
                            else if (condition == Conditions.Great)
                            {
                                isadd = false;
                                if (!string.IsNullOrEmpty(list[i].attributes[i1].Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(list[i].attributes[i1].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                    {
                                        if (resFloat1 > resFloat2)
                                        {
                                            isadd = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (condition == Conditions.Less)
                            {
                                isadd = false;
                                if (!string.IsNullOrEmpty(list[i].attributes[i1].Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(list[i].attributes[i1].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                    {
                                        if (resFloat1 < resFloat2)
                                        {
                                            isadd = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (condition == Conditions.GreatOrEqual)
                            {
                                isadd = false;
                                if (!string.IsNullOrEmpty(list[i].attributes[i1].Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(list[i].attributes[i1].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                    {
                                        if (resFloat1 >= resFloat2)
                                        {
                                            isadd = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (condition == Conditions.LessOrEqual)
                            {
                                isadd = false;
                                if (!string.IsNullOrEmpty(list[i].attributes[i1].Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(list[i].attributes[i1].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                    {
                                        if (resFloat1 <= resFloat2)
                                        {
                                            isadd = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                        if (isadd)
                        result.Add(list[i]);
                    }
                }
            }
            else
            {
                int n = 0;
                if (isHTML)
                {
                    while (n < list.Count)
                    {
                        if (list[n].attributes.Count > 0)
                        {
                            if (string.IsNullOrEmpty(value))
                            {
                                for (int i = 0; i < list[n].attributes.Count; i++)
                                    if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                                        result.Add(list[n]);
                            }
                            else
                            {
                                #region check condition
                                if (condition == Conditions.Equal)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                        if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && list[n].attributes[i].Value == value)
                                            result.Add(list[n]);
                                }
                                else if (condition == Conditions.NotEqual)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                        if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && list[n].attributes[i].Value != value)
                                            result.Add(list[n]);
                                }
                                else if (condition == Conditions.Great)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(list[n].attributes[i].Value))
                                        {
                                            float resFloat1, resFloat2;
                                            if (float.TryParse(list[n].attributes[i].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                                if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 > resFloat2) result.Add(list[n]);
                                        }
                                    }
                                }
                                else if (condition == Conditions.Less)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(list[n].attributes[i].Value))
                                        {
                                            float resFloat1, resFloat2;
                                            if (float.TryParse(list[n].attributes[i].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                                if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 < resFloat2) result.Add(list[n]);
                                        }
                                    }
                                }
                                else if (condition == Conditions.GreatOrEqual)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(list[n].attributes[i].Value))
                                        {
                                            float resFloat1, resFloat2;
                                            if (float.TryParse(list[n].attributes[i].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                                if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 >= resFloat2) result.Add(list[n]);
                                        }
                                    }
                                }
                                else if (condition == Conditions.LessOrEqual)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(list[n].attributes[i].Value))
                                        {
                                            float resFloat1, resFloat2;
                                            if (float.TryParse(list[n].attributes[i].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                                if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 <= resFloat2) result.Add(list[n]);
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                        GetAllAttributes(list[n].childNodes, result, name, value, condition);
                        n++;
                    }
                }
                else
                {
                    while (n < list.Count)
                    {
                        if (list[n].attributes.Count > 0)
                        {
                            if (string.IsNullOrEmpty(value))
                            {
                                for (int i = 0; i < list[n].attributes.Count; i++)
                                    if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                                        result.Add(list[n]);
                            }
                            else
                            {
                                #region check condition
                                if (condition == Conditions.Equal)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                        if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && list[n].attributes[i].Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                                            result.Add(list[n]);
                                }
                                else if (condition == Conditions.NotEqual)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                        if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !list[n].attributes[i].Value.Equals(value, StringComparison.OrdinalIgnoreCase))
                                            result.Add(list[n]);
                                }
                                else if (condition == Conditions.Great)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(list[n].attributes[i].Value))
                                        {
                                            float resFloat1, resFloat2;
                                            if (float.TryParse(list[n].attributes[i].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                                if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 > resFloat2) result.Add(list[n]);
                                        }
                                    }
                                }
                                else if (condition == Conditions.Less)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(list[n].attributes[i].Value))
                                        {
                                            float resFloat1, resFloat2;
                                            if (float.TryParse(list[n].attributes[i].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                                if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 < resFloat2) result.Add(list[n]);
                                        }
                                    }
                                }
                                else if (condition == Conditions.GreatOrEqual)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(list[n].attributes[i].Value))
                                        {
                                            float resFloat1, resFloat2;
                                            if (float.TryParse(list[n].attributes[i].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                                if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 >= resFloat2) result.Add(list[n]);
                                        }
                                    }
                                }
                                else if (condition == Conditions.LessOrEqual)
                                {
                                    for (int i = 0; i < list[n].attributes.Count; i++)
                                    {
                                        if (!string.IsNullOrEmpty(list[n].attributes[i].Value))
                                        {
                                            float resFloat1, resFloat2;
                                            if (float.TryParse(list[n].attributes[i].Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                                if (list[n].attributes[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 <= resFloat2) result.Add(list[n]);
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                        GetAllAttributes(list[n].childNodes, result, name, value, condition);
                        n++;
                    }
                }
            }
        }
        /// <summary>
        /// Осуществляет поиск ближайших дочерних узлов, которые содержат атрибуты
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public List<XmlDocumentNode> GetAttributes(List<XmlDocumentNode> list, string name)
        {
            return GetAttributes(list, name, null, Conditions.None);
        }
        /// <summary>
        /// Осуществляет поиск ближайших дочерних узлов, которые содержат атрибуты с указанным значением
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public List<XmlDocumentNode> GetAttributes(List<XmlDocumentNode> list, string name, string value, Conditions condition)
        {
            XmlDocumentAttributeCollection res = new XmlDocumentAttributeCollection();
            if (isHTML)
            {
                #region check conditions
                if (condition == Conditions.Equal)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && val.Value == value));
                    }
                }
                else if (condition == Conditions.NotEqual)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && val.Value != value));
                    }
                }
                else if (condition == Conditions.Great)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else
                        {
                            for (int i1 = 0; i1 < list[i].attributes.Count; i1++)
                            {
                                XmlDocumentAttribute attr = list[i].attributes[i1];
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(attr.Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                        if (attr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 > resFloat2) res.Add(attr.parent, attr);
                                }
                            }
                        }
                    }
                }
                else if (condition == Conditions.Less)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else
                        {
                            for (int i1 = 0; i1 < list[i].attributes.Count; i1++)
                            {
                                XmlDocumentAttribute attr = list[i].attributes[i1];
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(attr.Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                        if (attr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 < resFloat2) res.Add(attr.parent, attr);
                                }
                            }
                        }
                    }
                }
                else if (condition == Conditions.GreatOrEqual)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else
                        {
                            for (int i1 = 0; i1 < list[i].attributes.Count; i1++)
                            {
                                XmlDocumentAttribute attr = list[i].attributes[i1];
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(attr.Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                        if (attr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 >= resFloat2) res.Add(attr.parent, attr);
                                }
                            }
                        }
                    }
                }
                else if (condition == Conditions.LessOrEqual)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else
                        {
                            for (int i1 = 0; i1 < list[i].attributes.Count; i1++)
                            {
                                XmlDocumentAttribute attr = list[i].attributes[i1];
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(attr.Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                        if (attr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 <= resFloat2) res.Add(attr.parent, attr);
                                }
                            }
                        }
                    }
                }  
                #endregion
            }
            else
            {
                #region check conditions
                if (condition == Conditions.Equal)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && val.Value.Equals(value, StringComparison.OrdinalIgnoreCase)));
                    }
                }
                else if (condition == Conditions.NotEqual)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && !val.Value.Equals(value, StringComparison.OrdinalIgnoreCase)));
                    }
                }
                else if (condition == Conditions.Great)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else
                        {
                            for (int i1 = 0; i1 < list[i].attributes.Count; i1++)
                            {
                                XmlDocumentAttribute attr = list[i].attributes[i1];
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(attr.Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                        if (attr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 > resFloat2) res.Add(attr.parent, attr);
                                }
                            }
                        }
                    }
                }
                else if (condition == Conditions.Less)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else
                        {
                            for (int i1 = 0; i1 < list[i].attributes.Count; i1++)
                            {
                                XmlDocumentAttribute attr = list[i].attributes[i1];
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(attr.Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                        if (attr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 < resFloat2) res.Add(attr.parent, attr);
                                }
                            }
                        }
                    }
                }
                else if (condition == Conditions.GreatOrEqual)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else
                        {
                            for (int i1 = 0; i1 < list[i].attributes.Count; i1++)
                            {
                                XmlDocumentAttribute attr = list[i].attributes[i1];
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(attr.Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                        if (attr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 >= resFloat2) res.Add(attr.parent, attr);
                                }
                            }
                        }
                    }
                }
                else if (condition == Conditions.LessOrEqual)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (value == null) res.AddRange(list[i].Attributes.FindAll(val => val.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
                        else
                        {
                            for (int i1 = 0; i1 < list[i].attributes.Count; i1++)
                            {
                                XmlDocumentAttribute attr = list[i].attributes[i1];
                                if (!string.IsNullOrEmpty(attr.Value))
                                {
                                    float resFloat1, resFloat2;
                                    if (float.TryParse(attr.Value, out resFloat1) && float.TryParse(value, out resFloat2))
                                        if (attr.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && resFloat1 <= resFloat2) res.Add(attr.parent, attr);
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            return new XmlDocumentNodeCollection(res.ConvertAll<XmlDocumentNode>(new Converter<XmlDocumentAttribute, XmlDocumentNode>(val => val.parent)));
        }
        /// <summary>
        /// Убивает ссылки на родительские узлы и очищает список всех дочерних узлов и атрибутов
        /// </summary>
        public void Dispose()
        {
            AddNode = null;
            AddNodeElement = null;
            parent = null;
            attributes.Clear();
            attributes = null;
            childNodes.Clear();
            childNodes = null;
        }
        /// <summary>
        /// (перегружен)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(false);
        }
        /// <summary>
        /// (перегружен)
        /// </summary>
        /// <param name="smalWrite">Вывести сокращенную запись</param>
        /// <returns></returns>
        public string ToString(bool smalWrite)
        {
            StringBuilder res = new StringBuilder();
            ToStringChild("", res, this, smalWrite);
            return res.ToString();
        }
        /// <summary>
        /// Возвращает строку в формате JSON
        /// </summary>
        /// <returns></returns>
        public string ToJSON()
        {
            StringBuilder res = new StringBuilder();
            if (parent == null)
            {
                res.Append('{');
                for (int i = 0; i < childNodes.Count; i++)
                {
                    res.AppendFormat("\"{0}\":{2}{1}{2}{3}", childNodes[i].name, childNodes[i].innerText, childNodes[i].nodeType == XNodeType.JSONString ? "\"" : "", i == childNodes.Count - 1 ? "" : ",");
                }
                res.Append('}');
            }
            else
                res.AppendFormat("\"{0}\":{2}{1}{2}", name, innerText, nodeType == XNodeType.JSONString ? "\"" : "");
            return res.ToString();
        }

        private string WriteRecursive(XmlDocumentNode node, string tab)
        {
            StringBuilder res = new StringBuilder();
            for (int i = 0; i < node.childNodes.Count; i++)
            {
                XmlDocumentNode _node = node.childNodes[i];

                ToStringChild(tab, res, _node, false);
            }
            return res.ToString();
        }

        private void ToStringChild(string tab, StringBuilder res, XmlDocumentNode _node, bool smallWrite)
        {
            res.AppendFormat(tab + "<{0}", _node.name);
            for (int i1 = 0; i1 < _node.attributes.Count; i1++)
                res.AppendFormat(" {1}=\"{2}\"", i1, _node.attributes[i1].Name, _node.attributes[i1].Value);
            if (_node.childNodes.Count > 0 || _node.innerText != "")
            {
                res.Append(">");
                if (_node.innerText != "")
                    res.AppendFormat("{0}", _node.innerText);
                if (_node.childNodes.Count > 0)
                {
                    if (smallWrite)
                        res.Append(".....\n");
                    else
                    {
                        res.Append("\n");
                        res.Append(WriteRecursive(_node, tab + "  "));
                        res.AppendFormat("{0}</{1}>\n", tab, _node.Name);
                    }
                }
                else res.AppendFormat("</{0}>\n", _node.Name);
            }
            else res.AppendFormat("/>\n");
        }
        private void ParseXPath(List<XmlDocumentNode> list, string split, bool all)
        {
            if (string.IsNullOrEmpty(split))
            {
                list.Clear();
                return;
            }
            char c0 = split[0];
            char c1 = split[split.Length > 1 ? 1 : 0];
            if (c0 == '*')
            {
                if (split.Length > 1)
                    ParseXPath(list, split.Substring(1), all);
                else if (list.Count > 0)
                {
                    XmlDocumentNodeCollection _list = new XmlDocumentNodeCollection(list.Count);
                    for (int i = 0; i < list.Count; i++)
                        for (int i1 = 0; i1 < list[i].ChildNodes.Count; i1++)
                            _list.Add(list[i], list[i].ChildNodes[i1]);
                    list.Clear();
                    list.AddRange(_list);
                    _list.Clear();
                    _list.TrimExcess();
                }
            }
            else if (c0 == '[')
            {
                #region code
                if (c1 == '@') // ищем все атрибуты
                {
                    #region attributes
                    string splitSub = split.Substring(2, split.IndexOf(']') - 2);
                    string atrName = null;
                    string atrVal = null;
                    Conditions condition = Conditions.Equal;
                    int end = splitSub.IndexOf("=");

                    CheckCondition(splitSub, ref condition, ref end);

                    if (end >= 0) // поиск @attr1=@attr2 пока что отсекается
                    {
                        int indx1 = splitSub.IndexOf('\'');
                        int indx2 = splitSub.IndexOf('\'', indx1 + 1);
                        if (indx1 >= 0 && indx2 >= 0)
                            atrVal = splitSub.Substring(indx1 + 1, indx2 - indx1 - 1);
                        else condition = Conditions.None;
                    }
                    if (end >= 0)
                        atrName = splitSub.Substring(0, end).Trim();
                    List<XmlDocumentNode> _list = new List<XmlDocumentNode>(list.Count);
                    if (all) GetAllAttributes(list, _list, atrName, atrVal, condition);
                    else _list = GetAttributes(list, atrName, atrVal, condition);
                    list.Clear();
                    list.AddRange(_list);
                    _list.Clear(); 
                    #endregion
                }
                else
                {
                    string splitSub = split.Substring(1, split.IndexOf(']') - 1);
                    int int_res;
                    if (int.TryParse(splitSub, out int_res)) // это строка типа [2](только не забываем, что перед этим я уже удалил квадратные скобки)
                    {
                        int count = list.Count;
                        if (count > 0)
                        {
                            if (list[0].ChildNodes.Count > int_res)
                            {
                                if (all)
                                    list.Add(list[0].ChildNodes[int_res]);
                                else if (int_res < count)
                                    list.Add(list[int_res]);
                                list.RemoveRange(0, count);
                            }
                            else list.Clear();
                        }
                    }
                    else // это строка типа [naim/diam/price] (только не забываем, что перед этим я уже удалил квадратные скобки)
                    {
                        string[] _split = splitSub.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < _split.Length; i++)
                            ParseXPath(list, _split[i], all);
                    }
                } 
                #endregion
            }
            else if (c0 == '@') // ищем все атрибуты
            {
                #region attributes
                if (c1 == '*')
                {
                    XmlDocumentNodeCollection _list = new XmlDocumentNodeCollection();
                    GetAllAttributes(list, _list);
                    list.Clear();
                    list.AddRange(_list);
                }
                else
                {
                    string splitSub = split.Substring(1);
                    string atrName;
                    string atrVal = null;
                    Conditions condition = Conditions.Equal;
                    int end = splitSub.IndexOf('=');
                    CheckCondition(splitSub, ref condition, ref end);
                    if (end >= 0) // поиск @attr1=@attr2 пока что отсекается
                    {
                        int indx1 = splitSub.IndexOf('\'');
                        int indx2 = splitSub.IndexOf('\'', indx1 + 1);
                        atrVal = splitSub.Substring(indx1 + 1, indx2 - indx1 - 1).Trim();
                    }
                    atrName = splitSub.Substring(0, end).Trim();
                    List<XmlDocumentNode> _list = new List<XmlDocumentNode>(list.Count);
                    if (all)
                        GetAllAttributes(list, _list, atrName, atrVal, condition);
                    else
                        _list = GetAttributes(list, atrName, atrVal, condition);
                    list.Clear();
                    list.AddRange(_list);
                    _list.Clear();
                } 
                #endregion
            }
            else
            {
                XmlDocumentNodeCollection _list = new XmlDocumentNodeCollection(list.Count);
                if (isHTML)
                {
                    #region html
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].name.Equals(split, StringComparison.OrdinalIgnoreCase))
                            _list.Add(list[i].parent, list[i]);
                        else if (all)
                        {
                            XmlDocumentNodeCollection __list = new XmlDocumentNodeCollection(list[i].childNodes);
                            ParseXPath(__list, split, all);
                            _list.AddRange(__list);
                        }
                        else
                        {
                            XmlDocumentNodeCollection __list = new XmlDocumentNodeCollection(list[i].childNodes);
                            ParseXPath(__list, split, false);
                            _list.AddRange(__list);
                        }
                    } 
                    #endregion
                }
                else 
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].name.Equals(split, StringComparison.OrdinalIgnoreCase))
                            _list.Add(list[i].parent, list[i]);
                        else if (all)
                        {
                            XmlDocumentNodeCollection __list = new XmlDocumentNodeCollection(list[i].childNodes);
                            ParseXPath(__list, split, all);
                            _list.AddRange(__list);
                        }
                        else
                        {
                            XmlDocumentNodeCollection __list = new XmlDocumentNodeCollection(list[i].childNodes);
                            ParseXPath(__list, split, false);
                            _list.AddRange(__list);
                        }
                    }
                }
                list.Clear();
                list.AddRange(_list);
                _list.Clear();
            }
        }
        private void ParseXPath2(List<XmlDocumentNode> list, string split, bool all)
        {
            if (string.IsNullOrEmpty(split))
            {
                list.Clear();
                return;
            }
            char c0 = split[0];
            char c1 = split[1];
            if (c0 == '*')
            {
                if (split.Length > 1)
                    ParseXPath2(list, split.Substring(1), all);
                else if (list.Count > 0)
                {
                    XmlDocumentNodeCollection _list = new XmlDocumentNodeCollection(list.Count);
                    for (int i = 0; i < list.Count; i++)
                        for (int i1 = 0; i1 < list[i].ChildNodes.Count; i1++)
                            _list.Add(list[i], list[i].ChildNodes[i1]);
                    list.Clear();
                    list.AddRange(_list);
                    _list.Clear();
                    _list.TrimExcess();
                }
            }
            else if (c0 == '[')
            {
                if (c1 == '@') // ищем все атрибуты
                {
                    string splitSub = split.Substring(2, split.IndexOf(']') - 2);
                    string atrName = null;
                    string atrVal = null;
                    Conditions condition = Conditions.Equal;
                    int end = splitSub.IndexOf("=");

                    CheckCondition(splitSub, ref condition, ref end);

                    if (end >= 0) // поиск @attr1=@attr2 пока что отсекается
                    {
                        int indx1 = splitSub.IndexOf('\'');
                        int indx2 = splitSub.IndexOf('\'', indx1 + 1);
                        if (indx1 >= 0 && indx2 >= 0)
                            atrVal = splitSub.Substring(indx1 + 1, indx2 - indx1 - 1);
                        else condition = Conditions.None;
                    }
                    if (end >= 0)
                        atrName = splitSub.Substring(0, end).Trim();
                    List<XmlDocumentNode> _list = new List<XmlDocumentNode>(list.Count);
                    if (all) GetAllAttributes(list, _list, atrName, atrVal, condition);
                    else _list = GetAttributes(list, atrName, atrVal, condition);
                    list.Clear();
                    list.AddRange(_list);
                    _list.Clear();
                }
                else
                {
                    string splitSub = split.Substring(1, split.IndexOf(']') - 1);
                    int int_res;
                    if (int.TryParse(splitSub, out int_res)) // это строка типа [2](только не забываем, что перед этим я уже удалил квадратные скобки)
                    {
                        int count = list.Count;
                        if (count > 0)
                        {
                            if (all)
                                list.Add(list[0].ChildNodes[int_res]);
                            else if (int_res < count)
                                list.Add(list[int_res]);
                            list.RemoveRange(0, count);
                        }
                    }
                    else // это строка типа [naim/diam/price] (только не забываем, что перед этим я уже удалил квадратные скобки)
                    {
                        string[] _split = splitSub.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < _split.Length; i++)
                            ParseXPath2(list, _split[i], all);
                    }
                }
            }
            else if (c0 == '@') // ищем все атрибуты
            {
                if (c1 == '*')
                {
                    XmlDocumentNodeCollection _list = new XmlDocumentNodeCollection();
                    GetAllAttributes(list, _list);
                    list.Clear();
                    list.AddRange(_list);
                }
                else
                {
                    string splitSub = split.Substring(1);
                    string atrName;
                    string atrVal = null;
                    Conditions condition = Conditions.Equal;
                    int end = splitSub.IndexOf('=');
                    CheckCondition(splitSub, ref condition, ref end);
                    if (end >= 0) // поиск @attr1=@attr2 пока что отсекается
                    {
                        int indx1 = splitSub.IndexOf('\'');
                        int indx2 = splitSub.IndexOf('\'', indx1 + 1);
                        atrVal = splitSub.Substring(indx1 + 1, indx2 - indx1 - 1).Trim();
                    }
                    atrName = splitSub.Substring(0, end).Trim();
                    List<XmlDocumentNode> _list = new List<XmlDocumentNode>(list.Count);
                    if (all)
                        GetAllAttributes(list, _list, atrName, atrVal, condition);
                    else
                        _list = GetAttributes(list, atrName, atrVal, condition);
                    list.Clear();
                    list.AddRange(_list);
                    _list.Clear();
                }
            }
            else
            {
                XmlDocumentNodeCollection _list = new XmlDocumentNodeCollection(list.Count);
                if (isHTML)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].name.Equals(split, StringComparison.OrdinalIgnoreCase))
                            _list.Add(list[i].parent, list[i]);
                        else if (all)
                        {
                            List<XmlDocumentNode> __list = new List<XmlDocumentNode>(list[i].childNodes);
                            ParseXPath2(__list, split, all);
                            _list.AddRange(__list);
                        }
                        else
                        {
                            List<XmlDocumentNode> __list = new List<XmlDocumentNode>(list[i].childNodes);
                            ParseXPath2(__list, split, false);
                            _list.AddRange(__list);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].name.Equals(split, StringComparison.OrdinalIgnoreCase))
                            _list.Add(list[i].parent, list[i]);
                        //else if (all)
                        //{
                        //    XmlParserNodeCollection __list = new XmlParserNodeCollection(list[i].childNodes);
                        //    ParseXPath2(__list, split, all);
                        //    _list.AddRange(__list);
                        //}
                        //else
                        //{
                        //    XmlParserNodeCollection __list = new XmlParserNodeCollection(list[i].childNodes);
                        //    ParseXPath2(__list, split, false);
                        //    _list.AddRange(__list);
                        //}
                    }
                }
                list.Clear();
                list.AddRange(_list);
                _list.Clear();
            }
        }

        private static void CheckCondition(string splitSub, ref Conditions condition, ref int end)
        {
            if (splitSub.Contains("!") || splitSub.Contains("<") || splitSub.Contains(">"))
            {
                end = splitSub.IndexOf("!=");
                if (end < 0)
                {
                    end = splitSub.IndexOf(">=");
                    if (end < 0)
                    {
                        end = splitSub.IndexOf("<=");
                        if (end < 0)
                        {
                            end = splitSub.IndexOf(">");
                            if (end < 0)
                            {
                                end = splitSub.IndexOf("<");
                                if (end < 0)
                                {
                                    end = splitSub.IndexOf("=");
                                    if (end < 0)
                                    {
                                        end = splitSub.Length;
                                    }
                                    else condition = Conditions.Equal;
                                }
                                else condition = Conditions.Less;
                            }
                            else condition = Conditions.Great;
                        }
                        else condition = Conditions.LessOrEqual;
                    }
                    else condition = Conditions.GreatOrEqual;
                }
                else condition = Conditions.NotEqual;
            }
            else if (end < 0)
            {
                condition = Conditions.None;
                end = splitSub.Length;
            }
        }
        private string MatchEvaluatorMethod(Match match)
        {
            string k = "##" + (key++).ToString() + "##";
            mathes[k] = match.Value;
            return "/" + k;
        }

        /// <summary>
        /// Возвращает преобразованное значение из текущего аттрибута
        /// </summary>
        /// <param name="attributeName"></param>
        /// <returns>
        /// Возвращает преобразованное значение из аттрибута, 
        /// если такого аттрибута не существует, то возвращает значение по умолчанию для данного типа
        /// </returns>
        public string GetValue(string attributeName)
        {
            XmlDocumentAttribute attr = Attributes[attributeName];
            if (attr != null)
                return attr.Value;
            return null;
        }
        /// <summary>
        /// Возвращает преобразованное значение из указанного узла и его аттрибута
        /// </summary>
        /// <param name="childNodeName"></param>
        /// <param name="attributeName"></param>
        /// <returns>
        /// Возвращает преобразованное значение из аттрибута, 
        /// если такого аттрибута не существует, то возвращает значение по умолчанию для данного типа
        /// </returns>
        public string GetValue(string childNodeName, string attributeName)
        {
            XmlDocumentNode child = ChildNodes[childNodeName];
            if (child != null)
                return child.GetValue(attributeName);
            return null;
        }
        /// <summary>
        /// Возвращает преобразованное значение из текущего аттрибута
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="attributeName"></param>
        /// <returns>
        /// Возвращает преобразованное значение из аттрибута, 
        /// если такого аттрибута не существует, то возвращает значение по умолчанию для данного типа
        /// </returns>
        public T GetValue<T>(string attributeName)
        {
            XmlDocumentAttribute attr = Attributes[attributeName];
            if (attr != null)
            {
                return ParseValue<T>(attr.Value, Converter);
            }
            return default(T);
        }
        /// <summary>
        /// Возвращает преобразованное значение из указанного узла и его аттрибута
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="childNodeName"></param>
        /// <param name="attributeName"></param>
        /// <returns>
        /// Возвращает преобразованное значение из аттрибута, 
        /// если такого аттрибута не существует, то возвращает значение по умолчанию для данного типа
        /// </returns>
        public T GetValue<T>(string childNodeName, string attributeName)
        {
            XmlDocumentNode child = ChildNodes[childNodeName];
            if (child != null)
                return child.GetValue<T>(attributeName);
            return default(T);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        public static T ParseValue<T>(string value, ICustomConverter converter = null)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return default(T);

                if (typeof(T) == typeof(string)) return (T)(object)value.Replace("\\n", "\n");

                if (typeof(T).IsEnum)
                    return (T)Enum.Parse(typeof(T), value);

                if (typeof(T) == typeof(float))
                    return (T)System.Convert.ChangeType(value.Replace(",", "."), typeof(T), CurrentCulture);

                if (typeof(T).IsPrimitive)
                    return (T)System.Convert.ChangeType(value, typeof(T), CurrentCulture);

                if (converter != null)
                    return converter.Convert<T>(value);

                return (T)System.Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"value:'{value}' Type:'{typeof(T).Name}' >> {ex}");
            }

            return default(T);
        }
        /// <summary>
        /// Возвращает значение из узла nodeName, свойства InnerText, при это проверяя существует ли такой узел в документа
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns>
        /// Если узел существует, то возвращает значение из свойства InnerText, 
        /// в противном случае возвращает значение по умолчанию для указанного типа
        /// </returns>
        public string GetInnerTextValue(string nodeName)
        {
            XmlDocumentNode child = ChildNodes[nodeName];
            if (child != null)
            {
                return child.InnerText;
            }
            return null;
        }
        /// <summary>
        /// Возвращает значение из узла nodeName, свойства InnerText, при это проверяя существует ли такой узел в документа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodeName"></param>
        /// <returns>
        /// Если узел существует, то возвращает значение из свойства InnerText, 
        /// в противном случае возвращает значение по умолчанию для указанного типа
        /// </returns>
        public T GetInnerTextValue<T>(string nodeName)
        {
            XmlDocumentNode child = ChildNodes[nodeName];
            if (child != null)
                return ParseValue<T>(child.InnerText, Converter);
            return default(T);
        }
        /// <summary>
        /// Возвращает значение из свойства InnerText, при это проверяя существует ли такой узел в документа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>
        /// Если узел существует, то возвращает значение из свойства InnerText, 
        /// в противном случае возвращает значение по умолчанию для указанного типа
        /// </returns>
        public T GetInnerTextValue<T>()
        {
            string _innerText = InnerText;
            return ParseValue<T>(_innerText, Converter);
        }
        /// <summary>
        /// Возвращает значение из узла nodeName, свойства InnerText, при это проверяя существует ли такой узел в документа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodeName"></param>
        /// <param name="result"></param>
        /// <returns>
        /// Если узел существует, то возвращает значение из свойства InnerText, 
        /// в противном случае возвращает значение по умолчанию для указанного типа
        /// </returns>
        public bool GetInnerTextValue<T>(string nodeName, out T result)
        {
            XmlDocumentNode child = ChildNodes[nodeName];
            if (child != null)
            {
                result = ParseValue<T>(child.InnerText, Converter);
                return true;
            }
            result = default(T);
            return false;
        }
        /// <summary>
        /// Сравнивает текущий узел с указанным узлом
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public List<XmlDocumentNode> Compare(XmlDocumentNode node)
        {
            List<XmlDocumentNode> res = new List<XmlDocumentNode>();
            if (Name != node.Name)
                res.Add(this);
            else
            {
                bool isChanged = false;
                if (InnerText != node.InnerText)
                    res.Add(this);
                else if (this.Attributes.Count != node.Attributes.Count)
                    res.Add(this);
                else
                {
                    foreach (var attr in this.Attributes)
                    {
                        var _attr = node.Attributes[attr.Name];
                        if (_attr == null || _attr.value != attr.value)
                        {
                            res.Add(this);
                            isChanged = true;
                            break;
                        }
                    }
                }

                if (!isChanged)
                {
                    if (this.ChildNodes.Count != node.ChildNodes.Count)
                        res.Add(this);
                    else
                    {
                        for (int i = 0; i < ChildNodes.Count; i++)
                        {
                            if (node.ChildNodes[ChildNodes[i].Name] != null)
                            {
                                res.AddRange(node.ChildNodes[ChildNodes[i].Name].Compare(ChildNodes[i]));
                            }
                            else res.Add(ChildNodes[i]);
                        }
                    }
                }
            }

            return res;
        }

        #region IEnumerator
        public IEnumerator<XmlDocumentNode> GetEnumerator()
        {
            return ((IEnumerable<XmlDocumentNode>)childNodes).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<XmlDocumentNode>)childNodes).GetEnumerator();
        }
        #endregion

        //void OnRaiseChangeEvent(IXmlDocumentNode node)
        //{
        //    if (ChangeNode != null)
        //        ChangeNode(node);
        //    //var root = Root;
        //    //if (root != null)
        //    //    root.OnRaiseChangeEvent(node);
        //}
        internal void OnRaiseAddEvent(IXmlDocumentNode node, object sender)
        {
            if (sender != null)
            {
                if (AddNodeElement != null)
                    AddNodeElement(node, sender);
            }
            else
            {
                if (AddNode != null)
                    AddNode(node);
            }
            //var root = Root;
            //if (root != null)
            //    root.OnRaiseChangeEvent(node, sender);
        }
        internal void OnRaiseRemoveEvent(IXmlDocumentNode node, object sender)
        {
            if (sender != null)
            {
                if (RemoveNodeElement != null)
                    RemoveNodeElement(node, sender);
            }
            else
            {
                if (RemoveNode != null)
                    RemoveNode(node);
            }
            //var root = Root;
            //if (root != null)
            //    root.OnRaiseChangeEvent(node, sender);
        }
        internal void OnRaiseChangeEvent(IXmlDocumentNode node, object sender)
        {
            if (sender != null)
            {
                if (ChangeNodeElement != null)
                    ChangeNodeElement(node, sender);
                else
                {
                    var root = Root as XmlDocumentNode;
                    if (root != null)
                        root.OnRaiseChangeEvent(node, sender);
                }
            }
            else
            {
                if (ChangeNode != null)
                    ChangeNode(node);
                else
                {
                    var root = Root as XmlDocumentNode;
                    if (root != null)
                        root.OnRaiseChangeEvent(node, sender);
                }
            }
            //var root = Root;
            //if (root != null)
            //    root.OnRaiseChangeEvent(node, sender);
        }

        public class UnityConverter : ICustomConverter
        {
            public bool CanConvert(Type type)
            {
#if UNITY_2018_1_OR_NEWER
                return type == typeof(UnityEngine.Color) || type == typeof(UnityEngine.Color32) ||
               type == typeof(UnityEngine.Vector2) || type == typeof(UnityEngine.Vector2Int) ||
               type == typeof(UnityEngine.Vector3) || type == typeof(UnityEngine.Vector3Int) ||
               type == typeof(UnityEngine.Vector4) ||
               type == typeof(UnityEngine.Rect) || type == typeof(UnityEngine.RectInt); 
#endif
            }
            public virtual T Convert<T>(string value)
            {
                object result = new object();
                Convert(value, ref result, typeof(T));
                return (T)result;
//                CultureInfo ci = new CultureInfo("en-US");
//#if UNITY_2018_1_OR_NEWER
//                if (typeof(T) == typeof(UnityEngine.Color))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length > 2)
//                    {
//                        float r = float.Parse(bytes[0], ci);
//                        float g = float.Parse(bytes[1], ci);
//                        float b = float.Parse(bytes[2], ci);
//                        float a = 1;
//                        if (bytes.Length == 4)
//                            a = float.Parse(bytes[3], ci);
//                        object col = new UnityEngine.Color(r, g, b, a);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//                else if (typeof(T) == typeof(UnityEngine.Color32))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length > 2)
//                    {
//                        byte r = byte.Parse(bytes[0]);
//                        byte g = byte.Parse(bytes[1]);
//                        byte b = byte.Parse(bytes[2]);
//                        byte a = 1;
//                        if (bytes.Length == 4)
//                            a = byte.Parse(bytes[3]);
//                        object col = new UnityEngine.Color32(r, g, b, a);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//                else if (typeof(T) == typeof(UnityEngine.Vector2))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length > 1)
//                    {
//                        float r = float.Parse(bytes[0], ci);
//                        float g = float.Parse(bytes[1], ci);
//                        object col = new UnityEngine.Vector2(r, g);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//                else if (typeof(T) == typeof(UnityEngine.Vector2Int))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length > 1)
//                    {
//                        int r = int.Parse(bytes[0]);
//                        int g = int.Parse(bytes[1]);
//                        object col = new UnityEngine.Vector2Int(r, g);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//                else if (typeof(T) == typeof(UnityEngine.Vector3))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length > 2)
//                    {
//                        float r = float.Parse(bytes[0], ci);
//                        float g = float.Parse(bytes[1], ci);
//                        float b = float.Parse(bytes[2], ci);
//                        object col = new UnityEngine.Vector3(r, g, b);
//                        return (T)col;
//                    }
//                    else if (bytes.Length == 2)
//                    {
//                        float r = float.Parse(bytes[0], ci);
//                        float g = float.Parse(bytes[1], ci);
//                        float b = 0;
//                        object col = new UnityEngine.Vector3(r, g, b);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//                else if (typeof(T) == typeof(UnityEngine.Vector3Int))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length > 2)
//                    {
//                        int r = int.Parse(bytes[0]);
//                        int g = int.Parse(bytes[1]);
//                        int b = int.Parse(bytes[2]);
//                        object col = new UnityEngine.Vector3Int(r, g, b);
//                        return (T)col;
//                    }
//                    else if (bytes.Length == 2)
//                    {
//                        int r = int.Parse(bytes[0]);
//                        int g = int.Parse(bytes[1]);
//                        int b = 0;
//                        object col = new UnityEngine.Vector3Int(r, g, b);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//                else if (typeof(T) == typeof(UnityEngine.Vector4))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length > 2)
//                    {
//                        float r = float.Parse(bytes[0], ci);
//                        float g = float.Parse(bytes[1], ci);
//                        float b = float.Parse(bytes[2], ci);
//                        float a = 1;
//                        if (bytes.Length == 4)
//                            a = float.Parse(bytes[3], ci);
//                        object col = new UnityEngine.Vector4(r, g, b, a);
//                        return (T)col;
//                    }
//                    else if (bytes.Length == 2)
//                    {
//                        float r = float.Parse(bytes[0], ci);
//                        float g = float.Parse(bytes[1], ci);
//                        float b = 0;
//                        float a = 0;
//                        if (bytes.Length == 4)
//                            a = float.Parse(bytes[3], ci);
//                        object col = new UnityEngine.Vector4(r, g, b, a);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//                else if (typeof(T) == typeof(UnityEngine.Rect))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length == 4)
//                    {
//                        float x = float.Parse(bytes[0].Substring(bytes[0].IndexOf(":") + 1), ci);
//                        float y = float.Parse(bytes[1].Substring(bytes[1].IndexOf(":") + 1), ci);
//                        float w = float.Parse(bytes[2].Substring(bytes[2].IndexOf(":") + 1), ci);
//                        float h = float.Parse(bytes[3].Substring(bytes[3].IndexOf(":") + 1), ci);
//                        object col = new UnityEngine.Rect(x, y, w, h);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//                else if (typeof(T) == typeof(UnityEngine.RectInt))
//                {
//                    int n1 = value.IndexOf("(") + 1;
//                    int n2 = value.IndexOf(")");
//                    string val = value.Substring(n1, n2 - n1);
//                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
//                    if (bytes.Length == 4)
//                    {
//                        int x = int.Parse(bytes[0].Substring(bytes[0].IndexOf(":") + 1));
//                        int y = int.Parse(bytes[1].Substring(bytes[1].IndexOf(":") + 1));
//                        int w = int.Parse(bytes[2].Substring(bytes[2].IndexOf(":") + 1));
//                        int h = int.Parse(bytes[3].Substring(bytes[3].IndexOf(":") + 1));
//                        object col = new UnityEngine.RectInt(x, y, w, h);
//                        return (T)col;
//                    }
//                    else return default(T);
//                }
//#endif
//                if (typeof(T) == typeof(System.Guid))
//                    return (T)(object)System.Guid.Parse(value);
//                return (T)System.Convert.ChangeType(value, typeof(T));
            }
            public void Convert(string value, ref object result, Type returnedType = null)
            {
                if (returnedType == null)
                    returnedType = result.GetType();
#if UNITY_2018_1_OR_NEWER
                if (returnedType == typeof(UnityEngine.Color))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length > 2)
                    {
                        float r = float.Parse(bytes[0], CurrentCulture);
                        float g = float.Parse(bytes[1], CurrentCulture);
                        float b = float.Parse(bytes[2], CurrentCulture);
                        float a = 1;
                        if (bytes.Length == 4)
                            a = float.Parse(bytes[3], CurrentCulture);
                        object col = new UnityEngine.Color(r, g, b, a);
                        result = col;
                    }
                    else result = default(UnityEngine.Color);
                    return;
                }
                else if (returnedType == typeof(UnityEngine.Color32))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length > 2)
                    {
                        byte r = byte.Parse(bytes[0]);
                        byte g = byte.Parse(bytes[1]);
                        byte b = byte.Parse(bytes[2]);
                        byte a = 1;
                        if (bytes.Length == 4)
                            a = byte.Parse(bytes[3]);
                        object col = new UnityEngine.Color32(r, g, b, a);
                        result = col;
                    }
                    else result = default(UnityEngine.Color32);
                    return;
                }
                else if (returnedType == typeof(UnityEngine.Vector2))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length > 1)
                    {
                        float r = float.Parse(bytes[0], CurrentCulture);
                        float g = float.Parse(bytes[1], CurrentCulture);
                        object col = new UnityEngine.Vector2(r, g);
                        result = col;
                    }
                    else result = default(UnityEngine.Vector2);
                    return;
                }
                else if (returnedType == typeof(UnityEngine.Vector2Int))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length > 1)
                    {
                        int r = int.Parse(bytes[0]);
                        int g = int.Parse(bytes[1]);
                        object col = new UnityEngine.Vector2Int(r, g);
                        result = col;
                    }
                    else result = default(UnityEngine.Vector2Int);
                    return;
                }
                else if (returnedType == typeof(UnityEngine.Vector3))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length > 2)
                    {
                        float r = float.Parse(bytes[0], CurrentCulture);
                        float g = float.Parse(bytes[1], CurrentCulture);
                        float b = float.Parse(bytes[2], CurrentCulture);
                        object col = new UnityEngine.Vector3(r, g, b);
                        result = col;
                    }
                    else if (bytes.Length == 2)
                    {
                        float r = float.Parse(bytes[0], CurrentCulture);
                        float g = float.Parse(bytes[1], CurrentCulture);
                        object col = new UnityEngine.Vector3(r, g, 0);
                        result = col;
                    }
                    else result = default(UnityEngine.Vector3);
                    return;
                }
                else if (returnedType == typeof(UnityEngine.Vector3Int))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length > 2)
                    {
                        int r = int.Parse(bytes[0]);
                        int g = int.Parse(bytes[1]);
                        int b = int.Parse(bytes[2]);
                        object col = new UnityEngine.Vector3Int(r, g, b);
                        result = col;
                    }
                    else result = default(UnityEngine.Vector3Int);
                    return;
                }
                else if (returnedType == typeof(UnityEngine.Vector4))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length > 2)
                    {
                        float r = float.Parse(bytes[0], CurrentCulture);
                        float g = float.Parse(bytes[1], CurrentCulture);
                        float b = float.Parse(bytes[2], CurrentCulture);
                        float a = 1;
                        if (bytes.Length == 4)
                            a = float.Parse(bytes[3], CurrentCulture);
                        object col = new UnityEngine.Vector4(r, g, b, a);
                        result = col;
                    }
                    else result = default(UnityEngine.Vector4);
                    return;
                }
                else if (returnedType == typeof(UnityEngine.Rect))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length == 4)
                    {
                        float x = float.Parse(bytes[0].Substring(bytes[0].IndexOf(":") + 1), CurrentCulture);
                        float y = float.Parse(bytes[1].Substring(bytes[1].IndexOf(":") + 1), CurrentCulture);
                        float w = float.Parse(bytes[2].Substring(bytes[2].IndexOf(":") + 1), CurrentCulture);
                        float h = float.Parse(bytes[3].Substring(bytes[3].IndexOf(":") + 1), CurrentCulture);
                        object col = new UnityEngine.Rect(x, y, w, h);
                        result = col;
                    }
                    else result = default(UnityEngine.Rect);
                    return;
                }
                else if (returnedType == typeof(UnityEngine.RectInt))
                {
                    int n1 = value.IndexOf("(") + 1;
                    int n2 = value.IndexOf(")");
                    string val = value.Substring(n1, n2 - n1);
                    string[] bytes = val.Replace("{", "").Replace("}", "").Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                    if (bytes.Length == 4)
                    {
                        int x = int.Parse(bytes[0].Substring(bytes[0].IndexOf(":") + 1));
                        int y = int.Parse(bytes[1].Substring(bytes[1].IndexOf(":") + 1));
                        int w = int.Parse(bytes[2].Substring(bytes[2].IndexOf(":") + 1));
                        int h = int.Parse(bytes[3].Substring(bytes[3].IndexOf(":") + 1));
                        object col = new UnityEngine.RectInt(x, y, w, h);
                        result = col;
                    }
                    else 
                        result = default(UnityEngine.RectInt);
                    return;
                }
#endif
                result = System.Convert.ChangeType(value, returnedType, CurrentCulture);
            }
        }
    }
}
