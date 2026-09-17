using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object obj) {}
        public static void DontDestroyOnLoad(Object obj) {}
        public static T FindObjectOfType<T>() where T : Object => null;
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static implicit operator bool(Object obj) => obj != null;
    }

    public class Component : Object
    {
        public GameObject gameObject { get; } = new GameObject();
        public Transform transform { get; } = new Transform();
        public T GetComponent<T>() => default;
        public Component GetComponent(string type) => null;
        public T GetComponentInParent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T GetComponentInChildren<T>(bool includeInactive) => default;
        public T[] GetComponents<T>() => Array.Empty<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) => Array.Empty<T>();
    }

    public class Transform : Component
    {
        public Transform parent { get; set; }
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 forward => Vector3.forward;
        public int childCount => 0;
        public Transform GetChild(int index) => null;
        public Transform Find(string name) => null;
        public void SetParent(Transform value, bool worldPositionStays = true) {}
        public Vector3 TransformVector(Vector3 vector) => vector;
        public Vector3 TransformDirection(Vector3 direction) => direction;
    }

    public class RectTransform : Transform
    {
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour
    {
        protected Coroutine StartCoroutine(System.Collections.IEnumerator routine) => null;
        protected void StopCoroutine(Coroutine routine) {}
        protected void StopAllCoroutines() {}
        protected void Invoke(string methodName, float time) {}
        protected void CancelInvoke() {}
        protected void CancelInvoke(string methodName) {}
    }

    public class GameObject : Object
    {
        public GameObject() {}
        public GameObject(string name) { this.name = name; }
        public Transform transform { get; } = null;
        public bool activeSelf => true;
        public void SetActive(bool active) {}
        public T GetComponent<T>() => default;
        public Component GetComponent(string type) => null;
        public T GetComponentInChildren<T>(bool includeInactive = false) => default;
        public T AddComponent<T>() where T : new() => new T();
        public T[] GetComponents<T>() => Array.Empty<T>();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => Array.Empty<T>();
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2();
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector3 normalized => this;
        public void Normalize() {}
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => b;
        public static Vector3 SmoothDamp(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime) => target;
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a, Vector3 b) => a;
        public static Vector3 operator *(Vector3 a, float b) => a;
    }

    public struct Quaternion
    {
        public static Quaternion identity => new Quaternion();
        public static Quaternion Euler(float x, float y, float z) => new Quaternion();
        public static Quaternion LookRotation(Vector3 forward) => new Quaternion();
        public static Quaternion LookRotation(Vector3 forward, Vector3 upwards) => new Quaternion();
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => b;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1);
        public static Color black => new Color(0, 0, 0);
        public static Color clear => new Color(0, 0, 0, 0);
    }

    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) => value;
        public static float Clamp01(float value) => value;
        public static float Lerp(float a, float b, float t) => b;
        public static float Max(float a, float b) => a;
        public static int Max(int a, int b) => a;
        public static float Min(float a, float b) => a;
        public static float Abs(float value) => value;
        public static float Exp(float power) => 0f;
    }

    public static class Time
    {
        public static float deltaTime => 0.016f;
        public static float unscaledDeltaTime => 0.016f;
        public static float unscaledTime => 0f;
        public static float time => 0f;
    }

    public enum KeyCode { W, A, S, D, C, E, Q, Space, LeftShift, LeftControl, Escape }

    public static class Input
    {
        public static float GetAxis(string axisName) => 0f;
        public static float GetAxisRaw(string axisName) => 0f;
        public static bool GetButton(string buttonName) => false;
        public static bool GetButtonDown(string buttonName) => false;
        public static bool GetKey(KeyCode key) => false;
        public static bool GetKeyDown(KeyCode key) => false;
    }

    public class Camera : Behaviour
    {
        public static Camera main => null;
    }

    public class CharacterController : Behaviour
    {
        public bool isGrounded => false;
        public void Move(Vector3 motion) {}
    }

    public class Animator : Behaviour
    {
        public void SetBool(string name, bool value) {}
        public void SetFloat(string name, float value) {}
        public void SetInteger(string name, int value) {}
        public void SetTrigger(string name) {}
    }

    public class Renderer : Component
    {
        public void GetPropertyBlock(MaterialPropertyBlock properties) {}
        public void SetPropertyBlock(MaterialPropertyBlock properties) {}
    }

    public class MaterialPropertyBlock
    {
        public void SetFloat(string name, float value) {}
        public void SetColor(string name, Color value) {}
    }

    public class Texture : Object {}
    public class Texture2D : Texture
    {
        public int width => 0;
        public int height => 0;
        public Texture2D(int width, int height) {}
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) {}
        public bool LoadImage(byte[] data) => true;
    }
    public enum TextureFormat { RGBA32 }
    public class Sprite : Object
    {
        public Texture2D texture => null;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) => null;
    }
    public struct Rect
    {
        public Rect(float x, float y, float width, float height) {}
    }
    public class Canvas : Behaviour
    {
        public bool overrideSorting { get; set; }
        public int sortingOrder { get; set; }
    }
    public class Coroutine {}
    public class WaitForSeconds
    {
        public WaitForSeconds(float seconds) {}
    }

    public static class Debug
    {
        public static void Log(object message) {}
        public static void Log(object message, Object context) {}
        public static void LogWarning(object message) {}
        public static void LogWarning(object message, Object context) {}
        public static void LogError(object message) {}
        public static void LogError(object message, Object context) {}
    }

    public static class Application
    {
        public static bool isEditor => false;
        public static bool isPlaying => true;
        public static string persistentDataPath => "";
        public static RuntimePlatform platform => RuntimePlatform.WindowsPlayer;
        public static void OpenURL(string url) {}
    }

    public enum RuntimePlatform
    {
        WindowsPlayer,
        WindowsEditor,
        OSXPlayer,
        OSXEditor,
        LinuxPlayer,
        LinuxEditor,
        Android,
        IPhonePlayer,
        WebGLPlayer,
    }

    public static class PlayerPrefs
    {
        static readonly Dictionary<string, string> Store = new Dictionary<string, string>();

        public static string GetString(string key, string defaultValue = "") =>
            Store.TryGetValue(key ?? "", out var v) ? v : (defaultValue ?? "");

        public static void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            Store[key] = value ?? "";
        }

        public static void DeleteKey(string key)
        {
            if (!string.IsNullOrEmpty(key)) Store.Remove(key);
        }

        public static bool HasKey(string key) => !string.IsNullOrEmpty(key) && Store.ContainsKey(key);
        public static void Save() {}
    }

    public static class JsonUtility
    {
        public static string ToJson(object obj) => "{}";
        public static T FromJson<T>(string json) => default;
    }

    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) {}
    }

    public class TooltipAttribute : Attribute
    {
        public TooltipAttribute(string tooltip) {}
    }

    public class AddComponentMenuAttribute : Attribute
    {
        public AddComponentMenuAttribute(string menuName) {}
    }

    public class DisallowMultipleComponentAttribute : Attribute {}
    public class SerializeField : Attribute {}
    public class RequireComponentAttribute : Attribute
    {
        public RequireComponentAttribute(Type type) {}
    }
    public class DefaultExecutionOrderAttribute : Attribute
    {
        public DefaultExecutionOrderAttribute(int order) {}
    }
    public class RangeAttribute : Attribute
    {
        public RangeAttribute(float min, float max) {}
    }
    public class MinAttribute : Attribute
    {
        public MinAttribute(float min) {}
    }

    public class AndroidJavaObject : IDisposable
    {
        public AndroidJavaObject(string className, params object[] args) {}
        public T Call<T>(string method, params object[] args) => default;
        public void Call(string method, params object[] args) {}
        public T GetStatic<T>(string field) => default;
        public void SetStatic(string field, object value) {}
        public void Dispose() {}
    }

    public class AndroidJavaClass : AndroidJavaObject
    {
        public AndroidJavaClass(string className) : base(className) {}
    }

    public class AndroidJavaProxy
    {
        public AndroidJavaProxy(string javaInterface) {}
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();

    public class UnityEvent
    {
        public void AddListener(UnityAction call) {}
        public void RemoveListener(UnityAction call) {}
        public void RemoveAllListeners() {}
        public void Invoke() {}
    }

    public class UnityEvent<T0> : UnityEvent
    {
        public void AddListener(UnityAction<T0> call) {}
        public void RemoveListener(UnityAction<T0> call) {}
        public void Invoke(T0 arg0) {}
    }

    public class UnityEvent<T0, T1> : UnityEvent
    {
        public void AddListener(UnityAction<T0, T1> call) {}
        public void Invoke(T0 arg0, T1 arg1) {}
    }

    public class UnityEvent<T0, T1, T2> : UnityEvent
    {
        public void AddListener(UnityAction<T0, T1, T2> call) {}
        public void Invoke(T0 arg0, T1 arg1, T2 arg2) {}
    }

    public delegate void UnityAction<T0>(T0 arg0);
    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);
    public delegate void UnityAction<T0, T1, T2>(T0 arg0, T1 arg1, T2 arg2);
}

