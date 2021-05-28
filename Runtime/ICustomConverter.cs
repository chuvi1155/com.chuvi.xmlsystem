using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMLSystem.Xml
{
    public interface ICustomConverter
    {
        bool CanConvert(Type type);
        T Convert<T>(string value);
        void Convert(string value, ref object result, Type returnedType = null);
    }
}
