
#if ENABLE_MOONSHARP

using System;
using UniRx;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public interface IMessageWindow
    {
        void ShowMessage(string name, string text);

        void Hide();

        IObservable<Unit> OnShowMessageFinish();
    }

    public sealed class Message : Command
    {
        //----- params -----

        //----- field -----

        private IDisposable showMessageFinishDisposable = null;

        //----- property -----

        public override string CommandName { get { return "Message"; } }

        public IMessageWindow MessageWindow { get; set; }

        //----- method -----

        public override object GetCommandDelegate()
        {
            return (Func<string, string, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string text, string name = null)
        {
            var advEngine = AdvEngine.Instance;

            if (text == null)
            {
                MessageWindow.Hide();

                return DynValue.Void;
            }

            var displayName = name;

            if (showMessageFinishDisposable == null)
            {
                showMessageFinishDisposable = MessageWindow.OnShowMessageFinish()
                    .Subscribe(_ => advEngine.Resume())
                    .AddTo(Disposable);
            }

            MessageWindow.ShowMessage(displayName, text);
            
            return YieldWait;
        }
    }
}

#endif
