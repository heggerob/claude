// Minimal fake UnityEngine API surface, only for syntax/type checking outside Unity.
using System;
using System.Collections.Generic;
namespace UnityEngine {
  public class Object { public string name; public static void Destroy(Object o){} public static implicit operator bool(Object o){return o!=null;} }
  public class Component : Object { public GameObject gameObject; public Transform transform; public T GetComponent<T>(){return default(T);} }
  public class Behaviour : Component { public bool enabled; }
  public class MonoBehaviour : Behaviour {}
  public class ScriptableObject : Object { public static T CreateInstance<T>() where T: ScriptableObject, new() { return new T(); } }
  public class GameObject : Object { public GameObject(string n){} public Transform transform; public string tag; public T AddComponent<T>() where T: Component, new(){return new T();} public T GetComponent<T>(){return default(T);} public void SetActive(bool b){} }
  public class Transform : Component { public Vector3 position, localPosition, localScale; public Quaternion rotation, localRotation; public void SetParent(Transform p, bool w=true){} }
  public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;}
    public static Vector2 zero{get{return new Vector2(0,0);}} public static Vector2 one{get{return new Vector2(1,1);}} public static Vector2 right{get{return new Vector2(1,0);}} public static Vector2 left{get{return new Vector2(-1,0);}} public static Vector2 up{get{return new Vector2(0,1);}}
    public float magnitude{get{return (float)Math.Sqrt(x*x+y*y);}} public float sqrMagnitude{get{return x*x+y*y;}} public Vector2 normalized{get{float m=magnitude; return m>1e-5f? this/m : zero;}} public void Normalize(){this=normalized;}
    public static Vector2 operator+(Vector2 a,Vector2 b){return new Vector2(a.x+b.x,a.y+b.y);} public static Vector2 operator-(Vector2 a,Vector2 b){return new Vector2(a.x-b.x,a.y-b.y);} public static Vector2 operator-(Vector2 a){return new Vector2(-a.x,-a.y);}
    public static Vector2 operator*(Vector2 a,float b){return new Vector2(a.x*b,a.y*b);} public static Vector2 operator*(float b,Vector2 a){return a*b;} public static Vector2 operator/(Vector2 a,float b){return new Vector2(a.x/b,a.y/b);}
    public static bool operator==(Vector2 a,Vector2 b){return a.x==b.x&&a.y==b.y;} public static bool operator!=(Vector2 a,Vector2 b){return !(a==b);} public override bool Equals(object o){return o is Vector2 && (Vector2)o==this;} public override int GetHashCode(){return x.GetHashCode()^y.GetHashCode();}
    public static implicit operator Vector3(Vector2 v){return new Vector3(v.x,v.y,0);} public static implicit operator Vector2(Vector3 v){return new Vector2(v.x,v.y);}
    public static float Distance(Vector2 a,Vector2 b){return (a-b).magnitude;} public static Vector2 Lerp(Vector2 a,Vector2 b,float t){t=Mathf.Clamp01(t); return a+(b-a)*t;}
    public static float Dot(Vector2 a,Vector2 b){return a.x*b.x+a.y*b.y;} public static Vector2 ClampMagnitude(Vector2 v,float m){return v.magnitude>m? v.normalized*m : v;} public override string ToString(){return "("+x+", "+y+")";} }
  public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static Vector3 zero{get{return new Vector3(0,0,0);}} public static Vector3 one{get{return new Vector3(1,1,1);}}
    public static Vector3 operator+(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);} public static Vector3 operator-(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);} public static Vector3 operator*(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);} public static Vector3 operator*(float b,Vector3 a){return a*b;}
    public float magnitude{get{return (float)Math.Sqrt(x*x+y*y+z*z);}} public static Vector3 Lerp(Vector3 a,Vector3 b,float t){t=Mathf.Clamp01(t); return a+(b-a)*t;} }
  public struct Vector2Int { public int x,y; public Vector2Int(int x,int y){this.x=x;this.y=y;} }
  public struct Quaternion { public static Quaternion identity; public static Quaternion Euler(float x,float y,float z){return identity;} }
  public struct Matrix4x4 { public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s){return new Matrix4x4();} }
  public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}
    public static Color white{get{return new Color(1,1,1,1);}} public static Color black{get{return new Color(0,0,0,1);}} public static Color clear{get{return new Color(0,0,0,0);}}
    public static Color operator*(Color c,float f){return new Color(c.r*f,c.g*f,c.b*f,c.a*f);} public static Color operator+(Color a,Color b){return new Color(a.r+b.r,a.g+b.g,a.b+b.b,a.a+b.a);} public static implicit operator Color(Color32 c){return new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f);} }
  public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
  public struct Rect { public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} public float x,y,width,height; public float xMin{get{return x;}} public float xMax{get{return x+width;}} public float yMin{get{return y;}} public float yMax{get{return y+height;}} public Vector2 center{get{return new Vector2(x+width/2,y+height/2);}} public Vector2 size{get{return new Vector2(width,height);}} public Vector2 min{get{return new Vector2(x,y);}} public bool Contains(Vector2 p){return p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;} }
  public class RectOffset { public RectOffset(int a,int b,int c,int d){} }
  public static class Mathf { public const float PI=(float)Math.PI, Deg2Rad=(float)(Math.PI/180), Rad2Deg=(float)(180/Math.PI), Infinity=float.PositiveInfinity;
    public static float Sqrt(float f){return (float)Math.Sqrt(f);} public static float Max(float a,float b){return Math.Max(a,b);} public static int Max(int a,int b){return Math.Max(a,b);} public static float Min(float a,float b){return Math.Min(a,b);} public static int Min(int a,int b){return Math.Min(a,b);}
    public static float Abs(float f){return Math.Abs(f);} public static float Clamp(float v,float a,float b){return v<a?a:v>b?b:v;} public static int Clamp(int v,int a,int b){return v<a?a:v>b?b:v;} public static float Clamp01(float v){return Clamp(v,0,1);} public static float Lerp(float a,float b,float t){return a+(b-a)*Clamp01(t);}
    public static float InverseLerp(float a,float b,float v){return a!=b? Clamp01((v-a)/(b-a)) : 0;} public static float Atan2(float y,float x){return (float)Math.Atan2(y,x);} public static float Cos(float f){return (float)Math.Cos(f);} public static float Sin(float f){return (float)Math.Sin(f);} public static float Tan(float f){return (float)Math.Tan(f);} public static float Exp(float f){return (float)Math.Exp(f);}
    public static float Repeat(float t,float l){return t-(float)Math.Floor(t/l)*l;} public static float PingPong(float t,float l){t=Repeat(t,l*2); return l-Math.Abs(t-l);} public static int RoundToInt(float f){return (int)Math.Round(f, MidpointRounding.ToEven);} public static int CeilToInt(float f){return (int)Math.Ceiling(f);} public static int FloorToInt(float f){return (int)Math.Floor(f);} public static float Floor(float f){return (float)Math.Floor(f);} public static bool Approximately(float a,float b){return Math.Abs(a-b)<1e-6f;} public static float Sign(float f){return f>=0?1:-1;} public static float Pow(float a,float b){return (float)Math.Pow(a,b);} public static float SmoothStep(float a,float b,float t){t=Clamp01(t); t=t*t*(3-2*t); return a+(b-a)*t;} public static float MoveTowards(float c,float t,float d){return Math.Abs(t-c)<=d? t : c+Math.Sign(t-c)*d;} public static float DeltaAngle(float a,float b){float d=Repeat(b-a,360); if(d>180)d-=360; return d;} }
  public static class Random { static System.Random r=new System.Random(1); public static float value{get{return (float)r.NextDouble();}} public static float Range(float a,float b){return a+(b-a)*value;} public static int Range(int a,int b){return b>a? r.Next(a,b) : a;} public static Vector2 insideUnitCircle{get{float t=value*6.283f, d=(float)Math.Sqrt(value); return new Vector2((float)Math.Cos(t)*d,(float)Math.Sin(t)*d);}} public static void InitState(int s){r=new System.Random(s);} }
  public static class Time { public static float time, deltaTime, unscaledDeltaTime, unscaledTime, timeScale; }
  public static class Screen { public static int width, height; }
  public static class Debug { public static void Log(object o){} }
  public static class PlayerPrefs { static Dictionary<string,object> d=new Dictionary<string,object>(); public static string GetString(string k,string def){object v; return d.TryGetValue(k,out v)?(string)v:def;} public static void SetString(string k,string v){d[k]=v;} public static int GetInt(string k,int def){object v; return d.TryGetValue(k,out v)?(int)v:def;} public static void SetInt(string k,int v){d[k]=v;} public static float GetFloat(string k,float def){object v; return d.TryGetValue(k,out v)?(float)v:def;} public static void SetFloat(string k,float v){d[k]=v;} public static void Save(){} public static void DeleteAll(){d.Clear();} }
  public static class JsonUtility { public static T FromJson<T>(string s){return default(T);} public static string ToJson(object o){return "";} }
  public enum TextureFormat { RGBA32 } public enum FilterMode { Point } public enum TextureWrapMode { Clamp }
  public class Texture : Object {} 
  public class Texture2D : Texture { public Color[] pixels; public Texture2D(int w,int h){width=w;height=h;pixels=new Color[w*h];} public Texture2D(int w,int h,TextureFormat f,bool m):this(w,h){} public int width,height; public FilterMode filterMode; public TextureWrapMode wrapMode; public void SetPixel(int x,int y,Color c){if(x>=0&&y>=0&&x<width&&y<height)pixels[y*width+x]=c;} public Color GetPixel(int x,int y){return pixels[y*width+x];} public void Apply(){} public static Texture2D whiteTexture; }
  public enum SpriteMeshType { FullRect, Tight }
  public class Sprite : Object { public Texture2D texture; public Vector2 pivot; public static Sprite Create(Texture2D t,Rect r,Vector2 p,float ppu,uint extrude,SpriteMeshType m){return new Sprite{texture=t,pivot=new Vector2(p.x*r.width,p.y*r.height)};} }
  public enum SpriteDrawMode { Simple, Sliced, Tiled }
  public class Renderer : Component { public bool enabled; public int sortingOrder; }
  public class SpriteRenderer : Renderer { public Sprite sprite; public Color color; public SpriteDrawMode drawMode; public Vector2 size; }
  public enum CameraClearFlags { SolidColor }
  public class Camera : Behaviour { public static Camera main; public bool orthographic; public float orthographicSize; public CameraClearFlags clearFlags; public Color backgroundColor; public Vector3 ScreenToWorldPoint(Vector3 v){return v;} public Vector3 WorldToScreenPoint(Vector3 v){return v;} }
  public enum RigidbodyInterpolation2D { None, Interpolate } public enum CollisionDetectionMode2D { Discrete, Continuous }
  public class Rigidbody2D : Component { public float gravityScale; public bool freezeRotation; public RigidbodyInterpolation2D interpolation; public CollisionDetectionMode2D collisionDetectionMode;
#if UNITY_6000_0_OR_NEWER
    public Vector2 linearVelocity;
#else
    public Vector2 velocity;
#endif
  }
  public class Collider2D : Behaviour { public bool isTrigger; } public class CircleCollider2D : Collider2D { public float radius; } public class BoxCollider2D : Collider2D { public Vector2 size; }
  public struct RaycastHit2D { public Collider2D collider; public float fraction; public Vector2 point; }
  public struct ContactFilter2D { public ContactFilter2D NoFilter(){return this;} }
  public static class Physics2D { public static Vector2 gravity; public static int Linecast(Vector2 a,Vector2 b,ContactFilter2D f,List<RaycastHit2D> r){return 0;} public static int CircleCast(Vector2 o,float r,Vector2 d,ContactFilter2D f,List<RaycastHit2D> res,float dist){return 0;} }
  public enum KeyCode { None, W,A,S,D,UpArrow,DownArrow,LeftArrow,RightArrow,LeftShift,C,LeftControl,R,H,B,Escape,Alpha1,Alpha2,Alpha3 }
  public static class Input { public static bool GetKey(KeyCode k){return false;} public static bool GetKeyDown(KeyCode k){return false;} public static bool GetMouseButton(int b){return false;} public static bool GetMouseButtonDown(int b){return false;} public static Vector3 mousePosition; }
  public enum RuntimeInitializeLoadType { AfterSceneLoad, SubsystemRegistration }
  public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t){} }
  public class CreateAssetMenuAttribute : Attribute { public string menuName, fileName; }
  public class HeaderAttribute : Attribute { public HeaderAttribute(string s){} }
  public class TooltipAttribute : Attribute { public TooltipAttribute(string s){} }
  public class RequireComponent : Attribute { public RequireComponent(Type t){} }
  public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight } public enum FontStyle { Normal, Bold } public enum ScaleMode { StretchToFill }
  public class GUIStyleState { public Color textColor; public Texture2D background; }
  public class GUIStyle { public GUIStyle(GUIStyle o){} public int fontSize; public bool richText, wordWrap; public TextAnchor alignment; public FontStyle fontStyle; public GUIStyleState normal; public RectOffset padding; }
  public class GUIContent { public static GUIContent none; }
  public class GUISkin { public GUIStyle label, box, button, toggle; }
  public class GUILayoutOption {}
  public enum EventType { Layout, Repaint, MouseDown, MouseUp }
  public class Event { public static Event current = new Event(); public EventType type; }
  public static class GUILayoutUtility { public static Rect GetRect(float w,float h,params GUILayoutOption[] o){return new Rect(0,0,w,h);} }
  public static class GUI { public static GUISkin skin; public static Matrix4x4 matrix; public static Color color, backgroundColor; public static bool enabled;
    public static void Label(Rect r,string t,GUIStyle s){} public static bool Button(Rect r,string t){return false;} public static void DrawTexture(Rect r,Texture t){} public static void BeginGroup(Rect r){} public static void EndGroup(){} public static void Box(Rect r,GUIContent c,GUIStyle s){} public static void DrawTexture(Rect r,Texture t,ScaleMode m,bool a,float asp,Color c,float bw,float br){} }
  public static class GUILayout { public static void BeginArea(Rect r,GUIStyle s){} public static void EndArea(){} public static void Label(string t,GUIStyle s,params GUILayoutOption[] o){} public static bool Button(string t,params GUILayoutOption[] o){return false;}
    public static bool Toggle(bool v,string t,params GUILayoutOption[] o){return v;} public static void Space(float f){} public static Vector2 BeginScrollView(Vector2 p,params GUILayoutOption[] o){return p;} public static void EndScrollView(){} public static float HorizontalSlider(float v,float a,float b,params GUILayoutOption[] o){return v;} public static void BeginVertical(GUIStyle s,params GUILayoutOption[] o){} public static void FlexibleSpace(){} public static void BeginHorizontal(params GUILayoutOption[] o){} public static void EndHorizontal(){} public static void BeginVertical(params GUILayoutOption[] o){} public static void EndVertical(){}
    public static GUILayoutOption Height(float h){return null;} public static GUILayoutOption Width(float w){return null;} }
}
namespace UnityEngine.InputSystem.Controls { public class ButtonControl { public bool isPressed, wasPressedThisFrame; } public class KeyControl : ButtonControl {} public class Vector2Control { public UnityEngine.Vector2 ReadValue(){return new UnityEngine.Vector2();} } }
namespace UnityEngine.InputSystem { using Controls;
  public class Keyboard { public static Keyboard current; public KeyControl wKey,aKey,sKey,dKey,upArrowKey,downArrowKey,leftArrowKey,rightArrowKey,leftShiftKey,cKey,leftCtrlKey,rKey,hKey,bKey,escapeKey,digit1Key,digit2Key,digit3Key; }
  public class Mouse { public static Mouse current; public ButtonControl leftButton; public Vector2Control position; } }
