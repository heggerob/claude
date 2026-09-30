using UnityEngine;

namespace OdinsCoin
{
    public enum Expression { Neutral, Happy, Hurt }

    /// <summary>
    /// A hero's face: the dash eyes blink every few seconds, and swap for ^ ^ when happy or > < when hurt for a
    /// moment. Added by <see cref="HeroBuilder"/>; the game calls <see cref="Show"/>.
    /// </summary>
    public class Face : MonoBehaviour
    {
        /// <summary>How long a blink takes, seconds.</summary>
        public const float BlinkTime = 0.14f;

        public Transform neutral, happy, hurt;
        Expression current = Expression.Neutral;
        float until, nextBlink, blinkStart = -10f;
        readonly System.Random rng = new System.Random();

        /// <summary>Show an expression for a while, then go back to the plain dash eyes.</summary>
        public void Show(Expression e, float seconds)
        {
            current = e;
            until = Time.time + seconds;
            Apply();
        }

        /// <summary>Show an expression on whoever owns this transform, if they have a face.</summary>
        public static void On(Component who, Expression e, float seconds)
        {
            if (who == null) return;
            var f = who.GetComponentInChildren<Face>();
            if (f != null) f.Show(e, seconds);
        }

        /// <summary>
        /// The eyes' height during a blink started <paramref name="since"/> seconds ago: 1 open, down to 0.1 shut
        /// halfway, open again after <see cref="BlinkTime"/>.
        /// </summary>
        public static float BlinkScale(float since)
        {
            if (since < 0f || since >= BlinkTime) return 1f;
            float k = since / BlinkTime;
            return Mathf.Lerp(1f, 0.1f, 1f - Mathf.Abs(k * 2f - 1f));
        }

        /// <summary>Seconds until the next blink: every two to five seconds, never on a beat.</summary>
        public static float NextBlinkGap(double random01) { return 2f + (float)random01 * 3f; }

        void Apply()
        {
            if (neutral != null) neutral.gameObject.SetActive(current == Expression.Neutral);
            if (happy != null) happy.gameObject.SetActive(current == Expression.Happy);
            if (hurt != null) hurt.gameObject.SetActive(current == Expression.Hurt);
        }

        void Update()
        {
            if (current != Expression.Neutral && Time.time >= until) { current = Expression.Neutral; Apply(); }
            if (Time.time >= nextBlink) { blinkStart = Time.time; nextBlink = Time.time + NextBlinkGap(rng.NextDouble()); }
            if (neutral != null) neutral.localScale = new Vector3(1f, BlinkScale(Time.time - blinkStart), 1f);
        }
    }
}
