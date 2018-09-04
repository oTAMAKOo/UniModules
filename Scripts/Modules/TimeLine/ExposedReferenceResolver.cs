
using System;
using UniRx;
using UnityEngine;
using UnityEngine.Playables;

namespace Modules.TimeLine
{
	public class ExposedReferenceResolver<T> where T : UnityEngine.Object
    {
        //----- params -----

        //----- field -----

        private PlayableDirector playableDirector = null;
        private ExposedReference<T> reference;

        private Subject<ExposedReference<T>> onUpdateReference = null;

        //----- property -----

        public ExposedReference<T> Reference { get{ return reference; } }

        //----- method -----

        public ExposedReferenceResolver(PlayableDirector playableDirector, ExposedReference<T> reference)
        {
            this.playableDirector = playableDirector;
            this.reference = reference;

            Resolve();
        }

        public void Resolve()
        {
            GetValue();
        }

        public void SetValue(T value)
        {
            Clear();

            var playableGraph = GetPlayableGraph();
            var resolver = playableGraph.GetResolver();

            if (value != null)
            {
                reference.exposedName = Guid.NewGuid().ToString();
                resolver.SetReferenceValue(reference.exposedName, value);
            }

            if (onUpdateReference != null)
            {
                onUpdateReference.OnNext(reference);
            }
        }

        public T GetValue()
        {
            var playableGraph = GetPlayableGraph();
            var resolver = playableGraph.GetResolver();

            return Reference.Resolve(resolver);
        }

        public void Clear()
        {
            var playableGraph = GetPlayableGraph();
            var resolver = playableGraph.GetResolver();

            resolver.ClearReferenceValue(reference.exposedName);

            reference.exposedName = null;

            if (onUpdateReference != null)
            {
                onUpdateReference.OnNext(reference);
            }
        }

        private PlayableGraph GetPlayableGraph()
        {
            var playableGraph = playableDirector.playableGraph;

            if (!playableGraph.IsValid())
            {
                playableDirector.RebuildGraph();
                playableGraph = playableDirector.playableGraph;
            }

            return playableGraph;
        }

        public IObservable<ExposedReference<T>> OnUpdateReferenceAsObservable()
        {
            return onUpdateReference ?? (onUpdateReference = new Subject<ExposedReference<T>>());
        }
    }
}