namespace UnityEngine.Serialization
{
    public class FormerlySerializedAsAttribute : Attribute
    {
        public FormerlySerializedAsAttribute(string name) {}
    }
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;

    public class Graphic : Behaviour
    {
        public Color color { get; set; }
    }

    public class Text : Graphic
    {
        public string text { get; set; }
    }

    public class Image : Graphic
    {
        public Sprite sprite { get; set; }
        public float fillAmount { get; set; }
    }

    public class RawImage : Graphic
    {
        public Texture texture { get; set; }
    }

    public class Selectable : Behaviour
    {
        public bool interactable { get; set; }
        public ColorBlock colors { get; set; }
    }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
    }

    public class Button : Selectable
    {
        public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent();
        public class ButtonClickedEvent : UnityEvent {}
    }

    public class Slider : Selectable
    {
        public float value { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public float normalizedValue { get; set; }
        public SliderEvent onValueChanged { get; } = new SliderEvent();
        public class SliderEvent : UnityEvent<float> {}
    }

    public class ScrollRect : Behaviour
    {
        public RectTransform content { get; set; }
        public Vector2 normalizedPosition { get; set; }
        public float verticalNormalizedPosition { get; set; }
        public float horizontalNormalizedPosition { get; set; }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
    }

    public static class LayoutRebuilder
    {
        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot) {}
    }
}

