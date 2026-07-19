using System;
using System.Collections.Generic;
using DG.Tweening;

namespace Game.Core
{
    /// <summary>
    /// Plays a sequence of tween "steps" one at a time. <see cref="BlackjackLoop"/> fires its
    /// CardDealt/DealerHoleCardRevealed/HandSettled events synchronously (a whole initial deal or
    /// dealer turn happens within a single frame) — this queue lets the visual layer spread those
    /// events out over time without touching game logic.
    /// </summary>
    public class AnimationQueue
    {
        private readonly Queue<Func<Tween>> steps = new();
        private readonly List<Action> idleCallbacks = new();
        private Tween runningTween;

        public bool IsIdle => runningTween == null && steps.Count == 0;

        public void Enqueue(Func<Tween> step)
        {
            if (step == null)
            {
                return;
            }

            steps.Enqueue(step);
            RunNextIfIdle();
        }

        /// <summary>Invokes <paramref name="callback"/> immediately if idle, otherwise once the queue drains.</summary>
        public void WhenIdle(Action callback)
        {
            if (callback == null)
            {
                return;
            }

            if (IsIdle)
            {
                callback();
                return;
            }

            idleCallbacks.Add(callback);
        }

        public void KillAll()
        {
            steps.Clear();
            runningTween?.Kill();
            runningTween = null;
            idleCallbacks.Clear();
        }

        private void RunNextIfIdle()
        {
            if (runningTween != null || steps.Count == 0)
            {
                return;
            }

            Func<Tween> next = steps.Dequeue();
            Tween tween = next?.Invoke();

            if (tween == null)
            {
                RunNextIfIdle();
                return;
            }

            runningTween = tween;
            tween.OnComplete(OnStepComplete);
        }

        private void OnStepComplete()
        {
            runningTween = null;
            RunNextIfIdle();

            if (!IsIdle || idleCallbacks.Count == 0)
            {
                return;
            }

            List<Action> callbacks = new(idleCallbacks);
            idleCallbacks.Clear();
            foreach (Action callback in callbacks)
            {
                callback();
            }
        }
    }
}
