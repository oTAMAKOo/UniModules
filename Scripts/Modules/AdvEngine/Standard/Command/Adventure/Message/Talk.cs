
using System;
using UniRx;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class Talk : Command
    {
        //----- params -----

        //----- field -----

        private IDisposable showMessageFinishDisposable = null;

        //----- property -----

        public override string CommandName { get { return "Talk"; } }

        public IMessageWindow MessageWindow { get; set; }

        //----- method -----        

        public override object GetCommandDelegate()
        {
            return (Func<string, string, string, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, string text, string name = null)
        {
            var advEngine = AdvEngine.Instance;

            var displayName = name;

            if (string.IsNullOrEmpty(displayName))
            {
                if (!string.IsNullOrEmpty(identifier))
                {
                    var advCharacter = advEngine.ObjectManager.Get<AdvCharacter>(identifier);

                    if (advCharacter != null)
                    {
                        displayName = advCharacter.CharacterName;
                    }
                }
            }

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