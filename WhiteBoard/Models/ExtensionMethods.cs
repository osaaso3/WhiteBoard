using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board.Client.Models
{
    public static class ExtensionMethods
    {
        public static string ToImageUrl(this byte[] buffer, string format = "image/png")
        {
            return $"data:{format};base64,{Convert.ToBase64String(buffer)}";
        }
        public static TAttribute GetEnumAttribute<TAttribute>(this Enum value)
        where TAttribute : Attribute
        {
            var enumType = value.GetType();
            var name = Enum.GetName(enumType, value);
            return enumType.GetField(name).GetCustomAttributes(false).OfType<TAttribute>().SingleOrDefault();
        }
    }
}
