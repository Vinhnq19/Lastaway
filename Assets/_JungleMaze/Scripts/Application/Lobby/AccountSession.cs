using System;
using JungleMaze.Domain.Lobby;

namespace JungleMaze.App.Lobby
{
    /// <summary>
    /// Login/register for online play (AC12, AC13, AC17). The JWT and its expiry live in this object and nowhere
    /// else (Q6); the password field is cleared the moment it is sent, success or not. Joining a network room now
    /// needs a valid account token, while offline practice never uses this object. Every submit carries a request
    /// generation: a callback that outlives <see cref="Logout"/>/<see cref="Expire"/> or a newer submit is dropped,
    /// so a late answer never restores a token, never starts a follow-up login and never touches newer state.
    /// </summary>
    public sealed class AccountSession
    {
        private readonly IAccountApi _api;
        private readonly Func<DateTime> _utcNow;
        private AuthToken? _token;
        private int _request;

        public AccountSession(IAccountApi api, Func<DateTime> utcNow)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        }

        public string Username = "", Password = "";
        public bool RegisterMode;
        public bool Busy { get; private set; }
        public string Message { get; private set; } = "";

        /// <summary>Raised once per successful login.</summary>
        public event Action LoggedIn;

        /// <summary>Raised on logout or token expiry so in-RAM tokens can be cleared.</summary>
        public event Action LoggedOut;

        public bool IsValid => _token.HasValue && _utcNow() < _token.Value.ExpiresAtUtc;

        /// <summary>Null when there is no valid token; an expired one is dropped with the expiry notice (AC17).</summary>
        public string Token
        {
            get
            {
                if (IsValid) return _token.Value.AccessToken;
                if (_token.HasValue) Expire();
                return null;
            }
        }

        public string Validate()
        {
            var user = (Username ?? "").Trim();
            if (user.Length < LobbyLimits.UserMin || user.Length > LobbyLimits.UserMax) return "Tên đăng nhập cần 3–32 ký tự";
            var pass = Password ?? "";
            var min = RegisterMode ? LobbyLimits.PassMin : 1;
            if (pass.Length < min || pass.Length > LobbyLimits.PassMax)
                return RegisterMode ? "Mật khẩu cần 8–128 ký tự" : "Nhập mật khẩu";
            return null;
        }

        public bool Submit()
        {
            if (Busy) return false;
            var invalid = Validate();
            if (invalid != null) { Message = invalid; return false; }
            var user = Username.Trim();
            var pass = Password;
            Password = ""; // AC13: never kept after sending, even when the call fails
            var request = ++_request;
            Busy = true;
            Message = "";
            if (RegisterMode)
                _api.Register(user, pass, r =>
                {
                    if (request != _request) return; // logged out, expired or superseded: no follow-up login
                    if (!r.Ok) { Busy = false; Message = ApiMessages.For(ApiOp.Register, r); return; }
                    _api.Login(user, pass, t => OnLogin(t, request));
                });
            else _api.Login(user, pass, r => OnLogin(r, request));
            return true;
        }

        /// <summary>401 or passed expiry: drop the token, ask again, never resend (AC17).</summary>
        public void Expire()
        {
            _request++;
            Busy = false;
            _token = null;
            Message = ApiMessages.SessionExpired;
            LoggedOut?.Invoke();
        }

        public void Logout()
        {
            _request++;
            Busy = false;
            _token = null;
            Message = "";
            LoggedOut?.Invoke();
        }

        private void OnLogin(ApiResult<AuthToken> r, int request)
        {
            if (request != _request) return; // a stale answer never logs in nor touches newer state
            Busy = false;
            if (!r.Ok || string.IsNullOrEmpty(r.Value.AccessToken))
            {
                Message = r.Ok ? ApiMessages.AccountUnreachable : ApiMessages.For(ApiOp.Login, r);
                return;
            }
            if (_utcNow() >= r.Value.ExpiresAtUtc)
            {
                Message = ApiMessages.SessionExpired; // an already-expired token is not a session
                return;
            }
            _token = r.Value;
            Message = "";
            RegisterMode = false;
            LoggedIn?.Invoke();
        }
    }
}
