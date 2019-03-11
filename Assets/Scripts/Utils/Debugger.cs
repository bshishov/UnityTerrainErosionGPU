#if DEBUG
#define USE_REFLECTION
#endif

using System;
using System.Collections;
using System.Collections.Generic;

#if USE_REFLECTION
using System.Reflection;
#endif
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public class Debugger : LazySingleton<Debugger>
    {
        #region Settings
        public const char PathSeparator = '/';
        #endregion

        #region Style
        private const float Padding = 10f;
        private const float LineHeight = 20f;
        private const float HeaderColumn = 200f;
        private const float Opacity = 0.9f;
        private const string DefaultFontName = "Consolas";

        private static GUIStyle _boxStyle;
        private static GUIStyle _propertyHeaderStyle;
        private static GUIStyle _selectedBoxStyle;
        private static GUIStyle _contentStyle;
        private static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font != null)
                    return _font;

                _font = Font.CreateDynamicFontFromOSFont(DefaultFontName, 14);
                return _font;
            }
        }

        public static GUIStyle HeaderStyle
        {
            get
            {
                if (_boxStyle != null)
                    return _boxStyle;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal =
                    {
                        background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, Opacity))
                    },
                    contentOffset = new Vector2(5f, 0f),
                    font = DefaultFont
                };
                _boxStyle = style;
                return style;
            }
        }

        public static GUIStyle PropertyHeaderStyle
        {
            get
            {
                if (_propertyHeaderStyle != null)
                    return _propertyHeaderStyle;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal =
                    {
                        background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.3f, Opacity))
                    },
                    contentOffset = new Vector2(5f, 0f),
                    font = DefaultFont
                };
                _propertyHeaderStyle = style;
                return style;
            }
        }

        public static GUIStyle SelectedHeaderStyle
        {
            get
            {
                if (_selectedBoxStyle != null)
                    return _selectedBoxStyle;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal =
                    {
                        background = MakeTex(2, 2, new Color(0.4f, 0.0f, 0.0f, Opacity))
                    },
                    contentOffset = new Vector2(5f, 0f),
                    font = DefaultFont
                };
                _selectedBoxStyle = style;
                return style;
            }
        }

        public static GUIStyle ContentStyle
        {
            get
            {
                if (_contentStyle != null)
                    return _contentStyle;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal =
                    {
                        background = MakeTex(2, 2, new Color(0f, 0f, 0f, Opacity))
                    },
                    font = DefaultFont
                };
                _contentStyle = style;
                return style;
            }
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        #endregion

        #region Widgets
        public interface IWidget
        {
            Vector2 Size { get; }
            void Draw(Rect rect);
        }

        public interface INestedWidget : IWidget, IEnumerable<KeyValuePair<string, IWidget>>
        {
            bool GetExpanded(int index);
            void SetExpanded(int index, bool value);
        }

        public interface IValueWidget : IWidget
        {
            void SetValue(object o);
        }

        public interface IValueWidget<in T> : IValueWidget
        {
            void SetValue(T value);
        }

        public class Cache<TKey, TVal>
        {
            private readonly Dictionary<TKey, TVal> _storage = new Dictionary<TKey, TVal>();
            private readonly TVal _default;
            private readonly bool _useDefault;
            private readonly bool _useDefaultFn;
            private readonly Func<TKey, TVal> _defaultFn;

            public Cache()
            {
            }

            public Cache(TVal defaultVal) : this()
            {
                _useDefault = true;
                _default = defaultVal;
            }

            public Cache(Func<TKey, TVal> defaultFn) : this()
            {
                _useDefaultFn = defaultFn != null;
                _defaultFn = defaultFn;
            }

            public TVal Get(TKey key)
            {
                TVal val;
                if (_storage.TryGetValue(key, out val))
                    return val;

                if (_useDefaultFn)
                {
                    var newVal = _defaultFn(key);
                    _storage[key] = newVal;
                    return newVal;
                }

                if (_useDefault)
                    return _default;

                throw new KeyNotFoundException(String.Format("Missing {0}", key.ToString()));
            }

            public TVal Get(TKey key, Func<TVal> createFn)
            {
                TVal val;
                if (_storage.TryGetValue(key, out val))
                    return val;

                if (createFn != null)
                {
                    var newVal = createFn();
                    _storage[key] = newVal;
                    return newVal;
                }

                if (_useDefault)
                    return _default;

                throw new KeyNotFoundException(String.Format("Missing {0}", key.ToString()));
            }

            public bool ContainsKey(TKey key)
            {
                return _storage.ContainsKey(key);
            }

            public void Set(TKey key, TVal val)
            {
                if (_storage.ContainsKey(key))
                    _storage[key] = val;
                else
                    _storage.Add(key, val);
            }

            public void Clear()
            {
                _storage.Clear();
            }
        }

        public class StringWidget : IValueWidget<string>, IValueWidget<object>
        {
            public Vector2 Size { get; private set; }
            private string _value;

            public StringWidget()
            {
            }

            public StringWidget(string value) : this()
            {
                SetValue(value);
            }

            public StringWidget(object value) : this()
            {
                SetValue(value.ToString());
            }

            public void Draw(Rect rect)
            {
                GUI.Label(rect, _value);
            }

            public static Vector2 GetSize(string value)
            {
                var size = new Vector2(value.Length * 10f, LineHeight);
                size.x = Mathf.Max(size.x, 200);
                return size;
            }

            public void SetValue(string value)
            {
                _value = value;
                Size = GetSize(_value);
            }

            public void SetValue(object value)
            {
                _value = value.ToString();
                Size = GetSize(_value);
            }
        }

        public class NumericWidget : StringWidget, IValueWidget<int>, IValueWidget<float>
        {
            public void SetValue(int value)
            {
                SetValue(value.ToString());
            }

            public void SetValue(float value)
            {
                SetValue(value.ToString());
            }
        }

        public class TextureWidget : IValueWidget<Texture>
        {
            public const float DefaultWidth = 256f;
            public const float DefaultHeight = 256f;
            public Vector2 Size { get; private set; }
            private Texture _value;

            public TextureWidget(float width = DefaultWidth, float height = DefaultHeight)
            {
                Size = new Vector2(width, height);
            }

            public TextureWidget(Texture value, float width = DefaultWidth, float height = DefaultHeight) : this(width, height)
            {
                SetValue(value);
            }

            public void Draw(Rect rect)
            {
                GUI.DrawTexture(rect, _value);
            }

            public void SetValue(object o)
            {
                _value = o as Texture;
            }

            public void SetValue(Texture value)
            {
                _value = value;
            }
        }

        public class Logger : IWidget
        {
            public Vector2 Size { get; private set; }

            private readonly FixedSizeStack<string> _messages;
            private readonly bool _unityLog;

            public Logger(int size = 20, bool unityLog = true)
            {
                Size = new Vector2(400f, LineHeight * size);
                _messages = new FixedSizeStack<string>(size);
                _unityLog = unityLog;
            }

            public void Log(string message)
            {
                _messages.Push(message);
                if (_unityLog)
                    Debug.Log(message);
            }

            public void LogFormat(string message, params object[] args)
            {
                _messages.Push(string.Format(message, args));
                if (_unityLog)
                    Debug.LogFormat(message, args);
            }

            public void Draw(Rect rect)
            {
                var currentY = rect.y;
                foreach (var message in _messages)
                {
                    GUI.Label(new Rect(rect.x, currentY, rect.width, LineHeight), message);
                    currentY += LineHeight;
                }
            }
        }

        public class CustomUIWidget : IWidget
        {
            public Vector2 Size { get; private set; }
            private readonly Action<Rect> _drawAction;

            public CustomUIWidget(Vector2 size, Action<Rect> drawAction)
            {
                Size = size;
                _drawAction = drawAction;
            }

            public void Draw(Rect rect)
            {
                if (_drawAction != null)
                {
                    _drawAction(rect);
                }
                else
                {
                    GUI.Label(rect, "Missing DRAW function!");
                }
            }
        }
