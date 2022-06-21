using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System;

namespace XMLSystem.Xml
{
    public class XmlDocument : IXmlDocumentNode, IEnumerable<XmlDocumentNode>
    {
        XmlDocumentNode mainNode;
        private Dictionary<string, string> mathes;
        private int key = 0;
        public event System.Action<IXmlDocumentNode> ChangeNode;
        public event System.Action<IXmlDocumentNode, object> ChangeNodeElement;

        public string InnerText
        {
            get
            {
                return ((IXmlDocumentNode)mainNode).InnerText;
            }

            set
            {
                ((IXmlDocumentNode)mainNode).InnerText = value;
            }
        }

        public XNodeType NodeType
        {
            get
            {
                return ((IXmlDocumentNode)mainNode).NodeType;
            }
        }

        public bool IsHTML
        {
            get
            {
                return ((IXmlDocumentNode)mainNode).IsHTML;
            }
        }

        public XmlDocumentNode Parent
        {
            get
            {
                return null;
            }
        }

        public string Name
        {
            get
            {
                return mainNode != null ? ((IXmlDocumentNode)mainNode).Name : null;
            }

            set
            {
                if (mainNode != null)
                    ((IXmlDocumentNode)mainNode).Name = value;
            }
        }

        public XmlDocumentNodeCollection ChildNodes
        {
            get
            {
                return ((IXmlDocumentNode)mainNode).ChildNodes;
            }
        }

        public XmlDocumentAttributeCollection Attributes
        {
            get
            {
                return ((IXmlDocumentNode)mainNode).Attributes;
            }
        }

        public string Comentaries
        {
            get
            {
                return ((IXmlDocumentNode)mainNode).Comentaries;
            }
        }

        public XmlDocumentNode this[string name]
        {
            get
            {
                return ((IXmlDocumentNode)mainNode)[name];
            }
        }
        public XmlDocumentNode this[int indx]
        {
            get
            {
                return mainNode[indx];
            }
        }
        public int Count
        {
            get { return mainNode.ChildNodes.Count; }
        }

        static XmlDocument()
        {
            if (Converter == null)
                Converter = new XmlDocumentNode.UnityConverter();
        }

        public XmlDocument()
        {

        }
        public XmlDocument(string filename)
        {
            Load(File.ReadAllText(filename));
        }
        public XmlDocument(byte[] xml)
        {
            Load(xml);
        }
        public XmlDocument(Stream stream)
        {
            Load(stream);
        }

        public void Load(byte[] xml)
        {
            using (MemoryStream mstream = new MemoryStream(xml))
                Load(mstream);
        }
        public void Load(Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            Load(reader.ReadToEnd());
            reader.Close();
        }
        public void Load(string xmlText)
        {
            mainNode = new XmlDocumentNode(null);
            string nodeText;
            if (xmlText.IndexOf("?>") >= 0) nodeText = xmlText.Substring(xmlText.IndexOf("?>") + 2).Trim();
            else nodeText = xmlText;
			int indxDoctype = nodeText.IndexOf("<!DOCTYPE");
			if (indxDoctype >= 0)
				nodeText = xmlText.Substring(xmlText.IndexOf(">", indxDoctype) + 1).Trim();
			int start = 0;
            mathes = new Dictionary<string, string>();
            Regex regex = new Regex("\".*?\"");
            nodeText = regex.Replace(nodeText, MatchEvaluatorMethod);

            nodeText = nodeText.Replace("\n", string.Empty); //";::nl::;");
            nodeText = nodeText.Replace("\r", string.Empty);
            nodeText = nodeText.Replace("\t", string.Empty); //";::tab::;");

            Regex regex2 = new Regex(@"(\#\#\d+\#\#)");
            //foreach (var item in mathes)
            //    nodeText = nodeText.Replace(item.Key, item.Value);
            nodeText = regex2.Replace(nodeText, MatchEvaluatorMethod2);

            mainNode.Parse(nodeText, ref start);

            if(mainNode.NodeType == XNodeType.Coment && !string.IsNullOrEmpty(nodeText.Substring(start).Trim()))
                mainNode.Parse(nodeText, ref start);

            mathes.Clear();
            mathes = null;
        }

        void IXmlDocumentNode.OnRaiseChangeEvent(IXmlDocumentNode node)
        {
            if (ChangeNode != null)
                ChangeNode(node);
        }
        void IXmlDocumentNode.OnRaiseChangeEvent(IXmlDocumentNode node, object sender)
        {
            if (ChangeNodeElement != null)
                ChangeNodeElement(node, sender);
        }

