
namespace XMLSystem.Xml
{
    public enum XNodeType
    {
        None,
        Coment,
        InnerText,
        /// <summary>
        /// указывает на тип значения в JSON, это строка
        /// </summary>
        JSONString,
        /// <summary>
        /// указывает на тип значения в JSON, это любой другой тип, который не отображается в кавычках
        /// </summary>
        JSONOther,
    }
}