#if USE_REFLECTION
        public class DictionaryWidget<T1, T2> : INestedWidget, IValueWidget<IDictionary<T1, T2>>
        {
            public Vector2 Size { get; private set; }

            private IDictionary<T1, T2> _value;
            private readonly Cache<int, bool> _expanded = new Cache<int, bool>(false);
            private readonly Cache<T1, IValueWidget> _widgetsCache;

            public DictionaryWidget()
            {
                _widgetsCache = new Cache<T1, IValueWidget>(GetWidget);
            }

            public DictionaryWidget(IDictionary<T1, T2> dict) : this()
            {
                SetValue(dict);
            }

            private IValueWidget GetWidget(T1 key)
            {
                return GetDefaultWidget(typeof(T2));
            }

            public void Draw(Rect rect)
            {
            }

            public void SetValue(object o)
            {
                SetValue(o as IDictionary<T1, T2>);
            }

            public IEnumerator<KeyValuePair<string, IWidget>> GetEnumerator()
            {
                if (_value == null)
                    yield break;

                foreach (var kvp in _value)
                {
                    var widget = _widgetsCache.Get(kvp.Key);
                    if (widget != null)
                        widget.SetValue(kvp.Value);
                    yield return new KeyValuePair<string, IWidget>(kvp.Key.ToString(), widget);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool GetExpanded(int index)
            {
                return _expanded.Get(index);
            }

            public void SetExpanded(int index, bool value)
            {
                _expanded.Set(index, value);
            }

            public void SetValue(IDictionary<T1, T2> value)
            {
                if (value.Equals(_value))
                    return;
                _expanded.Clear();
                _value = value;
            }
        }

        public class EnumerableWidget<T> : INestedWidget, IValueWidget<IEnumerable<T>>
        {
            public Vector2 Size { get; private set; }

            private IEnumerable<T> _value;
            private readonly Cache<int, bool> _expanded = new Cache<int, bool>(false);
            private readonly Cache<int, IValueWidget> _widgetsCache;

            public EnumerableWidget()
            {
                _widgetsCache = new Cache<int, IValueWidget>(GetWidget);
            }

            public EnumerableWidget(IEnumerable<T> val) : this()
            {
                SetValue(val);
            }

            public void Draw(Rect rect)
            {
            }

            private IValueWidget GetWidget(int index)
            {
                var w = GetDefaultWidget(typeof(T));
                Debug.LogFormat("New w={0}", w);
                return w;
            }

            public void SetValue(IEnumerable<T> value)
            {
                if (value.Equals(_value))
                    return;

                _expanded.Clear();
                _value = value;
            }

            public void SetValue(object o)
            {
                SetValue((IEnumerable<T>)o);
            }

            public IEnumerator<KeyValuePair<string, IWidget>> GetEnumerator()
            {
                if (_value == null)
                    yield break;

                var i = 0;
                foreach (var v in _value)
                {
                    var widget = _widgetsCache.Get(i);
                    if (widget != null)
                        widget.SetValue(v);
                    yield return new KeyValuePair<string, IWidget>(i.ToString(), widget);
                    i += 1;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool GetExpanded(int index)
            {
                return _expanded.Get(index);
            }

            public void SetExpanded(int index, bool value)
            {
                _expanded.Set(index, value);
            }
        }

        public class ObjectWidget : INestedWidget, IValueWidget<object>
        {
            public Vector2 Size { get; private set; }

            private object _value;
            private Type _type;
            private PropertyInfo[] _props;
            private FieldInfo[] _fields;
            private readonly Cache<int, bool> _expanded = new Cache<int, bool>(false);
            private readonly Cache<PropertyInfo, IValueWidget> _propWidgetsCache;
            private readonly Cache<FieldInfo, IValueWidget> _fieldWidgetsCache;
            private readonly BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            public ObjectWidget()
            {
                Size = new Vector2(200, LineHeight);
                _propWidgetsCache = new Cache<PropertyInfo, IValueWidget>(GetWidget);
                _fieldWidgetsCache = new Cache<FieldInfo, IValueWidget>(GetWidget);
            }

            public ObjectWidget(object value) : this()
            {
                SetValue(value);
            }

            private IValueWidget GetWidget(PropertyInfo prop)
            {
                return GetDefaultWidget(prop.PropertyType); ;
            }

            private IValueWidget GetWidget(FieldInfo prop)
            {
                return GetDefaultWidget(prop.FieldType);
            }

            public void Draw(Rect rect)
            {
                if (_value == null)
                    GUI.Label(rect, "null");
                else
                    GUI.Label(rect, _type.Name);
            }

            public IEnumerator<KeyValuePair<string, IWidget>> GetEnumerator()
            {
                if (_value == null)
                    yield break;

                var i = 0;
                foreach (var field in _fields)
                {
                    IValueWidget widget = null;

                    try
                    {
                        // If property is expanded
                        // Do lazy widget lookup
                        if (_expanded.Get(i))
                        {
                            widget = _fieldWidgetsCache.Get(field);

                            if (widget != null)
                            {
                                var v = field.GetValue(_value);
                                widget.SetValue(v);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                    }

                    i += 1;
                    yield return new KeyValuePair<string, IWidget>(field.Name, widget);
                }

                foreach (var prop in _props)
                {
                    IValueWidget widget = null;

                    try
                    {
                        // If property is expanded
                        // Do lazy widget lookup
                        if (_expanded.Get(i))
                        {
                            widget = _propWidgetsCache.Get(prop);

                            if (widget != null)
                            {
                                var v = prop.GetValue(_value, null);
                                widget.SetValue(v);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                    }

                    i += 1;
                    yield return new KeyValuePair<string, IWidget>(string.Format("get {0}", prop.Name), widget);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool GetExpanded(int index)
            {
                return _expanded.Get(index);
            }

            public void SetExpanded(int index, bool value)
            {
                _expanded.Set(index, value);
            }

            public void SetValue(object value)
            {
                if (value.Equals(_value))
                    return;

                _value = value;
                _type = value.GetType();
                _props = _type.GetProperties(_bindingFlags);
                _fields = _type.GetFields(_bindingFlags);

                _expanded.Clear();
            }
        }

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
#endif
        public static IValueWidget<T> GetDefaultWidget<T>()
        {
            var type = typeof(T);
            return GetDefaultWidget(type) as IValueWidget<T>;
        }

        public static IValueWidget GetDefaultWidget(Type type)
        {
            if (type == typeof(string) || type == typeof(bool))
            {
                return new StringWidget();
            }

            if (type == typeof(int) || type == typeof(float))
            {
                return new NumericWidget();
            }
#if USE_REFLECTION
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();

                // List
                if (IsSubclassOfRawGeneric(typeof(IEnumerable<>), type))
                {
                    var elemType = typeof(EnumerableWidget<>).MakeGenericType(genericArguments[0]);
                    return (IValueWidget)Activator.CreateInstance(elemType);
                }

                // Dictionary
                if (IsSubclassOfRawGeneric(typeof(IDictionary<,>), type))
                {
                    var elemType = typeof(DictionaryWidget<,>).MakeGenericType(genericArguments[0], genericArguments[1]);
                    return (IValueWidget)Activator.CreateInstance(elemType);
                }
            }

            return new ObjectWidget();
#else
            return new StringWidget();
#endif
        }
        #endregion

        public class DrawingContext
        {
            public float Y = 0f;
            public int Index = 0;
            public bool CollapseRequested = false;
            public int Depth = 0;
            public int CursorIndex = 0;
        }

        public class DebugNode
        {
            public string Name;
            public bool IsExpanded = false;
            public float CacheTime = 1f;
            public IWidget Widget;

            private float _lastUpdate;
            private readonly Dictionary<string, DebugNode> _children
                = new Dictionary<string, DebugNode>();

            public DebugNode(string name)
            {
                Name = name;
            }

            private static void Draw(DrawingContext context, string header, ref bool isExpanded, IWidget widget, bool isActualNode)
            {
                var x = context.Depth * Padding;
                var style = HeaderStyle;

                if (!isActualNode)
                    style = PropertyHeaderStyle;

                // If selected
                if (context.Index == context.CursorIndex)
                {
                    style = SelectedHeaderStyle;
                    if (context.CollapseRequested)
                    {
                        isExpanded = !isExpanded;
                        context.CollapseRequested = false;
                    }
                }

                header = string.Format(isExpanded ? "- {0}" : "+ {0}", header);
                var headerRect = new Rect(x, context.Y, HeaderColumn - x, LineHeight);
                GUI.Label(headerRect, header, style);

                // Widget
                if (isExpanded && widget != null)
                {
                    var wSize = widget.Size;
                    var payloadRect = new Rect(HeaderColumn, context.Y, wSize.x, wSize.y);
                    GUI.Box(payloadRect, GUIContent.none, ContentStyle);
                    widget.Draw(payloadRect);
                    context.Y += Mathf.Max(widget.Size.y, LineHeight) - LineHeight;
                }

                context.Y += LineHeight;
                context.Index += 1;

                // Children
                if (isExpanded)
                {
                    var nestedWidget = widget as INestedWidget;
                    if (nestedWidget != null)
                    {
                        context.Depth += 1;
                        var localIdx = 0;
                        foreach (var kvp in nestedWidget)
                        {
                            var childExpanded = nestedWidget.GetExpanded(localIdx);
                            var refChildExpanded = childExpanded;
                            Draw(context, kvp.Key, ref refChildExpanded, kvp.Value, false);
                            if (refChildExpanded != childExpanded)
                                nestedWidget.SetExpanded(localIdx, refChildExpanded);

                            localIdx += 1;
                        }
                        context.Depth -= 1;
                    }
                }
            }

            public void Draw(DrawingContext context)
            {
                Draw(context, Name, ref IsExpanded, Widget, true);

                // Child nodes
                if (IsExpanded)
                {
                    context.Depth += 1;
                    foreach (var node in _children.Values)
                    {
                        node.Draw(context);
                    }
                    context.Depth -= 1;
                }
            }

            public DebugNode GetOrCreateChild(string name)
            {
                if (_children.ContainsKey(name))
                    return _children[name];

                var node = new DebugNode(name);
                _children.Add(name, node);
                return node;
            }

            public void Touch()
            {
                _lastUpdate = Time.deltaTime;
            }
        }

        public KeyCode OpenKey = KeyCode.F3;
        public KeyCode CollapseKey = KeyCode.F5;
        public KeyCode NavigateUp = KeyCode.PageUp;
        public KeyCode NavigateDown = KeyCode.PageDown;

        private bool _isOpened;
        private readonly DebugNode _root = new DebugNode("Debug")
        {
            IsExpanded = true,
            Widget = new StringWidget("F5 - Expand/Collapse, PageUp/PageDown - Navigation")
        };
        private Logger _defaultLog;
        private readonly Cache<string, DebugNode> _pathCache = new Cache<string, DebugNode>();
        private readonly DrawingContext _context = new DrawingContext();

        void Awake()
        {
            _defaultLog = GetLogger("Log");
            Display("Debug/Path cache", new Vector2(200, 20), rect =>
            {
                GUILayout.BeginArea(rect);
                if (GUILayout.Button("Clear cache"))
                {
                    Log("Debug/Log", "Clearing cache");
                    _pathCache.Clear();
                }
                GUILayout.EndArea();
            });

#if USE_REFLECTION
            GetNode("Debug/Dictionary").Widget = new DictionaryWidget<string, string>(new Dictionary<string, string>()
            {
                { "hello", "world"},
                {"1", "@" },
                {"foo", "bar" }
            });

            GetNode("Debug/Context").Widget = new ObjectWidget(_context);
            GetNode("Debug/Array").Widget = new EnumerableWidget<DebugNode>(new[] { _root, _root, _root, _root });
#endif
        }

        void Update()
        {
            if (Input.GetKeyDown(OpenKey))
            {
                if (!_isOpened)
                    Open();
                else
                    Close();
            }

            if (Input.GetKeyDown(NavigateDown))
                _context.CursorIndex += 1;

            if (Input.GetKeyDown(NavigateUp))
                _context.CursorIndex -= 1;

            if (Input.GetKeyDown(CollapseKey))
                _context.CollapseRequested = true;
        }

        public void Open()
        {
            _isOpened = true;
        }

        public void Close()
        {
            _isOpened = false;
        }

        public void Log(string message)
        {
            _defaultLog.Log(message);
        }

        public void LogFormat(string message, params object[] args)
        {
            _defaultLog.Log(string.Format(message, args));
        }

        public void Log(string message, UnityEngine.Object context)
        {
            _defaultLog.Log(message);
        }

        public DebugNode GetNode(string path)
        {
            return _pathCache.Get(path, () =>
            {
                var parts = path.Split(PathSeparator);
                return GetNode(parts);
            });
        }

        public DebugNode GetNode(params string[] path)
        {
            var node = _root;
            foreach (var nodeName in path)
            {
                node = node.GetOrCreateChild(nodeName);
            }

            return node;
        }

        public void Display(DebugNode node, string value)
        {
            var payload = node.Widget as StringWidget;
            if (payload != null)
            {
                payload.SetValue(value);
            }
            else
            {
                // New payload
                node.Widget = new StringWidget(value);
            }
            node.Touch();
        }

        public void Display(DebugNode node, Texture texture)
        {
            var payload = node.Widget as TextureWidget;
            if (payload != null)
            {
                payload.SetValue(texture);
            }
            else
            {
                node.Widget = new TextureWidget(texture);
            }

            node.Touch();
        }

        public void Display(DebugNode node, float value)
        {
            Display(node, value.ToString());
        }

        public void Display(string path, string value)
        {
            Display(GetNode(path), value);
        }

        public void Log(DebugNode node, string message)
        {
            var payload = node.Widget as Logger;
            if (payload != null)
            {
                // Existing payload
                payload.Log(message);
            }
            else
            {
                // New payload
                var p = new Logger();
                p.Log(message);
                node.Widget = p;
            }
            node.Touch();
        }

        public void Display(DebugNode node, Vector2 size, Action<Rect> drawAction)
        {
            node.Widget = new CustomUIWidget(size, drawAction);
            node.Touch();
        }

        public void DisplayFullPath(string value, params string[] path)
        {
            Display(GetNode(path), value);
        }

        public void Display(string path, float value)
        {
            Display(GetNode(path), value);
        }

        public void Log(string path, string message)
        {
            Log(GetNode(path), message);
        }

        public void LogFormat(string path, string message, params object[] args)
        {
            Log(GetNode(path), string.Format(message, args));
        }

        public void Display(string path, Texture texture)
        {
            Display(GetNode(path), texture);
        }

        public void Display(string path, Vector2 size, Action<Rect> drawAction)
        {
            Display(GetNode(path), size, drawAction);
        }

        public Logger GetLogger(string path)
        {
            var node = GetNode(path);
            var payload = GetNode(path).Widget as Logger;
            if (payload != null)
                return payload;

            // New payload
            var p = new Logger();
            node.Widget = p;
            return p;
        }

        void OnGUI()
        {
            if (!_isOpened)
                return;

            // Start from 0
            _context.Y = 0;
            _context.Index = 0;
            _context.Depth = 0;

            // Draw
            _root.Draw(_context);

            // Reset context
            _context.CollapseRequested = false;
            _context.CursorIndex = Mathf.Clamp(_context.CursorIndex, 0, _context.Index - 1);
        }
    }
}
