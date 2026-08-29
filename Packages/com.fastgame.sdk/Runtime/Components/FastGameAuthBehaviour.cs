using System;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FastGame
{
    /// <summary>Why <see cref="FastGameAuthBehaviour.OnAuthComplete"/> fired.</summary>
    public enum FastGameAuthCompleteReason
    {
        Login,
        Signup,
        PasswordRecovery,
        AlreadyAuthenticated
    }

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

    [Serializable] public class FastGameAuthCompleteEvent : UnityEvent<FastGameAuthCompleteReason> { }

    /// <summary>
    /// Auth UI controller (UE Fast Game Auth + page widgets).
    /// Designer vocabulary matches UE: Enter → Enter Password / Verify / Signup / Failed;
    /// Send Auth Code / Verify Auth Code → Signup | Assign New Password; Assign New Password.
    /// Unity hierarchy (MAP_2_AUTH):
    /// <code>
    /// Auth_Canvas
    ///   BG
    ///   EnterID_Canvas      → EnterIdCanvas
    ///   EnterPassword_Canvas
    ///   Signup_Canvas       → EnterSignupCanvas
    ///   OTP_Canvas          → EnterRecoveryOtpCanvas
    ///   NewPassword_Canvas  → EnterRecoveryResetCanvas
    ///   Error               → ErrorText
    /// </code>
    /// Assign canvases + buttons on this component — buttons wire in Awake when set.
    /// </summary>
    [AddComponentMenu("Fast Game/Auth")]
    public sealed class FastGameAuthBehaviour : MonoBehaviour
    {
        [Header("Client")]
        [Tooltip("Leave empty to use FastGameClientBehaviour.Instance")]
        public FastGameClientBehaviour ClientHost;

        [Header("Pages (canvases under Auth_Canvas)")]
        [Tooltip("Root AUTH canvas — kept active.")]
        [FormerlySerializedAs("AuthCanvas")]
        public GameObject AuthRootCanvas;
        [Tooltip("Enter ID page (EnterID_Canvas).")]
        [FormerlySerializedAs("EnterIdCanvas")]
        public GameObject EnterIdCanvas;
        [Tooltip("Existing user — enter password (EnterPassword_Canvas).")]
        public GameObject EnterPasswordCanvas;
        [Tooltip("New user — signup (Signup_Canvas).")]
        [FormerlySerializedAs("EnterSignupCanvas")]
        public GameObject SignupCanvas;
        [Tooltip("OTP send / verify (OTP_Canvas).")]
        [FormerlySerializedAs("EnterRecoveryOtpCanvas")]
        public GameObject OtpCanvas;
        [Tooltip("Recovery: new password + confirm (NewPassword_Canvas).")]
        [FormerlySerializedAs("EnterRecoveryResetCanvas")]
        public GameObject NewPasswordCanvas;
        [Tooltip("If true, Enter routes auto-switch pages (no need to wire SetActive in events).")]
        public bool AutoSwitchPages = true;
        [Tooltip("After ResetPassword succeeds, show Enter Password canvas for login.")]
        public bool ShowLoginAfterReset = true;
        [Tooltip("Show Enter ID page on Awake.")]
        public bool ShowEnterIdOnAwake = true;

        [Header("Buttons (optional — OnClick wired in Awake)")]
        public Button EnterButton;
        public Button LoginButton;
        public Button SignupButton;
        public Button SendCodeButton;
        public Button VerifyButton;
        public Button ResetPasswordButton;
        [Tooltip("Enter Password canvas — returns to Enter ID (keeps typed identity).")]
        public Button BackFromPasswordButton;
        [Tooltip("OTP canvas — returns to Enter ID.")]
        public Button BackFromOtpButton;
        [Tooltip("Enter Password canvas — forgot password → OTP recovery.")]
        public Button ForgotPasswordButton;
        [Obsolete("Use BackFromPasswordButton / BackFromOtpButton on the relevant canvases.")]
        public Button BackButton;

        [Header("OTP")]
        [Tooltip("When OTP page opens (signup verify or recovery), send code automatically once.")]
        public bool AutoSendOtpOnShow = true;
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
        bool _otpAutoSentThisVisit;

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
        [Tooltip("Fires once when the player has a session (login, signup, or password recovery).")]
        public FastGameAuthCompleteEvent OnAuthComplete;
        public FastGameAuthResultEvent OnLoginComplete;
        public FastGameAuthResultEvent OnSignupComplete;
        public FastGameAuthResultEvent OnRecoveryStepComplete;
        public FastGameUserEvent OnGetMeComplete;
        public FastGameBoolEvent OnCheckAuthenticationComplete;
        public FastGameStringEvent OnError;

        [Header("Next scene")]
        [Tooltip("Scene to load after auth completes.")]
        public string NextScene = FastGameSceneNames.Download;

        [Tooltip("Load NextScene automatically when auth completes.")]
        public bool AutoLoadNextOnComplete = true;

        [Tooltip("If already logged in on enter, skip UI and load the next scene.")]
        public bool CompleteWhenAlreadyAuthenticated = true;

        public FastGameSceneCompleteEvent OnSceneComplete;

        public FastGameEnterResult LastEnter { get; private set; }
        public FastGameEnterRoute LastEnterRoute { get; private set; } = FastGameEnterRoute.Failed;
        public bool IsForgotPasswordFlow { get; private set; }

        bool _sceneCompleted;

        public bool IsAuthenticated =>
            ClientHost != null ? ClientHost.Client?.Auth.IsAuthenticated == true
                : FastGameClientBehaviour.Instance?.Client?.Auth.IsAuthenticated == true;

        FastGameClient Client => FastGameClientBehaviour.RequireClient(ClientHost);

        void Awake()
        {
            WireButtons();
            if (AuthRootCanvas != null)
                AuthRootCanvas.SetActive(true);
            if (ShowEnterIdOnAwake)
                ShowEnterIdPage();
        }

        void WireButtons()
        {
            Wire(EnterButton, Enter);
            Wire(LoginButton, Login);
            Wire(SignupButton, Signup);
            Wire(SendCodeButton, SendCode);
            Wire(VerifyButton, VerifyCode);
            Wire(ResetPasswordButton, ResetPassword);
            Wire(BackFromPasswordButton, BackToEnterId);
            Wire(BackFromOtpButton, BackToEnterId);
            Wire(ForgotPasswordButton, BeginForgotPassword);
#pragma warning disable CS0618
            Wire(BackButton, BackToEnterId);
#pragma warning restore CS0618
        }

        void Start()
        {
            if (!CompleteWhenAlreadyAuthenticated || !IsAuthenticated)
                return;
            RaiseAuthComplete(FastGameAuthCompleteReason.AlreadyAuthenticated);
        }

        static void Wire(Button button, UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        void RaiseAuthComplete(FastGameAuthCompleteReason reason)
        {
            OnAuthComplete?.Invoke(reason);
            CompleteScene();
        }

        public void CompleteScene()
        {
            if (_sceneCompleted)
                return;
            _sceneCompleted = true;
            OnSceneComplete?.Invoke();
            if (AutoLoadNextOnComplete && !string.IsNullOrWhiteSpace(NextScene))
                SceneManager.LoadScene(NextScene);
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
        public void ShowEnterSignupPage() => ShowPage(SignupCanvas);
        public void ShowEnterRecoveryOtpPage() => ShowPage(OtpCanvas);
        public void ShowEnterRecoveryResetPage() => ShowPage(NewPasswordCanvas);

        /// <summary>Alias — recovery starts on OTP canvas.</summary>
        public void ShowEnterRecoveryPage() => ShowEnterRecoveryOtpPage();

        public void ShowPage(GameObject page)
        {
            if (AuthRootCanvas != null)
                AuthRootCanvas.SetActive(true);
            var wasOtp = OtpCanvas != null && OtpCanvas.activeSelf;
            SetPageActive(EnterIdCanvas, page == EnterIdCanvas);
            SetPageActive(EnterPasswordCanvas, page == EnterPasswordCanvas);
            SetPageActive(SignupCanvas, page == SignupCanvas);
            SetPageActive(OtpCanvas, page == OtpCanvas);
            SetPageActive(NewPasswordCanvas, page == NewPasswordCanvas);
            if (page != OtpCanvas)
                _otpAutoSentThisVisit = false;
            else if (!wasOtp)
                MaybeAutoSendOtp();
        }

        void MaybeAutoSendOtp()
        {
            if (!AutoSendOtpOnShow || _otpAutoSentThisVisit || Busy)
                return;
            if (LastEnterRoute != FastGameEnterRoute.VerifyId && !IsForgotPasswordFlow)
                return;
            _otpAutoSentThisVisit = true;
            SendCode();
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

        /// <summary>Forgot password — from Enter Password → OTP recovery (auto-sends code when enabled).</summary>
        public void BeginForgotPassword()
        {
            IsForgotPasswordFlow = true;
            ClearError();
            if (AutoSwitchPages)
                ShowEnterRecoveryOtpPage();
        }

        /// <summary>Back to Enter ID from Enter Password or OTP (keeps identity field).</summary>
        public void BackToEnterId()
        {
            IsForgotPasswordFlow = false;
            ClearError();
            ShowEnterIdPage();
            OnBackToEnterId?.Invoke();
        }

        /// <summary>Full reset — clears identity and all fields (legacy).</summary>
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
            IsForgotPasswordFlow = false;
            ShowEnterIdPage();
            OnBackToEnterId?.Invoke();

            if (!string.IsNullOrEmpty(clearErr))
                SetError(clearErr);
            else
                ClearError();
        }

        /// <summary>After login — PATCH display name.</summary>
        public void UpdateFullName() => _ = Run(UpdateFullNameAsync);

        /// <summary>OTP canvas — send / resend code (signup verify or recovery).</summary>
        public void SendCode() => _ = Run(SendOtpAsync);

        /// <summary>OTP canvas — verify code then advance to Signup or Reset.</summary>
        public void VerifyCode() => _ = Run(VerifyOtpAsync);

        /// <summary>Enter Recovery canvas — recovery 3/3 set new password.</summary>
        public void ResetPassword() => _ = Run(ConfirmPasswordRecoveryAsync);

        [Obsolete("Use BeginForgotPassword().")]
        public void BeginForgot() => BeginForgotPassword();

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

                IsForgotPasswordFlow = false;
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
                RaiseAuthComplete(FastGameAuthCompleteReason.Login);
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
                RaiseAuthComplete(FastGameAuthCompleteReason.Signup);
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
                else if (IsForgotPasswordFlow)
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

                if (!IsForgotPasswordFlow)
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

        /// <summary>Assign New Password (recovery confirm).</summary>
        public void AssignNewPassword() => ResetPassword();
        public void ConfirmPasswordRecovery() => ResetPassword();

        /// <summary>Obsolete — use <see cref="AssignNewPassword"/>.</summary>
        [Obsolete("Use Assign New Password (AssignNewPassword / ResetPassword).")]
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
                RaiseAuthComplete(FastGameAuthCompleteReason.PasswordRecovery);
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
