﻿﻿
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Modules.Devkit.Prefs;
using UniRx;
using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace Modules.Devkit.Spreadsheet
{
    public static class SpreadsheetConnectorPrefs
    {
        public static string accessTokenKey
        {
            get { return ProjectPrefs.GetString("SpreadsheetConnectorPrefs-accessTokenKey", string.Empty); }
            set { ProjectPrefs.SetString("SpreadsheetConnectorPrefs-accessTokenKey", value); }
        }

        public static string refreshTokenKey
        {
            get { return ProjectPrefs.GetString("SpreadsheetConnectorPrefs-refreshTokenKey", string.Empty); }
            set { ProjectPrefs.SetString("SpreadsheetConnectorPrefs-refreshTokenKey", value); }
        }

        public static string tokenExpiryKey
        {
            get { return ProjectPrefs.GetString("SpreadsheetConnectorPrefs-tokenExpiryKey", string.Empty); }
            set { ProjectPrefs.SetString("SpreadsheetConnectorPrefs-tokenExpiryKey", value); }
        }

        public static void Clear()
        {
            accessTokenKey = null;
            refreshTokenKey = null;
            tokenExpiryKey = null;
        }
    }

    // Google Spreadsheetにアクセスする為のクラス.
    // リファレンス： https://developers.google.com/google-apps/spreadsheets/
    public class SpreadsheetConnector
	{
        //----- params -----

        public enum AuthenticationState
        {
            SignIn,
            SignOut,
            WaitingAccessCode
        }

        //----- field -----

        private OAuth2Parameters parameters = null;
        private SpreadsheetsService service = null;

        private SpreadsheetConfig config = null;

        //----- property -----

        public AuthenticationState State { get; private set; }

        //----- method -----

        public void Initialize(SpreadsheetConfig config)
        {
            this.config = config;

            System.Net.ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;

            DateTime dt;

            parameters = GetRawOAuth2Parameters();

            parameters.AccessToken = SpreadsheetConnectorPrefs.accessTokenKey;
            parameters.RefreshToken = SpreadsheetConnectorPrefs.refreshTokenKey;

            if (DateTime.TryParse(SpreadsheetConnectorPrefs.tokenExpiryKey, out dt))
            {
                parameters.TokenExpiry = dt;
            }

            if (!string.IsNullOrEmpty(parameters.AccessToken))
            {
                service = CreateService(parameters);
                State = AuthenticationState.SignIn;
            }
            else
            {
                State = AuthenticationState.SignOut;
            }
        }

        public void OpenAccessCodeURL()
        {
            parameters = GetRawOAuth2Parameters();
            var authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl(parameters);

            State = AuthenticationState.WaitingAccessCode;

            Application.OpenURL(authorizationUrl);
        }

        public bool SignIn(string accessCode)
        {
            try
            {
                parameters = GetRawOAuth2Parameters();
                parameters.AccessCode = accessCode;

                // 同期WebRequest通信が中で走る.
                OAuthUtil.GetAccessToken(parameters);

                SpreadsheetConnectorPrefs.accessTokenKey  = parameters.AccessToken;
                SpreadsheetConnectorPrefs.refreshTokenKey = parameters.RefreshToken;
                SpreadsheetConnectorPrefs.tokenExpiryKey  = parameters.TokenExpiry.ToString();

                service = CreateService(parameters);

                State = AuthenticationState.SignIn;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SignOut();
                return false;
            }
        }

        public void SignOut()
        {
            parameters = GetRawOAuth2Parameters();

            SpreadsheetConnectorPrefs.accessTokenKey = null;
            SpreadsheetConnectorPrefs.refreshTokenKey = null;
            SpreadsheetConnectorPrefs.tokenExpiryKey = null;

            service = null;

            State = AuthenticationState.SignOut;
        }

        public SheetEntity[] GetSpreadsheet(string spreadsheetUrl)
        {
            var result = new List<SheetEntity>();

            if(ShowRequireSignInMessage()){ return result.ToArray(); }

            var entry = GetSpreadsheetEntry(spreadsheetUrl);

            if (entry == null){ return result.ToArray(); }

            foreach (var sheetEntry in entry.Worksheets.Entries.Cast<WorksheetEntry>())
            {
                var cellQuery = new CellQuery(sheetEntry.CellFeedLink);

                // 通信.
                var cellFeed = service.Query(cellQuery);

                var sheet = new SheetEntity(sheetEntry.Title.Text, sheetEntry.Updated, cellFeed.Entries.Cast<CellEntry>());

                result.Add(sheet);
            }

            return result.ToArray();
        }

        private bool ShowRequireSignInMessage()
        {
            var result = State != AuthenticationState.SignIn;

            if (result)
            {
                Debug.LogError("サインインされていません");
            }

            return result;
        }

        private SpreadsheetEntry GetSpreadsheetEntry(string spreadsheetId)
        {
            var feed = service.Query(new SpreadsheetQuery());

            var entry = feed.Entries.Cast<SpreadsheetEntry>().FirstOrDefault(x => x.AlternateUri.Content.Contains(spreadsheetId));

            if (entry == null)
            {
                Debug.LogError("対象のスプレッドシートが見つかりませんでした：\n" + spreadsheetId);
            }

            return entry;
        }

        private OAuth2Parameters GetRawOAuth2Parameters()
        {
            var authParameter = new OAuth2Parameters
            {
                ClientId = config.ClientId,
                ClientSecret = config.ClientSecret,
                RedirectUri = config.RedirectUri,
                Scope = config.Scope,
            };

            return authParameter;
        }

        private static SpreadsheetsService CreateService(OAuth2Parameters parameters)
        {
            var requestFactory = new GOAuth2RequestFactory("structuredcontent", "", parameters);

            return new SpreadsheetsService("") { RequestFactory = requestFactory };
        }
    }
}