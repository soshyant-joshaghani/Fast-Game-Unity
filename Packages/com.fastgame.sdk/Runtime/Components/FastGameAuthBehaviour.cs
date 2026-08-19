using System;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace FastGame
{
    /// <summary>ENTER route — mirrors UE EFastGameEnterRoute exec pins.</summary>
    public enum FastGameEnterRoute
    {
        Login,
        CompleteAccount,
        VerifyId,
        Register,
        Failed,
    }

    [Serializable] public class FastGameBoolEvent : UnityEvent<bool> { }
    [Serializable] public class FastGameStringEvent : UnityEvent<string> { }
    [Serializable] public class FastGameAuthResultEvent : UnityEvent<bool, int, string> { }
    [Serializable] public class FastGameEnterResultEvent : UnityEvent<FastGameEnterRoute, bool, string> { }
    [Serializable] public class FastGameUserEvent : UnityEvent<UserProfile> { }

    /// <summary>
    /// Auth UI controller (UE Fast Game Auth + page widgets).
    /// Hierarchy example:
    /// <code>
    /// AUTH (global canvas)
    ///   Enter ID Canvas
    ///   Enter Password Canvas
    ///   Enter Signup Canvas
    ///   Enter Recovery OTP Canvas
    ///   Enter Recovery Reset Canvas
    /// </code>
    /// Buttons → <see cref="Enter"/> / <see cref="Login"/> / <see cref="Signup"/> /
    /// <see cref="BeginForgot"/> / <see cref="SendCode"/> / <see cref="VerifyCode"/> /
    /// <see cref="ResetPassword"/> / <see cref="UpdateFullName"/> / <see cref="Back"/>.
    /// </summary>
    [AddComponentMenu("Fast Game/Auth")]
    public sealed class FastGameAuthBehaviour : MonoBehaviour
    {
        [Header("Client")]
        [Tooltip("Leave empty to use FastGameClientBehaviour.Instance")]
        public FastGameClientBehaviour ClientHost;

        [Header("Pages (canvases under AUTH)")]
        [Tooltip("Optional global AUTH root — kept active; leave empty if unused.")]
        public GameObject AuthCanvas;
        [Tooltip("Enter identity page (start).")]
        public GameObject EnterIdCanvas;
        [Tooltip("Existing user — enter password.")]
        public GameObject EnterPasswordCanvas;
        [Tooltip("New user — signup.")]
        public GameObject EnterSignupCanvas;
        [Tooltip("OTP send / verify (shared by recovery and signup verification).")]
        [FormerlySerializedAs("EnterRecoveryCanvas")]
        public GameObject EnterRecoveryOtpCanvas;
        [Tooltip("Recovery step 3: new password + confirm (after OTP verified).")]
        public GameObject EnterRecoveryResetCanvas;
        [Tooltip("If true, Enter routes auto-switch pages (no need to wire SetActive in events).")]
        public bool AutoSwitchPages = true;
        [Tooltip("After ResetPassword succeeds, show Enter Password canvas for login.")]
        public bool ShowLoginAfterReset = true;
        [Tooltip("Show Enter ID page on Awake.")]
        public bool ShowEnterIdOnAwake = true;

        [Header("Inputs (manual fallbacks if UI empty)")]
        public string Identity;
        public string Password;
        public string PasswordConfirm;
        public string FullName;
        public string OtpCode;
        public FastGameIdentityChannel Channel = FastGameIdentityChannel.Auto;

        [Header("UI — shared (TMP_InputField / InputField root)")]
        [Tooltip("Identity input on Enter ID canvas.")]
        public Component IdentityField;
        [Tooltip("Optional full name on signup canvas.")]
        public Component FullNameField;
        [Tooltip("Shared error label (TMP_Text or UI Text) shown on any auth failure.")]
        public Component ErrorText;

        [Header("UI — Login (Enter Password canvas)")]
        [FormerlySerializedAs("PasswordField")]
        public Component LoginPasswordField;

        [Header("UI — Signup (Enter Signup canvas)")]
        public Component SignupPasswordField;
        [FormerlySerializedAs("PasswordConfirmField")]
        public Component SignupPasswordConfirmField;

        [Header("UI — Recovery OTP canvas")]
        [FormerlySerializedAs("OtpCodeField")]
        public Component RecoveryOtpField;

        [Header("UI — Recovery Reset canvas")]
        public Component RecoveryPasswordField;
        public Component RecoveryPasswordConfirmField;

        public bool Busy { get; private set; }

        [Header("ENTER routes (extra hooks; pages switch automatically when AutoSwitchPages)")]
        public UnityEvent OnLoginRoute;
        public UnityEvent OnCompleteAccountRoute;
        public UnityEvent OnVerifyId;
        public UnityEvent OnRegisterRoute;
        [FormerlySerializedAs("OnEnterPassword")]
        public UnityEvent OnEnterPassword;
        [FormerlySerializedAs("OnSignupRoute")]
        public UnityEvent OnSignupRoute;
        [FormerlySerializedAs("OnRecoverPassword")]
        public UnityEvent OnRecoverPassword;
        [FormerlySerializedAs("OnVerifySignup")]
        public UnityEvent OnVerifySignup;
        public UnityEvent OnOtpVerified;
        public FastGameStringEvent OnEnterFailed;
        public FastGameEnterResultEvent OnEnterComplete;
        public UnityEvent OnBackToEnterId;

        [Header("Auth results")]
        public FastGameAuthResultEvent OnLoginComplete;
        public FastGameAuthResultEvent OnSignupComplete;
        public FastGameAuthResultEvent OnRecoveryStepComplete;
        public FastGameUserEvent OnGetMeComplete;
        public FastGameBoolEvent OnCheckAuthenticationComplete;
        public FastGameStringEvent OnError;

        public FastGameEnterResult LastEnter { get; private set; }
        public FastGameEnterRoute LastEnterRoute { get; private set; } = FastGameEnterRoute.Failed;
        public bool ForgotPassword { get; private set; }

        public bool IsAuthenticated =>
            ClientHost != null ? ClientHost.Client?.Auth.IsAuthenticated == true
                : FastGameClientBehaviour.Instance?.Client?.Auth.IsAuthenticated == true;

        FastGameClient Client => FastGameClientBehaviour.RequireClient(ClientHost);

        void Awake()
        {
            if (AuthCanvas != null)
                AuthCanvas.SetActive(true);
            if (ShowEnterIdOnAwake)
                ShowEnterIdPage();
        }

        string ReadIdentity() => FastGameUiText.Read(IdentityField, Identity).Trim();
        string ReadFullName() => FastGameUiText.Read(FullNameField, FullName);
        string ReadLoginPassword() => FastGameUiText.Read(LoginPasswordField, Password);
        string ReadSignupPassword() => FastGameUiText.Read(SignupPasswordField, Password);
        string ReadSignupConfirm() => FastGameUiText.Read(SignupPasswordConfirmField, PasswordConfirm);
        string ReadRecoveryPassword() => FastGameUiText.Read(RecoveryPasswordField, Password);
        string ReadRecoveryConfirm() => FastGameUiText.Read(RecoveryPasswordConfirmField, PasswordConfirm);
        string ReadOtp() => FastGameUiText.Read(RecoveryOtpField, OtpCode).Trim();

        void SetError(string message)
        {
            FastGameUiText.WriteLabel(ErrorText, message ?? "");
            if (!string.IsNullOrEmpty(message))
                OnError?.Invoke(message);
        }

        void ClearError() => FastGameUiText.WriteLabel(ErrorText, "");

        // --- Page helpers ---------------------------------------------------

        public void ShowEnterIdPage() => ShowPage(EnterIdCanvas);
        public void ShowEnterPasswordPage() => ShowPage(EnterPasswordCanvas);
        public void ShowEnterSignupPage() => ShowPage(EnterSignupCanvas);
        public void ShowEnterRecoveryOtpPage() => ShowPage(EnterRecoveryOtpCanvas);
        public void ShowEnterRecoveryResetPage() => ShowPage(EnterRecoveryResetCanvas);

        /// <summary>Alias — recovery starts on OTP canvas.</summary>
        public void ShowEnterRecoveryPage() => ShowEnterRecoveryOtpPage();

        public void ShowPage(GameObject page)
        {
            if (AuthCanvas != null)
                AuthCanvas.SetActive(true);
            SetPageActive(EnterIdCanvas, page == EnterIdCanvas);
            SetPageActive(EnterPasswordCanvas, page == EnterPasswordCanvas);
            SetPageActive(EnterSignupCanvas, page == EnterSignupCanvas);
            SetPageActive(EnterRecoveryOtpCanvas, page == EnterRecoveryOtpCanvas);
            SetPageActive(EnterRecoveryResetCanvas, page == EnterRecoveryResetCanvas);
        }

        static void SetPageActive(GameObject page, bool active)
        {
            if (page != null)
                page.SetActive(active);
        }

        void ApplyEnterRoute(FastGameEnterRoute route)
        {
            if (!AutoSwitchPages)
                return;
            switch (route)
            {
                case FastGameEnterRoute.Login:
                    ShowEnterPasswordPage();
                    break;
                case FastGameEnterRoute.CompleteAccount:
                case FastGameEnterRoute.Register:
                    ShowEnterSignupPage();
                    break;
                case FastGameEnterRoute.VerifyId:
                    ShowEnterRecoveryOtpPage();
                    break;
                default:
                    ShowEnterIdPage();
                    break;
            }
        }

        // --- Buttons (wire Button OnClick here) -----------------------------

        /// <summary>Enter ID canvas — Continue / Enter.</summary>
        public void Enter() => _ = Run(EnterAsync);

        /// <summary>Enter Password canvas — Login.</summary>
        public void Login() => _ = Run(LoginAsync);

        /// <summary>Register / Complete Account canvas — create or finish passwordless account.</summary>
        public void Signup() => _ = Run(FinishCredentialsAsync);
        public void Register() => Signup();
        public void CompleteAccount() => Signup();

        /// <summary>From Login screen — next OTP is recovery, then Set Password.</summary>
        public void BeginForgot()
        {
            ForgotPassword = true;
            if (AutoSwitchPages)
                ShowEnterRecoveryOtpPage();
        }

        /// <summary>After login — PATCH display name.</summary>
        public void UpdateFullName() => _ = Run(UpdateFullNameAsync);

        /// <summary>OTP canvas — send code (signup verify or recovery).</summary>
        public void SendCode() => _ = Run(SendOtpAsync);

        /// <summary>OTP canvas — verify code then advance to Signup or Reset.</summary>
        public void VerifyCode() => _ = Run(VerifyOtpAsync);

        /// <summary>Enter Recovery canvas — recovery 3/3 set new password.</summary>
        public void ResetPassword() => _ = Run(ConfirmPasswordRecoveryAsync);

        /// <summary>
        /// Back — clear ENTER identity + input fields, return to Enter ID canvas.
        /// </summary>
        public void Back()
        {
            string clearErr = null;
            try
            {
                ClearEnteredIdentity();
            }
            catch (Exception e)
            {
                clearErr = e.Message;
            }

            Identity = "";
            Password = "";
            PasswordConfirm = "";
            FullName = "";
            OtpCode = "";
            FastGameUiText.Write(IdentityField, "");
            FastGameUiText.Write(FullNameField, "");
            FastGameUiText.Write(LoginPasswordField, "");
            FastGameUiText.Write(SignupPasswordField, "");
            FastGameUiText.Write(SignupPasswordConfirmField, "");
            FastGameUiText.Write(RecoveryOtpField, "");
            FastGameUiText.Write(RecoveryPasswordField, "");
            FastGameUiText.Write(RecoveryPasswordConfirmField, "");

            LastEnter = null;
            LastEnterRoute = FastGameEnterRoute.Failed;
            ForgotPassword = false;
            ShowEnterIdPage();
            OnBackToEnterId?.Invoke();

            if (!string.IsNullOrEmpty(clearErr))
                SetError(clearErr);
            else
                ClearError();
        }

        // --- Async API ------------------------------------------------------

        public async Task EnterAsync()
        {
            ClearError();
            try
            {
                var identity = ReadIdentity();
                Identity = identity;
                var enter = await Client.Auth.EnterAsync(identity, Channel);
                LastEnter = enter;

                FastGameEnterRoute route;
                if (!enter.Exists)
                {
                    var needsVerify = false;
                    var gameCode = Client.Config.GameCode;
                    if (!string.IsNullOrWhiteSpace(gameCode))
                    {
                        var (verifyPhone, verifyEmail) =
                            await Client.Catalog.GetAuthRequirementsAsync(gameCode);
                        needsVerify = enter.IsPhone ? verifyPhone : verifyEmail;
                    }
                    route = needsVerify
                        ? FastGameEnterRoute.VerifyId
                        : FastGameEnterRoute.Register;
                }
                else if (enter.PasswordRequired)
                    route = FastGameEnterRoute.CompleteAccount;
                else
                    route = FastGameEnterRoute.Login;

                ForgotPassword = false;
                LastEnterRoute = route;
                ApplyEnterRoute(route);
                OnEnterComplete?.Invoke(route, true, "");
                switch (route)
                {
                    case FastGameEnterRoute.Login:
                        OnLoginRoute?.Invoke();
                        OnEnterPassword?.Invoke();
                        break;
                    case FastGameEnterRoute.Register:
                        OnRegisterRoute?.Invoke();
                        OnSignupRoute?.Invoke();
                        break;
                    case FastGameEnterRoute.CompleteAccount:
                        OnCompleteAccountRoute?.Invoke();
                        OnRecoverPassword?.Invoke();
                        break;
                    case FastGameEnterRoute.VerifyId:
                        OnVerifyId?.Invoke();
                        OnVerifySignup?.Invoke();
                        break;
                }
            }
            catch (Exception e)
            {
                LastEnterRoute = FastGameEnterRoute.Failed;
                var msg = e.Message;
                if (AutoSwitchPages)
                    ShowEnterIdPage();
                OnEnterComplete?.Invoke(FastGameEnterRoute.Failed, false, msg);
                OnEnterFailed?.Invoke(msg);
                SetError(msg);
            }
        }

        public async Task LoginAsync()
        {
            ClearError();
            try
            {
                var identity = ReadIdentity();
                var password = ReadLoginPassword();
                Password = password;
                await Client.Auth.LoginAsync(identity, password, Channel);
                OnLoginComplete?.Invoke(true, 200, "ok");
            }
            catch (Exception e)
            {
                FailAuth(OnLoginComplete, e);
            }
        }

        public async Task SignupAsync() => await FinishCredentialsAsync();

        public async Task FinishCredentialsAsync()
        {
            ClearError();
            try
            {
                var password = ReadSignupPassword();
                var confirm = ReadSignupConfirm();
                var fullName = ReadFullName();
                Password = password;
                PasswordConfirm = confirm;
                FullName = fullName;
                if (LastEnterRoute == FastGameEnterRoute.CompleteAccount)
                    await Client.Auth.CompleteAccountAsync(null, password, confirm, fullName, null);
                else
                    await Client.Auth.SignupAsync(null, password, confirm, fullName, null);
                OnSignupComplete?.Invoke(true, 200, "ok");
                OnLoginComplete?.Invoke(true, 200, "ok");
            }
            catch (Exception e)
            {
                FailAuth(OnSignupComplete, e);
            }
        }

        public void RequestPasswordRecovery() => SendCode();

        public async Task SendOtpAsync()
        {
            ClearError();
            try
            {
                if (LastEnterRoute == FastGameEnterRoute.VerifyId)
                    await Client.Auth.RequestSignupVerificationAsync(ReadIdentity());
                else if (ForgotPassword)
                    await Client.Auth.RequestPasswordRecoveryAsync(ReadIdentity());
                else
                    throw new FastGameException("Send Auth Code: use after Verify Id or Begin Forgot");
                OnRecoveryStepComplete?.Invoke(true, 200, "otp sent");
            }
            catch (Exception e)
            {
                FailAuth(OnRecoveryStepComplete, e);
            }
        }

        public async Task RequestPasswordRecoveryAsync() => await SendOtpAsync();

        public void VerifyPasswordRecovery() => VerifyCode();

        public async Task VerifyOtpAsync()
        {
            ClearError();
            try
            {
                if (LastEnterRoute == FastGameEnterRoute.VerifyId)
                {
                    await Client.Auth.VerifySignupVerificationAsync(ReadIdentity(), ReadOtp());
                    OnRecoveryStepComplete?.Invoke(true, 200, "otp ok");
                    if (AutoSwitchPages)
                        ShowEnterSignupPage();
                    OnOtpVerified?.Invoke();
                    return;
                }

                if (!ForgotPassword)
                    throw new FastGameException("Verify Auth Code: use after Verify Id or Begin Forgot");

                await Client.Auth.VerifyPasswordRecoveryAsync(ReadIdentity(), ReadOtp());
                OnRecoveryStepComplete?.Invoke(true, 200, "otp ok");
                if (AutoSwitchPages)
                    ShowEnterRecoveryResetPage();
                OnOtpVerified?.Invoke();
            }
            catch (Exception e)
            {
                FailAuth(OnRecoveryStepComplete, e);
            }
        }

        public async Task VerifyPasswordRecoveryAsync() => await VerifyOtpAsync();

        public void ConfirmPasswordRecovery() => ResetPassword();
        public void SetPassword() => ResetPassword();

        public async Task UpdateFullNameAsync()
        {
            ClearError();
            try
            {
                var name = ReadFullName();
                FullName = name;
                var user = await Client.Auth.UpdateFullNameAsync(name);
                OnGetMeComplete?.Invoke(user);
            }
            catch (Exception e)
            {
                SetError(e.Message);
            }
        }

        public async Task ConfirmPasswordRecoveryAsync()
        {
            ClearError();
            try
            {
                var password = ReadRecoveryPassword();
                var confirm = ReadRecoveryConfirm();
                Password = password;
                PasswordConfirm = confirm;
                await Client.Auth.ConfirmPasswordRecoveryAsync(
                    ReadIdentity(), password, confirm);
                OnRecoveryStepComplete?.Invoke(true, 200, "password set");
                OnLoginComplete?.Invoke(true, 200, "ok");
            }
            catch (Exception e)
            {
                FailAuth(OnRecoveryStepComplete, e);
            }
        }

        public void GetMe() => _ = Run(GetMeAsync);

        public async Task GetMeAsync()
        {
            ClearError();
            try
            {
                var me = await Client.Auth.GetMeAsync();
                OnGetMeComplete?.Invoke(me);
            }
            catch (Exception e)
            {
                SetError(e.Message);
            }
        }

        public void CheckAuthentication() => _ = Run(CheckAuthenticationAsync);

        public async Task CheckAuthenticationAsync()
        {
            ClearError();
            try
            {
                var ok = await Client.Auth.CheckAuthenticationAsync();
                OnCheckAuthenticationComplete?.Invoke(ok);
            }
            catch (Exception e)
            {
                OnCheckAuthenticationComplete?.Invoke(false);
                SetError(e.Message);
            }
        }

        public void Logout()
        {
            try
            {
                Client.Auth.Logout();
            }
            catch (Exception e)
            {
                SetError(e.Message);
            }
        }

        public void ClearEnteredIdentity()
        {
            try
            {
                Client.Auth.ClearEnteredIdentity();
            }
            catch (Exception e)
            {
                SetError(e.Message);
            }
        }

        public void ClearLocalCache()
        {
            try
            {
                Client.Auth.ClearLocalCache();
            }
            catch (Exception e)
            {
                SetError(e.Message);
            }
        }

        void FailAuth(FastGameAuthResultEvent evt, Exception e)
        {
            var code = TryParseStatus(e.Message);
            evt?.Invoke(false, code, e.Message);
            SetError(e.Message);
        }

        static int TryParseStatus(string message)
        {
            if (string.IsNullOrEmpty(message)) return 0;
            var colon = message.IndexOf(':');
            if (colon <= 0) return 0;
            return int.TryParse(message.Substring(0, colon).Trim(), out var code) ? code : 0;
        }

        async Task Run(Func<Task> action)
        {
            if (Busy) return;
            Busy = true;
            try
            {
                await action();
            }
            finally
            {
                Busy = false;
            }
        }
    }
}
