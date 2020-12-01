﻿using System;
using System.IO;
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

        public UserStateService(IStreamSerializer streamSerializer,
            IHttpClientState httpClientState,
            IDevice device)
        {
            _streamSerializer = streamSerializer;
            _httpClientState = httpClientState;
            Device = device;
        }

        public InstaUserShort CurrentUser { get; private set; }
        public IDevice Device { get; private set; }
        public string RankToken { get; private set; }
        public string CsrfToken { get; private set; }

        public void SetUser(InstaUserShort user)
        {
            CurrentUser = user;
            var cookies = _httpClientState.GetCookieContainer();
            var instaCookies = cookies.GetCookies(new Uri(Constants.BASE_URI));
            CsrfToken = instaCookies[Constants.CSRFTOKEN]?.Value ?? string.Empty;
            RankToken = $"{CurrentUser.Pk}_{Device.DeviceId}";
        }

        public void PerformLogout()
        {
            CurrentUser = null;
        }

        /// <summary>
        ///     Get current state info as Memory stream
        /// </summary>
        /// <returns>
        ///     State data as byte array
        /// </returns>
        public byte[] GetStateDataAsByteArray()
        {
            if (CurrentUser == null)
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
                    RankToken = $"{CurrentUser.Pk}_{Device.DeviceId}",
                    LoggedInUser = CurrentUser
                }
            };
            using var stream = _streamSerializer.Serialize(state);
            return stream.ToByteArray();
        }

        /// <summary>
        ///     Loads the state data from stream.
        /// </summary>
        /// <param name="bytes">The byte array containing state data.</param>
        public void LoadStateDataFromByteArray(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            var data = _streamSerializer.Deserialize<UserState>(stream);
            _httpClientState.SetCookies(data.Cookies);
            SetUser(data.UserSession.LoggedInUser);
            Device = data.Device;
            RankToken = data.UserSession.RankToken;
            CsrfToken = data.UserSession.CsrfToken;
        }
    }
}