using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMLSystem.Xml
{
    public interface IXmlDocumentNode
    {
        /// <summary>
        /// Возвращает этотже узел, если его имя совпадает с указанным(без учета регистра), в противном случае ищет в дочерних узлах
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        XmlDocumentNode this[string name] { get; }
        /// <summary>
        /// 
        /// </summary>
        string InnerText { get; set; }
        /// <summary>
        /// 
        /// </summary>
        XNodeType NodeType { get; }
        /// <summary>
        /// 
        /// </summary>
        bool IsHTML { get; }
        /// <summary>
        /// Возвращает родительский узел
        /// </summary>
        XmlDocumentNode Parent { get; }
        /// <summary>
        /// Возвращает имя узла
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Возвращает список дочерних узлов
        /// </summary>
        XmlDocumentNodeCollection ChildNodes { get; }
        /// <summary>
        /// Возвращает список атрибутов текущего узла
        /// </summary>
        XmlDocumentAttributeCollection Attributes { get; }
        /// <summary>
        /// Возвращает коментарии в данном узле
        /// </summary>
        string Comentaries { get; }

        /// <summary>
        /// Возвращает количество дочерних узлов
        /// </summary>
        int Count { get; }

        void AddChildNode(XmlDocumentNode node);
        /// <summary>
        /// Добавляет дочерний узел
        /// </summary>
        /// <param name="node"></param>
        /// <param name="type">Тип узла, указывается только в случае если после узел должен вернуть запись в формате JSON или HTML</param>
        void AddChildNode(XmlDocumentNode node, XNodeType type);
        /// <summary>
        /// Вставляет дочерний узел
        /// </summary>
        /// <param name="index"></param>
        /// <param name="node"></param>
        void InsertChildNode(int index, XmlDocumentNode node);
        /// <summary>
        /// Добавляет атрибут
        /// </summary>
        /// <param name="attribute"></param>
        void AddAttribute(XmlDocumentAttribute attribute);
        ///// <summary>
        ///// Добавляет атрибут
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="value"></param>
        //void AddAttribute(string key, string value);
        /// <summary>
        /// Добавляет атрибут
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">будет вызван метод ToString()</param>
        void AddAttribute(string key, object value);
        /// <summary>
        /// Осуществляет поиск узлов по пути XPath
        /// </summary>
        /// <param name="xPath"></param>
        /// <returns>В случае, если ни один узел не найден, возвращает список нулевой длины</returns>
        List<XmlDocumentNode> SelectNodes(string xPath);
        /// <summary>
        /// Осуществляет поиск узла по пути XPath и возвращает первый попавшийся
        /// </summary>
        /// <param name="xPath"></param>
        /// <returns>Если узел не найден, возвращает null</returns>
        XmlDocumentNode SelectNode(string xPath);
        /// <summary>
        /// Осуществляет поиск узла по пути XPath и возвращает первый попавшийся
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="node"></param>
        /// <returns>Если узел не найден, возвращает false</returns>
        bool TrySelectNode(string xPath, out XmlDocumentNode node);
        /// <summary>
        /// Осуществляет поиск узлов по пути XPath
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="node"></param>
        /// <returns>Если узел не найден, возвращает false</returns>
        bool TrySelectNodes(string xPath, out List<XmlDocumentNode> collection);
        /// <summary>
        /// Осуществляет поиск узлов по имени в текущем узле
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        /// <param name="name"></param>
        void GetAllNodes(List<XmlDocumentNode> list, List<XmlDocumentNode> result, string name);
        /// <summary>
        /// Осуществляет поиск дочерних узлов, которые содержат атрибуты
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        void GetAllAttributes(List<XmlDocumentNode> list, List<XmlDocumentNode> result);
        /// <summary>
        /// Осуществляет поиск дочерних узлов, которые содержат атрибуты с указанным значением
        /// </summary>
        /// <param name="list"></param>
        /// <param name="result"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void GetAllAttributes(List<XmlDocumentNode> list, List<XmlDocumentNode> result, string name, string value, Conditions condition);
        /// <summary>
        /// Осуществляет поиск ближайших дочерних узлов, которые содержат атрибуты
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        List<XmlDocumentNode> GetAttributes(List<XmlDocumentNode> list, string name);
        /// <summary>
        /// Осуществляет поиск ближайших дочерних узлов, которые содержат атрибуты с указанным значением
        /// </summary>
        /// <param name="list"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        List<XmlDocumentNode> GetAttributes(List<XmlDocumentNode> list, string name, string value, Conditions condition);
        /// <summary>
        /// Убивает ссылки на родительские узлы и очищает список всех дочерних узлов и атрибутов
        /// </summary>
        void Dispose();
        /// <summary>
        /// (перегружен)
        /// </summary>
        /// <param name="smalWrite">Вывести сокращенную запись</param>
        /// <returns></returns>
        string ToString(bool smalWrite);
        /// <summary>
        /// Возвращает строку в формате JSON
        /// </summary>
        /// <returns></returns>
        string ToJSON();
        //void OnRaiseAddEvent(IXmlDocumentNode node);
        //void OnRaiseAddEvent(IXmlDocumentNode node, object sender);
        //void OnRaiseRemoveEvent(IXmlDocumentNode node);
        //void OnRaiseRemoveEvent(IXmlDocumentNode node, object sender);
        //void OnRaiseChangeEvent(IXmlDocumentNode node);
        //void OnRaiseChangeEvent(IXmlDocumentNode node, object sender);
    }
}
