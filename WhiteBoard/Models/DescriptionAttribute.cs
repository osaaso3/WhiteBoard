using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Board.Client.Models
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class DescriptionAttribute : Attribute
    {
        public string Text { get; }
     
        public DescriptionAttribute(string text)
        {
            Text = text;
        }
    }
}
