﻿using System;
using System.IO;
using System.Net;
using InstaSharper.Abstractions.API.Services;
using InstaSharper.Abstractions.Device;
using InstaSharper.Abstractions.Models.User;
using InstaSharper.Abstractions.Models.UserState;
using InstaSharper.Abstractions.Serialization;
using InstaSharper.Http;
using InstaSharper.Utils;

namespace InstaSharper.API.Services
{
    internal class UserStateService : IUserStateService, IApiStateProvider
    {
        private readonly IHttpClientState _httpClientState;
        private readonly IStreamSerializer _streamSerializer;

        private InstaUserShort _user;

        public UserStateService(IStreamSerializer streamSerializer,
            IHttpClientState httpClientState,
            IDevice device)
        {
            _streamSerializer = streamSerializer;
            _httpClientState = httpClientState;
            Device = device;
        }

        public IDevice Device { get; private set; }
        public string RankToken { get; private set; }
        public string CsrfToken { get; private set; }

        public void SetUser(InstaUserShort user)
        {
            _user = user;
            var cookies = _httpClientState.GetCookieContainer();
            var instaCookies = cookies.GetCookies(new Uri(Constants.BASE_URI));
            CsrfToken = instaCookies[Constants.CSRFTOKEN]?.Value ?? string.Empty;
            RankToken = $"{_user.Pk}_{Device.DeviceId}";
        }

        public void PerformLogout()
        {
            _user = null;
        }

        /// <summary>
        ///     Get current state info as Memory stream
        /// </summary>
        /// <returns>
        ///     State data
        /// </returns>
        public Stream GetStateDataAsStream()
        {
            if (_user == null)
                throw new Exception("User must be authenticated");
            var cookies = _httpClientState.GetCookieContainer();
            var instaCookies = cookies.GetCookies(new Uri(Constants.BASE_URI));
            var state = new UserState
            {
                Cookies = cookies,
                Device = Device,
                UserSession = new UserSession
                {
                    CsrfToken = instaCookies[Constants.CSRFTOKEN]?.Value ?? string.Empty,
                    RankToken = $"{_user.Pk}_{Device.DeviceId}",
                    LoggedInUser = _user
                }
            };
            return _streamSerializer.Serialize(state);
        }

        /// <summary>
        ///     Loads the state data from stream.
        /// </summary>
        /// <param name="stream">The stream containing state data.</param>
        public void LoadStateDataFromStream(Stream stream)
        {
            var data = _streamSerializer.Deserialize<UserState>(stream);
            _httpClientState.SetCookies(data.Cookies);
            SetUser(data.UserSession.LoggedInUser);
            Device = data.Device;
            RankToken = data.UserSession.RankToken;
            CsrfToken = data.UserSession.CsrfToken;
        }
    }
}