        /// <summary>
        /// Загружает текст в формате JSON.
        /// Все загруженные элементы являются дочерними только для MainNode.
        /// </summary>
        /// <param name="jsonText"></param>
        public void LoadJSON(string jsonText)
        {
            mainNode = new XmlDocumentNode(null);
            mainNode.Name = "____JSON_BASE____";

            string nodeText = jsonText;
            int start = 0;
            mathes = new Dictionary<string, string>();
            Regex regex = new Regex("\".*?\"");
            nodeText = regex.Replace(nodeText, MatchEvaluatorMethod);

            nodeText = nodeText.Replace("\n", string.Empty);
            nodeText = nodeText.Replace("\r", string.Empty);
            nodeText = nodeText.Replace("\t", string.Empty);

            Regex regex2 = new Regex(@"(\#\#\d+\#\#)");
            nodeText = regex2.Replace(nodeText, MatchEvaluatorMethod2);

            XmlDocumentNode node = new XmlDocumentNode(mainNode);
            while (node.ParseJSON(nodeText, ref start))
            {
                mainNode.AddChildNode(node);
                node = new XmlDocumentNode(mainNode);
            }
            mathes.Clear();
            mathes = null;
        }
        //public void LoadHtml(string htmlText)
        //{
        //    mainNode = new XmlDocumentNode(null);
        //    string nodeText;
        //    if (htmlText.IndexOf("?>") >= 0) nodeText = htmlText.Substring(htmlText.IndexOf("?>") + 2).Trim();
        //    else nodeText = htmlText;
        //    mainNode.ParseHtml(ref nodeText);
        //}

        private string MatchEvaluatorMethod(Match match)
        {
            string k = "##" + (key++).ToString() + "##";
            mathes[k] = match.Value;
            return k;
        }
        private string MatchEvaluatorMethod2(Match match)
        {
            //string k = "##" + (key++).ToString() + "##";
            return mathes[match.Value];
        }

        public static XmlDocumentNode CreateNode(string name)
        {
            XmlDocumentNode node = new XmlDocumentNode(null);
            node.Name = name;
            return node;
        }
        public static XmlDocumentNode CreateNode(string name, XmlDocumentNode parent)
        {
            XmlDocumentNode node = new XmlDocumentNode(null);
            node.Name = name;
            parent.AddChildNode(node);
            return node;
        }
        public XmlDocumentAttribute CreateAttribute(string name, string value)
        {
            return new XmlDocumentAttribute(null, name, value);
        }
        public XmlDocumentAttribute AddAttribute(string name, string value)
        {
            return new XmlDocumentAttribute(mainNode, name, value);
        }
        public void SetMainNode(XmlDocumentNode node)
        {
            mainNode = node;
        }

        public override string ToString()
        {
            return mainNode != null ? mainNode.ToString() : base.ToString();
        }

        public void Save(string path)
        {
            StreamWriter stream = File.CreateText(path);
            stream.Write(mainNode.ToString());
            stream.Flush();
            stream.Close();
            stream.Dispose();
        }

        public void AddChildNode(XmlDocumentNode node)
        {
            ((IXmlDocumentNode)mainNode).AddChildNode(node);
            (mainNode as IXmlDocumentNode).OnRaiseChangeEvent(this);
        }

        public void AddChildNode(XmlDocumentNode node, XNodeType type)
        {
            ((IXmlDocumentNode)mainNode).AddChildNode(node, type);
            (mainNode as IXmlDocumentNode).OnRaiseChangeEvent(this);
        }

        public void InsertChildNode(int index, XmlDocumentNode node)
        {
            ((IXmlDocumentNode)mainNode).InsertChildNode(index, node);
            (mainNode as IXmlDocumentNode).OnRaiseChangeEvent(this);
        }

        public void AddAttribute(XmlDocumentAttribute attribute)
        {
            ((IXmlDocumentNode)mainNode).AddAttribute(attribute);
            (mainNode as IXmlDocumentNode).OnRaiseChangeEvent(this);
        }

        void IXmlDocumentNode.AddAttribute(string key, string value)
        {
            ((IXmlDocumentNode)mainNode).AddAttribute(new XmlDocumentAttribute(mainNode, key, value));
            (mainNode as IXmlDocumentNode).OnRaiseChangeEvent(this);
        }

        public void AddAttribute(string key, object value)
        {
            ((IXmlDocumentNode)mainNode).AddAttribute(key, value);
            (mainNode as IXmlDocumentNode).OnRaiseChangeEvent(this);
        }

        public List<XmlDocumentNode> SelectNodes(string xPath)
        {
            return ((IXmlDocumentNode)mainNode).SelectNodes(xPath);
        }

        public XmlDocumentNode SelectNode(string xPath)
        {
            return ((IXmlDocumentNode)mainNode).SelectNode(xPath);
        }

        public bool TrySelectNode(string xPath, out XmlDocumentNode node)
        {
            return ((IXmlDocumentNode)mainNode).TrySelectNode(xPath, out node);
        }

