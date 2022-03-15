
using System;
using System.Collections.Generic;

namespace Modules.Networking
{
    public enum DataFormat
    {
        Json,
        MessagePack,
    }

    public interface IWebRequest
    {
        //----- params -----

        //----- field -----

        //----- property -----

        /// <summary> ���N�G�X�g�^�C�v. </summary>
        string Method { get; }

        /// <summary> URL. </summary>
        string HostUrl { get; }

        /// <summary> ���N�G�X�gURL. </summary>
        string Url { get; }

        /// <summary> �w�b�_�[���. </summary>
        IDictionary<string, Tuple<bool, string>> Headers { get; }
        
        /// <summary> URL�p�����[�^. </summary>
        IDictionary<string, object> UrlParams { get; }

        /// <summary> ����M�f�[�^�̈��k. </summary>
        bool Compress { get; }

        /// <summary> �ʐM�f�[�^�t�H�[�}�b�g. </summary>
        DataFormat Format { get; }

        /// <summary> �^�C���A�E�g����(�b). </summary>
        int TimeOutSeconds { get; }

        /// <summary> �ʐM����. </summary>
        bool Connecting { get; }

        /// <summary> �X�e�[�^�X�R�[�h. </summary>
        string StatusCode { get; }

        //----- method -----

        void Initialize(string hostUrl, bool compress, DataFormat format = DataFormat.MessagePack);

        IObservable<TResult> Get<TResult>(IProgress<float> progress = null) where TResult : class;

        IObservable<TResult> Post<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;

        IObservable<TResult> Put<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;

        IObservable<TResult> Patch<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;

        IObservable<TResult> Delete<TResult>(IProgress<float> progress = null) where TResult : class;

        void Cancel(bool throwException = false);

        string GetUrlParamsString();

        string GetHeaderString();

        string GetBodyString();
    }
}