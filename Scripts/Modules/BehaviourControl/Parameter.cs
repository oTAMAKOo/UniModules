
using System;
using System.Linq;

namespace Modules.BehaviorControl
{
    public sealed class Parameter
    {
        //----- params -----

        //----- field -----

        private string[] contents = null;

        private Action<string> onError = null;

        //----- property -----

        //----- method -----

        public Parameter(string[] contents, Action<string> onError)
        {
            this.contents = contents;
            this.onError = onError;
        }

        public T Get<T>(int index)
        {
            var content = contents.ElementAtOrDefault(index);

            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException();
            }

            var result = default(T);

            try
            {
                result = (T)Convert.ChangeType(content, typeof(T));
            }
            catch (FormatException exception)
            {
                if (onError != null)
                {
                    var message = string.Format("型変換に失敗しました。\n{0}", exception.Message);

                    onError.Invoke(message);
                }

                return default(T);
            }

            return result;
        }
    }
}