namespace UnityEngine.SceneManagement
{
    public static class SceneManager
    {
        public static void LoadScene(string sceneName) {}
        public static void LoadScene(int sceneBuildIndex) {}
    }
}

namespace UnityEngine.Networking
{
    public class DownloadHandler
    {
        public string text => "{}";
        public byte[] data => Array.Empty<byte>();
    }

    public class DownloadHandlerBuffer : DownloadHandler {}

    public class UploadHandler {}

    public class UploadHandlerRaw : UploadHandler
    {
        public UploadHandlerRaw(byte[] data) {}
    }

    public class UnityWebRequestAsyncOperation
    {
        public bool isDone => true;
    }

    public class UnityWebRequest : IDisposable
    {
        public enum Result
        {
            InProgress,
            Success,
            ConnectionError,
            ProtocolError,
            DataProcessingError,
        }

        public UnityWebRequest(string url, string method) {}
        public UploadHandler uploadHandler { get; set; }
        public DownloadHandler downloadHandler { get; set; } = new DownloadHandlerBuffer();
        public bool isNetworkError => false;
        public bool isHttpError => false;
        public Result result => Result.Success;
        public long responseCode => 200;
        public string error => null;
        public void SetRequestHeader(string name, string value) {}
        public UnityWebRequestAsyncOperation SendWebRequest() => new UnityWebRequestAsyncOperation();
        public void Dispose() {}
        public static string EscapeURL(string s) => Uri.EscapeDataString(s ?? "");
        public static UnityWebRequest Get(string url) => new UnityWebRequest(url, "GET");
    }
}
