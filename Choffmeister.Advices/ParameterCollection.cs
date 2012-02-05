using System.Collections.Generic;
using System.Linq;

namespace Choffmeister.Advices
{
    public class ParameterCollection
    {
        private Dictionary<string, object> _dictionary;

        public object this[int index]
        {
            get { return _dictionary.ElementAt(index); }
        }

        public object this[string name]
        {
            get { return _dictionary[name]; }
        }

        public Dictionary<string, object> Dictionary
        {
            get { return new Dictionary<string, object>(_dictionary); }
        }

        public string[] AllParameterNames
        {
            get
            {
                string[] names = new string[_dictionary.Keys.Count];
                _dictionary.Keys.CopyTo(names, 0);

                return names;
            }
        }

        public object[] AllParameterValues
        {
            get
            {
                object[] values = new object[_dictionary.Values.Count];
                _dictionary.Values.CopyTo(values, 0);

                return values;
            }
        }

        public ParameterCollection()
        {
            _dictionary = new Dictionary<string, object>();
        }

        public void Add(string name, object value)
        {
            _dictionary.Add(name, value);
        }
    }
}