        public bool TrySelectNodes(string xPath, out List<XmlDocumentNode> collection)
        {
            return ((IXmlDocumentNode)mainNode).TrySelectNodes(xPath, out collection);
        }

        public void GetAllNodes(List<XmlDocumentNode> list, List<XmlDocumentNode> result, string name)
        {
            ((IXmlDocumentNode)mainNode).GetAllNodes(list, result, name);
        }

        public void GetAllAttributes(List<XmlDocumentNode> list, List<XmlDocumentNode> result)
        {
            ((IXmlDocumentNode)mainNode).GetAllAttributes(list, result);
        }

        public void GetAllAttributes(List<XmlDocumentNode> list, List<XmlDocumentNode> result, string name, string value, Conditions condition)
        {
            ((IXmlDocumentNode)mainNode).GetAllAttributes(list, result, name, value, condition);
        }

        public List<XmlDocumentNode> GetAttributes(List<XmlDocumentNode> list, string name)
        {
            return ((IXmlDocumentNode)mainNode).GetAttributes(list, name);
        }

        public List<XmlDocumentNode> GetAttributes(List<XmlDocumentNode> list, string name, string value, Conditions condition)
        {
            return ((IXmlDocumentNode)mainNode).GetAttributes(list, name, value, condition);
        }

        public void Dispose()
        {
            ((IXmlDocumentNode)mainNode).Dispose();
        }

        public string ToString(bool smalWrite)
        {
            if (mainNode == null) return "";
            return ((IXmlDocumentNode)mainNode).ToString(smalWrite);
        }

        public string ToJSON()
        {
            return ((IXmlDocumentNode)mainNode).ToJSON();
        }

        public IEnumerator<XmlDocumentNode> GetEnumerator()
        {
            return ((IEnumerable<XmlDocumentNode>)mainNode).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<XmlDocumentNode>)mainNode).GetEnumerator();
        }

        public static implicit operator XmlDocumentNode(XmlDocument doc)
        {
            if (doc == null) return null;
            return doc.mainNode;
        }
        /// <summary>
        /// Текущий конвертер, который используется в методах 'GetInnerTextValue' и 'GetValue'
        /// </summary>
        public static ICustomConverter Converter { get { return XmlDocumentNode.Converter; } set { XmlDocumentNode.Converter = value; } }

        public List<XmlDocumentNode> Compare(XmlDocument doc)
        {
            List<XmlDocumentNode> res = new List<XmlDocumentNode>();
            if (mainNode.Name != doc.mainNode.Name)
                res.Add(mainNode);
            else
            {
                bool isChanged = false;
                if (mainNode.InnerText != doc.mainNode.InnerText)
                    res.Add(mainNode);
                else if (mainNode.Attributes.Count != doc.mainNode.Attributes.Count)
                    res.Add(mainNode);
                else
                {
                    foreach (var attr in mainNode.Attributes)
                    {
                        var _attr = doc.mainNode.Attributes[attr.Name];
                        if (_attr == null || _attr.value != attr.value)
                        {
                            res.Add(mainNode);
                            isChanged = true;
                            break;
                        }
                    }
                }

                if (!isChanged)
                {
                    if (mainNode.ChildNodes.Count != doc.mainNode.ChildNodes.Count)
                        res.Add(mainNode);
                    else
                    {
                        for (int i = 0; i < ChildNodes.Count; i++)
                        {
                            if (doc.mainNode.ChildNodes[ChildNodes[i].Name] != null)
                            {
                                res.AddRange(doc.mainNode.ChildNodes[ChildNodes[i].Name].Compare(ChildNodes[i]));
                            }
                            else res.Add(ChildNodes[i]);
                        }
                    }
                }
            }

            return res;
        }

        public static string EncodeToInnerText(string xml)
        {
            string temp = xml;

            temp = temp.Replace("&", "&amp;");
            temp = temp.Replace("<", "&lt;");
            temp = temp.Replace(">", "&gt;");
            temp = temp.Replace("'", "&apos;");
            temp = temp.Replace("\"", "&quot;");
            temp = temp.Replace("\n", "&#10;");
            temp = temp.Replace(" ", "&#160;");

            return temp;
        }

        public static string DecodeToXML(string innertext)
        {
            string temp = innertext;
            if (temp.Contains("&"))
            {
                temp = temp.Replace("&amp;", "&");
                temp = temp.Replace("&lt;", "<");
                temp = temp.Replace("&gt;", ">");
                temp = temp.Replace("&apos;", "'");
                temp = temp.Replace("&quot;", "\"");
                temp = temp.Replace("&nl;", "\n");
                temp = temp.Replace("&#10;", "\n");
                temp = temp.Replace("&#160;", " ");
            }

            return temp;
        }
    }
}
